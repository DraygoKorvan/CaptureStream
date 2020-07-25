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
using SENetworkAPI;
using System.Collections.Concurrent;
using LCDText2;
using System.Diagnostics;

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
			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(COMID, NETWORKNAME);
			}
			if (!init)
			{
				HudAPI = new HudAPIv2(RegisterHudAPI);
				init = true;
				offline = MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE;
				isServer = offline || MyAPIGateway.Multiplayer.IsServer;
				isDedicated = isServer && MyAPIGateway.Utilities.IsDedicated;
				time.Start();
			}

			foreach(var controller in controllers)
			{
				controller.Value.SendUpdate(time.ElapsedTicks);
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
