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
		MyStringId PanelType;
		NetSync<ulong> CurrentBuffer;
		MyDefinitionId TexDef;
		public VideoPlayerScript Script;
		bool init = false;
		double m_Scale = 1.0d;
		bool isInit = false;
		long Selected = -1;

		private static IMyTerminalControlCombobox ComboListBox;

		public double Scale
		{
			get
			{
				return m_Scale;
			}
			private set
			{
				if(m_Scale != value)
				{
					m_Scale = value;
				}
			}
		}

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
            base.Init(objectBuilder);
			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(LCDWriterCore.COMID, LCDWriterCore.NETWORKNAME);
			}
			CurrentBuffer = new NetSync<ulong>(this, TransferType.Both, 1);
		}

		public override void OnAddedToContainer()
		{
			if (this.Entity.Physics == null)
				return;
			TextPanel = Entity as IMyTextPanel;
			this.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_10TH_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME | VRage.ModAPI.MyEntityUpdateEnum.EACH_100TH_FRAME;


			InitScript();


		}
		string step = string.Empty;



		private void InitScript()
		{
			//TextPanel.CustomDataChanged += TextPanel_CustomDataChanged;
			Script = null;
			TextPanel_ScriptChanged(TextPanel);//call it
			init = true;
		}
		private void TextPanel_ScriptChanged(IMyTerminalBlock obj)
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
				Script = null;

				//TextDisplay.Message.Clear();
				return;
			}
			if(Selected < Obj.Count)
			{
				//CurrentBuffer = Obj[(int)Selected].Value;
				if (LCDWriterCore.instance != null)
				{
					
				}
			}
        }

		public override void UpdateBeforeSimulation()
		{
			if (LCDWriterCore.instance == null || LCDWriterCore.instance.isDedicated)
				return;


			if (Selected <= 0)
			{

			}
			if (Script == null)
			{
				return;
			}
			Script.Update();
		}


		internal void SetChannel(ulong currentChannel)
		{
			//network updates set this. 
			CurrentBuffer.Value = currentChannel;
			TextPanel_ScriptChanged(this.Entity as IMyTerminalBlock);
        }



		internal void UpdateScript()
		{
			if(Selected == -1)
			{
				TextPanel_ScriptChanged(this.Entity as IMyTerminalBlock);
            }
		}


		public override void OnRemovedFromScene()
		{
			
			base.OnRemovedFromScene();
		}
		private static MyStringId None = MyStringId.GetOrCompute("None");
        public static void CreateTerminalControls()
		{
			if(ComboListBox == null)
			{
				ComboListBox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyTextPanel>("Script List");
				ComboListBox.Enabled = ControlEnabled;
				ComboListBox.Title = MyStringId.GetOrCompute("Script List");
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
            }
			
		}

		public static void AddLineItem(MyStringId Item)
		{
			var LineItem = new MyTerminalControlComboBoxItem();
			LineItem.Key = Obj.Count;
			LineItem.Value = Item;
			Obj.Add(LineItem);
		}

		private static long Getter(IMyTerminalBlock block)
		{
			LocalLCDWriterComponent OutValue = null;

			OutValue = block.GameLogic.GetAs<LocalLCDWriterComponent>();
			if(OutValue == null)
			{
				MyAPIGateway.Utilities.ShowMessage("Outvalue", "Null");
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
			TextPanel_ScriptChanged(TextPanel); 
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
			if (Script != null)
				Script.Close();
		}
	}
}
