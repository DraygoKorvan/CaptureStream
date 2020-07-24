using LCDText2;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace LocalLCD
{
	public class LCDScript
	{
		public IMyTextPanel Panel;
		public StringBuilder OutputString = new StringBuilder();
		IMyGridTerminalSystem GTS;

		public string Name = "Script";

		public IMyTerminalBlock Me
		{
			get
			{
				return Panel;
			}
		}
		public IMyGridTerminalSystem GridTerminalSystem
		{
			get
			{
				return GTS;
			}
		}

		public LCDScript(IMyTextPanel panel)
		{
			this.Panel = panel;
			
			var grid = (IMyCubeGrid)panel.Parent;
			GTS = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
		}


		public virtual void Update()
		{

		}
		public virtual void Update10()
		{

		}
		public virtual void Update100()
		{

		}

		public virtual void Echo(string EchoString)
		{
			//ignore
		}

		public virtual void SetStream(VideoBuffer buffer)
		{

		}

		public virtual void Close()
		{

		}

	}
}
