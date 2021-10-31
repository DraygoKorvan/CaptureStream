using Draygo.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Draygo.API.HudAPIv2;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace LocalLCD
{
	public class DebugMonitor
	{
		StringBuilder DebugString = new StringBuilder();
		HUDMessage debugText;
		BoxUIText debugUI;

		public int RecieveVideoStream { get; internal set; }
		public int RecieveAudioStream { get; internal set; }
		public int RecieveNetworkStream { get; internal set; }


		public int RecieveVideoStreamAvg { get; internal set; }
		public int RecieveAudioStreamAvg { get; internal set; }
		public int RecieveNetworkStreamAvg { get; internal set; }
		public int VideoByteLength { get; internal set; }
		public int VideoCharWidth { get; internal set; }
		public int VideoStride { get; internal set; }
		public int VideoHeight { get; internal set; }
		public ushort FrameRate { get; internal set; }
		public int FrameBytes { get; internal set; }

		public int VideoBufferSize { get; internal set; }
		public int AudioBufferSize { get; internal set; }
		public int AudioSampleRate { get; internal set; }
		public int AudioAverageBytesPerSecond { get; internal set; }
		public int AudioBytes { get; internal set; }
		public bool Visible
		{
			get
			{
				if (debugUI == null)
					return false;
				return debugUI.Visible;
			}
			set
			{
				if (debugUI != null)
				{
					debugUI.Visible = value;
				}
			}
		}
		const int LINEHEIGHT = 13;
		public void Start(HudAPIv2 api)
		{
			debugText = new HUDMessage();
			debugText.InitialColor = Color.Yellow;
			debugText.Message = DebugString;
			debugText.Font = "monospace";

			var boxdef = HudAPIv2.APIinfo.GetBoxUIDefinition(MyStringId.GetOrCompute("Default"));
			debugUI = new BoxUIText();
			debugUI.SetDefinition(boxdef);
			debugUI.Origin = new Vector2I(-340, 0);
			debugUI.Width = 340;
			debugUI.SetTextContent(debugText);
			debugUI.Visible = true;
			debugUI.Height = 12 * LINEHEIGHT + boxdef.Min.X;
			debugUI.BackgroundColor = Color.White * 0.2f;


			debugText.Scale = 10; //10pt
			debugText.Blend = BlendTypeEnum.PostPP;

		}
		public void Update()
		{
			DebugString.Clear();

			if (RecieveNetworkStreamAvg == 0)
				RecieveNetworkStreamAvg = RecieveNetworkStreamAvg;
			RecieveNetworkStreamAvg = updateCounter(RecieveNetworkStreamAvg, RecieveNetworkStream);

			RecieveNetworkStream = 0;
			DebugString.AppendLine($"RVS: {RecieveVideoStream,-10:N0} packet bytes");
			DebugString.AppendLine($"RAS: {RecieveAudioStream,-10:N0} packet bytes  {LCDWriterCore.isLocalMuted}");
			DebugString.AppendLine($"RNS: {RecieveNetworkStreamAvg * 60,-10:N0} bytes/s");
			DebugString.AppendLine($"Video: {VideoByteLength,10:N0} frame second bytes");
			DebugString.AppendLine($"       {FrameBytes,10:N0} bytes");
			DebugString.AppendLine($"       {VideoStride,10:N0} stride");
			DebugString.AppendLine($"       {VideoCharWidth,10:N0} charwidth");
			DebugString.AppendLine($"       {VideoHeight,10:N0} height");
			DebugString.AppendLine($"       {FrameRate,10:N0} fps");
			DebugString.AppendLine($" AudioBS: {AudioBufferSize} VideoBS: {VideoBufferSize}");
			DebugString.AppendLine($" AudioABS: {AudioAverageBytesPerSecond} ASR: {AudioSampleRate}");
			DebugString.AppendLine($" {AudioBytes} bytes");

		}

		public static int updateCounter(int oldvalue, int newvalue)
		{
			return (int)MathHelper.Lerp(oldvalue, newvalue, 1d / 60d);
		}

	}
}
