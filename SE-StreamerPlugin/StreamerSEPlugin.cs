using CaptureStream;
using Sandbox.ModAPI;
using System;
using System.Threading;
using VRage.Plugins;
using System.Windows.Forms;
using System.IO;
using VRage;
using System.IO.Pipes;
using static CaptureStream.CaptureStreamForm;
using VRage.Utils;
using System.Diagnostics;

namespace SE_StreamerPlugin
{
	public class StreamerSEPlugin : IPlugin
	{
		private static Mutex mutex = null;
		const string appName = "BitStream";
		private static Stream Audio;
		private static Stream Video;

		Thread RecorderApplicationthread;
		Thread CommunicationThreadVideo, CommunicationThreadAudio;

		Thread mainthread;

		static CaptureStreamForm form;
		public void Dispose()
		{
			CaptureStreamForm.ActiveForm?.Close();
		}

		public void Init(object gameInstance)
		{
			mutex = new Mutex(true, appName, out bool createdNew);

			if (!createdNew)
			{
				return;
			}

			process = Process.GetCurrentProcess();
			mainthread = Thread.CurrentThread;

			RecorderApplicationthread = new Thread(StartApplication);
			RecorderApplicationthread.IsBackground = true; 
			RecorderApplicationthread.Start();
			CommunicationThreadVideo = new Thread(ModCommunicationVideo);
			CommunicationThreadVideo.IsBackground = true;
			CommunicationThreadVideo.Start();
			CommunicationThreadAudio = new Thread(ModCommunicationAudio);
			CommunicationThreadAudio.IsBackground = true;
			CommunicationThreadAudio.Start();
			//MyLog.Default.WriteLine(gameInstance.ToString());
		}



		public void StartApplication()
		{

			Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			
			//TODO: detect when form not found and exit

			form = new CaptureStreamForm();
			form.toggleMuteAudio += Form_toggleEnableAudio;
			Application.Run(form);
		}

		bool audioMute = true;

		private void Form_toggleEnableAudio(object sender, EnableAudioEventArgs e)
		{
			audioMute = e.audioMuted;
			Control?.Invoke((int)(e.audioMuted ? ControlFlags.AudioDisable : ControlFlags.AudioEnable));

		}

