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


		public static AnonymousPipeServerStream VideoStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
		public static AnonymousPipeServerStream AudioStream = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

		bool recording = false;
		bool playback = false;
		int control = 0;
		long frame = 0;

		Pen drawingpen = new Pen(Color.Red);
		Rectangle Rectangle = new Rectangle(0, 0, 1920, 1080);
		int recordheight = 238;
		int recordwidth = 420;
		Graphics gs;
		Bitmap bmpreader, bmppreview; //bmpbuffer;
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
		BinaryWriter outFile, outAudio;
		private MMDevice device;
		private WasapiOut blankplayer;
		private SilenceProvider silence;
		private AcmStream resamplePCM;


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

			text_encoding.Text = sourceFormat.BitsPerSample.ToString();
			
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
			outAudio.Write(0);
			outAudio.Flush();
			outAudio.Close();
			//outAudio = new BinaryWriter(new FileStream("mydataA2", FileMode.Create));
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
					outAudio.Write(header.getBytes());
				}
				outAudio.Write(bytesread / 2);
				outAudio.Write(output, 0, bytesread / 2);
				outAudio.Flush();
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
		//byte[] bmpframe;
		bool wait = false;
		bool playing = false;
		void UpdateStreaming()
		{
			while(true)
			{

				if(recording)
				{
					if(!Timer.IsRunning)
					{
						//just started
						Timer.Start();
						tick = 0;
						frame = 0;
						blankplayer.Play();
						needaudioheader = true;
						audio.StartRecording();
						outAudio = new BinaryWriter(AudioStream);
						outFile = new BinaryWriter(VideoStream);
						//outAudio = new BinaryWriter(new FileStream("mydataA", FileMode.Create));
						//outFile = new BinaryWriter(new FileStream("mydata", FileMode.Create));
						outFile.Write(header.getBytes());
						outFile.Flush();

					}
					if((framerate) * tick + framerateadd <= Timer.ElapsedTicks)
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

						/*using (Graphics cv = Graphics.FromImage(bmpbuffer))
						{
							cv.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
							cv.DrawImage(bmpreader, 0, 0, bmpbuffer.Width, bmpbuffer.Height);
						}*/

						frameout = EncodeImageToChar(recordwidth, recordheight, BitmapToByteArray(new Bitmap(bmpreader, recordwidth, recordheight)));

						int stride = recordwidth;
						int bytes = recordwidth * recordheight * 2;
						if(bytes != frameout.Length)
						{
							throw new InvalidOperationException();
						}
						//frameout = new byte[bytes];

						outFile.Write(control);
						outFile.Write((ushort)stride);
						outFile.Write((ushort)recordwidth);
						outFile.Write(frameout);

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
						control = 1;
						outFile.Write(control);
						outFile.Close();
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

		public static byte[] BitmapToByteArray(Bitmap bitmap)
		{
			BitmapData bmpdata = null;
			try
			{
				bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
				int numbytes = bmpdata.Stride * bitmap.Height;
				byte[] bytedata = new byte[numbytes];
				IntPtr ptr = bmpdata.Scan0;
				Marshal.Copy(ptr, bytedata, 0, numbytes);
				return bytedata;
			}
			finally
			{
				if (bmpdata != null)
					bitmap.UnlockBits(bmpdata);
			}
		}

		byte[] EncodeImageToChar(int height, int width, byte[] encodedFrame)
		{
			byte[] output = new byte[2 * height * width];
			Parallel.For(0, height * width, i => {
				byte r = encodedFrame[(i * 3) + 2];
				byte g = encodedFrame[(i * 3) + 1];
				byte b = encodedFrame[(i * 3)];
				BitConverter.GetBytes((ushort)ColorToChar(r, g, b)).CopyTo(output, i * 2);
			});
			return output;
		}

		char ColorToChar(byte r, byte g, byte b)
		{
			return (char)((uint)0x3000 + ((r >> 3) << 10) + ((g >> 3) << 5) + (b >> 3));
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
				//if(bmpbuffer != null)
					//bmpbuffer.Dispose();
				bmpreader = new Bitmap(Rectangle.Width, Rectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				//bmpbuffer = new Bitmap(recordwidth, recordheight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				bmppreview = new Bitmap(Rectangle.Width, Rectangle.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				Preview.Image = bmppreview;

				/*BitmapData bmpData = bmpbuffer.LockBits(new Rectangle(0, 0, bmpbuffer.Width, bmpbuffer.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bmpbuffer.PixelFormat);
				header = new videoheader();
				header.width = bmpData.Width;
				header.height = bmpData.Height;
				header.stride = bmpData.Stride;
				header.framerate = playbackframerate;
				bmpbuffer.UnlockBits(bmpData);
				frameout = new byte[header.stride * header.height];*/
			}
				
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



		private void UpdatePosY(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 0)
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
					recordwidth = newval + newval % 4;
			}
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}
		bool running = true;

		private void Playback_Click(object sender, EventArgs e)
		{
			if (!recording)
				playback = true;
		}

		private void onClosing(object sender, FormClosingEventArgs e)
		{
			running = false;
			recording = false;
			playback = false;
			MediaFoundationApi.Shutdown();
		}

		private void UpdateResY(object sender, EventArgs e)
		{
			if (recording)
				return;
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					recordheight = newval + newval % 4;
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
