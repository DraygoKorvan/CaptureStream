using ParallelTasks;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace LCDStreamerMod
{
	public class FrameTaskManger
	{
		int framerate;
		int videolength;
		int audiolength;


		string[] frames;
		byte[] videodata;
		byte[] audioframe;

		public bool isComplete = false;

		public bool quit = false;
		public bool draining = false;

		public string GetFrame(int frame)
		{
			if (quit)
				return null;
			return frames[frame];
		}

		public byte[] GetAudio(out int bytes)
		{
			bytes = audiolength;
			return audioframe;
		}


		public void StartBackground(int framerate, byte[] video, int videolength, byte[] audio, int audiobytes)
		{
			quit = false;
			draining = false;
			isComplete = false;
			this.framerate = framerate;
			this.videodata = video;
			this.videolength = videolength;
			this.audioframe = audio;
			this.audiolength = audiobytes;
			var task = MyAPIGateway.Parallel.StartBackground(Queue, Complete);
			
		}

		private void Complete()
		{
			
		}


		public void Queue()
		{
			try
			{

				if (frames == null || frames.Length < framerate)
				{
					//grow the cache. 
					frames = new string[framerate];
				}
				int ptr = 0;
				for (int i = 0; i < framerate; i++)
				{

					int control = BitConverter.ToInt32(videodata, ptr);
					ushort stride = BitConverter.ToUInt16(videodata, ptr + sizeof(int));
					ushort height = BitConverter.ToUInt16(videodata, ptr + sizeof(int) + sizeof(ushort));
					if (control == 1)
					{
						quit = true;
						return;
					}
					ptr += sizeof(int) + sizeof(ushort) * 2;


					frames[i] = getString( ptr, stride, height);

					ptr += stride * height;

				}
			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLine(ex);
			}
			isComplete = true;
			MyLog.Default.WriteLine("Completed Renders");
		}

		char[] buffer = new char[1];
		private string getString(int videoptr, int width, int height)
		{

			int length = (width * height) / 2 + height;
			MyLog.Default.WriteLine($"VideoController getString {length} {videoptr} {width} {height}");
			if (buffer == null || buffer.Length < length)
				buffer = new char[length];
			int ptr = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x += 2)
				{
					buffer[ptr++] = (char)BitConverter.ToUInt16(videodata, videoptr + (((y * width) + x)));

				}
				buffer[ptr++] = '\n';
			}
			//MyLog.Default.WriteLine($"Charbuffer {length} {ptr}");
			return new string(buffer, 0, length);
		}
	}
}
