using LocalLCD;
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
using static LCDText2.VideoBuffer;

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
		bool registeredmessagehandler = false;
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

			//var def = new packetheader() { steamid = 0, type = 0 };
			registeredmessagehandler = true;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(videostreamcommand, RecievedMessage);
		}

		protected override void UnloadData()
		{
			if(registeredmessagehandler)
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(videostreamcommand, RecievedMessage);
			base.UnloadData();
		}


		[ProtoContract]
		public struct packetheader
		{
			[ProtoMember(1)]
			public ushort type;
			[ProtoMember(2)]
			public ulong steamid;
			[ProtoMember(3)]
			public byte[] packed;
		}
		private void recievedMessageInternal(byte[] obj, int length, ushort type, ulong steamid)
		{
			//MyLog.Default.WriteLineAndConsole("recievedMessageInternal " + length.ToString());
			//MyLog.Default.WriteLine($"Value check {BitConverter.ToInt32(obj, offset)}");
			//MyLog.Default.WriteLineAndConsole($"isServer {online} {isServer} {steamid == (MyAPIGateway.Multiplayer?.MyId ?? 0)}");
			if (online && isServer && steamid == MyAPIGateway.Multiplayer.MyId)
			{
				SendMessageToOthersExcept(obj, length,  type);
			}
			VideoBuffer buffer;
			lock (videoBuffer)
			{
				
				if (!videoBuffer.TryGetValue(steamid, out buffer))
				{
					MyAPIGateway.Utilities.ShowMessage("Creating Channel ", steamid.ToString());
					videoBuffer.Add(steamid, buffer = new VideoBuffer(steamid));
				}
			}

			switch(type)
			{
				case 0: //use this for control in the future, add remove listeners?
					return;
				case 1:
					//MyLog.Default.WriteLineAndConsole("AddToAudioBuffer " + length.ToString());
					buffer.AddToAudioBuffer(obj, length);
					return;
				case 2:
					//MyLog.Default.WriteLineAndConsole("AddToVideoBuffer " + length.ToString());
					buffer.AddToVideoBuffer(obj,  length);
					return;
				default:
					return;
			}
		}
		private void RecievedMessage(byte[] obj)
		{
			LCDWriterCore.debugMonitor.RecieveNetworkStream += obj.Length;
			//MyLog.Default.WriteLineAndConsole($"RecievedMessage getting header {obj.Length}");
			try
			{
				var message = MyAPIGateway.Utilities.SerializeFromBinary<packetheader>(obj);
				//MyLog.Default.WriteLineAndConsole($"RecievedMessage packet {message.steamid} {message.type}");
				recievedMessageInternal(message.packed,  message.packed.Length, message.type, message.steamid);
			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole(ex.ToString());
			}

		}

		private void RecieveAudioStream(byte[] audio, int length)
		{
			LCDWriterCore.debugMonitor.RecieveAudioStream = length;
			recievedMessageInternal(audio, length, 1, MyAPIGateway.Multiplayer?.MyId ?? (ulong)0);

			if (!online)
				return;
			if(!isServer)
			{
				var packed = new byte[length];
				Buffer.BlockCopy(audio, 0, packed, 0, length);
				var packet = new packetheader() { type = 1, steamid = MyAPIGateway.Multiplayer.MyId, packed = packed };
				var message = MyAPIGateway.Utilities.SerializeToBinary(packet);
				MyAPIGateway.Multiplayer.SendMessageToServer(videostreamcommand, message);
			}

		}



		private void RecieveVideoStream(byte[] video, int length)
		{
			//MyLog.Default.WriteLineAndConsole("RecieveVideoStream " + length.ToString()) ;
			//MyAPIGateway.Utilities.ShowMessage("GotPacket", length.ToString());
			LCDWriterCore.debugMonitor.RecieveVideoStream = length;

			//length = EncodeImageToChar(video, length);

			if (isServer)
			{
				recievedMessageInternal(video,   length, 2, MyAPIGateway.Multiplayer?.MyId ?? (ulong)0);
				return;
			}
			if (!online)
				return;
			if (!isServer)
			{
				var packed = new byte[length];
				Buffer.BlockCopy(video, 0, packed, 0, length);
				var packet = new packetheader() { type = 2, steamid = MyAPIGateway.Multiplayer.MyId, packed = packed };
				var message = MyAPIGateway.Utilities.SerializeToBinary(packet);

				MyAPIGateway.Multiplayer.SendMessageToServer(videostreamcommand, message);
			}
		}
		private void SendMessageToOthersExcept(byte[] obj, int length, ushort type)
		{
			//MyLog.Default.WriteLineAndConsole("SendMessageToOthersExcept");

			var connectedidents = new List<IMyPlayer>();
			if (MyAPIGateway.Multiplayer?.Players == null)
				return;
			MyAPIGateway.Multiplayer.Players.GetPlayers(connectedidents);
			//MyLog.Default.WriteLineAndConsole("gotplayers?");

			var packed = new byte[length];
			Buffer.BlockCopy(obj, 0, packed, 0, length);
			var header = new packetheader() { type = type, steamid = MyAPIGateway.Multiplayer.MyId , packed = packed };
			byte[] message = MyAPIGateway.Utilities.SerializeToBinary(header);
					
				//MyLog.Default.WriteLine($"header {header.type} {header.steamid}");
	
	
			foreach (IMyPlayer id in connectedidents)
			{

				if (id.SteamUserId == MyAPIGateway.Multiplayer.MyId)
					continue;
				//MyLog.Default.WriteLineAndConsole($"sendMessageTo  {id.SteamUserId}  {message.Length}" );
				MyAPIGateway.Multiplayer.SendMessageTo(videostreamcommand, message, id.SteamUserId);
			}

		}
		//byte[] encodingbuffer = new byte[1]; // will grow to match. 
		/// <summary>
		/// Begin frame encoding code below
		/// </summary>



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

			if (!registered)
			{
				tick++;

				if (tick == 60)
				{
					//MyLog.Default.WriteLineAndConsole("Sending Registration Request to Plugin");
					MyAPIGateway.Utilities.SendModMessage(20982309832901, new MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>(RecieveAudioStream, RecieveVideoStream, RecieveControl));
				}
				if (tick == 60)
					tick = 0;
			}
		}



		private void RecieveControl(int obj)
		{
			MyLog.Default.WriteLineAndConsole("Registration Complete");
			registered = true;
		}
	}
}
