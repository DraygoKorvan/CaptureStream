﻿using CaptureStream;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		audioheader streamaudioheader;
		videoheader streamvideoheader;
		bool haveaudioheader = false;
		bool havevideoheader = false;
		Thread RecorderApplicationthread;
		Thread CommunicationThreadVideo, CommunicationThreadAudio;
		object instance;
		Thread mainthread;
		static CaptureStreamForm form;
		public void Dispose()
		{
			
		}

		public void Init(object gameInstance)
		{
			mutex = new Mutex(true, appName, out bool createdNew);

			if (!createdNew)
			{
				return;
			}
			RecorderApplicationthread = new Thread(StartApplication);
			RecorderApplicationthread.IsBackground = true;
			RecorderApplicationthread.Start();
			CommunicationThreadVideo = new Thread(ModCommunicationVideo);
			CommunicationThreadVideo.IsBackground = true;
			CommunicationThreadVideo.Start();
			CommunicationThreadAudio = new Thread(ModCommunicationAudio);
			CommunicationThreadAudio.IsBackground = true;
			CommunicationThreadAudio.Start();
			process = Process.GetCurrentProcess();
			mainthread = Thread.CurrentThread;
			//MyLog.Default.WriteLine(gameInstance.ToString());
		}



		public static void StartApplication()
		{

			Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			form = new CaptureStreamForm();
			Application.Run(form);
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
					//MyAPIGateway.Session.OnSessionReady += Session_OnSessionReady;
					registerevents = true;
				}
			}
			else
			{
				SendAudio = null;
				SendVideo = null;
				Control = null;
				MyAPIUtilities.Static.UnregisterMessageHandler(videostreamcommand, RequestStreams);
				registerevents = false;
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

					
						continue;
					}
					if (Audio.CanRead)
					{
						if (!haveaudioheader)
						{

							Audio.Read(transferabuffer, 0, audioheader.Length());
							streamaudioheader = audioheader.getFromBytes(transferabuffer);
							MyLog.Default.WriteLine("Plugin: ModCommunication - Got Audio Header");
							MyLog.Default.WriteLine($"{streamaudioheader.SampleRate}");
							MyLog.Default.WriteLine($"{streamaudioheader.Channels}");
							MyLog.Default.WriteLine($"{streamaudioheader.BitsPerSample}");
							MyLog.Default.WriteLine($"{streamaudioheader.AverageBytesPerSecond}");
							haveaudioheader = true;
							MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Audio header" + audioheader.Length().ToString());
							SendAudio?.Invoke(transferabuffer, audioheader.Length());

						}
						else
						{
							Audio.Read(transferabuffer, 0, sizeof(int));
							int bytes = BitConverter.ToInt32(transferabuffer, 0);
							if (bytes == 0)
							{
								//MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Audio EOS ");
								SendAudio?.Invoke(transferabuffer, sizeof(int));
								haveaudioheader = false;//reset
														//havevideoheader = false;

								continue;
							}
							if (bytes + sizeof(int) > transferabuffer.Length)
							{
								var oldbuf = transferabuffer;
								transferabuffer = new byte[bytes + sizeof(int)];//grow automatically. 
								Buffer.BlockCopy(oldbuf, 0, transferabuffer, 0, 4);//move size header to new array. 
							}

							Audio.Read(transferabuffer, sizeof(int), bytes);
							//MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Audio - " + (bytes + sizeof(int)).ToString());
							SendAudio?.Invoke(transferabuffer, bytes + sizeof(int));
						}

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
						continue;
					}
					if (Video.CanRead)
					{
						if (!havevideoheader)
						{
							var read = Video.Read(transfervbuffer, 0, videoheader.Length());
							havevideoheader = true;
							streamvideoheader = videoheader.getFromBytes(transfervbuffer);
							//MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Video header " + (videoheader.Length()).ToString());
							SendVideo?.Invoke(transfervbuffer, videoheader.Length());
							
						}
						else
						{
							var read = Video.Read(transfervbuffer, 0, sizeof(int) + sizeof(ushort) * 2);
							while(read < sizeof(int) + sizeof(ushort) * 2)
							{
								read += Video.Read(transfervbuffer, read, sizeof(int) + sizeof(ushort) * 2 - read);
								if (process.HasExited)
									break;
							}
							int control = BitConverter.ToInt32(transfervbuffer, 0);

							ushort stride = BitConverter.ToUInt16(transfervbuffer, sizeof(int));
							ushort height = BitConverter.ToUInt16(transfervbuffer, sizeof(ushort) + sizeof(int));

							//MyLog.Default.WriteLine($"Video Packet Header: c {control} s {stride} h {height}");
							if (control == 1)
							{
								//MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Video header EOS " + (sizeof(int) + sizeof(ushort) * 2).ToString());
								SendVideo?.Invoke(transfervbuffer, sizeof(int) + sizeof(ushort) * 2);
								havevideoheader = false;
								haveaudioheader = false;
								continue;

							}
							int bytes = stride * height;
							if (bytes + sizeof(int) + sizeof(ushort) * 2 > transfervbuffer.Length)
							{
								var oldbuffer = transfervbuffer;
								transfervbuffer = new byte[bytes + sizeof(int) + sizeof(ushort) * 2];
								Buffer.BlockCopy(oldbuffer, 0, transfervbuffer, 0, sizeof(int) + sizeof(ushort) * 2);
							}
							int rest = 0;
							do
							{
								rest += Video.Read(transfervbuffer, rest + sizeof(int) + sizeof(ushort) * 2, bytes - rest);
								if (process.HasExited)
									break;
							}
							while (rest < bytes);
							//MyLog.Default.WriteLine("Plugin: ModCommunication - Sending Video - " + (rest + sizeof(int) + sizeof(ushort) * 2).ToString());
							SendVideo?.Invoke(transfervbuffer, bytes + sizeof(int) + sizeof(ushort) * 2);
						}
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
				if(haveaudioheader)
					SendAudio(streamaudioheader.getBytes(), audioheader.Length());
				if(havevideoheader)
					SendVideo(streamvideoheader.getBytes(), videoheader.Length());
			}
		}
		
	}
}
