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

namespace LocalLCD
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class LCDWriterCore : MySessionComponentBase
	{
		bool init = false;
		public HudAPIv2 HudAPI;
		public static LCDWriterCore instance;
		public bool isDedicated = false;
		public bool isServer = false;
		public const ushort COMID = 8723;
		public const string NETWORKNAME = "LCDPlayer";

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
				LocalLCDWriterComponent.CreateTerminalControls();
				isServer = MyAPIGateway.Multiplayer.IsServer;//only want to send net if server
				isDedicated = isServer && MyAPIGateway.Utilities.IsDedicated;

			}

		}

		private void RegisterHudAPI()
		{

		}
	}
}
