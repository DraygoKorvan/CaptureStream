using NAudio;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
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


		public static AnonymousPipeServerStream VideoStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
		public static AnonymousPipeServerStream AudioStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

		bool recording = false;
		bool playback = false;
		int control = 0;
		long frame = 0;

		Pen drawingpen = new Pen(Color.Red);
		Rectangle Rectangle = new Rectangle(0, 0, 1920, 1080);
		int recordheight = 236;
		int recordwidth = 420;
		Graphics gs;
		Bitmap bmpbuffer, bmpreader, bmppreview;
		Stopwatch Timer;
		long tick = 0;
		long tickremainder = 0;
		long framerateadd = 0;
		WasapiLoopbackCapture audio;
		WaveFormat format = new AdpcmWaveFormat(44100, 2);

		WaveFormat sourceFormat;
		long framerate = 1;
		int playbackframerate = 20;
		long frameremainder = 0;
		private MMDevice device;
		private WasapiOut blankplayer;
		private SilenceProvider silence;
		//private AcmStream resamplePCM;


		private videoheader header;
		private audioheader aheader;
		long frameratemonitor = 0;
		long audiolengthmonitor = 0;

		public CaptureStreamForm()
		{
			InitializeComponent();

			SetDefaultValues();
			//Rectangle = new Rectangle(531, 136, 852, 480);
			drawingpen.Width = 1;
			//bmppreview = new Bitmap(Rectangle.Width, Rectangle.Height);
			//Preview.Image = bmppreview;
			//VideoStream.WriteTimeout = 1000;
			//AudioStream.WriteTimeout = 1000;

	

			Timer = new Stopwatch();

			
			frameremainder = Stopwatch.Frequency % playbackframerate;
			framerate = Stopwatch.Frequency / playbackframerate;
			using (var enumerator = new MMDeviceEnumerator())
			{
				device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
			}

			audio = new WasapiLoopbackCapture();
			sourceFormat = audio.WaveFormat;
			//resamplePCM = new WaveFloatTo16Provider(new WaveProvider);
			var buffer = new byte[16];


			//resamplePCM = new AcmStream(WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, 44100, 1, 176400, 4, 16), format);
			MediaFoundationApi.Startup();
			blankplayer = new WasapiOut();
			silence = new SilenceProvider(sourceFormat);
			blankplayer.Init(silence);

			text_encoding.Text = sourceFormat.Encoding.ToString();
			
			audio.DataAvailable += Audio_DataAvailable;
			audio.RecordingStopped += Audio_RecordingStopped;
			UpdateBmp();
			StartBackgroundWorker();

		}
		bool needaudioheader = false;
		public struct audioheader
		{
			public int SampleRate;
			public int Channels;
			public int BitsPerSample;
			public int AverageBytesPerSecond;

			public byte[] getBytes()
			{
				var retval = new byte[sizeof(int) * 4];
				BitConverter.GetBytes(SampleRate).CopyTo(retval, 0);
				BitConverter.GetBytes(Channels).CopyTo(retval, sizeof(int));
				BitConverter.GetBytes(BitsPerSample).CopyTo(retval, sizeof(int) * 2);
				BitConverter.GetBytes(AverageBytesPerSecond).CopyTo(retval, sizeof(int) * 3);
				return retval;

			}
			public static audioheader getHeader(BinaryReader source)
			{
				var data = source.ReadBytes(sizeof(int) * 4);
				var retval = new audioheader();
				retval.SampleRate =				BitConverter.ToInt32(data, sizeof(int) * 0);
				retval.Channels =				BitConverter.ToInt32(data, sizeof(int) * 1);
				retval.BitsPerSample =			BitConverter.ToInt32(data, sizeof(int) * 2);
				retval.AverageBytesPerSecond =	BitConverter.ToInt32(data, sizeof(int) * 3);
				return retval;
			}

			public static audioheader getFromBytes(byte[] bytes)
			{
				var header = new audioheader();
				header.SampleRate = BitConverter.ToInt32(bytes, 0);
				header.Channels = BitConverter.ToInt32(bytes, sizeof(int));
				header.BitsPerSample = BitConverter.ToInt32(bytes, sizeof(int) * 2);
				header.AverageBytesPerSecond = BitConverter.ToInt32(bytes, sizeof(int) * 3);
				return header;
			}
			public static int Length()
			{
				return sizeof(int) * 4;
			}
		}

		private void Audio_RecordingStopped(object sender, StoppedEventArgs e)
		{
			if(AudioStream != null)
			{

				AudioStream.Write(new byte[1] { 0 }, 0, 1);
				AudioStream.Flush();
				AudioStream.WaitForPipeDrain();
			}
		}
		private byte[] StereoToMono(byte[] input)
		{
			byte[] output = new byte[input.Length / 2];
			int outputIndex = 0;
			for (int n = 0; n < input.Length; n += 4)
			{
				// copy in the first 16 bit sample
				output[outputIndex++] = input[n];
				output[outputIndex++] = input[n + 1];
			}
			return output;
		}
		private void Audio_DataAvailable(object sender, WaveInEventArgs e)
		{
			if (e.BytesRecorded == 0)
				return;
			var buffer = e.Buffer;
			if (!recording)
				return;
			//ok lets write to a temp file, read it to conver.
			using (var str = new RawSourceWaveStream(buffer, 0, e.BytesRecorded, audio.WaveFormat))
			{
				var six = new WaveFloatTo16Provider(str);
				//var outstream = new RawSourceWaveStream(six, six.WaveFormat);
				byte[] newbuffer = new byte[e.BytesRecorded];
				var bytesread = six.Read(newbuffer, 0, e.BytesRecorded);
				var output = StereoToMono(newbuffer);
				if (needaudioheader)
				{
					needaudioheader = false;
					aheader = new audioheader();
					aheader.BitsPerSample = six.WaveFormat.BitsPerSample / 2;
					aheader.SampleRate = six.WaveFormat.SampleRate;
					aheader.AverageBytesPerSecond = six.WaveFormat.AverageBytesPerSecond / 2;
					aheader.Channels = six.WaveFormat.Channels / 2;
					var dat = aheader.getBytes();
					AudioStream.Write(dat, 0, dat.Length);
					AudioStream.Flush();
					AudioStream.WaitForPipeDrain();
				}
				AudioStream.Write(BitConverter.GetBytes(bytesread / 2), 0 , sizeof(int));
				AudioStream.Write(output, 0, bytesread / 2);
				AudioStream.Flush();
				AudioStream.WaitForPipeDrain();
				audiolengthmonitor = bytesread / 2;
			}
		}
		public struct videoheader
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
			public static videoheader getFromBytes(byte[] bytes)
			{
				var header = new videoheader();
				header.width = BitConverter.ToInt32(bytes, 0);
				header.height = BitConverter.ToInt32(bytes, sizeof(int));
				header.stride = BitConverter.ToInt32(bytes, sizeof(int) * 2);
				header.framerate = BitConverter.ToInt32(bytes, sizeof(int) * 3);
				return header;
			}

			public static int Length()
			{
				return sizeof(int) * 4;
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
			AudioLength.Text = string.Format("{0:N0}", audiolengthmonitor.ToString());
			if (recording)
				RecordingText.Text = "Recording";
			else
				RecordingText.Text = "Stopped Recording";
		}
		public delegate void RefreshCallback();
		byte[] outcolor = new byte[3];
		byte[] frameout;
		byte[] bmpframe;


		void UpdateStreaming()
		{
			while(true)
			{

				if(recording)
				{
					if(!Timer.IsRunning)
					{
						

						if(!VideoStream.IsConnected || !AudioStream.IsConnected)
						{
							recording = false;
							return;
						}
						//just started
						Timer.Start();
						tick = 0;
						frame = 0;
						blankplayer.Play();
						needaudioheader = true;
						audio.StartRecording();

						//outAudio = new BinaryWriter(new FileStream("mydataA", FileMode.Create));
						//outFile = new BinaryWriter(new FileStream("mydata", FileMode.Create));
						var headerbytes = header.getBytes();
						VideoStream.Write(headerbytes, 0, headerbytes.Length);
						VideoStream.Flush();
						VideoStream.WaitForPipeDrain();

					}
					if((framerate) * tick + framerateadd <= Timer.ElapsedTicks)
					{
						var start = Timer.ElapsedTicks;
						tick++;
						tickremainder += frameremainder;
						if(tickremainder > framerate)
						{
							tickremainder -= framerate;
							framerateadd = 1;//prevent capture desync. 
						}
						framerateadd = 0;
						if(RefreshBitmaps)
						{
							RefreshRecordingBitmaps();
						}
						//Rectangle bounds = new Rectangle(Rectangle.Right, Rectangle.Top, Rectangle.Width, Rectangle.Height);
						using (Graphics g = Graphics.FromImage(bmpreader))
						{
							g.CopyFromScreen(Rectangle.Location, Point.Empty, Rectangle.Size);
						}

						using (Graphics cv = Graphics.FromImage(bmpbuffer))
						{
							//cv.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
							cv.DrawImage(bmpreader, 0, 0, recordwidth, recordheight);
						}

						System.Drawing.Imaging.BitmapData bmpData = bmpbuffer.LockBits(new Rectangle(0, 0, recordwidth, recordheight), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpbuffer.PixelFormat);

						IntPtr ptr = bmpData.Scan0;
						int stride = Math.Abs(bmpData.Stride);
						int bytes = stride * bmpData.Height;
						if (bytes != frameout.Length)
						{
							throw new InvalidOperationException();
						}
						//frameout = new byte[bytes];
						Marshal.Copy(ptr, frameout, 0, bytes);

						VideoStream.Write(BitConverter.GetBytes(control), 0, sizeof(int));
						VideoStream.Write(BitConverter.GetBytes((ushort)stride), 0, sizeof(ushort));
						VideoStream.Write(BitConverter.GetBytes((ushort)bmpData.Height), 0, sizeof(ushort));
						VideoStream.Write(frameout, 0, frameout.Length);

						bmpbuffer.UnlockBits(bmpData);

						VideoStream.Flush();
						VideoStream.WaitForPipeDrain();
						frameratemonitor = Timer.ElapsedTicks - start;
					}
				}
				else
				{

					if (Timer.IsRunning)
					{

						Timer.Stop();
						control = 1;
						VideoStream.Write(BitConverter.GetBytes(control), 0 , sizeof(int));
						VideoStream.Flush();
						VideoStream.WaitForPipeDrain();
						//VideoStream.Close();
						//outFile = new BinaryWriter(new FileStream("mydata2", FileMode.Create));
						audio.StopRecording();
						blankplayer.Stop();

						
					}
					if (!running)
						return;
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
			RefreshBitmaps = true;
			RefreshPreview = true;
			if (!recording)
			{

				RefreshRecordingBitmaps();
				RefreshPreviewBitmap();





			}
				
		}
		private void RefreshPreviewBitmap()
		{
			RefreshPreview = false;

			
			if (bmppreview != null)
				bmppreview.Dispose();
			bmppreview = new Bitmap(Rectangle.Width, Rectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			Preview.Image = bmppreview;

		}
		private void RefreshRecordingBitmaps()
		{
			if (bmpbuffer != null)
				bmpbuffer.Dispose();
			if (bmpreader != null)
				bmpreader.Dispose();
			RefreshBitmaps = false;
			bmpreader = new Bitmap(Rectangle.Width, Rectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			bmpbuffer = new Bitmap(recordwidth, recordheight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
			BitmapData bmpData = bmpbuffer.LockBits(new Rectangle(0, 0, bmpbuffer.Width, bmpbuffer.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpbuffer.PixelFormat);
			header = new videoheader();
			header.width = bmpData.Width;
			header.height = bmpData.Height;
			header.stride = bmpData.Stride;
			header.framerate = playbackframerate;
			bmpbuffer.UnlockBits(bmpData);

			frameout = new byte[header.stride * header.height];
		}

		private void RecordOnClick(object sender, MouseEventArgs e)
		{
			recording = !recording;
		}



		private void UpdatePosX(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 0)
					Rectangle.X = newval;
				
			}
		}

		bool RefreshBitmaps = false;
		bool RefreshPreview = false;


		private void UpdatePosY(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 0)
					Rectangle.Y = newval;
			}
		}
		private void UpdateSizeX(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.Width = newval;
				UpdateBmp();
			}
		}
		private void UpdateSizeY(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					Rectangle.Height = newval;
				UpdateBmp();
			}
		}
		private void UpdateResX(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					recordwidth = newval + newval % 4;
			}
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}
		public bool running = true;

		private void Playback_Click(object sender, EventArgs e)
		{
			if (!recording)
				playback = true;
		}



		public void onClosing(object sender, FormClosingEventArgs e)
		{
			running = false;
			recording = false;
			playback = false;
			MediaFoundationApi.Shutdown();
		}

		private void UpdateResY(object sender, EventArgs e)
		{

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
				if(RefreshPreview)
				{
					RefreshPreviewBitmap();
				}
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
