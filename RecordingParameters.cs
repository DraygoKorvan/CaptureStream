using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class RecordingParameters
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
		public InterpolationMode interpolationMode = InterpolationMode.Default;
		public SmoothingMode smoothingMode = SmoothingMode.Default;
		public PixelFormat pixelFormat = PixelFormat.Format24bppRgb;
		public float audioBalance = 0.5f;
		public float leftVolume = 1.0f;
		public float rightVolume = 1.0f;
		public int compressionRate = 10;
		public int sampleRate = 24000;//22050;

		public RecordingParameters(int posX, int posY, int sizeX, int sizeY, int resX, int resY, int framerate, InterpolationMode iMode, SmoothingMode sMode)
		{
			this.PosX = posX;
			this.PosY = posY;
			this.SizeX = sizeX;
			this.SizeY = sizeY;
			this.ResX = resX;
			this.ResY = resY;
			FrameRate = framerate;
			interpolationMode = iMode;
			smoothingMode = sMode;

		}

		public RecordingParameters(RecordingParameters copyFrom)
		{
			this.PosX = copyFrom.PosX;
			this.PosY = copyFrom.PosY;
			this.SizeX = copyFrom.SizeX;
			this.SizeY = copyFrom.SizeY;
			this.ResX = copyFrom.ResX;
			this.ResY = copyFrom.ResY;
			this.FrameRate = copyFrom.FrameRate;
			this.running = copyFrom.running;
			this.RecordingMs = copyFrom.RecordingMs;
			this.audioBalance = copyFrom.audioBalance;
			this.leftVolume = copyFrom.leftVolume;
			this.rightVolume = copyFrom.rightVolume;
			this.pixelFormat = copyFrom.pixelFormat;
			this.interpolationMode = copyFrom.interpolationMode;
			this.smoothingMode = copyFrom.smoothingMode;
			this.compressionRate = copyFrom.compressionRate;
		}
	}
}
