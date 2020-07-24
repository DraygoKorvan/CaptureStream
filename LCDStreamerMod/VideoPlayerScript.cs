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
using static LCDText2.VideoBuffer;

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




		internal void PlayAudio(byte[] audioframes, int size, AudioHeader header)
		{
			if(AudioEmitter != null)
				AudioEmitter.PlaySound(audioframes, size, header.SampleRate, 1, 200);
		}




		private Sandbox.ModAPI.Ingame.IMyTextSurface surface;

		public VideoPlayerScript(Sandbox.ModAPI.IMyTextPanel panel)
		{
			AudioEmitter = new MyEntity3DSoundEmitter((MyEntity)panel);

		}

		public static VideoPlayerScript Factory(Sandbox.ModAPI.IMyTextPanel Panel)
		{
			return new VideoPlayerScript(Panel);
		}

		public void Update()
		{
			

        }

		public void Main()
		{

		}


		private void AudioEmitter_StoppedPlaying(MyEntity3DSoundEmitter obj)
		{


		}


		

		public void PlayNextFrame(string chars)
		{
				//TODO, display frame on unsynced dummy. 
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
