using CaptureStream;
using LCDStreamerMod;
using LocalLCD;
using System;
using System.Collections.Generic;
using VRage.Utils;

namespace LCDText2
{
	public class VideoBuffer
	{
		public ulong steamid;
		public long nextFrame = 0;

		

		byte[][] videostorage = new byte[10][];
		byte[][] audiostorage = new byte[10][];
		int[] vptr = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		int[] aptr = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		int[] taskQueue = new int[10];
		int[] completeQueue = new int[10];
		int[] framecounter = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		int[] maxframes = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		bool[] hasKeyFrame = new bool[10] { false, false, false, false, false, false, false, false, false, false };
		byte[][] keyFrame = new byte[10][] { new byte[0], new byte[0], new byte[0], new byte[0], new byte[0], new byte[0], new byte[0], new byte[0], new byte[0], new byte[0]};

		Stack<FrameTask> waitingOnKeyframe = new Stack<FrameTask>();

		int audioposition = 0;
		int videoposition = 0;
		int writeaudioposition = -1;
		int writevideoposition = -1;
		int audiosize = -1;
		int videosize = -1;
		int decodevideosize = 0;

		bool closing = false;
		bool paused = true;

		public bool hasaudioheader = false;
		public bool hasvideoheader = false;

		private int averageBytesPerSecondAudio = 0;
		public int sampleRate = 0;

		private int videoStride = 0;
		private int videoHeight = 0;
		private int videoFramerate = 0;
		private int videoFrameSize = 0;

		public Stack<FrameTask> taskPool = new Stack<FrameTask>();

		public bool Paused
		{
			get
			{
				return paused;
			}
			set
			{
				paused = value;

			}
		}

		public VideoBuffer(ulong steamid)
		{
			if (steamid == 0)
				steamid = 2;
			this.steamid = steamid;
			//MyLog.Default.WriteLine("VideoBuffer channel " + steamid.ToString());
			LCDWriterCore.instance.AddBuffer(this);
		}
		bool gotfirstaudiokeyframe = false;
		bool gotfirstvideokeyframe = false;
		internal void AddToAudioBuffer(byte[] obj,  int length)
		{

			int control = BitConverter.ToInt32(obj, 0);
			if(control != 1 && (!gotfirstvideokeyframe && !gotfirstaudiokeyframe))
			{
				//discard audio until we get a video keyframe to keep sync. 
				return;
			}
			int bytes = BitConverter.ToInt32(obj, sizeof(int));
			int SampleRate = BitConverter.ToInt32(obj, sizeof(int) * 2);
			int AverageBytesPerSecond = BitConverter.ToInt32(obj, sizeof(int) * 3);
			LCDWriterCore.debugMonitor.AudioBytes = bytes;
			LCDWriterCore.debugMonitor.AudioSampleRate = SampleRate;
			LCDWriterCore.debugMonitor.AudioAverageBytesPerSecond = AverageBytesPerSecond;
			int offset = sizeof(int) * 4;
			length -= sizeof(int) * 4;
			//MyLog.Default.WriteLine($"Add Data to Audio Buffer {offset} {bytes} {length} {AverageBytesPerSecond}");
			if(averageBytesPerSecondAudio != AverageBytesPerSecond)
			{
				sampleRate = SampleRate;
				InitializeAudioBuffer(AverageBytesPerSecond);
			}
			if (bytes <= length)
			{
				if(control == 1)
				{
					gotfirstaudiokeyframe = true;
					audiosize++;
					if (audiosize >= 9)
					{
						audiosize--;
					}
					else
					{
						writeaudioposition += 1;
					}
					writeaudioposition %= 10;
					if (paused && audiosize >= 2 && videosize >= 2)
						paused = false;
					aptr[writeaudioposition] = 0;
				}
				//MyLog.Default.WriteLine($"Writing to {writeaudioposition} at {aptr[writeaudioposition]} row length {audioHeader.AverageBytesPerSecond} ");
				if (aptr[writeaudioposition] + bytes <= AverageBytesPerSecond)
				{
					//MyLog.Default.WriteLine($"Single Write, do not advance");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], bytes);
					aptr[writeaudioposition] += bytes;
					//MyLog.Default.WriteLine($"new aptr {aptr[writeaudioposition]}");
				}
				else
				{
					//MyLog.Default.WriteLine($"Double Write, advance!");
					int remainder = AverageBytesPerSecond - aptr[writeaudioposition];

					bytes -= remainder;
					//MyLog.Default.WriteLine($"Write remainder {remainder} remaining length {length}");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], remainder);
					aptr[writeaudioposition] += remainder;
					audiosize++;
					
					if (audiosize >= 9)
					{
						audiosize--;

					}
					else
					{
						writeaudioposition += 1;
					}
					writeaudioposition %= 10;
					if (paused && audiosize >= 2 && videosize >= 2)
						paused = false;

					aptr[writeaudioposition] = 0;
					//MyLog.Default.WriteLine($"Write {length} at {writeaudioposition} aptr {aptr[writeaudioposition]}");
					Buffer.BlockCopy(obj, offset + remainder, audiostorage[writeaudioposition], aptr[writeaudioposition], bytes);
					aptr[writeaudioposition] += bytes;
					//MyLog.Default.WriteLine($"new aptr {aptr[writeaudioposition]}");
				}
			}
		}

