﻿using Sandbox.Common.ObjectBuilders;
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
using LCDStreamerMod;

namespace LocalLCD
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel), true)]
	public class LocalLCDWriterComponent : MyGameLogicComponent
	{
		MyObjectBuilder_EntityBase ObjectBuilder;
		IMyTextPanel TextPanel;
		ulong m_CurrentBuffer;

		public ulong CurrentBuffer
		{
			get
			{
				return m_CurrentBuffer;
			}
			set
			{
				MyLog.Default.WriteLine($"set CurrentBuffer {value}");
				if(m_CurrentBuffer != value)
				{
					Changed_Channel(m_CurrentBuffer, value);
					m_CurrentBuffer = value;
				}

			}
		}


		//public VideoPlayerScript Script;
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
			
			base.Init(objectBuilder);
			ulong currentchannel;
			if(LCDWriterCore.TryRegisterAndGetChannel(this, out currentchannel))
			{
				UpdateChannelInternal(currentchannel);

			}
			else
			{
				m_CurrentBuffer = 1;
			}

			this.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
		}

		public override void UpdateOnceBeforeFrame()
		{

			if (!controls_created)
			{

				CreateTerminalControls();
			}
			//get all the panels

		}

		private void Changed_Channel(ulong arg1, ulong arg2)
		{
			MyLog.Default.WriteLine($"Changed Channel {arg1} -> {arg2}");
			//MyAPIGateway.Utilities.ShowMessage("Player", $"Changed Channel {arg1} -> {arg2}");
			if (arg1 > 1)
				LCDWriterCore.instance.Unsubscribe(this, arg1);
			if(arg2 > 1)
			{
				LCDWriterCore.instance.Subscribe(this, arg2);
				//if (Script == null)
				//	Script = new VideoPlayerScript(this.Entity as IMyTextPanel);
			}

			LCDWriterCore.instance.UpdateChannel(this, arg2, MyAPIGateway.Multiplayer?.MyId ?? 0);
		}
		public void Register(VideoPlayerTSS script)
		{
			lock(surfaces)
			{
				if(!surfaces.Contains(script))
					surfaces.Add(script);
			}
		}
		public void Unregister(VideoPlayerTSS script)
		{
			lock (surfaces)
			{
				if (surfaces.Contains(script))
					surfaces.Remove(script);
			}
		}

		internal void UpdateChannelInternal(ulong channel)
		{
			if (m_CurrentBuffer > 1)
				LCDWriterCore.instance.Unsubscribe(this, m_CurrentBuffer);
			if (channel > 1)
			{
				LCDWriterCore.instance.Subscribe(this, channel);
				//if (Script == null)
				//	Script = new VideoPlayerScript(this.Entity as IMyTextPanel);
			}
			else
			{
				//Script.Close();
				//Script = null;
			}
		}

		internal void PlayAudio(byte[] audioframes, int bytes, int sampleRate)
		{
			foreach (var surface in surfaces)
			{
				surface.PlayAudio(audioframes, bytes, sampleRate);
			}
			//if(Script != null)
			//	Script.PlayAudio(audioframes, bytes, sampleRate);
		}
		List<VideoPlayerTSS> surfaces = new List<VideoPlayerTSS>();
		public override void OnAddedToContainer()
		{


			TextPanel = Entity as IMyTextPanel;
			this.NeedsUpdate |= VRage.ModAPI.MyEntityUpdateEnum.EACH_FRAME;
			var textprov = TextPanel as IMyTextSurfaceProvider;


		}



		private void TextPanel_ChannelChanged(IMyTerminalBlock obj)
		{
			if(obj == null)
			{
				return;
			}
			if (Selected == -1)
			{
				if (CurrentBuffer == 1)
				{
					Selected = 0;
					
				}
				else
				{

				}
			}
			if (Selected == 0)
			{
				//if(Script != null)
				//{
				//	Script.DeletePlayer();
				//}
				CurrentBuffer = 1;
				return;
			}
			if(Selected < Obj.Count)
			{
				//if(Script == null)
				//	Script = new VideoPlayerScript(this.Entity as IMyTextPanel);
				ulong channel;
				if(SteamIdGetter.TryGetValue(Selected, out channel))
				{
					MyAPIGateway.Utilities.ShowMessage("Player", $"Selected Channel {channel}");
					CurrentBuffer = channel;
				}

			}
        }
		public override void UpdateAfterSimulation()
		{
			//if(Script != null)
			//	Script.Update();
		}

		internal void SetFontSize(float width, float height)
		{
			foreach(var surface in surfaces)
			{
				surface.SetFontSize(width, height);
			}
			//if (Script != null)
			//	Script.SetFontSize(width, height);
		}


		internal void SetChannel(ulong currentChannel)
		{


			//network updates set this. 
			//CurrentBuffer.Value = currentChannel;
			TextPanel_ChannelChanged(this.Entity as IMyTerminalBlock);
        }

		internal void PlayNextFrame(string[] data, int offset, int width, int stride, int height)
		{
			foreach (var surface in surfaces)
			{
				surface.PlayNextFrame(data, offset, width, stride, height);
			}
			//if (Script != null)
			//	Script.PlayNextFrame(data, offset, width, stride, height);
		}

		public override void OnRemovedFromScene()
		{
			
			base.OnRemovedFromScene();
		}
		public static bool controls_created = false;
		private static MyStringId None = MyStringId.GetOrCompute("None");
        public static void CreateTerminalControls()
		{
			if (controls_created)
				return;
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
				LCDWriterCore.instance.Unsubscribe(this, CurrentBuffer);
			
			//if (Script != null)
			//	Script.Close();
		}
	}
}
