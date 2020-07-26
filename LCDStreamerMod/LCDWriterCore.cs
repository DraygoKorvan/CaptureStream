using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using Draygo.API;
using VRageMath;
using VRage.Utils;
using Sandbox.ModAPI;
using VRage.ModAPI;
using System.Runtime.InteropServices;
using VRage;
using System.Collections.Concurrent;
using LCDText2;
using System.Diagnostics;
using ProtoBuf;
using VRage.Game.ModAPI;

namespace LocalLCD
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class LCDWriterCore : MySessionComponentBase
	{
		bool init = false;
		public HudAPIv2 HudAPI;
		public static LCDWriterCore instance;
		public bool isDedicated = false;
		public bool offline = false;
		public bool isServer = false;
		public const ushort COMID = 8723;
		public const string NETWORKNAME = "LCDPlayer";
		Dictionary<ulong, VideoController> controllers = new Dictionary<ulong, VideoController>();
		public LCDWriterCore()
		{
			instance = this;
		}

		Stopwatch time = new Stopwatch();



		public override void UpdateAfterSimulation()
		{
			if (!init)
			{
				HudAPI = new HudAPIv2(RegisterHudAPI);
				init = true;
				offline = MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE;
				if (!offline)
					MyAPIGateway.Multiplayer.RegisterMessageHandler(COMID, UpdateChannelRecieve);
				isServer = offline || MyAPIGateway.Multiplayer.IsServer;
				isDedicated = isServer && MyAPIGateway.Utilities.IsDedicated;
				time.Start();
			}

			foreach(var controller in controllers)
			{
				controller.Value.SendUpdate(time.ElapsedTicks);
			}
		}
		[ProtoContract]
		public struct ChannelUpdateMessage
		{
			[ProtoMember(1)]
			public long EntityId;
			[ProtoMember(2)]
			public ulong Channel;
			[ProtoMember(3)]
			public ulong Setter;
		}
		
		private void UpdateChannelRecieve(byte[] obj)
		{
			try
			{
				var update = MyAPIGateway.Utilities.SerializeFromBinary<ChannelUpdateMessage>(obj);
				if (isServer)
				{
					SendToEveryoneExcept(obj, update.Setter);
				}
				UpdateInternal(update.EntityId, update.Channel);
			}
			catch
			{

			}


		}

		private void UpdateInternal(long entityId, ulong channel)
		{
			ChannelRegister value;
			if(ChannelDictionary.TryGetValue(entityId, out value))
			{
				value.channel = channel;
				if(value.component != null)
				{
					value.component.UpdateChannelInternal(channel);
				}
			}
		}

		private void SendUpdate(long entityId, ulong arg2, ulong requester)
		{
			if (offline)
				return;
			var update = MyAPIGateway.Utilities.SerializeToBinary(new ChannelUpdateMessage() { EntityId = entityId, Channel = arg2, Setter = requester });
			if (isServer)
			{
				SendToEveryoneExcept(update, requester);
			}
			else
			{
				MyAPIGateway.Multiplayer.SendMessageToServer(COMID, update);
			}
		}
		static List<IMyPlayer> players = new List<IMyPlayer>();
		static void SendToEveryoneExcept(byte[] obj, ulong steamidexception)
		{
			if (!instance.init)
				return;
			if (!instance.isServer)
				return;
			if (instance.offline)
				return;
			players.Clear();
			MyAPIGateway.Multiplayer.Players.GetPlayers(players);
			foreach(IMyPlayer player in players)
			{
				if (player.SteamUserId != steamidexception)
					MyAPIGateway.Multiplayer.SendMessageTo(COMID, obj, player.SteamUserId);
			}
		}

		private void RegisterHudAPI()
		{

		}

		internal void Unsubscribe(LocalLCDWriterComponent localLCDWriterComponent, ulong steamid)
		{
			VideoController controller;
			if (controllers.TryGetValue(steamid, out controller))
			{
				controller.UnSubscribe(localLCDWriterComponent);
			}
		}

		internal void Subscribe(LocalLCDWriterComponent localLCDWriterComponent, ulong steamid)
		{
			VideoController controller;
			if (controllers.TryGetValue(steamid, out controller))
			{
				controller.Subscribe(localLCDWriterComponent, time.ElapsedTicks);
			}
		}
		public class ChannelRegister
		{
			public ulong channel = 0;
			public LocalLCDWriterComponent component;
		}


		public static Dictionary<long, ChannelRegister> ChannelDictionary = new Dictionary<long, ChannelRegister>();
		internal void UpdateChannel(LocalLCDWriterComponent Comp, ulong arg2, ulong requester)
		{
			ChannelRegister value;
			if(!ChannelDictionary.TryGetValue(Comp.Entity.EntityId, out value))
			{
				value = new ChannelRegister();
				value.component = Comp;
				ChannelDictionary.Add(Comp.Entity.EntityId, value);
			}
			value.channel = arg2;
			if(!offline)
				SendUpdate(Comp.Entity.EntityId, arg2, requester);
		}

		internal static bool TryRegisterAndGetChannel(LocalLCDWriterComponent Comp, out ulong channel)
		{
			ChannelRegister reg;
			var ret = ChannelDictionary.TryGetValue(Comp.Entity.EntityId, out reg);
			if(ret)
			{
				channel = reg.channel;
				if (reg.component == null)
					reg.component = Comp;
				return true;
			}
			reg = new ChannelRegister();
			reg.component = Comp;
			ChannelDictionary.Add(Comp.Entity.EntityId, reg);
			channel = 0;
			return false;
		}


		internal void AddBuffer(VideoBuffer videoBuffer)
		{
			MyLog.Default.WriteLine("AddBuffer");
			VideoController Component;
			if (controllers.TryGetValue(videoBuffer.steamid, out Component))
			{
				Component = new VideoController(videoBuffer);

			}
			else
			{
				Component = new VideoController(videoBuffer);
				controllers.Add(videoBuffer.steamid, Component);
				MyLog.Default.WriteLine("AddLineItem Called from AddBuffer");
				LocalLCDWriterComponent.AddLineItem(MyStringId.GetOrCompute(videoBuffer.steamid.ToString()), videoBuffer.steamid);
			}
			if (time.IsRunning)
			{
				Component.SetRunTime(time.ElapsedTicks);
			}
		}

		protected override void UnloadData()
		{
			if (time.IsRunning)
				time.Stop();
			time = null;
			base.UnloadData();
		}
	}
}
