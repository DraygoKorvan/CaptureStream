using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CaptureStream
{
	public partial class CaptureStreamForm : Form
	{
		[DllImport("User32.dll")]
		public static extern IntPtr GetDC(IntPtr hwnd);
		[DllImport("User32.dll")]
		public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);


		bool recording = false;
		bool drawcapturebox = false;
		Pen drawingpen = new Pen(Color.Red);
		Rectangle Rectangle = new Rectangle(531, 136, 852, 480);
		int recordheight = 240;
		int recordwidth = 426;
		Graphics gs;
		Bitmap bmpbuffer, bmpreader, bmppreview;
		Stopwatch Timer;
		long tick = 0;
		long tickremainder = 0;
		long framerateadd = 0;
		WasapiLoopbackCapture audio;
		WaveFormat format = new WaveFormat(44100, 32, 1);
		WaveFormat sourceFormat;
		long framerate = 1;
		int playbackframerate = 20;
		long frameremainder = 0;
		BinaryWriter outFile, outAudio;
		private MMDevice device;
		private WasapiOut blankplayer;
		private SilenceProvider silence;

		private streamheader header;

		long frameratemonitor = 0;

		public CaptureStreamForm()
		{
			InitializeComponent();

			SetDefaultValues();
			Rectangle = new Rectangle(531, 136, 852, 480);
			drawingpen.Width = 1;
			bmppreview = new Bitmap(Rectangle.Width, Rectangle.Height);
			Preview.Image = bmppreview;
			
			
			Timer = new Stopwatch();

			
			frameremainder = Stopwatch.Frequency % playbackframerate;
			framerate = Stopwatch.Frequency / playbackframerate;
			using (var enumerator = new MMDeviceEnumerator())
			{
				device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
			}

			audio = new WasapiLoopbackCapture();
			sourceFormat = audio.WaveFormat;
			blankplayer = new WasapiOut();
			silence = new SilenceProvider(sourceFormat);
			blankplayer.Init(silence);

			text_encoding.Text = sourceFormat.Encoding.ToString();
			
			audio.DataAvailable += Audio_DataAvailable;
			audio.RecordingStopped += Audio_RecordingStopped;
			UpdateBmp();
			StartBackgroundWorker();

		}

		private void Audio_RecordingStopped(object sender, StoppedEventArgs e)
		{
			outAudio.Close();
			//outAudio = new BinaryWriter(new FileStream("mydataA2", FileMode.Create));
		}

		private void Audio_DataAvailable(object sender, WaveInEventArgs e)
		{
			if (e.BytesRecorded == 0)
				return;
			var buffer = e.Buffer;

			outAudio.Write(buffer);
			//text_encoding.Text = sourceFormat.Encoding.ToString();


		}
		public struct streamheader
		{
			public int width;
			public int height;
			public int stride;
			public int framerate;

			public byte[] getBytes()
			{
				var retval = new byte[sizeof(int) * 4];
				BitConverter.GetBytes(width).CopyTo(retval, 0);
				BitConverter.GetBytes(height).CopyTo(retval, sizeof(int));
				BitConverter.GetBytes(stride).CopyTo(retval, sizeof(int)*2);
				BitConverter.GetBytes(framerate).CopyTo(retval, sizeof(int) * 3);
				return retval;

			}
		}
		private void StartBackgroundWorker()
		{
			if(!backgroundWorker.IsBusy)
			{

				backgroundWorker.RunWorkerAsync();
			}
			if(!ImageUpdater.IsBusy)
			{
				ImageUpdater.RunWorkerAsync();
			}
		}

		private void BackGroundWorkerDoWork(object sender, DoWorkEventArgs e)
		{
			UpdateStreaming();
		}
		private void UpdateImagePreview()
		{
			Preview.Refresh();
			FrameTimeMonitor.Text = string.Format("{0:N0} ms", (frameratemonitor / (double)Stopwatch.Frequency) * 1000);
		}
		public delegate void RefreshCallback();
		byte[] outcolor = new byte[3];
		byte[] frameout;
		byte[] bmpframe;
		bool wait = false;
		void UpdateStreaming()
		{
			while(running)
			{

				if(recording)
				{
					if(!Timer.IsRunning)
					{
						//just started
						Timer.Start();
						tick = 0;

						blankplayer.Play();
						audio.StartRecording();
						outAudio = new BinaryWriter(new FileStream("mydataA", FileMode.Create));
						outFile = new BinaryWriter(new FileStream("mydata", FileMode.Create));
						outFile.Write(header.getBytes());
						outFile.Flush();

					}
					if(  (framerate ) * tick + framerateadd <= Timer.ElapsedTicks)
					{
						var start = Timer.ElapsedTicks;
						wait = false;
						tick++;
						tickremainder += frameremainder;
						if(tickremainder > framerate)
						{
							tickremainder -= framerate;
							framerateadd = 1;//prevent capture desync. 
						}
						framerateadd = 0;
							
						//Rectangle bounds = new Rectangle(Rectangle.Right, Rectangle.Top, Rectangle.Width, Rectangle.Height);
						using (Graphics g = Graphics.FromImage(bmpreader))
						{
							g.CopyFromScreen(Rectangle.Location, Point.Empty, Rectangle.Size);

						}
						using (Graphics cv = Graphics.FromImage(bmpbuffer))
						{
							cv.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							cv.DrawImage(bmpreader, 0, 0, bmpbuffer.Width, bmpbuffer.Height);
						}

						System.Drawing.Imaging.BitmapData bmpData = bmpbuffer.LockBits(new Rectangle(0, 0, bmpbuffer.Width, bmpbuffer.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpbuffer.PixelFormat);

						IntPtr ptr = bmpData.Scan0;
						int stride = Math.Abs(bmpData.Stride);
						int bytes = stride * bmpData.Height;
						if(bytes != frameout.Length)
						{
							throw new InvalidOperationException();
						}
						//frameout = new byte[bytes];
						Marshal.Copy(ptr, frameout, 0, bytes);
						
						outFile.Write(frameout);

						bmpbuffer.UnlockBits(bmpData);
						outFile.Flush();
						frameratemonitor = Timer.ElapsedTicks - start;
					}
					else
					{
						wait = true;
					}	



				}
				else
				{

					if (Timer.IsRunning)
					{
						Timer.Stop();
						outFile.Close();
						//outFile = new BinaryWriter(new FileStream("mydata2", FileMode.Create));
						audio.StopRecording();
						blankplayer.Stop();

						
					}
				}
				Thread.Sleep(0);
			}



		}

		private void SetDefaultValues()
		{
			text_posX.Text = Rectangle.X.ToString();
			text_posY.Text = Rectangle.Y.ToString();
			text_ResX.Text = recordwidth.ToString();
			text_ResY.Text = recordheight.ToString();
			text_SizeX.Text = Rectangle.Width.ToString();
			text_SizeY.Text = Rectangle.Height.ToString();

		}

		private void UpdateBmp()
		{
			if(!recording)
			{
				if(bmpbuffer != null)
					bmpbuffer.Dispose();
				bmpreader = new Bitmap(Rectangle.Width, Rectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				bmpbuffer = new Bitmap(recordwidth, recordheight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

				System.Drawing.Imaging.BitmapData bmpData = bmpbuffer.LockBits(new Rectangle(0, 0, bmpbuffer.Width, bmpbuffer.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpbuffer.PixelFormat);
				header = new streamheader();
				header.width = bmpData.Width;
				header.height = bmpData.Height;
				header.stride = bmpData.Stride;
				header.framerate = playbackframerate;
				bmpbuffer.UnlockBits(bmpData);
				frameout = new byte[header.stride * header.height];
			}
				
		}

		private void RecordOnClick(object sender, MouseEventArgs e)
		{
			recording = !recording;
			drawcapturebox = false;
		}

		private void DrawCaptureBoxEnable(object sender, MouseEventArgs e)
		{
			if(!recording)
			drawcapturebox = !drawcapturebox;
		}

		private void UpdatePosX(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.X = newval;
				
			}
		}



		private void UpdatePosY(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.Y = newval;
			}
		}
		private void UpdateSizeX(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.Width = newval;
				UpdateBmp();
			}
		}
		private void UpdateSizeY(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.Height = newval;
				UpdateBmp();
			}
		}
		private void UpdateResX(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					recordwidth = newval;
			}
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}
		bool running = true;
		private void onClosing(object sender, FormClosingEventArgs e)
		{
			running = false;
		}

		private void UpdateResY(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					recordheight = newval;
			}
		}

		private void UpdateImageDoWork(object sender, DoWorkEventArgs e)
		{
			while (running)
			{

				try
				{
					Preview.Invoke(new RefreshCallback(this.UpdateImagePreview));
				}
				catch
				{ 
				
				}
				using (Graphics gsc = Graphics.FromImage(bmppreview))
					gsc.CopyFromScreen(Rectangle.Location, Point.Empty, Rectangle.Size);
				Thread.Sleep(0);
			}
		}
	}
}
