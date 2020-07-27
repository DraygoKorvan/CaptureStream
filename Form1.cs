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
using System.Windows.Threading;

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


		Pen drawingpen = new Pen(Color.Red);
		//Rectangle Rectangle = new Rectangle(0, 0, 1920, 1080);
		//int recordheight = 236;
		//int recordwidth = 420;

		Bitmap bmppreview;
		Stopwatch Timer;

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

		public static bool  isConnected = false;


		long audiolengthmonitor = 0;
		private VideoRecorder video = new VideoRecorder();
		private RecordingParemeters videorecordingsettings = new RecordingParemeters(0, 0, 1920, 1080, 420, 236, 20);

		int frame = 0;
		int audioframe = 0;

		public CaptureStreamForm()
		{
			InitializeComponent();

			SetDefaultValues();
			drawingpen.Width = 1;




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

			video.Data_Available += Video_Data_Available;
			video.Recording_Stopped += Video_Recording_Stopped;
			//UpdateBmp();
			StartBackgroundWorker();
			running = true;
			var dispatcherthread = new Thread(UpdateStreaming);
			dispatcherthread.IsBackground = true;
			dispatcherthread.Start();
		}


		private void Video_Recording_Stopped(object sender, VideoStoppedArgs e)
		{
			if(isConnected)
			{
				VideoStream.Flush();
				VideoStream.WaitForPipeDrain();
			}

		}

		public void Video_Data_Available(object sender, VideoEventArgs e)
		{

			frame++;
			var ch = Thread.CurrentThread.ManagedThreadId;
			if (isConnected)
			{
				VideoStream.Write(e.payload, 0, e.payload.Length);
				VideoStream.Flush();
				VideoStream.WaitForPipeDrain();
			}

		}



		private void Audio_RecordingStopped(object sender, StoppedEventArgs e)
		{
			if(isConnected)
			{
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
			using (var str = new RawSourceWaveStream(buffer, 0, e.BytesRecorded, audio.WaveFormat))
			{
				var six = new WaveFloatTo16Provider(str);
				byte[] newbuffer = new byte[e.BytesRecorded];
				var bytesread = six.Read(newbuffer, 0, e.BytesRecorded);
				var output = StereoToMono(newbuffer);
				int control = 0;
				if(audioframe == 0)
				{
					control = 1;
				}
				audioframe++;
				if(isConnected)
				{
					AudioStream.Write(BitConverter.GetBytes(control), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(bytesread / 2), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(six.WaveFormat.SampleRate), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(six.WaveFormat.AverageBytesPerSecond / 2), 0, sizeof(int));
					AudioStream.Write(output, 0, bytesread / 2);
					AudioStream.Flush();
					AudioStream.WaitForPipeDrain();
					
				}
				audiolengthmonitor = bytesread / 2;
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
			//UpdateStreaming();
		}
		private void UpdateImagePreview()
		{
			Preview.Refresh();
			Frame_Monitor.Text = string.Format("{0:N0}", frame);
			FrameTimeMonitor.Text = string.Format("{0:N0} ms", videorecordingsettings.RecordingMs);
			AudioLength.Text = string.Format("{0:N0}", audiolengthmonitor);
			if (recording)
				RecordingText.Text = "Recording";
			else
				RecordingText.Text = "Stopped Recording";
		}
		public delegate void RefreshCallback();



		void UpdateStreaming()
		{
			
			while(true)
			{
				
				if (recording)
				{

					if(!Timer.IsRunning)
					{
						Timer.Start();
						
						blankplayer.Play();
						audioframe = 0;
						frame = 0;
						audio.StartRecording();
						video.Start(videorecordingsettings, Dispatcher.CurrentDispatcher);
					}



				}
				else
				{

					if (Timer.IsRunning)
					{
						videorecordingsettings.running = false;
						video.Stop();
						Timer.Stop();

						audio.StopRecording();
						blankplayer.Stop();
					}
					if (!running)
						return;
				}
				//Dispatcher.Run();
				Thread.Sleep(0);
			}
		}

		private void SetDefaultValues()
		{
			text_posX.Text = videorecordingsettings.PosX.ToString();
			text_posY.Text = videorecordingsettings.PosY.ToString();
			text_ResX.Text = videorecordingsettings.ResX.ToString();
			text_ResY.Text = videorecordingsettings.ResY.ToString();
			text_SizeX.Text = videorecordingsettings.SizeX.ToString();
			text_SizeY.Text = videorecordingsettings.SizeY.ToString();
			FrameRate_Text.Text = videorecordingsettings.FrameRate.ToString();
		}

		private void UpdateBmp()
		{

			RefreshPreview = true;
			if (!recording)
			{

				//RefreshRecordingBitmaps();
				RefreshPreviewBitmap();





			}
				
		}
		private void RefreshPreviewBitmap()
		{
			RefreshPreview = false;

			
			if (bmppreview != null)
				bmppreview.Dispose();
			bmppreview = new Bitmap(videorecordingsettings.SizeX, videorecordingsettings.SizeY, PixelFormat.Format24bppRgb);
			Preview.Image = bmppreview;

		}


		private void RecordOnClick(object sender, MouseEventArgs e)
		{
			recording = !recording;
			videorecordingsettings.running = recording;

		}



		private void UpdatePosX(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 0)
					videorecordingsettings.PosX = newval;
				
			}
		}

		bool RefreshPreview = false;


		private void UpdatePosY(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 0)
					videorecordingsettings.PosY = newval;
			}
		}
		private void UpdateSizeX(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					videorecordingsettings.SizeX = newval;
				UpdateBmp();
			}
		}
		private void UpdateSizeY(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
					videorecordingsettings.SizeY = newval;
				UpdateBmp();
			}
		}
		private void UpdateResX(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
				{
					videorecordingsettings.ResX = newval;

				}
					
			}
		}

		private void UpdateResY(object sender, EventArgs e)
		{

			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval > 0)
				{
					videorecordingsettings.ResY = newval;
				}
				
			}
		}

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}
		public bool running = true;





		public void onClosing(object sender, FormClosingEventArgs e)
		{
			running = false;
			recording = false;

			MediaFoundationApi.Shutdown();
		}

		private void FrameRate_Text_TextChanged(object sender, EventArgs e)
		{

			var tox = ((TextBox)sender);
			if (int.TryParse(tox.Text, out int newval))
			{
				if (recording)
				{
					tox.Text = videorecordingsettings.FrameRate.ToString();
				}
				if (newval > 0)
				{
					videorecordingsettings.FrameRate = newval;

				}
					
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
				var loc = new Rectangle(videorecordingsettings.PosX, videorecordingsettings.PosY, videorecordingsettings.SizeX, videorecordingsettings.SizeY);
				using (Graphics gsc = Graphics.FromImage(bmppreview))
					gsc.CopyFromScreen(loc.Location, Point.Empty, loc.Size);
				Thread.Sleep(0);
			}
		}
	}
}
