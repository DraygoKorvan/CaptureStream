using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CaptureStream
{
	public class FrameWork
	{
		private int posX = 0;
		private int posY = 0;
		private int sizeX = 0;
		private int sizeY = 0;
		private int resX = 0;
		private int resY = 0;

		public byte[] result = new byte[1];
		public int outbytes = 1;
		public bool stopped = false;

		private bool reallocate = true;
		private Bitmap source;
		private Bitmap destination;

		private iSEVideoEncoder encoder;

		public int PosX
		{
			get => posX;
			set
			{
				if (posX != value)
				{
					posX = value;
					reallocate = true;
				}

			}
		}
		public int PosY
		{
			get => posY;
			set
			{
				if (posY != value)
				{
					posY = value;
					reallocate = true;
				}

			}
		}
		public int SizeX
		{
			get => sizeX;
			set
			{
				if (sizeX != value)
				{
					sizeX = value;
					reallocate = true;
				}

			}
		}
		public int SizeY
		{
			get => sizeY;
			set
			{
				if (sizeY != value)
				{
					sizeY = value;
					reallocate = true;
				}

			}
		}
		public int ResX
		{
			get => resX;
			set
			{
				if (resX != value)
				{
					resX = value;
					reallocate = true;
				}

			}
		}
		public int ResY
		{
			get => resY;
			set
			{
				if (resY != value)
				{
					resY = value;
					reallocate = true;
				}

			}
		}

		public PixelFormat Format
		{
			get => format;
			set
			{
				format = value;
				reallocate = true;
			}

		}

		private int framerate;
		public long framems;
		private bool running;
		public bool isKeyframe;

		public byte[] outbuffer;
		public byte[] keyFrame;
		public int keyFrameln;
		public int imageln;
		private int stride;
		private int width;
		private int height;

		InterpolationMode iMode = InterpolationMode.Default;
		SmoothingMode sMode = SmoothingMode.Default;
		PixelFormat format = PixelFormat.Format24bppRgb;

		public FrameWork()
		{
			encoder = new M0454VideoEncoder();
		}

		internal void Prepare(RecordingParameters recordingParemeters, bool isKeyFrame)
		{
			PosX = recordingParemeters.PosX;
			PosY = recordingParemeters.PosY;
			SizeX = recordingParemeters.SizeX;
			SizeY = recordingParemeters.SizeY;
			ResX = recordingParemeters.ResX;
			ResY = recordingParemeters.ResY;
			running = recordingParemeters.running;
			stopped = false;
			framerate = recordingParemeters.FrameRate;
			this.isKeyframe = isKeyFrame;
			iMode = recordingParemeters.interpolationMode;
			sMode = recordingParemeters.smoothingMode;
			Format = recordingParemeters.pixelFormat;
		}

		public void GetScreenshot()
		{
			if (reallocate)
			{
				reallocate = false;
				source = new Bitmap(SizeX, SizeY, PixelFormat.Format24bppRgb);
				destination = new Bitmap(ResX, ResY, Format);
			}
			using (Graphics g = Graphics.FromImage(source))
			{
				//g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.CopyFromScreen(PosX, PosY, 0, 0, new Size(SizeX, SizeY));
			}
		}

		public FrameWork DoWork()
		{
			var framemsstart = DateTime.Now.Ticks;


			using (Graphics cv = Graphics.FromImage(destination))
			{
				cv.SmoothingMode = sMode;
				cv.InterpolationMode = iMode;
				cv.CompositingMode = CompositingMode.SourceCopy;
				cv.DrawImage(source, 0, 0, ResX, ResY);
			}

			BitmapData bmpData = destination.LockBits(new Rectangle(0, 0, ResX, ResY), ImageLockMode.ReadOnly, destination.PixelFormat);

			IntPtr ptr = bmpData.Scan0;
			stride = Math.Abs(bmpData.Stride);
			imageln = stride * bmpData.Height;
			outbuffer = new byte[imageln];

			Marshal.Copy(ptr, outbuffer, 0, imageln);

			if (format == PixelFormat.Format24bppRgb)
			{
				imageln = ConvertTo16bpp(outbuffer, stride, bmpData.Width, bmpData.Height, out int newstride);
				stride = newstride;
			}

			height = bmpData.Height;
			width = bmpData.Width;

			//outbytes = imageln + sizeof(int) * 2 + sizeof(ushort) * 4;


			destination.UnlockBits(bmpData);
			framems = DateTime.Now.Ticks - framemsstart;
			framems /= 10000;
			return this;
		}

		public FrameWork DoEncode()
		{

			if(isKeyframe)
			{
				outbuffer = encoder.Encode(outbuffer, stride, width, height, imageln);
			}
			else
			{
				outbuffer = encoder.Encode(outbuffer, keyFrame, stride, width, height, imageln, keyFrameln);
			}
			imageln = outbuffer.Length;
			
			return PackHeader();
		}

		public FrameWork PackHeader()
		{
			result = new byte[imageln + sizeof(int) * 2 + sizeof(ushort) * 4];

			int control = 0;
			if (isKeyframe)
				control = 1;
			Buffer.BlockCopy(BitConverter.GetBytes(control), 0, result, 0, sizeof(int));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)stride), 0, result, sizeof(int), sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)height), 0, result, sizeof(int) + sizeof(ushort), sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)width), 0, result, sizeof(int) + sizeof(ushort) * 2, sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)framerate), 0, result, sizeof(int) + sizeof(ushort) * 3, sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes(imageln), 0, result, sizeof(int) + sizeof(ushort) * 4, sizeof(int));
			Buffer.BlockCopy(outbuffer, 0, result, sizeof(int) *2 + sizeof(ushort) * 4, imageln);
			outbytes = imageln + sizeof(int) * 2 + sizeof(ushort) * 4;
			return this;
		} 


		byte[] encodingBuffer = new byte[0];
		int ConvertTo16bpp(byte[] encodedFrame, int stride, int width, int height, out int newstride)
		{
			newstride = stride;
			if (encodedFrame.Length < sizeof(int) + sizeof(ushort) * 2)
				return encodedFrame.Length;


			newstride = ((stride / 3) * 2);
			newstride += (newstride % 4);

			int encodedlength = newstride * height;

			if (encodingBuffer.Length < encodedlength)
			{
				encodingBuffer = new byte[encodedlength];
			}

			for (int i = 0; i < height; i++)
			{

				int adjust = i * stride;

				int encadjust = i * newstride;

				for (int ii = 0, eii = 0; ii + 2 < stride; ii += 3, eii += 2)
				{
					byte r = encodedFrame[adjust + ii + 2];
					byte g = encodedFrame[adjust + ii + 1];
					byte b = encodedFrame[adjust + ii];
					BitConverter.GetBytes(ColorToUShort(r, g, b)).CopyTo(encodingBuffer, encadjust + eii);
				}
			}
			Buffer.BlockCopy(encodingBuffer, 0, encodedFrame, 0, encodedlength);
			return encodedlength;
		}

		ushort ColorToUShort(byte r, byte g, byte b)
		{
			return (ushort)(((r >> 3) << 10) + ((g >> 3) << 5) + (b >> 3));
		}
	}
}