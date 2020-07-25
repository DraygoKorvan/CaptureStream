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

			MyAPIGateway.Utilities.SendModMessage(20982309832901, new MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>(RecieveAudioStream, RecieveVideoStream, RecieveControl));
			if (MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE)
				return;

			online = true;

			isServer = MyAPIGateway.Multiplayer.IsServer;

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
			if(isServer)
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
				videoBuffer.Add(steamid, buffer = new VideoBuffer(steamid));
			}
			switch(type)
			{
				case 0: //use this for control in the future, add remove listeners?
					return;
				case 1:
					buffer.AddToAudioBuffer(obj, offset, length);
					return;
				case 2:
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
			if(isServer)
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
			//---------------------------
			//------- PREFORMAT GOES HERE
			//---------------------------
			int offset = 0;
			if(firstvideopacket)
			{
				offset += VideoBuffer.VideoHeader.Length();
				firstvideopacket = false;
			}
			var newlength = EncodeImageToChar(video, offset);
			length = newlength;
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
		int EncodeImageToChar(byte[] encodedFrame, int offset)
		{

			int control = BitConverter.ToInt32(encodedFrame, offset);
			ushort stride = BitConverter.ToUInt16(encodedFrame, offset + sizeof(int));
			ushort height = BitConverter.ToUInt16(encodedFrame, offset + sizeof(uint));
			offset += sizeof(int) + sizeof(uint) * 2;
			ushort newstride = (ushort)((stride / 3) *2);
			newstride += (ushort)(newstride % 2);
			int encodedlength = newstride * height + offset;
			if (encodingbuffer.Length < encodedlength)
			{
				encodingbuffer = new byte[encodedlength];
			}
		
			MyAPIGateway.Parallel.For(0, height, i => {
				int adjust = offset + i * stride;
				int encadjust = offset + i * newstride;
				for(int ii = 0; ii + 2 < stride; ii+= 3)
				{
					byte r = encodedFrame[adjust + (ii * 3) + 2];
					byte g = encodedFrame[adjust + (ii * 3) + 1];
					byte b = encodedFrame[adjust + (ii * 3)];
					BitConverter.GetBytes(ColorToChar(r, g, b)).CopyTo(encodingbuffer, encadjust + ii * 2);
				}
			});
			Buffer.BlockCopy(BitConverter.GetBytes(newstride), 0, encodedFrame, offset + sizeof(int), sizeof(ushort));
			Buffer.BlockCopy(encodingbuffer, offset, encodedFrame, offset, encodedlength);
			return encodedlength;
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
			registered = true;
		}
	}
}
