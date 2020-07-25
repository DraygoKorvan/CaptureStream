using LCDText2;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
		byte[] videoframes = new byte[0];
		int videoptr = 0;
		byte[] audioframes;

		List<LocalLCDWriterComponent> subscribers = new List<LocalLCDWriterComponent>();

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
			if (subscribers.Count == 0)
				SetRunTime(elapsedTicks);
			if (subscribers.Contains(localLCDWriterComponent))
				return;
			subscribers.Add(localLCDWriterComponent);
		}

		internal void UnSubscribe(LocalLCDWriterComponent localLCDWriterComponent)
		{
			if (subscribers.Contains(localLCDWriterComponent))
				return;
			subscribers.Remove(localLCDWriterComponent);
		}

		internal void SendUpdate(long elapsedTicks)
		{
			if(nextAudioTick <= elapsedTicks)
			{
				nextAudioTick += tickspersecond;
				var bytes = videoBuffer.ReadAudio(out audioframes);
				if(bytes != 0)
				{
					//do process.
					foreach(var lcd in subscribers)
					{
						lcd.PlayAudio(audioframes, bytes, videoBuffer.audioHeader);
					}
				}
				
			}
			if (nextVideoFrame <= elapsedTicks)
			{
				nextVideoFrame += tickspersecond / videoBuffer.videoHeader.framerate;
				if (videoptr >= videoframes.Length)
				{
					var bytes = videoBuffer.ReadVideo(out videoframes);
					videoptr = 0;
				}
				int control = BitConverter.ToInt32(videoframes, videoptr);
				ushort stride = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int));
				ushort height = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort));
				videoptr += sizeof(int) + sizeof(ushort) * 2;
				var length = stride * height;
				string s_frame = getString(videoframes, videoptr, stride, height);
				//convert to string
				foreach (var lcd in subscribers)
				{
					lcd.PlayNextFrame(s_frame);
				}

				videoptr += length;
			}
		}

		char[] charbuffer = new char[1];
		private string getString(byte[] videoframes, int videoptr, int width, int height)
		{
			int length = (width * height) / 2 + height;
			if(charbuffer.Length < length)
				charbuffer = new char[length];

			for (int y = 0; y < height; y++)
			{
				int adv = (y * width) + y;
				for (int x = 0; x < width; x++)
				{
					charbuffer[adv + x + 1] = (char)BitConverter.ToUInt16(videoframes, videoptr + (((y * width) + x) * 2));
				}
				charbuffer[adv] = '\n';
			}

			return new string(charbuffer, 0, length);
		}


	}
}