		bool registerevents = false;
		byte[] transferabuffer = new byte[4722];
		byte[] transfervbuffer = new byte[300000];
		public void Update()
		{
			//MyLog.Default.WriteLineAndConsole("Plugin - Update " + (SendAudio == null).ToString() + (SendVideo == null).ToString() );
			if (MyAPIGateway.Session != null)
			{


				//MyLog.Default.WriteLineAndConsole("Plugin - Got Session ");
				if (!registerevents)
				{
					MyLog.Default.WriteLineAndConsole("Plugin - Registered Events");
					MyAPIUtilities.Static.RegisterMessageHandler(videostreamcommand, RequestStreams);
					registerevents = true;
				}
			}
			else
			{
				if(registerevents)
				{
					MyLog.Default.WriteLineAndConsole("Plugin - Released Events");
					SendAudio = null;
					SendVideo = null;
					Control = null;
					MyAPIUtilities.Static.UnregisterMessageHandler(videostreamcommand, RequestStreams);
					registerevents = false;
				}

			}
			//MyLog.Default.WriteLineAndConsole("Status: " + CommunicationThread.ThreadState.ToString());
		}
		private void ModCommunicationAudio()
		{
			while (mainthread.ThreadState != System.Threading.ThreadState.Stopped)
			{
				try
				{
					if (Audio == null)
					{
						Audio = new AnonymousPipeClientStream(PipeDirection.In, CaptureStreamForm.AudioStream.GetClientHandleAsString());

						if (Video != null)
							CaptureStreamForm.isConnected = true;
						continue;
					}
					if (Audio.CanRead)
					{
						int headersize = sizeof(int) * 4;
						int read = 0;
						do
						{
							read += Audio.Read(transferabuffer, read, headersize - read);
						}
						while (read < headersize);
						
						int bytes = BitConverter.ToInt32(transferabuffer, sizeof(int));
						
						//int samplerate = BitConverter.ToInt32(transferabuffer, sizeof(int));
						if (bytes + headersize > transferabuffer.Length)
						{
							var oldbuf = transferabuffer;
							transferabuffer = new byte[bytes + headersize];//grow automatically. 
							Buffer.BlockCopy(oldbuf, 0, transferabuffer, 0, headersize);//move size header to new array. 
						}
						int rest = 0;
						do
						{
							rest += Audio.Read(transferabuffer, rest + headersize, bytes - rest);
						}
						while (rest < bytes);
						SendAudio?.Invoke(transferabuffer, bytes + headersize);
					}

				
				
				}
				catch (Exception ex)
				{
					MyLog.Default.WriteLineAndConsole(ex.ToString());
				}
				if (process.HasExited)
					break;
				Thread.Sleep(0);
			}
		}
		public void ModCommunicationVideo()
		{
			while(mainthread.ThreadState != System.Threading.ThreadState.Stopped)
			{
				try
				{

					if ( Video == null)
					{
						

						Video = new AnonymousPipeClientStream(PipeDirection.In, CaptureStreamForm.VideoStream.GetClientHandleAsString());
						if (Audio != null)
							CaptureStreamForm.isConnected = true;

						continue;
					}
					if (Video.CanRead)
					{
						int headersize = sizeof(int) * 2 + sizeof(ushort) * 4;
						var read = Video.Read(transfervbuffer, 0, headersize);
						while(read < headersize)
						{
							read += Video.Read(transfervbuffer, read, headersize);
							if (process.HasExited)
								break;
						}
						//int control = BitConverter.ToInt32(transfervbuffer, 0);

						//ushort stride = BitConverter.ToUInt16(transfervbuffer, sizeof(int));
						//ushort height = BitConverter.ToUInt16(transfervbuffer, sizeof(ushort) + sizeof(int));
						//ushort width = BitConverter.ToUInt16(transfervbuffer, sizeof(ushort) * 2 + sizeof(int));
						//ushort framerate = BitConverter.ToUInt16(transfervbuffer, sizeof(ushort) * 3 + sizeof(int));
						int bytes = BitConverter.ToInt32(transfervbuffer, sizeof(ushort) * 4 + sizeof(int));
						//MyLog.Default.WriteLine($"Video Packet Header: b {bytes}");

						//int bytes = stride * height;
						if (bytes + headersize > transfervbuffer.Length)
						{
							var oldbuffer = transfervbuffer;
							transfervbuffer = new byte[bytes + headersize];
							Buffer.BlockCopy(oldbuffer, 0, transfervbuffer, 0, headersize);
						}
						int rest = 0;
						do
						{
							rest += Video.Read(transfervbuffer, rest + headersize, bytes - rest);
							if (process.HasExited)
								break;
						}
						while (rest < bytes);
						//MyLog.Default.WriteLine($"Plugin: ModCommunication - Sending Video - {bytes + headersize}");
						SendVideo?.Invoke(transfervbuffer, bytes + headersize);
					
					}
				}
				catch (Exception ex)
				{
					MyLog.Default.WriteLineAndConsole(ex.ToString());
				}
				if (process.HasExited)
					break;
				Thread.Sleep(0);
			}
		}

		readonly long videostreamcommand = 20982309832901;
		//readonly long videostreamid = 20982309832902;

		private Action<byte[], int> SendAudio;
		private Action<byte[], int> SendVideo;
		private Action<int> Control;
		private Process process;

		enum ControlFlags : int
		{
			Start = 1,
			AudioEnable = 2,
			AudioDisable = 3
		}

		private void RequestStreams(object obj)
		{
			MyLog.Default.WriteLine("Plugin: RequestStreams - Got Request");
			if(obj is MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>)
			{
				MyLog.Default.WriteLine("Plugin: RequestStreams - Registered");
				var items = (MyTuple<Action<byte[], int>, Action<byte[], int>, Action<int>>)obj;
				SendAudio = items.Item1;
				SendVideo = items.Item2;
				Control = items.Item3;
				Control(1);
				Control((int)(audioMute ? ControlFlags.AudioDisable : ControlFlags.AudioEnable));
			}
		}
		
	}
}
