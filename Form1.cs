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
using System.Drawing.Drawing2D;
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

			SetDefaultValues();
			AudioDeviceChanger.SelectedIndex = selected;

			Timer = new Stopwatch();

			frameremainder = Stopwatch.Frequency % playbackframerate;
			framerate = Stopwatch.Frequency / playbackframerate;

			var buffer = new byte[16];

			initAudioRecorder();

			video.Data_Available += Video_Data_Available;
			video.Recording_Stopped += Video_Recording_Stopped;
			encoder.Data_Available += Encoder_Data_Available;

			StartBackgroundWorker();
			running = true;
			var dispatcherthread = new Thread(UpdateStreaming)
			{
				IsBackground = true
			};
			dispatcherthread.Start();
		}

		private void Encoder_Data_Available(object sender, VideoEventArgs e)
		{

			frame++;
			
			framebytes = e.payload.outbytes;
			if (isConnected)
			{
				
				VideoStream.Write(e.payload.result, 0, e.payload.outbytes);
				VideoStream.Flush();
				VideoStream.WaitForPipeDrain();

				video.ReturnWork(e.payload);
			}

		}

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
			sourceFormat = audio.WaveFormat;
			text_encoding.Text = sourceFormat.Encoding.ToString();
			blankplayer = new WasapiOut(device, AudioClientShareMode.Shared, false, 0);
			silence = new SilenceProvider(sourceFormat);
			blankplayer.Init(silence);
			audio.DataAvailable += Audio_DataAvailable;
			audio.RecordingStopped += Audio_RecordingStopped;
			AudioMonitor = device.AudioMeterInformation;
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
				byte[] output = new byte[e.BytesRecorded / 2];
				//var bytesread = six.Read(newbuffer, 0, e.BytesRecorded);
				var volout = new StereoToMonoProvider16(six);
				volout.LeftVolume =  (1f - videorecordingsettings.audioBalance) * videorecordingsettings.leftVolume;
				volout.RightVolume = (videorecordingsettings.audioBalance) * videorecordingsettings.rightVolume;
				//byte[] output = new byte[bytesread / 2];
				var bytesread = volout.Read(output, 0, e.BytesRecorded / 2);
				//var output = StereoToMono(newbuffer);
				int control = 0;
				if(audioframe == 0)
				{
					control = 1;
				}
				audioframe++;
				if(isConnected)
				{
					AudioStream.Write(BitConverter.GetBytes(control), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(bytesread), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(volout.WaveFormat.SampleRate), 0, sizeof(int));
					AudioStream.Write(BitConverter.GetBytes(volout.WaveFormat.AverageBytesPerSecond), 0, sizeof(int));
					AudioStream.Write(output, 0, bytesread);
					AudioStream.Flush();
					AudioStream.WaitForPipeDrain();
				}
				audiolengthmonitor = bytesread;
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
						
						audio.StartRecording();
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
			InterpolationMode interpolationMode = videorecordingsettings.interpolationMode;
			SmoothingMode smoothingMode = videorecordingsettings.smoothingMode;

			AA_Combobox.DataSource = Enum.GetNames(typeof(SmoothingMode));
			AA_Combobox.SelectedItem = smoothingMode.ToString();
			AA_Combobox.SelectedText = smoothingMode.ToString();

			Interpolation_Combobox.DataSource = Enum.GetNames(typeof(InterpolationMode));
			Interpolation_Combobox.SelectedItem = interpolationMode.ToString();
			Interpolation_Combobox.SelectedText = interpolationMode.ToString();

			AudioDeviceChanger.DataSource = playbackDevices;
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

			AudioDeviceChanger.Enabled = !recording;
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
					gsc.CopyFromScreen(loc.Location, Point.Empty, loc.Size);
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
	}
}
