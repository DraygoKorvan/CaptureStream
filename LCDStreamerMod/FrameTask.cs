using CaptureStream;
using LCDText2;
using ParallelTasks;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace LCDStreamerMod
{
	public class FrameTask 
	{

		public bool isComplete = false;

		public bool quit = false;
		public bool draining = false;

		iSEVideoCodec[] videoCodecs = new iSEVideoCodec[3];
		iSEVideoCodec currentCodec;

		public bool IsKeyFrame
		{
			get
			{
				return isKeyFrame;
			}
		}
		public int Height
		{
			get
			{
				return height;
			}
		}
		public int Stride
		{
			get
			{
				return stride;
			}
		}

		public FrameTask()
		{
			videoCodecs[0] = new M0424VideoEncoder();
			videoCodecs[1] = new D8x8VideoCodec();
			videoCodecs[2] = new D4x4VideoCodec();
		}


		private void Complete()
		{
			
		}


		public void Queue()
		{

		}

		bool isKeyFrame = false;
		int offset;
		int encodedlength;
		ushort stride;
		ushort height;
		ushort width;
		byte[] source;
		byte[] destination;
		byte[] lastdecoded;
		byte[] lastKeyframe;
		int destinationoffset;
		int returnint;
		ParallelTasks.Task task;
		Action<FrameTask, int> completionCallback;

		internal void Prepare(byte[] source, int offset, int encodedlength, ushort stride, ushort height, ushort width, byte[] destination, int destinationoffset, int returnint, FrameControlFlags flags, Action<FrameTask, int> taskComplete)
		{
			this.isComplete = false;
			this.source = source;
			this.offset = offset;
			this.encodedlength = encodedlength;
			this.stride = stride;
			this.height = height;
			this.width = width;
			this.destination = destination;
			this.destinationoffset = destinationoffset;
			this.returnint = returnint;
			if (flags.HasFlag(videoCodecs[0].EncodingFlag))
			{
				currentCodec = videoCodecs[0];
			}
			if (flags.HasFlag(videoCodecs[1].EncodingFlag))
			{
				currentCodec = videoCodecs[1];
			}
			if (flags.HasFlag(videoCodecs[2].EncodingFlag))
			{
				currentCodec = videoCodecs[2];
			}
			completionCallback = taskComplete;
			if (flags.HasFlag(FrameControlFlags.IsKeyFrame))
			{
				isKeyFrame = true;
			}
			else
			{
				isKeyFrame = false;
			}

			
		}

		public void StartBackground(byte[] keyFrame = null)
		{
			lastKeyframe = keyFrame;
			task = MyAPIGateway.Parallel.StartBackground(DoWork);
		}

		private void onComplete()
		{
			//MyLog.Default.WriteLine("onComplete");
			completionCallback(this, returnint);
		}

		public void DoWork()
		{
			//MyLog.Default.WriteLine("Decoding");
			try
			{
				lastdecoded = currentCodec.Decode(source, offset, lastKeyframe, stride, width, height);
				Buffer.BlockCopy(lastdecoded, 0, destination, destinationoffset, lastdecoded.Length);//write
				this.isComplete = true;
			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLine(ex.ToString());
			}

			//MyLog.Default.WriteLine("Complete");
			MyAPIGateway.Utilities.InvokeOnGameThread(onComplete);
			
		}
	}
}
