using LocalLCD;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Data.Audio;
using VRage.Game.Entity;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace LCDStreamerMod
{
	[MyTextSurfaceScript("VideoStreamer", "Video Stream Player")]
	public class VideoPlayerTSS : MyTSSCommon
	{
		public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;
		LocalLCDWriterComponent LCDWriter;
		IMyTextPanel m_Panel;
		private Vector2 m_screenAspectRatio;

		MyEntity3DSoundEmitter AudioEmitter;
		IMyTextSurface m_Surface;
		byte[] soundBuffer = new byte[14110];
		int buffersize = 14110;
		int samplerate = 14110;
		bool havebuffer = false;
		bool playnext = false;

		public VideoPlayerTSS(IMyTextSurface surface, IMyCubeBlock block, Vector2 ScreenSize) : base (surface, block, ScreenSize)
		{
			m_Surface = surface;
			surface.ScriptBackgroundColor = Color.Black;
			m_Panel = block as IMyTextPanel;
			LCDWriter = m_Panel.GameLogic.GetAs<LocalLCDWriterComponent>();
			if(LCDWriter != null)
				LCDWriter.Register(this);

			AudioEmitter = new MyEntity3DSoundEmitter((MyEntity)m_Panel);
			AudioEmitter.StoppedPlaying += AudioEmitter_StoppedPlaying;

			if (surface.SurfaceSize.X > surface.SurfaceSize.Y)
				m_screenAspectRatio = new Vector2(1f, surface.SurfaceSize.Y / surface.SurfaceSize.X);
			else
				m_screenAspectRatio = new Vector2(surface.SurfaceSize.X / surface.SurfaceSize.Y, 1f);
		}

		private void AudioEmitter_StoppedPlaying(MyEntity3DSoundEmitter obj)
		{
			if (havebuffer == true)
			{
				
				AudioEmitter.PlaySound(soundBuffer, 1, 40);//buffersize, samplerate
				havebuffer = false;
			}

		}

		public void PlayAudio(byte[] audioframes, int size, int sampleRate)
		{
			if (AudioEmitter == null)
				return;
			//MyAPIGateway.Utilities.ShowNotification($"PlayAudio {size} {audioframes[0]} {audioframes[1]} {audioframes[2]} {audioframes[3]}", 1000);
			//samplerate = sampleRate;
			//buffersize = Math.Min(audioframes.Length, size);
			//if(AudioEmitter.IsPlaying)
			//{
			//	havebuffer = true;
			//	if (buffersize != soundBuffer.Length)
			//		soundBuffer = new byte[buffersize];
			//	Buffer.BlockCopy(audioframes, 0, soundBuffer, 0, buffersize);
			//}
			//else
			//{
			//	havebuffer = false;
			
			AudioEmitter.PlaySound(audioframes, 1, 40, MySoundDimensions.D2);//size, sampleRate, 
			//}
		}
		const float ADJ = 25f;
		float m_FontSizeRatio = 1f;
		List<MySprite> sprites = new List<MySprite>();
		internal void PlayNextFrame(string[] strings, int offset, int width, int stride, int height)
		{
			if (m_Surface == null)
				return;
			sprites.Clear();
			SetFontSize(width, height);
			//var strings = getString(data, offset, width, stride, height);
			var xcenter = m_Surface.SurfaceSize.X / 2f;
			var ystep = m_Surface.SurfaceSize.Y / (float)height;
			MySprite line;
			Vector2 pos = m_Surface.SurfaceSize / 2;
			pos.Y -= (0.04f * m_FontSizeRatio * ADJ * (float)height);
			for (int i = 0; i < height; i++)
			{
				line = new MySprite(SpriteType.TEXT, strings[i], pos, m_Surface.SurfaceSize, fontId: "Mono Color")
				{
					RotationOrScale = 0.08f * m_FontSizeRatio
				};
				pos.Y += line.RotationOrScale * ADJ;
				sprites.Add(line);
			}
			//Following is Gwindalmir hax. 
			if (sprites?.Count > 0)
			{
				//var comp = (panel.Render as MyRenderComponentScreenAreas);

				(m_Panel.Render as MyRenderComponentScreenAreas)?.RenderSpritesToTexture(0, sprites,
					new Vector2I(m_Surface.TextureSize), m_screenAspectRatio, Color.Black, 0);
			}

		}
		public void SetFontSize(float width, float height)
		{
			if (height <= 1f)
				height = 1f;
			if (width <= 1f)
				width = 1f;
			m_FontSizeRatio = Math.Min((236 / height), (472 / width));
		}
		public override void Dispose()
		{
			if (LCDWriter != null)
				LCDWriter.Unregister(this);
		}
	}
}
