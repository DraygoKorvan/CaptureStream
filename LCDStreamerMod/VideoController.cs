using CaptureStream;
using LCDText2;
using Sandbox.ModAPI;
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
		int framecnt = 0;

		List<LocalLCDWriterComponent> subscribers = new List<LocalLCDWriterComponent>();

		iSEVideoEncoder decoder = new M0454VideoEncoder();

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
		byte[] decodedKeyframe;
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
						//MyLog.Default.WriteLineAndConsole($"VideoController Send-Update Audio Second: Subs - {subscribers.Count}");
						//do process.
						
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

				//MyLog.Default.WriteLineAndConsole($"Play Frame {nextVideoFrame}  Elapsed {elapsedTicks} Ticks: {tickspersecond} {videoptr} {videobytes}");
				
				//MyLog.Default.WriteLineAndConsole($"VideoController Send-Update Video Second: Subs - {subscribers.Count}");
				if (videoframes == null || videoptr >= videobytes)
				{
					return;//wait.
				}
				int control = BitConverter.ToInt32(videoframes, videoptr);
				ushort stride = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int));
				ushort height = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort));
				ushort width = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 2);
				ushort framerate = BitConverter.ToUInt16(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 3);
				int frameln = BitConverter.ToInt32(videoframes, videoptr + sizeof(int) + sizeof(ushort) * 4);
				LCDWriterCore.debugMonitor.VideoByteLength = videoframes.Length;
				LCDWriterCore.debugMonitor.VideoCharWidth = width;
				LCDWriterCore.debugMonitor.VideoStride = stride;
				LCDWriterCore.debugMonitor.VideoHeight = height;
				LCDWriterCore.debugMonitor.FrameBytes = frameln;
				LCDWriterCore.debugMonitor.FrameRate = framerate;
				if (framerate == 0)
					framerate = 20;
				nextVideoFrame += (tickspersecond / framerate);
				MyLog.Default.WriteLine($"Video Packet Header: c {control} s {stride} h {height} fs {frameln} srcln {videoframes.Length} ptr {videoptr}");
				MyLog.Default.Flush();
				videoptr += sizeof(int) * 2 + sizeof(ushort) * 4;
				var frameDecoder = new byte[frameln];
				
				Buffer.BlockCopy(videoframes, videoptr, frameDecoder, 0, frameln);
				videoptr += frameln;
				if (control == 1)
				{
					frameDecoder = decoder.Decode(frameDecoder, null, stride, width, height);
					decodedKeyframe = frameDecoder;
				}
				else
				{
					frameDecoder = decoder.Decode(frameDecoder, decodedKeyframe, stride, width, height);
				}
				

				//var length = stride * height;
				string s_frame = getString(frameDecoder, 0, width, stride, height);
				
				framecnt++;
				//MyLog.Default.WriteLine($"Frame Counter {framecnt}");
				//convert to string
				foreach (var lcd in subscribers)
				{
					lcd.PlayNextFrame(s_frame);
				}

				//videoptr += length;
			}
		}

		char[] charbuffer = new char[1];
		private string getString(byte[] videoframes, int offset, int width, int stride, int height)
		{
			//MyLog.Default.WriteLine($"VideoController getString {videoframes.Length} {videoptr} {width} {height}");



			int length = (width * height) + height;
			if(charbuffer.Length < length)
				charbuffer = new char[length];

			//int charbufferadv = 0;
			int ptr = 0;
			for (int y = 0; y < height; y++)
			{
				var ystride = (y * stride);
				for (int x = 0; x < width * 2; x += 2)
				{

					//byte r = videoframes[ystride + x];
					//byte g = videoframes[ystride + x + 1];
					//byte b = videoframes[ystride + x + 2];
					//ptr++;
					//var tmp = (chat)
					// bytes = tmp.
					charbuffer[ptr++] = (char)((uint)0x3000 + BitConverter.ToUInt16(videoframes, offset + ystride +  x));
				}
				//ptr++;
				charbuffer[ptr++] = '\n';
			}
			//MyLog.Default.WriteLine($"Charbuffer {length} {ptr}");
			//throw new Exception("KEKW");
			return new string(charbuffer, 0, length);
		}


	}
}