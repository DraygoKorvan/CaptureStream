using CaptureStream;
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
			RecorderApplicationthread.Start();

		}

		public static void StartApplication()
		{

			Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new CaptureStreamForm());
		}
		bool registerevents = false;
		byte[] transferabuffer = new byte[4722];
		byte[] transfervbuffer = new byte[300000];
		public void Update()
		{
			if(MyAPIGateway.Session != null)
			{
				if(!registerevents)
				{
					MyAPIUtilities.Static.RegisterMessageHandler(videostreamcommand, RequestStreams);
					//MyAPIGateway.Session.OnSessionReady += Session_OnSessionReady;
					registerevents = true;
				}
			}
			else
			{
				MyAPIUtilities.Static.UnregisterMessageHandler(videostreamcommand, RequestStreams);
				registerevents = false;
			}
			if(Audio == null || Video == null)
			{
				Audio = new AnonymousPipeClientStream(PipeDirection.Out, CaptureStreamForm.AudioStream.GetClientHandleAsString());
				Video = new AnonymousPipeClientStream(PipeDirection.Out, CaptureStreamForm.VideoStream.GetClientHandleAsString());
				return;
			}
			if (Audio.CanRead)
			{
				if (!haveaudioheader)
				{
					Audio.Read(transferabuffer, 0, audioheader.Length());
					streamaudioheader = audioheader.getFromBytes(transferabuffer);
					SendAudio?.Invoke(transferabuffer, audioheader.Length());
				
				}
				else
				{
					Audio.Read(transferabuffer, 0, sizeof(int));
					int bytes = BitConverter.ToInt32(transferabuffer, 0);
					if(bytes == 0)
					{
						SendAudio?.Invoke(transferabuffer, sizeof(int));
						haveaudioheader = false;//reset
						return;
					}
					if(bytes + sizeof(int) > transferabuffer.Length)
					{
						var oldbuf = transferabuffer;
						transferabuffer = new byte[bytes + sizeof(int)];//grow automatically. 
						Buffer.BlockCopy(oldbuf, 0, transferabuffer, 0, 4);//move size header to new array. 
					}
					Audio.Read(transferabuffer, sizeof(int), bytes);
					SendAudio?.Invoke(transferabuffer, bytes + sizeof(int));
				}

			}
			if (Video.CanRead)
			{
				if (!havevideoheader)
				{
					var read = Video.Read(transfervbuffer, 0, videoheader.Length());
					streamvideoheader = videoheader.getFromBytes(transfervbuffer);
					SendVideo?.Invoke(transfervbuffer, videoheader.Length());
				}
				else
				{
					var read = Video.Read(transfervbuffer, 0, sizeof(int) + sizeof(ushort) * 2);
					int control = BitConverter.ToInt32(transfervbuffer, 0);
					ushort stride = BitConverter.ToUInt16(transfervbuffer, sizeof(int));
					ushort height = BitConverter.ToUInt16(transfervbuffer, sizeof(ushort) + sizeof(int));
					if (control == 1)
					{
						SendVideo?.Invoke(transfervbuffer, sizeof(int) + sizeof(ushort) * 2);
						havevideoheader = false;
						return;

					}
					int bytes = stride * height;
					if(bytes + sizeof(int) + sizeof(ushort) * 2 > transfervbuffer.Length)
					{
						var oldbuffer = transfervbuffer;
						transfervbuffer = new byte[bytes + sizeof(int) + sizeof(ushort) * 2];
						Buffer.BlockCopy(oldbuffer, 0, transfervbuffer, 0, sizeof(int) + sizeof(ushort) * 2);
					}
					var rest = Video.Read(transfervbuffer, sizeof(int) + sizeof(ushort) * 2, bytes);
					SendVideo?.Invoke(transfervbuffer, bytes + sizeof(int) + sizeof(ushort) * 2);
				}
			}
		
		}

		readonly long videostreamcommand = 20982309832901;
		//readonly long videostreamid = 20982309832902;

		private Action<byte[], int> SendAudio;
		private Action<byte[], int> SendVideo;
		private Action<int> Control;
		
		private void RequestStreams(object obj)
		{
			if(obj is MyTuple<Action<byte[]>, Action<byte[]>, Action<int>>)
			{
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
