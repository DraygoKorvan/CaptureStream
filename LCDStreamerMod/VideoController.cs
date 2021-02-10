using CaptureStream;
using LCDText2;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Game.GUI.TextPanel;
using VRage.Utils;

namespace LocalLCD
{
	public class VideoController
	{
		public long LastFrameTime = 0;
		public long frame = 0;
		public long nextAudioTick = 0;
		public long nextVideoFrame = 0;
		private VideoBuffer videoBuffer;
		static long tickspersecond = Stopwatch.Frequency;
		static long videoupdate = Stopwatch.Frequency / 20;
		byte[] videoframes;
		int videoptr = 0;
		byte[] audioframes;
		int framecnt = 0;

		List<LocalLCDWriterComponent> subscribers = new List<LocalLCDWriterComponent>();

		//iSEVideoEncoder decoder = new M0424VideoEncoder();

		public VideoController(VideoBuffer videoBuffer)
		{
			this.videoBuffer = videoBuffer;
		}

		internal void SetRunTime(long elapsedTicks)
		{
			LastFrameTime = elapsedTicks;
			nextAudioTick = elapsedTicks;
			nextVideoFrame = elapsedTicks;
		}

		internal void Subscribe(LocalLCDWriterComponent localLCDWriterComponent, long elapsedTicks)
		{
			MyLog.Default.WriteLineAndConsole($"VideoController Subscribe {localLCDWriterComponent.Entity.EntityId}");
			if (subscribers.Count == 0)
				SetRunTime(elapsedTicks);
			if (subscribers.Contains(localLCDWriterComponent))
				return;
			subscribers.Add(localLCDWriterComponent);
		}

		internal void UnSubscribe(LocalLCDWriterComponent localLCDWriterComponent)
		{
			MyLog.Default.WriteLineAndConsole($"VideoController UnSubscribe {localLCDWriterComponent.Entity.EntityId}");
			if (subscribers.Contains(localLCDWriterComponent))
				return;
			subscribers.Remove(localLCDWriterComponent);
		}
		int audiobytes = 0;
		int videobytes = 0;
		int sampleRate = 0;

		bool first = true;
		bool started = false;
//		byte[] decodedKeyframe;


		internal void SendUpdate(long elapsedTicks)
		{
			if(nextAudioTick <= elapsedTicks)
			{
				int newaudiobytes;
				int newvideobytes;
				byte[] newaudioframes;
				byte[] newvideoframes;
				int newsampleRate;
				if (videoBuffer.GetNextSecond(out newaudioframes, out newaudiobytes, out newvideoframes, out newvideobytes, out newsampleRate))
				{
					started = true;
					audiobytes = newaudiobytes;
					videobytes = newvideobytes;
					audioframes = newaudioframes;
					videoframes = newvideoframes;
					this.sampleRate = newsampleRate;
					videoptr = 0;
					framecnt = 0;
					if (!first && nextAudioTick + Stopwatch.Frequency <= elapsedTicks)
					{
						nextAudioTick += Stopwatch.Frequency;
					}
					else
					{
						nextAudioTick = elapsedTicks + Stopwatch.Frequency;
					}
					nextVideoFrame = elapsedTicks;
					if (audiobytes != 0)
					{

						foreach (var lcd in subscribers)
						{
							if (!LCDWriterCore.isLocalMuted || ( MyAPIGateway.Session.OnlineMode != VRage.Game.MyOnlineModeEnum.OFFLINE && videoBuffer.steamid != MyAPIGateway.Multiplayer.MyId))
								lcd.PlayAudio(audioframes, audiobytes, sampleRate);
						}
					}

				}
				
			}
			if (started && nextVideoFrame <= elapsedTicks)
			{

				if (videoframes == null || videoptr >= videobytes)
				{
					return;//wait.
				}
				//int control = BitConverter.ToInt32(videoframes, videoptr);

				ushort stride = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int));
				ushort height = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort));
				ushort width = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 2);
				ushort framerate = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 3);
				//int frameln = BitConverter.ToInt32(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 4);
				LCDWriterCore.debugMonitor.VideoByteLength = videoframes.Length;
				LCDWriterCore.debugMonitor.VideoCharWidth = width;
				LCDWriterCore.debugMonitor.VideoStride = stride;
				LCDWriterCore.debugMonitor.VideoHeight = height;
				LCDWriterCore.debugMonitor.FrameRate = framerate;
				if (framerate == 0)
					return;
				if (framerate > 60)
					framerate = 60;
				nextVideoFrame += (tickspersecond / framerate);


				videoptr += sizeof(int) * 1 + sizeof(ushort) * 4;
				//string s_frame = getString(videoframes, videoptr, width, stride, height);
				var strings = getString(videoframes, videoptr, width, stride, height);
				foreach (var lcd in subscribers)
				{
					//lcd.PrepareFrame(videoframes, videoptr, stride * height, stride, width, height);
					//lcd.SetFontSize((float)width, (float)height);

					lcd.PlayNextFrame(strings, videoptr, width, stride, height);
				}
				videoptr += stride * height;
				framecnt++;


			}
		}

		char[] charbuffer = new char[1];
		private string[] getString(byte[] videoframes, int offset, int width, int stride, int height)
		{
			var output = new string[height];
			if (charbuffer.Length < width)
				charbuffer = new char[width];

			for (int y = 0; y < height; y++)
			{
				int ptr = 0;
				var ystride = (y * stride);
				for (int x = 0; x < width * 2; x += 2)
				{
					charbuffer[ptr++] = (char)((uint)0x3000 + BitConverter.ToUInt16(videoframes, ystride + x + offset));
				}
				output[y] = new string(charbuffer, 0, width);
			}
			return output;
		}



	}
}