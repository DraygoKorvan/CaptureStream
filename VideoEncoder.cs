using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CaptureStream
{


	public class VideoEncoder
	{
		private List<FrameWork> workList = new List<FrameWork>();
		public delegate void VideoEncoderDataAvailableHandler(object sender, VideoEventArgs e);
		public event VideoEncoderDataAvailableHandler Data_Available;
		RecordingParameters options;

		Thread encodingThread;
		bool running = true;
		public void Start(RecordingParameters control)
		{
			options = control;
			encodingThread = new Thread(EncodeUpdate);
			encodingThread.IsBackground = true;
			encodingThread.Start();
			running = true;
		}
		List<Task<FrameWork>> RunningTasks = new List<Task<FrameWork>>();

		byte[] keyFrame;
		int keyFrameLn;

		private void RaiseDataAvailable(FrameWork framedata)
		{
			var args = new VideoEventArgs(framedata);
			Data_Available?.Invoke(this, args);
		}
		public delegate void AddWorkHandler(FrameWork work);
		public void AddJob(FrameWork work)
		{
			AddWork(work);
			//var job = new AddWorkHandler(AddWork);
			//if(running)
			//workDispatcher.BeginInvoke(DispatcherPriority.Normal, job, work);
		}

		private void AddWork(FrameWork work)
		{
			if(running)
			{
				lock(workList)
				{
					workList.Add(work);
				}

			}

		}

		private void EncodeUpdate()
		{
			//workDispatcher = Dispatcher.CurrentDispatcher;
			while(running)
			{
				Thread.Sleep(0);
				if(RunningTasks.Count > 0)
				{
					var first = RunningTasks.First();
					if(first.IsCompleted)
					{
						RunningTasks.RemoveAt(0);
						var job = first.Result;
						RaiseDataAvailable(job);
					}
				}

				while(workList.Count > 0)
				{
					FrameWork work;
					lock(workList)
					{
						work = workList.First();
						workList.RemoveAt(0);
					}
	

					if(work.isKeyFrame)
					{
						keyFrame = work.uncompressedFrame;
						keyFrameLn = work.imageln;
						RunningTasks.Add(Task.Run(work.DoEncode));
						
					}
					else
					{
						work.keyFrame = keyFrame;
						work.keyFrameln = keyFrameLn;
						RunningTasks.Add(Task.Run(work.DoEncode));

					}
				}
			}
			workList.Clear();
		}

		internal void Stop()
		{
			running = false;
		}
	}
}
