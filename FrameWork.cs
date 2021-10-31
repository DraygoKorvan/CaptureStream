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

		private iSEVideoCodec[] encoder = new iSEVideoCodec[3];

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
		public bool isKeyFrame;


		public byte[] outbuffer;
		public byte[] uncompressedFrame;
		public byte[] keyFrame;
		public int keyFrameln;
		public int imageln;
		public int stride;
		public int width;
		public int height;
		private int compressionRate;

		InterpolationMode iMode = InterpolationMode.Default;
		SmoothingMode sMode = SmoothingMode.Default;
		PixelFormat format = PixelFormat.Format24bppRgb;

		public FrameWork()
		{
			encoder[0] = new M0424VideoCodec();
			encoder[1] = new D8x8VideoCodec();
			encoder[2] = new D4x4VideoCodec();
		}

		internal void Prepare(RecordingParameters recordingParemeters, bool isKeyFrame, int compression)
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
			this.isKeyFrame = isKeyFrame;
			iMode = recordingParemeters.interpolationMode;
			sMode = recordingParemeters.smoothingMode;
			Format = recordingParemeters.pixelFormat;
			compressionRate = compression;
			encoder[0].Threshold = compression;
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
			uncompressedFrame = new byte[imageln];

			Marshal.Copy(ptr, uncompressedFrame, 0, imageln);

			if (format == PixelFormat.Format24bppRgb)
			{
				imageln = ConvertTo16bpp(uncompressedFrame, stride, bmpData.Width, bmpData.Height, out int newstride);
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
		byte[][] buffers = new byte[3][];
		int selectedencoder = 0;
		public FrameWork DoEncode()
		{

			if (isKeyFrame)
			{
				//buffers[0] = encoder[0].Encode(uncompressedFrame, stride, width, height, imageln);
				buffers[1] = encoder[1].Encode(uncompressedFrame, stride, width, height, imageln);
				buffers[2] = encoder[2].Encode(uncompressedFrame, stride, width, height, imageln);
			}
			else
			{
				//buffers[0] = encoder[0].Encode(uncompressedFrame, keyFrame, stride, width, height, imageln, keyFrameln);
				buffers[1] = encoder[1].Encode(uncompressedFrame, keyFrame, stride, width, height, imageln, keyFrameln);
				buffers[2] = encoder[2].Encode(uncompressedFrame, keyFrame, stride, width, height, imageln, keyFrameln);
			}
			if(buffers[1].Length < buffers[2].Length)
			{
				outbuffer = buffers[1];
				selectedencoder = 1;
				imageln = buffers[1].Length;
			}
			else
			{
				outbuffer = buffers[2];
				selectedencoder = 2;
				imageln = buffers[2].Length;
			}
			//if (buffers[0].Length < buffers[1].Length)
			//{
			//	outbuffer = buffers[0];
			//	selectedencoder = 0;
			//}
			//else
			//{
				//outbuffer = buffers[1];
				//selectedencoder = 1;
			//}
			//imageln = outbuffer.Length;

			return PackHeader();
		}



		public FrameWork PackHeader()
		{
			result = new byte[imageln + sizeof(int) * 2 + sizeof(ushort) * 4];
			var control = FrameControlFlags.VideoFrame;
			if (isKeyFrame)
				control |= FrameControlFlags.IsKeyFrame;
			control |= encoder[selectedencoder].EncodingFlag;
			Buffer.BlockCopy(BitConverter.GetBytes((uint)control), 0, result, 0, sizeof(uint));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)stride), 0, result, sizeof(int), sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)height), 0, result, sizeof(int) + sizeof(ushort), sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)width), 0, result, sizeof(int) + sizeof(ushort) * 2, sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes((ushort)framerate), 0, result, sizeof(int) + sizeof(ushort) * 3, sizeof(ushort));
			Buffer.BlockCopy(BitConverter.GetBytes(imageln), 0, result, sizeof(int) + sizeof(ushort) * 4, sizeof(int));
			Buffer.BlockCopy(outbuffer, 0, result, sizeof(int) * 2 + sizeof(ushort) * 4, imageln);
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