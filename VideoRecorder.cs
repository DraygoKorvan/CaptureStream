using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CaptureStream
{
	public class VideoEventArgs
	{
		public FrameWork payload;

		public VideoEventArgs(FrameWork frame)
		{
			payload = frame;

		}
	}
	
	public class VideoStoppedArgs
	{

	}
	public class VideoRecorder
	{
		public delegate void VideoDataAvailableHandler(object sender, VideoEventArgs e);
		public delegate void RecordingStoppedHandler(object sender, VideoStoppedArgs e);
		public event VideoDataAvailableHandler Data_Available;
		public event RecordingStoppedHandler Recording_Stopped;
		Thread thread;
		Stopwatch timer = new Stopwatch();
		long frequency = Stopwatch.Frequency;
		long frametime;
		int frames = 0;
		int maxframes = 20;
		long nextFrame, nextFrameSecond;
		RecordingParameters RecordingParemeters;

		private void RaiseDataAvailable(FrameWork framedata)
		{
			var args = new VideoEventArgs(framedata);
			Data_Available?.Invoke(this, args);
		}

		private void RaiseStoppedRecording()
		{
			Recording_Stopped?.Invoke(this, new VideoStoppedArgs());
		}

		public void Start(RecordingParameters control)
		{

			this.RecordingParemeters = control;
			frametime = Stopwatch.Frequency / control.FrameRate;
			//timer.Start();
			maxframes = control.FrameRate;
			thread = new Thread(Update);
			thread.IsBackground = true;
			thread.Start();
		}

		public void Stop()
		{
			RecordingParemeters.running = false;
		}
		Stack<FrameWork> workstack = new Stack<FrameWork>();

		public void ReturnWork(FrameWork obj)
		{
			InternalGetWork(obj);
		}

		private void InternalGetWork(FrameWork obj)
		{
			lock(workstack)
			{
				workstack.Push(obj);
			}
	
		}
		int compression = 0;
		RecordingParameters currentFrameParams;
		private void Update()
		{
			timer.Start();
			nextFrame = timer.ElapsedTicks;
			nextFrameSecond = timer.ElapsedTicks;
			var lastFrametime = timer.ElapsedMilliseconds;
			var Tasks = new List<Task<FrameWork>>();

			while (true)
			{
				Thread.Sleep(0);
				if(Tasks.Count > 0)
				{
					var first = Tasks.First();
					if(first.IsCompleted)
					{
						var work = first.Result;
						RaiseDataAvailable(work);
						RecordingParemeters.RecordingMs = work.framems;
						//workstack.Push(work);
						Tasks.RemoveAt(0);
					}
					continue;
				}
				if (!RecordingParemeters.running)
				{
					break;
				}
				bool needframe = false;
				bool keyFrame = false;

				if (frames < maxframes)
				{
					if(nextFrame > timer.ElapsedTicks)
					{
						continue;
					}
					frames++;
					nextFrame += frametime;
					needframe = RecordingParemeters.running;
				}
				if (frames > maxframes || nextFrameSecond <= timer.ElapsedTicks)
				{
					needframe = RecordingParemeters.running;
					keyFrame = true;
					currentFrameParams = new RecordingParameters(RecordingParemeters);//only change on keyframe. 
					compression = RecordingParemeters.compressionRate;
					frames = 0;
					nextFrame = timer.ElapsedTicks + frametime;
					nextFrameSecond += frequency;
				}
				if (!needframe)
				{
					continue;
				}
				FrameWork p;
				if (workstack.Count > 0)
				{
					lock(workstack)
					{
						p = workstack.Pop();
					}

				}
				else
				{
					p = new FrameWork();
				}
				p.Prepare(currentFrameParams, keyFrame, compression);
				p.GetScreenshot();
				var task = Task.Run(p.DoWork);
				Tasks.Add(task);
				
			}
			timer.Stop();
			RaiseStoppedRecording();
		}

	}
}