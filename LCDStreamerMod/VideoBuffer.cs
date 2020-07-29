using LocalLCD;
using System;
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

		int audioposition = 0;
		int videoposition = 0;
		int writeaudioposition = -1;
		int writevideoposition = -1;
		int audiosize = -1;
		int videosize = -1;

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

		internal void AddToAudioBuffer(byte[] obj,  int length)
		{

			int control = BitConverter.ToInt32(obj, 0);
			int bytes = BitConverter.ToInt32(obj, sizeof(int));
			int SampleRate = BitConverter.ToInt32(obj, sizeof(int) * 2);
			int AverageBytesPerSecond = BitConverter.ToInt32(obj, sizeof(int) * 3);
			int offset = sizeof(int) * 4;
			length -= sizeof(int) * 4;
			MyLog.Default.WriteLine($"Add Data to Audio Buffer {offset} {bytes} {length} {AverageBytesPerSecond}");
			if(averageBytesPerSecondAudio != AverageBytesPerSecond)
			{
				sampleRate = SampleRate;
				InitializeAudioBuffer(AverageBytesPerSecond);
			}
			if (bytes <= length)
			{
				if(control == 1)
				{
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
				if (aptr[writeaudioposition] + length < AverageBytesPerSecond)
				{
					//MyLog.Default.WriteLine($"Single Write, do not advance");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], length);
					aptr[writeaudioposition] += length;
					//MyLog.Default.WriteLine($"new aptr {aptr[writeaudioposition]}");
				}
				else
				{
					//MyLog.Default.WriteLine($"Double Write, advance!");
					int remainder = AverageBytesPerSecond - aptr[writeaudioposition];
					
					length -= remainder;
					//MyLog.Default.WriteLine($"Write remainder {remainder} remaining length {length}");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], remainder);
					aptr[writeaudioposition] = AverageBytesPerSecond;
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
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], length);
					aptr[writeaudioposition] += length;
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
						Buffer.BlockCopy(oldstorage[i], 0, audiostorage[i], 0, aptr[i]);
				}
			}


		}

		internal void AddToVideoBuffer(byte[] obj, int length)
		{
			int offset = 0;

			if (length < sizeof(int) * 2 + sizeof(ushort) * 4) 
				return;
			int control = BitConverter.ToInt32(obj, offset);
			ushort stride = BitConverter.ToUInt16(obj, offset + sizeof(int));
			ushort height = BitConverter.ToUInt16(obj, offset + sizeof(int) + sizeof(ushort));
			ushort width = BitConverter.ToUInt16(obj, offset + sizeof(int) + sizeof(ushort) * 2);
			ushort framerate = BitConverter.ToUInt16(obj, offset + sizeof(int) + sizeof(ushort) * 3);
			int framesize = BitConverter.ToInt32(obj, offset + sizeof(int) + sizeof(ushort) * 4);
			MyLog.Default.WriteLine($"Packet Header: c {control} w {width} s {stride} h {height} f {framerate} fs {framesize}");


			if(stride * height * framerate > videoStride * videoHeight * videoFramerate)
			{
				videoFrameSize = framesize;
				videoStride = stride;
				videoHeight = height;
				videoFramerate = framerate;
				InitializeVideoBuffer();//remap
			}

			
			int writebytes = framesize + sizeof(int) * 2 + sizeof(ushort) * 4;
			
			MyLog.Default.WriteLine($"Writing to Video buffer? {writebytes} {offset} {length}");
			if (writebytes <= length)
			{
				if(control == 1)
				{
					videosize++;
					if (videosize >= 9)
					{
						videosize--;
					}
					else
					{
						writevideoposition += 1;
					}
					writevideoposition %= 10;
					vptr[writevideoposition] = 0;
					if (paused && audiosize >= 2 && videosize >= 2)
						paused = false;
				}
				if (writevideoposition == -1 || vptr[writevideoposition] + writebytes > videostorage[writevideoposition].Length)
					return;//discard
				//MyLog.Default.WriteLine($"Read {offset} {writebytes}  {obj.Length}");
				MyLog.Default.WriteLine($"Write {vptr[writevideoposition]} {writebytes} {videostorage[writevideoposition].Length}");
				Buffer.BlockCopy(obj, offset, videostorage[writevideoposition], vptr[writevideoposition], writebytes);
				vptr[writevideoposition] += writebytes;

			}
		}

		private void InitializeVideoBuffer()
		{

			var oldstorage = videostorage;
			for (int i = 0; i < 10; i++)
			{
				videostorage[i] = new byte[videoFramerate * (videoStride* videoHeight + sizeof(int) * sizeof(ushort) * 4)];
				if(oldstorage[i] != null)
					Buffer.BlockCopy(oldstorage[i], 0, videostorage[i], 0, vptr[i]);
			}

		}
		internal bool GetNextSecond(out byte[] audiobuffer, out int audiolen,  out byte[] videobuffer, out int videolen, out int sampleRate)
		{
			sampleRate = this.sampleRate;
			MyLog.Default.WriteLine($"VideoBuffer GetNextSecond audiosize {audiosize} videosize {videosize}");
			if (paused || audiosize <= 1 || videosize <= 1)
			{
				if (!paused && !closing)
					paused = true;
				audiobuffer = null;
				videobuffer = null;
				audiolen = 0;
				videolen = 0;
				return false;//wait
			}
			audiobuffer = audiostorage[audioposition];
			audiolen = aptr[audioposition];
			aptr[audioposition] = 0;
			audioposition = (audioposition + 1) % 10;
			audiosize--;
	
			videobuffer = videostorage[videoposition];
			videolen = vptr[videoposition];
			vptr[videoposition] = 0;
			videoposition = (videoposition + 1) % 10;
			videosize--;
			if (audiosize <= 1 || videosize <= 1)
				paused = true;
			return true;
		}
	}
}