		private void InitializeAudioBuffer(int averageBytesPerSecond)
		{
			if(averageBytesPerSecondAudio < averageBytesPerSecond)
			{
				averageBytesPerSecondAudio = averageBytesPerSecond;
				var oldstorage = audiostorage;
				for (int i = 0; i < 10; i++)
				{
					audiostorage[i] = new byte[averageBytesPerSecond];
					if(oldstorage[1] != null)
					{
						Buffer.BlockCopy(oldstorage[i], 0, audiostorage[i], 0, aptr[i]);
					}
						
				}
			}


		}
		const int VIDEOHEADERSIZE = sizeof(int) * 2 + sizeof(ushort) * 4;
		internal void AddToVideoBuffer(byte[] obj, int length)
		{
			//int offset = 0;
			
			if (length < VIDEOHEADERSIZE) 
				return;
			FrameControlFlags control = (FrameControlFlags)BitConverter.ToUInt32(obj, 0);
			ushort stride = BitConverter.ToUInt16(obj,  sizeof(int));
			ushort height = BitConverter.ToUInt16(obj,  sizeof(int) + sizeof(ushort));
			ushort width = BitConverter.ToUInt16(obj,  sizeof(int) + sizeof(ushort) * 2);
			ushort framerate = BitConverter.ToUInt16(obj,  sizeof(int) + sizeof(ushort) * 3);
			int framesize = BitConverter.ToInt32(obj,  sizeof(int) + sizeof(ushort) * 4);
			//MyLog.Default.WriteLine($"Packet Header: c {control} w {width} s {stride} h {height} f {framerate} fs {framesize} l {length}");


			if(stride * height * framerate > videoStride * videoHeight * videoFramerate)
			{
				videoFrameSize = framesize;
				videoStride = stride;
				videoHeight = height;
				videoFramerate = framerate;

				//TODO pause tasks

				InitializeVideoBuffer();//remap
			}

			
			int writebytes = stride * height + VIDEOHEADERSIZE - sizeof(int);
			
			//MyLog.Default.WriteLine($"Writing to Video buffer? {writebytes} {offset} {length}");
			if (framesize + VIDEOHEADERSIZE <= length)
			{
				if(control.HasFlag(FrameControlFlags.IsKeyFrame))
				{
					try
					{
						gotfirstvideokeyframe = true;
						videosize++;
						if (writevideoposition != -1)
						{
							if(maxframes[writevideoposition] != framecounter[writevideoposition])
							{
								maxframes[writevideoposition] = framecounter[writevideoposition];
								if (taskQueue[writevideoposition] == 0 && completeQueue[writevideoposition] == maxframes[writevideoposition])
								{
									decodevideosize++;
								}
							}

						}


						if (videosize >= 9)
						{
							decodevideosize--;
							videosize--;
						}
						else
						{
							writevideoposition += 1;
						}
						writevideoposition %= 10;
						maxframes[writevideoposition] = framerate;
						completeQueue[writevideoposition] = 0;
						framecounter[writevideoposition] = 0;
						hasKeyFrame[writevideoposition] = false;
						keyFrame[writevideoposition] = null;
						taskQueue[writevideoposition] = 0;
						vptr[writevideoposition] = 0;
						if (paused && audiosize >= 2 && videosize >= 2)
							paused = false;
					}
					catch (Exception ex)
					{
						MyLog.Default.WriteLine(ex.ToString());
					}

				}
				else if(!gotfirstvideokeyframe)
				{
					return;
				}
				if (writevideoposition == -1 || vptr[writevideoposition] + writebytes > videostorage[writevideoposition].Length)
					return;//discard
				framecounter[writevideoposition]++;
				FrameTask task;
				lock(taskPool)
				{
					if (taskPool.Count > 0)
					{
						task = taskPool.Pop();
					}
					else
					{
						task = new FrameTask();
					}
				}
				taskQueue[writevideoposition]++;
				//MyLog.Default.WriteLine($"Preparing Task {writevideoposition} {taskQueue[writevideoposition]} ");
				task.Prepare(obj, VIDEOHEADERSIZE, framesize, stride, height, width, videostorage[writevideoposition], vptr[writevideoposition] + VIDEOHEADERSIZE - sizeof(int), writevideoposition, control, TaskComplete);
				if (task.IsKeyFrame || hasKeyFrame[writevideoposition] )
				{
					task.StartBackground(keyFrame[writevideoposition]);
				}
				else
				{
					lock(waitingOnKeyframe)
					{
						waitingOnKeyframe.Push(task);
					}
				}

				//MyLog.Default.WriteLine($"Read {offset} {writebytes}  {obj.Length}");
				//MyLog.Default.WriteLine($"Write {vptr[writevideoposition]} {writebytes} {videostorage[writevideoposition].Length}");
				Buffer.BlockCopy(obj, 0, videostorage[writevideoposition], vptr[writevideoposition], VIDEOHEADERSIZE - sizeof(int));
				vptr[writevideoposition] += writebytes;

			}
		}

