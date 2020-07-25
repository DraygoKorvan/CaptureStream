using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using Draygo.API;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Utils;
using Sandbox.Definitions;
using VRage.Game;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using LCDText2;
using SENetworkAPI;

namespace LocalLCD
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), true)]
	public class LocalLCDWriterComponent : MyNetworkGameLogicComponent
	{
		MyObjectBuilder_EntityBase ObjectBuilder;
		IMyTextPanel TextPanel;
		NetSync<ulong> CurrentBuffer;
		public VideoPlayerScript Script;
		long Selected = -1;

		private static IMyTerminalControlCombobox ComboListBox;

		public static bool SupportsMultipleBlocks
		{
			get
			{
				return true;
			}
			private set
			{

			}
		}

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			
			ObjectBuilder = objectBuilder;
			

			if (!(MyAPIGateway.Session.OnlineMode == VRage.Game.MyOnlineModeEnum.OFFLINE) && !NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(LCDWriterCore.COMID, LCDWriterCore.NETWORKNAME);
			}
			base.Init(objectBuilder);
			
			CurrentBuffer = new NetSync<ulong>(this, TransferType.Both, 1);
			CurrentBuffer.SyncOnLoad = true;
			CurrentBuffer.LimitToSyncDistance = true;
			CurrentBuffer.ValueChanged += Changed_Channel;

			if (!controls_created)
			{

				CreateTerminalControls();
			}
		}

		private void Changed_Channel(ulong arg1, ulong arg2)
		{
			MyAPIGateway.Utilities.ShowMessage("Player", $"Changed Channel {arg1} -> {arg2}");
			if (arg1 > 1)
				LCDWriterCore.instance.Unsubscribe(this, arg1);
			if(arg2 > 1)
				LCDWriterCore.instance.Subscribe(this, arg2);
		}

		internal void PlayAudio(byte[] audioframes, int bytes, VideoBuffer.AudioHeader audioHeader)
		{

			if(Script != null)
				Script.PlayAudio(audioframes, bytes, audioHeader);
		}

		public override void OnAddedToContainer()
		{


			TextPanel = Entity as IMyTextPanel;
			this.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
			
		}

		private void TextPanel_ChannelChanged(IMyTerminalBlock obj)
		{
			if(obj == null)
			{
				return;
			}
			if (Selected == -1)
			{
				if (CurrentBuffer.Value == 1)
				{
					Selected = 0;
					
				}
				else
				{

				}
			}
			if (Selected == 0)
			{
				if(Script != null)
				{
					Script.DeletePlayer();
				}
				CurrentBuffer.Value = 1;
				return;
			}
			if(Selected < Obj.Count)
			{
				if(Script == null)
					Script = new VideoPlayerScript(this.Entity as IMyTextPanel);
				ulong channel;
				if(SteamIdGetter.TryGetValue(Selected, out channel))
				{
					MyAPIGateway.Utilities.ShowMessage("Player", "Selected Channel 2");
					CurrentBuffer.Value = channel;
				}

			}
        }
		public override void UpdateAfterSimulation()
		{
			if(Script != null)
				Script.Update();
		}
		internal void PlayNextFrame(string s_frame)
		{
			MyLog.Default.WriteLineAndConsole($"LocalLCDWriterComponent PlayNextFrame- {Script != null}");
			if (Script != null)
				Script.PlayNextFrame(s_frame);
		}



		internal void SetChannel(ulong currentChannel)
		{
			//network updates set this. 
			//CurrentBuffer.Value = currentChannel;
			TextPanel_ChannelChanged(this.Entity as IMyTerminalBlock);
        }



		public override void OnRemovedFromScene()
		{
			
			base.OnRemovedFromScene();
		}
		public static bool controls_created = false;
		private static MyStringId None = MyStringId.GetOrCompute("None");
        public static void CreateTerminalControls()
		{
			controls_created = true;
			if (ComboListBox == null)
			{
				
				ComboListBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyTextPanel>("Script List");
				ComboListBox.Enabled = ControlEnabled;
				ComboListBox.Title = MyStringId.GetOrCompute("Channel List");
				ComboListBox.Visible = ControlVisible;
				ComboListBox.ComboBoxContent = (x) => GetContent(x);
				ComboListBox.SupportsMultipleBlocks = SupportsMultipleBlocks;
				ComboListBox.Setter = Setter;
				ComboListBox.Getter = (x) => Getter(x);
				MyAPIGateway.TerminalControls.AddControl<IMyTextPanel>(ComboListBox);

				var LineItem = new MyTerminalControlComboBoxItem();
				LineItem.Key = 0;
				LineItem.Value = None;
				Obj.Add(LineItem);
				SteamIdGetter.Add(LineItem.Key, 0);
				LineGetter.Add(0, LineItem.Key);
			}
			
		}
		static Dictionary<long, ulong> SteamIdGetter = new Dictionary<long, ulong>();
		static Dictionary<ulong, long> LineGetter = new Dictionary<ulong, long>();
		public static void AddLineItem(MyStringId Item, ulong steamid)
		{
			if (LineGetter.ContainsKey(steamid))
				return;
			var LineItem = new MyTerminalControlComboBoxItem();
			LineItem.Key = Obj.Count;
			LineItem.Value = Item;
			Obj.Add(LineItem);
			SteamIdGetter.Add(LineItem.Key, steamid);
			LineGetter.Add(steamid, LineItem.Key);
		}

		private static long Getter(IMyTerminalBlock block)
		{
			LocalLCDWriterComponent OutValue = null;

			OutValue = block.GameLogic.GetAs<LocalLCDWriterComponent>();
			if(OutValue == null)
			{
				//MyAPIGateway.Utilities.ShowMessage("Outvalue", "Null");
				return 0;
			}
           
			return OutValue.GetSelectedRow();
			
		}

		private long GetSelectedRow()
		{
			if(Selected != -1)
				return Selected;
			return 0;
		}

		private static void Setter(IMyTerminalBlock block, long arg2)
		{
			LocalLCDWriterComponent OutValue = null;


			OutValue = block.GameLogic.GetAs<LocalLCDWriterComponent>();
			if (OutValue == null)
			{
				return;
			}

			OutValue.SetSelectedRow(arg2);
		}

		private void SetSelectedRow(long arg)
		{
			Selected = arg;
			TextPanel_ChannelChanged(TextPanel); 
		}

		private static List<MyTerminalControlComboBoxItem> Obj = new List<MyTerminalControlComboBoxItem>();
		private static void GetContent(List<MyTerminalControlComboBoxItem> obj)
		{
			foreach(var item in Obj)
			{
				obj.Add(item);
			}
		}

		private static bool ControlVisible(IMyTerminalBlock arg)
		{
			if (arg is IMyTextPanel)
			{
				return true;
			}
			return false;
		}

		private static bool ControlEnabled(IMyTerminalBlock arg)
		{
			if(arg is IMyTextPanel)
			{
				return true;
			}
			return false;
		}

		public override void MarkForClose()
		{
			if (Selected > 0)
				LCDWriterCore.instance.Unsubscribe(this, CurrentBuffer.Value);
			
			if (Script != null)
				Script.Close();
		}
	}
}
