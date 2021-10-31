using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.Compression;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Size = System.Drawing.Size;

namespace CaptureStream
{
	public partial class CaptureStreamForm : Form
	{
		[DllImport("User32.dll")]
		public static extern IntPtr GetDC(IntPtr hwnd);
		[DllImport("User32.dll")]
		public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);

		public readonly string SEDEFAULT = Environment.ExpandEnvironmentVariables(@"%appdata%\SpaceEngineers");

		public const string TMPFILENAME = "vidfile.sevm";
		public const string VIDEOLISTNAME = "Videos.txt";

		public static AnonymousPipeServerStream VideoStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
		public static AnonymousPipeServerStream AudioStream = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

		bool recording = false;

		Bitmap bmppreview;
		Stopwatch Timer;

		WasapiLoopbackCapture audio;
		WaveFormat format = new AdpcmWaveFormat(44100, 2);

		WaveFormat sourceFormat;
		private BufferedWaveProvider sourceProvider;
		private WaveFloatTo16Provider wfto16prov;
		private StereoToMonoProvider16 monovolumeprovider;
		private WaveFormatConversionProvider formatconv;
		long framerate = 1;
		int playbackframerate = 20;
		long frameremainder = 0;
		private MMDevice device;
		private WasapiOut blankplayer;
		private ISampleProvider silence;

		public static bool  isConnected = false;

		long audiolengthmonitor = 0;
		private VideoRecorder video = new VideoRecorder();
		private VideoEncoder encoder = new VideoEncoder();
		private RecordingParameters videorecordingsettings = new RecordingParameters(0, 0, 1920, 1080, 420, 236, 20, InterpolationMode.NearestNeighbor, SmoothingMode.Default);

		int frame = 0;
		int audioframe = 0;
		int framebytes = 0;
		bool audiomuted = true;

		AudioMeterInformation AudioMonitor;

		public float leftChannelMonitor = 0f;
		public float rightChannelMonitor = 0f;

		public static CaptureStreamForm instance;
		public class EnableAudioEventArgs
		{
			public bool audioMuted = false;
		}

		public delegate void EnableLocalAudioPlaybackHandler(object sender, EnableAudioEventArgs e);

		public event EnableLocalAudioPlaybackHandler toggleMuteAudio;

		public List<MMDevice> playbackDevices = new List<MMDevice>();

		static class DisplayTools
		{
			[DllImport("gdi32.dll")]
			static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

			private enum DeviceCap
			{
				Desktopvertres = 117,
				Desktophorzres = 118
			}

			public static Size GetPhysicalDisplaySize()
			{
				Graphics g = Graphics.FromHwnd(IntPtr.Zero);
				IntPtr desktop = g.GetHdc();

				int physicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.Desktopvertres);
				int physicalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.Desktophorzres);

