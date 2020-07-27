using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class RecordingParemeters
	{
		public int PosX;
		public int PosY;
		public int SizeX;
		public int SizeY;
		public int ResX;
		public int ResY;
		public int FrameRate;
		public bool running;
		public long RecordingMs;


		public RecordingParemeters(int posX, int posY, int sizeX, int sizeY, int resX, int resY, int framerate)
		{
			this.PosX = posX;
			this.PosY = posY;
			this.SizeX = sizeX;
			this.SizeY = sizeY;
			this.ResX = resX;
			this.ResY = resY;
			FrameRate = framerate;
		}
	}
}
