using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace LCDText2
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class VideoBufferServer : MySessionComponentBase
	{
		readonly ushort videostreamcommand = 32901;
		readonly long videostreamid = 20982309832902;
		private bool registered = false;
		private bool online = false;
		private bool isServer = false;
		static VideoBufferServer instance;
		Dictionary<ulong, VideoBuffer> videoBuffer = new Dictionary<ulong, VideoBuffer>();
		public VideoBufferServer()
		{
			instance = this;
		}
		public override void BeforeStart()
		{
			if (MyAPIGateway.Utilities == null)
				MyAPIGateway.Utilities = MyAPIUtilities.Static;
			MyLog.Default.WriteLineAndConsole("Sending Registration Request to Plugin");
			MyAPIGateway.Utilities.SendModMessage(20982309832901, new MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>(RecieveAudioStream, RecieveVideoStream, RecieveControl));


			online = !(MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE);
			isServer = MyAPIGateway.Multiplayer.IsServer || !online;

			if (MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE)
				return;



			

			var def = new packetheader() { steamid = 0, type = 0 };
			headerpacketlength = MyAPIGateway.Utilities.SerializeToBinary(def).Length;
			packetheaderbuffer = new byte[headerpacketlength];
			MyAPIGateway.Multiplayer.RegisterMessageHandler(videostreamcommand, RecievedMessage);
		}

		int headerpacketlength = 0;
		byte[] packetheaderbuffer;
		[ProtoContract]
		public struct packetheader
		{
			[ProtoMember(1)]
			public ushort type;
			[ProtoMember(2)]
			public ulong steamid;
		}
		private void recievedMessageInternal(byte[] obj, int offset, int length, ushort type, ulong steamid)
		{
			MyLog.Default.WriteLineAndConsole("recievedMessageInternal " + length.ToString());
			if (online && isServer)
			{
				foreach(IMyPlayer id in idents)
				{

					if (id.SteamUserId == steamid)
						continue;
					
					MyAPIGateway.Multiplayer.SendMessageTo(videostreamcommand, obj, id.SteamUserId);
				}
			}
			VideoBuffer buffer;
			if(!videoBuffer.TryGetValue(steamid, out buffer))
			{
				MyAPIGateway.Utilities.ShowMessage("Creating Buffer ", steamid.ToString());
				videoBuffer.Add(steamid, buffer = new VideoBuffer(steamid));
			}
			switch(type)
			{
				case 0: //use this for control in the future, add remove listeners?
					return;
				case 1:
					MyLog.Default.WriteLineAndConsole("AddToAudioBuffer " + length.ToString());
					buffer.AddToAudioBuffer(obj, offset, length);
					return;
				case 2:
					MyLog.Default.WriteLineAndConsole("AddToVideoBuffer " + length.ToString());
					buffer.AddToVideoBuffer(obj, offset, length);
					return;
				default:
					return;
			}
		}
		private void RecievedMessage(byte[] obj)
		{
			try
			{
				Buffer.BlockCopy(obj, 0, packetheaderbuffer, 0, headerpacketlength);
				var packet = MyAPIGateway.Utilities.SerializeFromBinary<packetheader>(packetheaderbuffer);
				recievedMessageInternal(obj, headerpacketlength, obj.Length - headerpacketlength, packet.type, packet.steamid);
			}
			catch
			{

			}

		}

		private void RecieveAudioStream(byte[] audio, int length)
		{
			//MyAPIGateway.Utilities.ShowMessage("GotPacket", length.ToString());
			MyLog.Default.WriteLineAndConsole("RecieveAudioStream " + length.ToString());

			//MyAPIGateway.Utilities.ShowMessage("GotPacket", length.ToString());
			if (isServer)
			{
				recievedMessageInternal(audio, 0, length, 1, 0);
				return;
			}
			if (!online)
				return;
			var header = new packetheader() { type = 1, steamid = MyAPIGateway.Multiplayer.MyId };
			var pheader = MyAPIGateway.Utilities.SerializeToBinary(header);
			var message = new byte[length + pheader.Length];//REEEE
			Buffer.BlockCopy(pheader, 0, message, 0, pheader.Length);
			Buffer.BlockCopy(audio, 0, message, pheader.Length, length);
			MyAPIGateway.Multiplayer.SendMessageToServer(videostreamcommand, message);
		}
		bool firstvideopacket = true;
		private void RecieveVideoStream(byte[] video, int length)
		{
			MyLog.Default.WriteLineAndConsole("RecieveVideoStream " + length.ToString()) ;
			//MyAPIGateway.Utilities.ShowMessage("GotPacket", length.ToString());
			//---------------------------
			//------- PREFORMAT GOES HERE
			//---------------------------

			if(firstvideopacket)
			{
				
				firstvideopacket = false;
			}
			else
			{
				length = EncodeImageToChar(video, length);
				
			}

			
			//int newLength = output.Length; set the new length of the frame?

			//---------------------------
			//------- END PREFORMAT------
			//---------------------------
			if (isServer)
			{
				recievedMessageInternal(video, 0,  length, 2, 0);
				return;
			}
			if (!online)
				return;
			var header = new packetheader() { type = 2, steamid = MyAPIGateway.Multiplayer.MyId };
			var pheader = MyAPIGateway.Utilities.SerializeToBinary(header);
			var message = new byte[length + pheader.Length];//REEEE
			Buffer.BlockCopy(pheader, 0, message, 0, pheader.Length);
			Buffer.BlockCopy(video, 0, message, pheader.Length, length);
			MyAPIGateway.Multiplayer.SendMessageToServer(videostreamcommand, message);
		}

		byte[] encodingbuffer = new byte[1]; // will grow to match. 
		/// <summary>
		/// Begin frame encoding code below
		/// </summary>
		int EncodeImageToChar(byte[] encodedFrame, int length)
		{
			if (encodedFrame.Length < sizeof(int) + sizeof(ushort) * 2)
				return encodedFrame.Length;
			int control = BitConverter.ToInt32(encodedFrame, 0 );
			ushort stride = BitConverter.ToUInt16(encodedFrame,   sizeof(int));
			ushort height = BitConverter.ToUInt16(encodedFrame, sizeof(int) + sizeof(ushort));
			//MyLog.Default.WriteLine($"Video Packet Header: c {control} s {stride} h {height}");
			var offset = sizeof(int) + sizeof(ushort) * 2;
			ushort newstride = (ushort)((stride / 3) *2);
			newstride += (ushort)(newstride % 2);
			//840
			//MyLog.Default.WriteLine("Mod - EncodeImageToChar newstride " + newstride.ToString());
			int encodedlength = newstride * height + offset;
			//MyLog.Default.WriteLine("Mod - EncodeImageToChar encodeln " + encodedlength.ToString());
			//199928
			if (encodingbuffer.Length < encodedlength)
			{
				encodingbuffer = new byte[encodedlength];
			}
			//len 299888
			//encodeln 199928
			MyAPIGateway.Parallel.For(0, height - 1, i => {
				//237
				int adjust = offset + i * stride;
				//adj 8 + 237 * 1260
				//adj = 298628
				int encadjust = i * newstride;
			//encadj 237 * 840
			//encadj 199080
				for (int ii = 0, eii = 0 ; ii + 2 < stride; ii+= 3, eii += 2)
				{
					//ii = 1257
					//adj + ii 299885
					byte r = encodedFrame[adjust + ii + 2];
					byte g = encodedFrame[adjust + ii + 1];
					byte b = encodedFrame[adjust + ii];
					BitConverter.GetBytes(ColorToChar(r, g, b)).CopyTo(encodingbuffer, encadjust + eii);
				}
			});
			Buffer.BlockCopy(BitConverter.GetBytes(newstride), 0, encodedFrame, sizeof(int) + sizeof(ushort), sizeof(ushort));
			Buffer.BlockCopy(encodingbuffer, 0, encodedFrame, offset, encodedlength - offset);
			return encodedlength + offset;
		}
		ushort ColorToChar(byte r, byte g, byte b)
		{
			return (ushort)((uint)0x3000 + ((r >> 3) << 10) + ((g >> 3) << 5) + (b >> 3));
		}


		int tick = 0;
		private List<IMyPlayer> oldplayers = new List<IMyPlayer>();
		private List<IMyPlayer> idents = new List<IMyPlayer>();

		public override void UpdateAfterSimulation()
		{
			//need to get new clients. 
			var swap = oldplayers;
			oldplayers = idents;
			idents = swap;
			idents.Clear();
			MyAPIGateway.Multiplayer?.Players?.GetPlayers(idents);
			foreach(var player in idents)
			{
				if(!oldplayers.Contains(player))
				{
					SendHeaders(player);
				}
			}
			if (!registered)
			{
				tick++;

				if (tick == 60)
				{
					MyLog.Default.WriteLineAndConsole("Sending Registration Request to Plugin");
					MyAPIGateway.Utilities.SendModMessage(20982309832901, new MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>(RecieveAudioStream, RecieveVideoStream, RecieveControl));
				}
				if (tick == 60)
					tick = 0;
			}
		}

		private void SendHeaders(IMyPlayer player)
		{
			if (!isServer)
				return;
			foreach(var buffer in videoBuffer)
			{
				if(buffer.Value.hasaudioheader)
				{
					var header = new packetheader() { type = 1, steamid = MyAPIGateway.Multiplayer?.MyId ?? 0 };
					var pheader = MyAPIGateway.Utilities.SerializeToBinary(header);
					var obj = buffer.Value.audioHeader.GetBytes();
					var message = new byte[pheader.Length + obj.Length];
					Buffer.BlockCopy(pheader, 0, message, 0, pheader.Length);
					Buffer.BlockCopy(obj, 0, message, pheader.Length, obj.Length);
					MyAPIGateway.Multiplayer.SendMessageTo(videostreamcommand, message, player.SteamUserId);
				}
				if(buffer.Value.hasvideoheader)
				{
					var header = new packetheader() { type = 2, steamid = MyAPIGateway.Multiplayer?.MyId ?? 0 };
					var pheader = MyAPIGateway.Utilities.SerializeToBinary(header);
					var obj = buffer.Value.videoHeader.GetBytes();
					var message = new byte[pheader.Length + obj.Length];
					Buffer.BlockCopy(pheader, 0, message, 0, pheader.Length);
					Buffer.BlockCopy(obj, 0, message, pheader.Length, obj.Length);
					MyAPIGateway.Multiplayer.SendMessageTo(videostreamcommand, message, player.SteamUserId);
				}

			}
		}

		private void RecieveControl(int obj)
		{
			MyLog.Default.WriteLineAndConsole("Registration Complete");
			registered = true;
		}
	}
}