				return new Size(physicalScreenWidth, physicalScreenHeight);
			}


		}

		public CaptureStreamForm()
		{
			instance = this;
			InitializeComponent();
			int selected = 0;
			
			using (var enumerator = new MMDeviceEnumerator())
			{
				device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);


				for (int i = 0; i < WaveOut.DeviceCount; i++)
				{
					var adddevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)[i];
					if (device.InstanceId == adddevice.InstanceId)
					{
						selected = i;
					}
					playbackDevices.Add(adddevice);
				}
			}

			var garbage = (int)System.Windows.SystemParameters.PrimaryScreenHeight; //dunno why but having this line here makes the next two lines give the physical width/height of the primary display. 
			videorecordingsettings.SizeX = Screen.PrimaryScreen.Bounds.Width;
			videorecordingsettings.SizeY = Screen.PrimaryScreen.Bounds.Height;
			


			SetDefaultValues();
			//AudioDeviceChanger.SelectedIndex = selected;
			
			Timer = new Stopwatch();

			frameremainder = Stopwatch.Frequency % playbackframerate;
			framerate = Stopwatch.Frequency / playbackframerate;


			initAudioRecorder();

			video.Data_Available += Video_Data_Available;
			video.Recording_Stopped += Video_Recording_Stopped;
			encoder.Data_Available += Encoder_Data_Available;

			FileSaverBackground.WorkerReportsProgress = true;
			FileSaverBackground.WorkerSupportsCancellation = true;

			StartBackgroundWorker();
			running = true;
			var dispatcherthread = new Thread(UpdateStreaming)
			{
				IsBackground = true
			};
			dispatcherthread.Start();
		}

		byte[] lastframe;
		int lastframestride;
		int lastframewidth;
		int lastframeheight;
		private void Encoder_Data_Available(object sender, VideoEventArgs e)
		{

			frame++;
			
			framebytes = e.payload.outbytes;

			lastframe = e.payload.uncompressedFrame;
			lastframestride = e.payload.stride;
			lastframeheight = e.payload.height;
			lastframewidth = e.payload.width;
			if(outFile != null)
			{
				lock(outFile)
				{
					outFile.Write(e.payload.result, 0, e.payload.outbytes);
					outFile.Flush();
				}

			}

			if (isConnected)
			{
				
				VideoStream.Write(e.payload.result, 0, e.payload.outbytes);
				VideoStream.Flush();
				VideoStream.WaitForPipeDrain();

				video.ReturnWork(e.payload);
			}
	

		}
		//WaveFileWriter fileOut;
		BinaryWriter outFile;
		private void initAudioRecorder()
		{
			if(audio != null)
			{
				audio.DataAvailable -= Audio_DataAvailable;
				audio.RecordingStopped -= Audio_RecordingStopped;
				audio.Dispose();
			}
			if(blankplayer != null)
			{
				blankplayer.Dispose();
			}
			audio = new WasapiLoopbackCapture(device);
			
			
			if(sourceProvider == null || sourceFormat != audio.WaveFormat)
			{
				sourceFormat = audio.WaveFormat;
				sourceProvider = null;
				monovolumeprovider = null;
				wfto16prov = null;
				formatconv?.Dispose();

				sourceProvider = new BufferedWaveProvider(sourceFormat);
				sourceProvider.ReadFully = false;
				wfto16prov = new WaveFloatTo16Provider(sourceProvider);
				
				monovolumeprovider = new StereoToMonoProvider16(wfto16prov);
				
				formatconv = new WaveFormatConversionProvider(new WaveFormat(videorecordingsettings.sampleRate, 16, 1), monovolumeprovider); // was 24000...

			}
			if(formatconv.WaveFormat.SampleRate != videorecordingsettings.sampleRate)
			{

				formatconv.Dispose();
				formatconv = new WaveFormatConversionProvider(new WaveFormat(videorecordingsettings.sampleRate, 16, 1), monovolumeprovider);//was mono provider

				
			}
		

			text_encoding.Text = sourceFormat.Encoding.ToString();

			blankplayer = new WasapiOut(device, AudioClientShareMode.Shared, false, 0);
			
			silence = new SilenceProvider(sourceFormat).ToSampleProvider();

			AudioDevice_Text.ForeColor = Color.Black;

			try
            {
				blankplayer.Init(silence);
			} 
			catch 
            {
				AudioDevice_Text.ForeColor = Color.Red;
			}
			audio.DataAvailable += Audio_DataAvailable;
			audio.RecordingStopped += Audio_RecordingStopped;
			AudioMonitor = device.AudioMeterInformation;
		}


		private void Video_Recording_Stopped(object sender, VideoStoppedArgs e)
		{

			if (isConnected)
			{
				VideoStream.Flush();
				VideoStream.WaitForPipeDrain();
			}

		}

		public void Video_Data_Available(object sender, VideoEventArgs e)
		{
			encoder.AddJob(e.payload);
		}



		private void Audio_RecordingStopped(object sender, StoppedEventArgs e)
		{
			if(isConnected)
			{
				AudioStream.Flush();
				AudioStream.WaitForPipeDrain();
			}

		}

		private void Audio_DataAvailable(object sender, WaveInEventArgs e)
		{
			if (e.BytesRecorded == 0)
				return;
			var buffer = e.Buffer;
			if (!recording)
				return;

			sourceProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
			var bps =  sourceProvider.WaveFormat.AverageBytesPerSecond;
				

			byte[] output = new byte[e.BytesRecorded];


			monovolumeprovider.LeftVolume = (1f - videorecordingsettings.audioBalance) * videorecordingsettings.leftVolume * 2;
			monovolumeprovider.RightVolume = (videorecordingsettings.audioBalance) * videorecordingsettings.rightVolume * 2;
			


				
				var bytesread = formatconv.Read(output, 0, e.BytesRecorded);

				int control = 0;
				if(audioframe == 0)
				{
					control = 1;
				}
				audioframe++;
				if(outFile != null)
				{
					lock(outFile)
					{
					    
						outFile.Write(BitConverter.GetBytes(control), 0, sizeof(int));
						outFile.Write(BitConverter.GetBytes(bytesread), 0, sizeof(int));
						outFile.Write(BitConverter.GetBytes(formatconv.WaveFormat.SampleRate), 0, sizeof(int));//volout
				
						outFile.Write(BitConverter.GetBytes(formatconv.WaveFormat.AverageBytesPerSecond), 0, sizeof(int));
						outFile.Write(output, 0, bytesread);
						outFile.Flush();
					}
				}
				if (isConnected)
				{
					AudioStream.Write(BitConverter.GetBytes(control), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(bytesread ), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(formatconv.WaveFormat.SampleRate), 0, sizeof(int));//volout
					AudioStream.Write(BitConverter.GetBytes(formatconv.WaveFormat.AverageBytesPerSecond), 0, sizeof(int));
					AudioStream.Write(output, 0, bytesread);
					AudioStream.Flush();
					AudioStream.WaitForPipeDrain();
				}

				audiolengthmonitor = bytesread ;
			//}
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
		private void UpdateMonitors()
		{
			Preview.Refresh();
			Frame_Monitor.Text = string.Format("{0:N0}", frame);
			FrameTimeMonitor.Text = string.Format("{0:N0} ms", videorecordingsettings.RecordingMs);
			FrameMonitorText.Text = string.Format("{0:N0}", framebytes);
			AudioLength.Text = string.Format("{0:N0}", audiolengthmonitor);
			if (recording)
				RecordingText.Text = "Recording";
			else
				RecordingText.Text = "Stopped Recording";
			if(recording)
			{
				if(AudioMonitor != null)
				{
					if(AudioMonitor.PeakValues.Count == 2)
					{
						leftChannelMonitor = AudioMonitor.PeakValues[0] * (1f - videorecordingsettings.audioBalance) * videorecordingsettings.leftVolume;
						rightChannelMonitor = AudioMonitor.PeakValues[1] * (videorecordingsettings.audioBalance) * videorecordingsettings.rightVolume;
					}
				}
			}
			else
			{
				leftChannelMonitor = 0;
				rightChannelMonitor = 0f;
			}
			leftVolumeMeter.Amplitude = leftChannelMonitor;
			rightVolumeMeter.Amplitude = rightChannelMonitor;

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
						if (enableWriteToFile)
						{
							outFile?.Flush();
							outFile?.Dispose();
							outFile = new BinaryWriter(new FileStream(TMPFILENAME, FileMode.Create, FileAccess.Write));
						}


						audio.StartRecording();
						//if(fileOut == null)
						//	fileOut = new WaveFileWriter("out.wav", formatconv.WaveFormat);
						video.Start(videorecordingsettings);
						encoder.Start(videorecordingsettings);

						
					}
				}
				else
				{
					if (Timer.IsRunning)
					{
						videorecordingsettings.running = false;
						video.Stop();
						encoder.Stop();
						Timer.Stop();
						outFile?.Flush();
						outFile?.Dispose();
						outFile = null;
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
			SampleRateTextBox.Text = videorecordingsettings.sampleRate.ToString();
			
			InterpolationMode interpolationMode = videorecordingsettings.interpolationMode;
			SmoothingMode smoothingMode = videorecordingsettings.smoothingMode;

			AA_Combobox.DataSource = Enum.GetNames(typeof(SmoothingMode));
			AA_Combobox.SelectedItem = smoothingMode.ToString();
			AA_Combobox.SelectedText = smoothingMode.ToString();

			Interpolation_Combobox.DataSource = Enum.GetNames(typeof(InterpolationMode));
			Interpolation_Combobox.SelectedItem = interpolationMode.ToString();
			Interpolation_Combobox.SelectedText = interpolationMode.ToString();

			AudioDeviceChanger.DataSource = playbackDevices;

			saveVideoRecording.AddExtension = true;
			saveVideoRecording.DefaultExt = "sevm";
			saveVideoRecording.OverwritePrompt = true;
			saveVideoRecording.InitialDirectory = SEDEFAULT;
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

		private void UpdateSampleRate(object sender, EventArgs e)
		{
			if (recording)
			{
				SampleRateTextBox.Text = videorecordingsettings.sampleRate.ToString();
				return;
			}
			if (int.TryParse(((TextBox)sender).Text, out int newval))
			{
				if (newval >= 8000 && videorecordingsettings.sampleRate != newval)
				{
					videorecordingsettings.sampleRate = newval;
					initAudioRecorder();
				}
				else
				{
					//SampleRateTextBox.Text = videorecordingsettings.sampleRate.ToString();
				}
			}
		}

		public bool running = true;

		public void onClosing(object sender, FormClosingEventArgs e)
		{
			running = false;
			recording = false;

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
					Preview.Invoke(new RefreshCallback(this.UpdateMonitors));
				}
				catch
				{ 
				
				}
				var loc = new Rectangle(videorecordingsettings.PosX, videorecordingsettings.PosY, videorecordingsettings.SizeX, videorecordingsettings.SizeY);
				using (Graphics gsc = Graphics.FromImage(bmppreview))
				{
					gsc.SmoothingMode = videorecordingsettings.smoothingMode;
					gsc.InterpolationMode = videorecordingsettings.interpolationMode;
					gsc.CopyFromScreen(loc.Location, System.Drawing.Point.Empty, loc.Size);
				}
				Thread.Sleep(0);
			}
		}

		private void SmoothingModeChanged(object sender, EventArgs e)
		{
			try
			{
				SmoothingMode selectedMode = (SmoothingMode)Enum.Parse(typeof(SmoothingMode), AA_Combobox.SelectedValue.ToString());
				if (selectedMode == SmoothingMode.Invalid)
				{

					AA_Combobox.SelectedItem = videorecordingsettings.smoothingMode.ToString();
					AA_Combobox.SelectedText = videorecordingsettings.smoothingMode.ToString();
					return;
				}
				videorecordingsettings.smoothingMode = selectedMode;
			}
			catch
			{

			}
		}

		private void InterpolationModeChanged(object sender, EventArgs e)
		{
			try
			{
				InterpolationMode selectedMode = (InterpolationMode)Enum.Parse(typeof(InterpolationMode), Interpolation_Combobox.SelectedValue.ToString());
				if (selectedMode == InterpolationMode.Invalid)
				{

					AA_Combobox.SelectedItem = videorecordingsettings.interpolationMode.ToString();
					AA_Combobox.SelectedText = videorecordingsettings.interpolationMode.ToString();
					return;
				}
				videorecordingsettings.interpolationMode = selectedMode;
			}
			catch
			{

			}
		}

		private void BalanceChanged(object sender, EventArgs e)
		{
			int balvalue = BalanceSlider.Value;
			float controlvalue = (float)BalanceSlider.Value / 91f;
			if (BalanceSlider.Value == 45)
			{
				controlvalue = 0.5f;
			}
			videorecordingsettings.audioBalance = controlvalue;

			var leftbalance = 1f - videorecordingsettings.audioBalance;
			LeftBalanceText.Text = string.Format("{0:N0}", leftbalance * 100f);
			RightBalanceText.Text = string.Format("{0:N0}", videorecordingsettings.audioBalance * 100f);

		}

		private void LeftVolumeChanged(object sender, EventArgs e)
		{
			videorecordingsettings.leftVolume = (float)(91 - LeftVolumeSlider.Value) / 91f;
			LeftVolumeText.Text = string.Format("{0:N0}", videorecordingsettings.leftVolume * 100f);
		}

		private void RightVolumeChanged(object sender, EventArgs e)
		{
			videorecordingsettings.rightVolume = (float)(91 - RightVolumeSlider.Value) / 91f;
			RightVolumeText.Text = string.Format("{0:N0}", videorecordingsettings.rightVolume * 100f);
		}



		private void LeftVolumeText_Click(object sender, EventArgs e)
		{

		}

		private void EnableDither(object sender, EventArgs e)
		{
			if(DitherOnBox.Checked)
			{
				videorecordingsettings.pixelFormat = PixelFormat.Format16bppRgb555;
			}
			else
			{
				videorecordingsettings.pixelFormat = PixelFormat.Format24bppRgb;
			}
		}

		private void AudioDeviceCombobox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var selectedevice = AudioDeviceChanger.SelectedItem as MMDevice;
			if (recording)
				AudioDeviceChanger.SelectedItem = device;
			else
			{
				if (selectedevice == null)
					return;
				device = selectedevice;
				initAudioRecorder();
			}
		}

		private void MuteAudio_Changed(object sender, EventArgs e)
		{
			audiomuted = MuteCheckbox.Checked;
			RaiseHandleToggleAudio(audiomuted);
		}

		private void RaiseHandleToggleAudio(bool audioMuted)
		{
			toggleMuteAudio?.Invoke(this, new EnableAudioEventArgs() { audioMuted = audioMuted });
		}

		private void RecordOnClick(object sender, MouseEventArgs e)
		{
			recording = !recording;
			videorecordingsettings.running = recording;

			AudioDeviceChanger.Enabled = !recording;
			Save.Enabled = !recording;
			toFileCheckbox.Enabled = !recording;
		}
		//const string VIDEOFILECACHE = "videoCache";
		//const string AUDIOFILECACHE = "audioCache";

		private void saveFileDialog_FileOk(object sender, CancelEventArgs e)
		{
			RecordingButton.Enabled = false;
			if (e.Cancel)
				return;

			if (!FileSaverBackground.IsBusy)
			{
				var task = new FileSaveJob(TMPFILENAME, VIDEOLISTNAME, saveVideoRecording.FileName);
				FileSaverBackground.RunWorkerAsync(task);
			}
			
		}
		public class FileSaveJob
		{
			BinaryReader inFile;
			BinaryWriter outFile;
			StreamWriter videoListFile;

			public bool process;

			public bool complete;

			double totalLength;


			public FileSaveJob(string video, string videolistname, string outFileName)
			{

				inFile = new BinaryReader(new FileStream(video, FileMode.Open, FileAccess.Read));
				//readAudioFile = new BinaryReader(new FileStream(audio, FileMode.Open, FileAccess.Read));
				totalLength = inFile.BaseStream.Length;
				//FilePath.
				outFile = new BinaryWriter(new FileStream(outFileName, FileMode.Create, FileAccess.Write));
				var dir = Path.GetDirectoryName(outFileName);
				using (videoListFile = File.AppendText($"{dir}\\{videolistname}"))
				{
					
					videoListFile.WriteLine(Path.GetFileName(outFileName));
				}	
				complete = false;
				process = true;

			}

			public int DoWork()
			{
				if(process)
				{

					return DoVideoCopy();
				}
				else
				{
					if(outFile != null && outFile.BaseStream.CanWrite)
					{
						outFile.Flush();
						outFile.Dispose();

					}

					complete = true;
					return 100;
				}
			}
			const int READCHUNK = 65536;
			byte[] readbuffer = new byte[READCHUNK];

			long videobytesread = 0;
			long audiobytesread = 0;
			private int DoVideoCopy()
			{
				var videoread = inFile.Read(readbuffer, 0, READCHUNK);
				videobytesread += videoread;
				outFile.Write(readbuffer, 0, videoread);
				if (inFile.BaseStream.Position == inFile.BaseStream.Length)
				{
					outFile.Write(2);//eof byte
					process = false;
					inFile.Close();
					inFile.Dispose();
				}
				return (int)((videobytesread * 100L) / totalLength);
			}



		}
		private void SaveFile(object sender, EventArgs e)
		{
			var res = saveVideoRecording.ShowDialog();

		}

		private void FileSaverBackground_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			SavingProgress.Value = e.ProgressPercentage;
		}

		private void FileSaverBackground_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			SavingProgress.Value = 0;
			RecordingButton.Enabled = true;
		}

		private void SaveFileWork(object sender, DoWorkEventArgs e)
		{
			FileSaveJob job = (FileSaveJob)e.Argument;
			var worker = (BackgroundWorker)sender;
			while (!job.complete)
			{
				Thread.Sleep(0);

				int progress = job.DoWork();

				worker.ReportProgress(progress);
			}
		}

		bool enableWriteToFile = true;

		private void CheckedtoFile(object sender, EventArgs e)
		{
			enableWriteToFile = toFileCheckbox.Checked;
		}

		private void CopyToClipboard_Click(object sender, EventArgs e)
		{
			if(!recording)
			{
				GetScreenShot();
			}
			if (lastframe != null)
			{
				Clipboard.SetText(getText(lastframe, lastframewidth, lastframestride, lastframeheight));
			}

		}

		public void GetScreenShot()
		{
			var source = new Bitmap(videorecordingsettings.SizeX, videorecordingsettings.SizeY, PixelFormat.Format24bppRgb);
			var	destination = new Bitmap(videorecordingsettings.ResX, videorecordingsettings.ResY, videorecordingsettings.pixelFormat);

			using (Graphics g = Graphics.FromImage(source))
			{
				//g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.CopyFromScreen(videorecordingsettings.PosX, videorecordingsettings.PosY, 0, 0, new Size(videorecordingsettings.SizeX, videorecordingsettings.SizeY));
			}
			using (Graphics cv = Graphics.FromImage(destination))
			{
				cv.SmoothingMode = videorecordingsettings.smoothingMode;
				cv.InterpolationMode = videorecordingsettings.interpolationMode;
				cv.CompositingMode = CompositingMode.SourceCopy;
				cv.DrawImage(source, 0, 0, videorecordingsettings.ResX, videorecordingsettings.ResY);
			}

			BitmapData bmpData = destination.LockBits(new Rectangle(0, 0, videorecordingsettings.ResX, videorecordingsettings.ResY), ImageLockMode.ReadOnly, destination.PixelFormat);

			IntPtr ptr = bmpData.Scan0;
			lastframestride = Math.Abs(bmpData.Stride);
			var imageln = lastframestride * bmpData.Height;
			lastframe = new byte[imageln];

			Marshal.Copy(ptr, lastframe, 0, imageln);

			if (videorecordingsettings.pixelFormat == PixelFormat.Format24bppRgb)
			{
				imageln = ConvertTo16bpp(lastframe, lastframestride, bmpData.Width, bmpData.Height, out int newstride);
				lastframestride = newstride;
			}

			lastframeheight = bmpData.Height;
			lastframewidth = bmpData.Width;

			destination.UnlockBits(bmpData);
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


		char[] charbuffer = new char[10];
		private string getText(byte[] videoframe, int width, int stride, int height)
		{

			if (charbuffer.Length < width)
				charbuffer = new char[width * height + height];
			int ptr = 0;
			for (int y = 0; y < height; y++)
			{

				var ystride = (y * stride);
				for (int x = 0; x < width * 2; x += 2)
				{
					charbuffer[ptr++] = (char)((uint)0x3000 + BitConverter.ToUInt16(videoframe, ystride + x));
				}
				charbuffer[ptr++] = '\n';
			}
			return new string(charbuffer, 0, (width * height + height));
		}
	}
}
