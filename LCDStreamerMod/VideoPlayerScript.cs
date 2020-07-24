using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using Sandbox.ModAPI;
using System.IO;
using IMyTextSurfaceProvider = Sandbox.ModAPI.IMyTextSurfaceProvider;
using VRage.Game.GUI.TextPanel;
using Sandbox.Game.Entities.Blocks;
using System.Diagnostics;

namespace LocalLCD
{
	public class VideoPlayerScript
	{
		MemoryStream test = new MemoryStream();
		MyEntity3DSoundEmitter AudioEmitter;
		bool playing = false;
		bool done = false;
		bool error = false;
		BinaryReader VideoReader, AudioReader;
		Stopwatch time = new Stopwatch();
		long tickspersecond = Stopwatch.Frequency;
		long ticksperframe = Stopwatch.Frequency / 20;
		long audioqueues = 0;
		struct headerdata
		{
			public int width;
			public int height;
			public int stride;
			public int framerate;
			public static headerdata getFromBytes(byte[] bytes)
			{
				var header = new headerdata();
				header.width = BitConverter.ToInt32(bytes, 0);
				header.height = BitConverter.ToInt32(bytes, sizeof(int));
				header.stride = BitConverter.ToInt32(bytes, sizeof(int) * 2);
				header.framerate = BitConverter.ToInt32(bytes, sizeof(int) * 3);
				return header;
			}
		}

		public struct audioheader
		{
			public int SampleRate;
			public int Channels;
			public int BitsPerSample;
			public int AverageBytesPerSecond;
			public static audioheader getFromBytes(byte[] bytes)
			{
				var header = new audioheader();
				header.SampleRate = BitConverter.ToInt32(bytes, 0);
				header.Channels = BitConverter.ToInt32(bytes, sizeof(int));
				header.BitsPerSample = BitConverter.ToInt32(bytes, sizeof(int) * 2);
				header.AverageBytesPerSecond = BitConverter.ToInt32(bytes, sizeof(int) * 3);
				return header;
			}

		}
		audioheader m_audioheader;
		headerdata header;
		long framecounter = 0;
		long offset = 0;

		private Sandbox.ModAPI.Ingame.IMyTextSurface surface;

		public VideoPlayerScript(Sandbox.ModAPI.IMyTextPanel panel)
		{
			panel.Font = "Mono Color";
			panel.FontSize = 0.1f;
			AudioEmitter = new MyEntity3DSoundEmitter((MyEntity)panel);
	
			surface = (panel as IMyTextSurfaceProvider)?.GetSurface(0);
			if(surface != null)
			{
				surface.ContentType = ContentType.SCRIPT;
				surface.BackgroundColor = Color.Black;
				surface.Alignment = TextAlignment.CENTER;
				surface.ScriptForegroundColor = Color.White;
				surface.ScriptBackgroundColor = Color.Black;
			}

		}

		public static VideoPlayerScript Factory(Sandbox.ModAPI.IMyTextPanel Panel)
		{
			return new VideoPlayerScript(Panel);
		}

		public void Update()
		{
			
			try
			{
				//totally fixing
				
				if (!done)
				{
					//OutputString.AppendLine("Running");
					Main();
				}
				else
				{
					if(time.IsRunning)
						time.Stop();
					if (VideoReader != null)
					{
						VideoReader.Dispose();
						VideoReader.Close();
						VideoReader = null;
					}

				}

			}
			catch(Exception ex)
			{

			}
        }

