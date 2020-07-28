using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CaptureStream
{
	public class VideoEventArgs
	{
		public byte[] payload;

		public VideoEventArgs(byte[] load)
		{
			payload = load;

		}
	}

	public class VideoStoppedArgs
	{

	}
	class VideoRecorder
	{
		public delegate void VideoDataAvailableHandler(object sender, VideoEventArgs e);
		public delegate void RecordingStoppedHandler(object sender, VideoStoppedArgs e);
		public event VideoDataAvailableHandler Data_Available;
		public event RecordingStoppedHandler Recording_Stopped;
		Thread thread;
		Stopwatch timer = new Stopwatch();
		long frequency = Stopwatch.Frequency;
		long frametime;
		int frames = 0;
		int maxframes = 20;
		Dispatcher dispatcher;
		long nextFrame, nextFrameSecond;
		RecordingParameters RecordingParemeters;

		private void RaiseDataAvailable(byte[] framedata)
		{
			var args = new VideoEventArgs(framedata);
			Data_Available?.Invoke(this, args);
		}

		private void RaiseStoppedRecording()
		{
			Recording_Stopped?.Invoke(this, new VideoStoppedArgs());
		}

		public void Start(RecordingParameters control, Dispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
			this.RecordingParemeters = control;
			frametime = Stopwatch.Frequency / control.FrameRate;
			//timer.Start();
			maxframes = control.FrameRate;
			thread = new Thread(Update);
			thread.IsBackground = true;
			thread.Start();
		}

		public void Stop()
		{
			RecordingParemeters.running = false;
		}

		private void Update()
		{
			timer.Start();
			nextFrame = timer.ElapsedTicks;
			nextFrameSecond = timer.ElapsedTicks;
			var lastFrametime = timer.ElapsedMilliseconds;
			var Tasks = new List<Task<FrameWork>>();
			var workstack = new Stack<FrameWork>();
			while (true)
			{
				Thread.Sleep(0);
				if(Tasks.Count > 0)
				{
					var first = Tasks.First();
					if(first.IsCompleted)
					{
						var work = first.Result;
						RaiseDataAvailable(work.result);
						RecordingParemeters.RecordingMs = work.framems ;
						workstack.Push(work);
						Tasks.RemoveAt(0);
					}
					
				}
				if (!RecordingParemeters.running)
				{
					break;
				}
				bool needframe = false;
				bool keyFrame = false;

				if (frames < maxframes)
				{
					if(nextFrame > timer.ElapsedTicks)
					{
						continue;
					}
					frames++;
					nextFrame += frametime;
					needframe = RecordingParemeters.running;
				}
				if (frames > maxframes || nextFrameSecond <= timer.ElapsedTicks)
				{
					needframe = RecordingParemeters.running;
					keyFrame = true;
					frames = 0;
					nextFrame = timer.ElapsedTicks + frametime;
					nextFrameSecond += frequency;
				}
				if (!needframe)
				{
					continue;
				}
				FrameWork p;
				if (workstack.Count > 0)
					 p = workstack.Pop();
				else
				{
					p = new FrameWork();
				}
				p.Prepare(RecordingParemeters, keyFrame);
				p.GetScreenshot();
				var task = Task.Run(p.DoWork);
				Tasks.Add(task);
			}
			timer.Stop();
			RaiseStoppedRecording();
		}



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

			private iSEVideoEncoder encoder = new NullVideoEncoder();

			public int PosX 
			{ 
				get => posX;
				set
				{
					if(posX != value)
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
			private bool isKeyframe;
			InterpolationMode iMode = InterpolationMode.Default;
			SmoothingMode sMode = SmoothingMode.Default;
			PixelFormat format = PixelFormat.Format24bppRgb;

			public FrameWork()
			{
				encoder = new NullVideoEncoder();
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
				int stride = Math.Abs(bmpData.Stride);
				int imageln = stride * bmpData.Height;

				var imgbuffer = new byte[imageln];

				Marshal.Copy(ptr, imgbuffer, 0, imageln);
				if (format == PixelFormat.Format24bppRgb)
				{
					imageln= ConvertTo16bpp(imgbuffer, stride, bmpData.Width, bmpData.Height, out int newstride);
					stride = newstride;
				}

				int control = 0;
				if (isKeyframe)
					control = 1;

				//encode here. 

				outbytes = imageln + sizeof(int) + sizeof(ushort) * 4;
				result = new byte[outbytes];
				Buffer.BlockCopy(BitConverter.GetBytes(control), 0, result, 0, sizeof(int));
				
				Buffer.BlockCopy(BitConverter.GetBytes((ushort)stride),0, result, sizeof(int), sizeof(ushort));
				Buffer.BlockCopy(BitConverter.GetBytes((ushort)bmpData.Height), 0, result, sizeof(int) + sizeof(ushort), sizeof(ushort));
				Buffer.BlockCopy(BitConverter.GetBytes((ushort)bmpData.Width), 0, result, sizeof(int) + sizeof(ushort) * 2, sizeof(ushort));
				Buffer.BlockCopy(BitConverter.GetBytes((ushort)framerate), 0, result, sizeof(int) + sizeof(ushort) * 3, sizeof(ushort));
				Buffer.BlockCopy(imgbuffer, 0, result, sizeof(int) + sizeof(ushort) * 4, imageln);
				//imgbuffer = encoder.Encode(imgbuffer);


				destination.UnlockBits(bmpData);
				framems = DateTime.Now.Ticks - framemsstart;
				framems /= 10000;
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
}