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

namespace LocalLCD
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class LCDWriterCore : MySessionComponentBase
	{
		bool init = false;
		public HudAPIv2 HudAPI;
		public static LCDWriterCore instance;
		public static double ScaleAdjust = 7.16d;
		//public static double YAdjust = 2.735d;
		public Dictionary<MyStringId, Func<IMyTextPanel, LCDScript>> TypeDef = new Dictionary<MyStringId, Func<IMyTextPanel, LCDScript>>();
		public Dictionary<long, long> ScriptHashTable = new Dictionary<long, long>();
		public Dictionary<long, LocalLCDWriterComponent> LocalLCDWriter = new Dictionary<long, LocalLCDWriterComponent>();
		public bool isDedicated = false;
		public bool isServer = false;
		private static Action Register;
		private static readonly ushort REGISTERNET = 20383;
		internal static readonly Guid ModGuid = new Guid("edef9e17-23ae-4e92-ad9e-a2444958307b");

		public override void UpdateAfterSimulation()
		{
			if (!init)
			{
				HudAPI = new HudAPIv2(RegisterHudAPI);
				init = true;
				LocalLCDWriterComponent.CreateTerminalControls();
				InitScripts();
				InitLCDScreens();
				isServer = MyAPIGateway.Multiplayer.IsServer;//only want to send net if server
				isDedicated = isServer && MyAPIGateway.Utilities.IsDedicated;
				if(MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE  )
				{
					if (!isServer)
						MyAPIGateway.Multiplayer.RegisterMessageHandler(REGISTERNET, ServerMessageToClient);
					else
						MyAPIGateway.Multiplayer.RegisterMessageHandler(REGISTERNET, ClientMessageToServer);
				}
			}
		}
		public void UpdateLCDForNetwork(IMyTerminalBlock block, LocalLCDWriterComponent localLCDWriterComponent)
		{
			if (!MyAPIGateway.Multiplayer.MultiplayerActive)
				return;
			if(isServer)
			{
				UpdateClients(block, localLCDWriterComponent);
				return;
            }
			SendMessageToServer(block, localLCDWriterComponent);
			return;
		}

		static NetworkMessage Message;
		static Serializer NetworkSerializer = new Serializer(null);
		byte[] buffer = new byte[NetworkMessage.Size];
		private void SendMessageToServer(IMyTerminalBlock block, LocalLCDWriterComponent localLCDWriterComponent)
		{
			Message.EntityId = block.EntityId;
			Message.CurrentScript = localLCDWriterComponent.Script?.Name.GetHashCode() ?? 0;
			NetworkSerializer.Init(buffer);
			Message.Serialize(ref NetworkSerializer);
			MyAPIGateway.Multiplayer.SendMessageToServer(REGISTERNET, buffer, true);
		}

        private void UpdateClients(IMyTerminalBlock block, LocalLCDWriterComponent localLCDWriterComponent)
		{
			if(!isServer)
			{
				return;
			}
			Message.EntityId = block.EntityId;
			Message.CurrentScript = localLCDWriterComponent.Script?.Name.GetHashCode() ?? 0;
			NetworkSerializer.Init(buffer);
			Message.Serialize(ref NetworkSerializer);
			MyAPIGateway.Multiplayer.SendMessageToOthers(REGISTERNET, buffer, true);
		}

		private void ServerMessageToClient(byte[] obj)
		{
			if (obj.Length == buffer.Length)
			{
				NetworkSerializer.Init(obj);
				NetworkMessage.Deserialize(ref NetworkSerializer, out Message);
				LocalLCDWriterComponent ScriptComponent;
				if (LocalLCDWriter.TryGetValue(Message.EntityId, out ScriptComponent))
				{
					long scriptindex;
					if (ScriptHashTable.TryGetValue(Message.CurrentScript, out scriptindex))
					{
						ScriptComponent.SetScript(scriptindex);
					}
				}
			}
		}

		private void ClientMessageToServer(byte[] obj)
		{
			if(isServer)
			{
				return;
			}
			if(obj.Length == buffer.Length)
			{
				NetworkSerializer.Init(obj);
				NetworkMessage.Deserialize(ref NetworkSerializer, out Message);
				LocalLCDWriterComponent ScriptComponent;
				if (LocalLCDWriter.TryGetValue(Message.EntityId, out ScriptComponent))
                {
					long scriptindex;
					if(ScriptHashTable.TryGetValue(Message.CurrentScript, out scriptindex))
					{
						ScriptComponent.SetScript(scriptindex);
						MyAPIGateway.Multiplayer.SendMessageToOthers(REGISTERNET, obj);//relay to clients. 
					}

				}
            }
		}

		public static void RegisterForNetwork(IMyEntity Ent, LocalLCDWriterComponent Component)
		{
			if(instance == null)
			{
				Register += () =>
				{
					RegisterForNetwork(Ent, Component);
				};
				return;
            }
			instance.RegisterEntityForNetwork(Ent, Component);
		}

		private void RegisterEntityForNetwork(IMyEntity ent, LocalLCDWriterComponent component)
		{
			if(LocalLCDWriter.ContainsKey(ent.EntityId))
			{
				LocalLCDWriter.Remove(ent.EntityId);
			}
			if(!ent.MarkedForClose)
			{
				ent.OnClose += UnregisterEntityForNetwork;
				LocalLCDWriter.Add(ent.EntityId, component);
			}

		}

		private void UnregisterEntityForNetwork(IMyEntity obj)
		{
			if (LocalLCDWriter.ContainsKey(obj.EntityId))
			{
				LocalLCDWriter.Remove(obj.EntityId);
			}
		}

		private void InitScripts()
		{
			//call your LCDScript static methods here.
			var Script = MyStringId.GetOrCompute("MyTestScript");
            TypeDef.Add(Script, VideoPlayerScript.Factory);
			long i = 0;
			ScriptHashTable.Add("MyTestScript".GetHashCode(), ++i);
			LocalLCDWriterComponent.AddLineItem(Script);
			foreach(var comp in LocalLCDWriter)
			{
				comp.Value.UpdateScript();
            }
		}

		public Dictionary<MyStringId, Vector3D> Offsets = new Dictionary<MyStringId, Vector3D>();
		public Dictionary<MyStringId, Vector2D> Max = new Dictionary<MyStringId, Vector2D>();

		private void InitLCDScreens()
		{
			var LargePanel = MyStringId.GetOrCompute("LargeLCDPanel");
			Offsets.Add(LargePanel, new Vector3D(-1.215, 1.215, -1.02));
			Max.Add(LargePanel, new Vector2D(2.43d, 2.43d));

			var LargeTextPanel = MyStringId.GetOrCompute("LargeTextPanel");
			Offsets.Add(LargeTextPanel, new Vector3D(-1.19, 0.68, -1.02));
			Max.Add(LargeTextPanel, new Vector2D(1.19d *2d, .68d*2d));

			var LargeLCDPanelWide = MyStringId.GetOrCompute("LargeLCDPanelWide");
			Offsets.Add(LargeLCDPanelWide, new Vector3D(-2.465, 1.20, -1.02));
			Max.Add(LargeLCDPanelWide, new Vector2D(2.465d * 2d, 1.20d * 2d));


			var SmallTextPanel = MyStringId.GetOrCompute("SmallTextPanel");
			Offsets.Add(SmallTextPanel, new Vector3D(-.21, 0.21, -0.155));
			Max.Add(SmallTextPanel, new Vector2D(.21d * 2d, .21d * 2d));

			var SmallLCDPanel = MyStringId.GetOrCompute("SmallLCDPanel");
			Offsets.Add(SmallLCDPanel, new Vector3D(-0.725, 0.725, -0.105));
			Max.Add(SmallLCDPanel, new Vector2D(0.725d * 2d, 0.725 * 2d));

			var SmallLCDPanelWide = MyStringId.GetOrCompute("SmallLCDPanelWide");
			Offsets.Add(SmallLCDPanelWide, new Vector3D(-1.48, 0.725, -0.105));
			Max.Add(SmallLCDPanelWide, new Vector2D(2.96d, 1.45d));
		}

		[StructLayout(LayoutKind.Sequential)]
		struct NetworkMessage
		{
			public long EntityId;
			public long CurrentScript;

			public static readonly int Size = TypeExtensions.SizeOf<NetworkMessage>();

			public void Serialize(ref Serializer serializer)
			{
				serializer.Write(EntityId);
				serializer.Write(CurrentScript);

			}

			public static void Deserialize(ref Serializer serializer, out NetworkMessage msg)
			{
				msg.EntityId = serializer.ReadInt64();
				msg.CurrentScript = serializer.ReadInt64();
			}
		}

		struct Serializer
		{
			ushort[] ushortArray;
			long[] longArray;
			ulong[] ulongArray;

			int offset;
			byte[] bytes;

			public Serializer(object dummy)
			{
				ushortArray = new ushort[1];
				longArray = new long[1];
				ulongArray = new ulong[1];
				offset = 0;
				bytes = null;
			}

			public void Init(byte[] bytes)
			{
				offset = 0;
				this.bytes = bytes;
			}

			public ushort ReadUInt16()
			{
				Buffer.BlockCopy(bytes, offset, ushortArray, 0, sizeof(ushort));
				offset += sizeof(ushort);
				return ushortArray[0];
			}

			public long ReadInt64()
			{
				Buffer.BlockCopy(bytes, offset, longArray, 0, sizeof(long));
				offset += sizeof(long);
				return longArray[0];
			}

			public ulong ReadUInt64()
			{
				Buffer.BlockCopy(bytes, offset, ulongArray, 0, sizeof(ulong));
				offset += sizeof(ulong);
				return ulongArray[0];
			}

			public void Write(ushort value)
			{
				ushortArray[0] = value;
				Buffer.BlockCopy(ushortArray, 0, bytes, offset, sizeof(ushort));
				offset += sizeof(ushort);
			}

			public void Write(long value)
			{
				longArray[0] = value;
				Buffer.BlockCopy(longArray, 0, bytes, offset, sizeof(long));
				offset += sizeof(long);
			}

			public void Write(ulong value)
			{
				ulongArray[0] = value;
				Buffer.BlockCopy(ulongArray, 0, bytes, offset, sizeof(ulong));
				offset += sizeof(ulong);
			}
		}



		public static Vector2D LCDMax = new Vector2D(10, 10);
		public static Vector3D LCDOffset = new Vector3D(0, 0, 0);

		HudAPIv2.MenuSliderInput slidercontrol;
		HudAPIv2.MenuSliderInput xdistcontrol , ydistcontrol, zdistcontrol;
		HudAPIv2.MenuSliderInput MaxXdistcontrol, MaxYdistcontrol;
		private void RegisterHudAPI()
		{
			instance = this;
			if (Register != null)
			{
				Register();
			}
			var rootmenu = new HudAPIv2.MenuRootCategory("Developer", HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Developer");
			slidercontrol = new HudAPIv2.MenuSliderInput("Text Scale", rootmenu, 0.5f, "Text SizeAdjust", onSubmit, SliderUpdate, OnCancel);
			xdistcontrol = new HudAPIv2.MenuSliderInput("Text X Offset", rootmenu, 0.5f, "Text X Adjust", onXSubmit, SliderXUpdate, OnXCancel);
			ydistcontrol = new HudAPIv2.MenuSliderInput("Text Y Offset", rootmenu, 0.5f, "Text Y Adjust", onYSubmit, SliderYUpdate, OnYCancel);
			zdistcontrol = new HudAPIv2.MenuSliderInput("Text Z Offset", rootmenu, 0.5f, "Text Z Adjust", onZSubmit, SliderZUpdate, OnZCancel);
			MaxXdistcontrol = new HudAPIv2.MenuSliderInput("Text MaxX Offset", rootmenu, 0.5f, "Text MaxX Adjust", onMXSubmit, SliderMXUpdate, OnMXCancel);
			MaxYdistcontrol = new HudAPIv2.MenuSliderInput("Text MaxY Offset", rootmenu, 0.5f, "Text MaxY Adjust", onMYSubmit, SliderMYUpdate, OnMYCancel);
		}

		private void OnCancel()
		{
			ScaleAdjust = (double)SliderUpdate(slidercontrol.InitialPercent);
		}

		private object SliderUpdate(float arg)
		{
			ScaleAdjust = MathHelper.Lerp(6.44d, 7.16d, arg);
			return ScaleAdjust;
        }

		private void onSubmit(float obj)
		{
			SliderUpdate(obj);
			slidercontrol.InitialPercent = obj;
        }
		private void OnXCancel()
		{
			LCDOffset.X = (double)SliderXUpdate(xdistcontrol.InitialPercent);
		}

		private object SliderXUpdate(float arg)
		{
			LCDOffset.X = MathHelper.Lerp(-3d, 3d, arg);
			return LCDOffset.X;
		}

		private void onXSubmit(float obj)
		{
			SliderXUpdate(obj);
			xdistcontrol.InitialPercent = obj;
		}

		private void OnYCancel()
		{
			LCDOffset.Y = (double)SliderYUpdate(ydistcontrol.InitialPercent);
		}

		private object SliderYUpdate(float arg)
		{
			LCDOffset.Y = MathHelper.Lerp(-2d, 2d, arg);
			return LCDOffset.Y;
		}

		private void onYSubmit(float obj)
		{
			SliderYUpdate(obj);
			ydistcontrol.InitialPercent = obj;
		}
		private void OnZCancel()
		{
			LCDOffset.Z = (double)SliderZUpdate(zdistcontrol.InitialPercent);
		}

		private object SliderZUpdate(float arg)
		{
			LCDOffset.Z = MathHelper.Lerp(-2d, 2d, arg);
			return LCDOffset.Z;
		}

		private void onZSubmit(float obj)
		{
			SliderZUpdate(obj);
			zdistcontrol.InitialPercent = obj;
		}
		private void OnMXCancel()
		{
			LCDMax.X = (double)SliderMXUpdate(MaxXdistcontrol.InitialPercent);
		}

		private object SliderMXUpdate(float arg)
		{
			LCDMax.X = MathHelper.Lerp(-4d, 4d, arg);
			return LCDMax.X;
		}

		private void onMXSubmit(float obj)
		{
			SliderMXUpdate(obj);
			MaxXdistcontrol.InitialPercent = obj;
		}

		private void OnMYCancel()
		{
			LCDMax.Y = (double)SliderMYUpdate(MaxYdistcontrol.InitialPercent);
		}

		private object SliderMYUpdate(float arg)
		{
			LCDMax.Y = MathHelper.Lerp(-4d, 4d, arg);
			return LCDMax.Y;
		}

		private void onMYSubmit(float obj)
		{
			SliderMYUpdate(obj);
			MaxYdistcontrol.InitialPercent = obj;
		}


		public LCDScript GetScript(MyStringId Name, IMyTextPanel Panel)
		{
			Func<IMyTextPanel, LCDScript> Value;
			if (TypeDef.TryGetValue(Name, out Value))
			{
				return Value( Panel);
			}
			return null;
		}

	}
}