		public void TaskComplete(FrameTask task, int writePostion)
		{

			if (task.IsKeyFrame)
			{
				hasKeyFrame[writePostion] = true;
				if (keyFrame[writePostion] == null || keyFrame[writePostion].Length != task.Height * task.Stride)
				{
					keyFrame[writePostion] = new byte[task.Height * task.Stride];
				}
				Buffer.BlockCopy(videostorage[writePostion], VIDEOHEADERSIZE - sizeof(int), keyFrame[writePostion], 0, task.Height * task.Stride);
			//= task.getUnencryptedFrame();
			lock (waitingOnKeyframe)
			{
				while (waitingOnKeyframe.Count > 0)
				{
					waitingOnKeyframe.Pop().StartBackground(keyFrame[writePostion]);
				}
			}
			}

			lock(taskPool)
			{
				taskPool.Push(task);
			}
			
			completeQueue[writePostion]++;
			taskQueue[writePostion]--;
			//MyLog.Default.WriteLine($"Task complete {writePostion} {taskQueue[writePostion]} {completeQueue[writePostion]} {maxframes[writePostion]}");

			if(completeQueue[writePostion] == maxframes[writePostion])
			{
				decodevideosize++;
			}

		}

		private void InitializeVideoBuffer()
		{

			var oldstorage = videostorage;

			for (int i = 0; i < 10; i++)
			{
				videostorage[i] = new byte[videoFramerate * (videoStride * videoHeight + (VIDEOHEADERSIZE - sizeof(int)))];
				if (oldstorage != null && oldstorage[i] != null)
					Buffer.BlockCopy(oldstorage[i], 0, videostorage[i], 0, vptr[i]);
			}



		}
		internal bool GetNextSecond(out byte[] audiobuffer, out int audiolen,  out byte[] videobuffer, out int videolen, out int sampleRate)
		{
			sampleRate = this.sampleRate;
			//MyLog.Default.WriteLine($"VideoBuffer GetNextSecond audiosize {audiosize} videosize {videosize} decodedvideo {decodevideosize}");
			if (paused || audiosize <= 1 || decodevideosize <= 1)
			{
				if (!paused && !closing)
					paused = true;
				audiobuffer = null;
				videobuffer = null;
				audiolen = 0;
				videolen = 0;
				return false;//wait
			}
			audiolen = aptr[audioposition];
			audiobuffer = new byte[audiolen];
			Buffer.BlockCopy(audiostorage[audioposition], 0, audiobuffer, 0, audiolen);
			//audiobuffer = audiostorage[audioposition];
			
			aptr[audioposition] = 0;
			audioposition = (audioposition + 1) % 10;
			audiosize--;
	
			videobuffer = videostorage[videoposition];
			videolen = vptr[videoposition];
			vptr[videoposition] = 0;
			videoposition = (videoposition + 1) % 10;
			videosize--;
			decodevideosize--;
			LCDWriterCore.debugMonitor.VideoBufferSize = videosize;
			LCDWriterCore.debugMonitor.AudioBufferSize = audiosize;
			if (audiosize <= 1 || videosize <= 1)
				paused = true;
			return true;
		}
	}
}