		public void Main()
		{
			if(playing == false)
			{
				if(MyAPIGateway.Utilities.FileExistsInLocalStorage("caramellgirls", typeof(byte[])))
				{
					
					VideoReader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage("caramellgirls", typeof(byte[]));
					ReadHeader(VideoReader);
					AudioEmitter.StoppedPlaying += AudioEmitter_StoppedPlaying;
					playing = true;
					AudioReader = MyAPIGateway.Utilities.ReadBinaryFileInLocalStorage("caramellgirlsaudio", typeof(byte[]));
					ReadAudioHeader(AudioReader);
					channelbuffer = new byte[m_audioheader.AverageBytesPerSecond];
					audiobuffer = new byte[m_audioheader.AverageBytesPerSecond / 2];
					audiobuffer2 = new byte[m_audioheader.AverageBytesPerSecond / 2];
					time.Start();
					audioqueues = 0;
					framecounter = 0;
					
					ticksperframe = Stopwatch.Frequency / header.framerate;
					offset = Stopwatch.Frequency / 10;
					tickspersecond = Stopwatch.Frequency;
					
					//EnqueueAudioBuffer();
				}
				else
				{

				}
			}
			if(playing)
			{

				if (ticksperframe * framecounter < time.ElapsedTicks)
				{
					framecounter++;
					PlayNextFrame();
				}

				if (tickspersecond * audioqueues + offset < time.ElapsedTicks)
				{

					audioqueues++;

					bufferblock--;
					EnqueueAudioBuffer();
				}
			}
		}
		byte[] channelbuffer, audiobuffer, audiobuffer2;
		int readaudio = 0;
		int bufferblock = 0;
		private void EnqueueAudioBuffer()
		{
				if (AudioReader.BaseStream.CanRead)
					readaudio = AudioReader.Read(channelbuffer, 0, channelbuffer.Length);
				if (readaudio == 0)
					return;
				AudioEmitter.PlaySound(channelbuffer, readaudio, m_audioheader.SampleRate, 1, 200);
		}

		private void SplitChannels()
		{
			for(int i = 0, a = 0; i + 3 < readaudio; i+=4, a += 2)
			{
				audiobuffer[a] = channelbuffer[i];
				audiobuffer[a + 1] = channelbuffer[i + 1];
				audiobuffer2[a] = channelbuffer[i + 2];
				audiobuffer2[a + 1] = channelbuffer[i + 3];
			}
		}

		private void AudioEmitter_StoppedPlaying(MyEntity3DSoundEmitter obj)
		{


		}
		private void ReadAudioHeader(BinaryReader audioReader)
		{
			m_audioheader = audioheader.getFromBytes(audioReader.ReadBytes(sizeof(int) * 4));

		}
		private void ReadHeader(BinaryReader videoReader)
		{
			header = headerdata.getFromBytes(videoReader.ReadBytes(sizeof(int) * 4));
			linebytes = new byte[header.stride];
			linechars = new char[header.width];
		}
		
		byte[] colorbyte = new byte[3];
		byte[] linebytes;
		char[] linechars;
		MySprite line;
		const float ADJ = 25f;
		private void SkipNextFrame()
		{
			for(int i = 0; i < header.height; i++)
			{
				if(VideoReader.Read(linebytes, 0, linebytes.Length) != linebytes.Length)
				{
					playing = false;
					done = true;
					return;
				}
			}
		}
		private void PlayNextFrame()
		{
			try
			{
				
				Vector2 pos = surface.SurfaceSize / 2;
				pos.Y -= (header.height * 0.08f * 0.5f) * ADJ;
				int control = VideoReader.ReadInt32();
				if (control == 1)
				{
					playing = false;
					done = true;
					return;
				}
				using (var frame = surface.DrawFrame())
				{
					for (int y = 0; y < header.height; y++)
					{
						if( VideoReader.Read(linebytes, 0, linebytes.Length) != linebytes.Length)
						{
							playing = false;
							done = true;
							return;
						}

						for (int x = 0, c = 0; x + 2 < header.stride; x += 3, c++)
						{
							linechars[c] = Rgb(linebytes[x + 2], linebytes[x + 1], linebytes[x]);
							
						}
						//done = true;
						line = new MySprite(SpriteType.TEXT, new string(linechars), pos, surface.SurfaceSize, fontId: "Mono Color")
						{
							RotationOrScale = 0.08f
						};
						pos.Y += line.RotationOrScale * ADJ;
						frame.Add(line);
					}
				}
			}
			catch (Exception ex)
			{

				playing = false;
				done = true;
				error = true;
				VideoReader.Dispose();
			}
		}

		static char Rgb(byte r, byte g, byte b)
		{
			return (char)((uint)0x3000 + ((r >> 3) << 10) + ((g >> 3) << 5) + (b >> 3));
		}
		public void Close()
		{
			if(VideoReader != null)
			{
				VideoReader.Dispose();
				VideoReader.Close();
				VideoReader = null;
			}
			if (AudioReader != null)
			{
				AudioReader.Dispose();
				AudioReader.Close();
				AudioReader = null;
			}
		}
	}
}
