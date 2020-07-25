using LCDText2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		internal void SendUpdate(long elapsedTicks)
		{
			if(nextAudioTick <= elapsedTicks)
			{
				int newaudiobytes;
				int newvideobytes;
				byte[] newaudioframes;
				byte[] newvideoframes;
				if (videoBuffer.GetNextSecond(out newaudioframes, out newaudiobytes, out newvideoframes, out newvideobytes))
				{
					audiobytes = newaudiobytes;
					videobytes = newvideobytes;
					audioframes = newaudioframes;
					videoframes = newvideoframes;
					videoptr = 0;
					nextAudioTick += tickspersecond;
					if (audiobytes != 0)
					{
						MyLog.Default.WriteLineAndConsole($"VideoController Send-Update Audio Second: Subs - {subscribers.Count}");
						//do process.
						foreach (var lcd in subscribers)
						{
							lcd.PlayAudio(audioframes, audiobytes, videoBuffer.audioHeader);
						}
					}

				}
				
			}
			if (nextVideoFrame <= elapsedTicks)
			{
				if (videoBuffer.videoHeader.framerate == 0)
					return;
				nextVideoFrame += tickspersecond / videoBuffer.videoHeader.framerate;
				MyLog.Default.WriteLineAndConsole($"VideoController Send-Update Video Second: Subs - {subscribers.Count}");
				if (videoframes == null || videoptr >= videobytes)
				{
					return;//wait.
					
				}
				int control = BitConverter.ToInt32(videoframes, videoptr);
				ushort stride = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int));
				ushort height = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort));
				MyLog.Default.WriteLine($"Video Packet Header: c {control} s {stride} h {height}");
				videoptr += sizeof(int) + sizeof(ushort) * 2;
				if (control == 1)
				{
					return;

				}
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
			MyLog.Default.WriteLine($"VideoController getString {videoframes.Length} {videoptr} {width} {height}");
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