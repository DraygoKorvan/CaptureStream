using LocalLCD;
using System;
using VRage.Utils;

namespace LCDText2
{
	public class VideoBuffer
	{
		public ulong steamid;
		public long nextFrame = 0;

		public struct AudioHeader
		{
			public int SampleRate;
			public int Channels;
			public int BitsPerSample;
			public int AverageBytesPerSecond;

			public byte[] GetBytes()
			{
				var retval = new byte[sizeof(int) * 4];
				BitConverter.GetBytes(SampleRate).CopyTo(retval, 0);
				BitConverter.GetBytes(Channels).CopyTo(retval, sizeof(int));
				BitConverter.GetBytes(BitsPerSample).CopyTo(retval, sizeof(int) * 2);
				BitConverter.GetBytes(AverageBytesPerSecond).CopyTo(retval, sizeof(int) * 3);
				return retval;

			}

			public static AudioHeader GetFromBytes(byte[] bytes, int offset)
			{
				var header = new AudioHeader();
				MyLog.Default.WriteLine("AudioHeader:");
				header.SampleRate = BitConverter.ToInt32(bytes, 0 + offset);
				MyLog.Default.WriteLine($"SampleRate: {header.SampleRate}");
				header.Channels = BitConverter.ToInt32(bytes, sizeof(int) + offset);
				MyLog.Default.WriteLine($"Channels: {header.Channels}");
				header.BitsPerSample = BitConverter.ToInt32(bytes, sizeof(int) * 2 + offset);
				MyLog.Default.WriteLine($"BitsPerSample: {header.BitsPerSample}");
				header.AverageBytesPerSecond = BitConverter.ToInt32(bytes, sizeof(int) * 3 + offset);
				MyLog.Default.WriteLine($"AverageBytesPerSecond: {header.AverageBytesPerSecond}");
				return header;
			}

			public static int Length()
			{
				return sizeof(int) * 4;
			}
		}

		public struct VideoHeader
		{
			public int width;
			public int height;
			public int stride;
			public int framerate;

			public byte[] GetBytes()
			{
				var retval = new byte[sizeof(int) * 4];
				BitConverter.GetBytes(width).CopyTo(retval, 0);
				BitConverter.GetBytes(height).CopyTo(retval, sizeof(int));
				BitConverter.GetBytes(stride).CopyTo(retval, sizeof(int) * 2);
				BitConverter.GetBytes(framerate).CopyTo(retval, sizeof(int) * 3);
				return retval;
			}

			public static VideoHeader GetFromBytes(byte[] bytes, int offset)
			{
				var header = new VideoHeader();
				MyLog.Default.WriteLine("VideoHeader:");
				header.width = BitConverter.ToInt32(bytes, 0 + offset);
				MyLog.Default.WriteLine("width " + header.width.ToString());
				header.height = BitConverter.ToInt32(bytes, sizeof(int) + offset);
				MyLog.Default.WriteLine("height " + header.height.ToString());
				header.stride = BitConverter.ToInt32(bytes, sizeof(int) * 2 + offset);
				MyLog.Default.WriteLine("stride " + header.stride.ToString());
				header.framerate = BitConverter.ToInt32(bytes, sizeof(int) * 3 + offset);
				MyLog.Default.WriteLine("framerate " + header.framerate.ToString());
				return header;
			}

			public static int Length()
			{
				return sizeof(int) * 4;
			}

		}

		byte[][] videostorage = new byte[10][];
		byte[][] audiostorage = new byte[10][];
		int[] vptr = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		int[] aptr = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		int audioposition = 0;
		int videoposition = 0;
		int writeaudioposition = 0;
		int writevideoposition = 0;
		int audiosize = 0;
		int videosize = 0;

		bool closing = false;
		bool paused = true;

		public bool hasaudioheader = false;
		public bool hasvideoheader = false;

		public AudioHeader audioHeader;
		public VideoHeader videoHeader;

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
			int offset = 0;
			if(!hasaudioheader)
			{
				
				audioHeader = AudioHeader.GetFromBytes(obj, 0);
				
				offset += AudioHeader.Length();
				length -= AudioHeader.Length();
				InitializeAudioBuffer();
			}
			if (length <= 0)
				return;
			
			int bytes = BitConverter.ToInt32(obj, offset);
			offset += sizeof(int);
			length -= sizeof(int);
			//MyLog.Default.WriteLine($"Add Data to Audio Buffer {offset} {bytes} {length}");
			if (bytes <= length)
			{

				//MyLog.Default.WriteLine($"Writing to {writeaudioposition} at {aptr[writeaudioposition]} row length {audioHeader.AverageBytesPerSecond} ");
				if (aptr[writeaudioposition] + length < audioHeader.AverageBytesPerSecond)
				{
					//MyLog.Default.WriteLine($"Single Write, do not advance");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], length);
					aptr[writeaudioposition] += length;
					//MyLog.Default.WriteLine($"new aptr {aptr[writeaudioposition]}");
				}
				else
				{
					//MyLog.Default.WriteLine($"Double Write, advance!");
					int remainder = audioHeader.AverageBytesPerSecond - aptr[writeaudioposition];
					
					length -= remainder;
					//MyLog.Default.WriteLine($"Write remainder {remainder} remaining length {length}");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], remainder);
					aptr[writeaudioposition] = audioHeader.AverageBytesPerSecond;
					audiosize = audiosize + 1;
					
					if (audiosize >= 9)
					{
						audiosize--;

					}
					else
					{
						writeaudioposition += 1;
					}
					writeaudioposition %= 10;
					if (paused && audiosize >= 3 && videosize >= 3)
						paused = false;

					aptr[writeaudioposition] = 0;
					//MyLog.Default.WriteLine($"Write {length} at {writeaudioposition} aptr {aptr[writeaudioposition]}");
					Buffer.BlockCopy(obj, offset, audiostorage[writeaudioposition], aptr[writeaudioposition], length);
					aptr[writeaudioposition] += length;
					//MyLog.Default.WriteLine($"new aptr {aptr[writeaudioposition]}");
				}
			}
		}

		private void InitializeAudioBuffer()
		{
			if(hasaudioheader)
			{
				var oldstorage = audiostorage;
				for(int i = 0; i < 10; i++)
				{
					audiostorage[i] = new byte[audioHeader.AverageBytesPerSecond];
					Buffer.BlockCopy(oldstorage[i], 0, audiostorage[i], 0, aptr[i]);
				}
			}
			else
			{
				hasaudioheader = true;
				for (int i = 0; i < 10; i++)
				{
					audiostorage[i] = new byte[audioHeader.AverageBytesPerSecond];
				}
			}
		}

		internal void AddToVideoBuffer(byte[] obj, int length)
		{
			int offset = 0;
			if (!hasvideoheader)
			{
				videoHeader = VideoHeader.GetFromBytes(obj, 0);

				offset += VideoHeader.Length();
				length -= VideoHeader.Length();
				closing = false;
				InitializeVideoBuffer();
			}
			if (length <= 0)
				return;

			int control = BitConverter.ToInt32(obj, offset);
			ushort stride = BitConverter.ToUInt16(obj, offset + sizeof(int));
			ushort height = BitConverter.ToUInt16(obj, offset + sizeof(int) + sizeof(ushort));
			//MyLog.Default.WriteLine($"Packet Header: c {control} s {stride} h {height}");
			if (control == 1)
			{
				closing = true;
				hasvideoheader = false;
				hasaudioheader = false;
				return;
			}

			if(stride * height > videoHeader.stride * videoHeader.height)
			{
				videoHeader.stride = stride;
				videoHeader.height = height;
				InitializeVideoBuffer();//remap
			}

			
			int writebytes = stride * height + sizeof(int) + sizeof(ushort) * 2;
			
			//MyLog.Default.WriteLine($"Writing to Video buffer? {writebytes} {offset} {length}");
			if (writebytes <= length)
			{
				nextFrame++;
				//MyLog.Default.WriteLine($"Read {offset} {writebytes}  {obj.Length}");
				//MyLog.Default.WriteLine($"Write {vptr[writevideoposition]} {writebytes}  {videostorage[writevideoposition].Length}");
				Buffer.BlockCopy(obj, offset, videostorage[writevideoposition], vptr[writevideoposition], writebytes);
				vptr[writevideoposition] += writebytes;
				if (nextFrame == videoHeader.framerate)
				{
					nextFrame = 0;
					videosize += 1;

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
					if (paused && audiosize >= 3 && videosize >= 3)
						paused = false;
				}
			}
		}

		private void InitializeVideoBuffer()
		{
			if (hasvideoheader)
			{
				var oldstorage = videostorage;
				for (int i = 0; i < 10; i++)
				{
					videostorage[i] = new byte[videoHeader.framerate * (videoHeader.stride * videoHeader.width + sizeof(int) * sizeof(ushort) * 2)];
					Buffer.BlockCopy(oldstorage[i], 0, videostorage[i], 0, vptr[i]);
				}
			}
			else
			{
				hasvideoheader = true;
				for (int i = 0; i < 10; i++)
				{
					videostorage[i] = new byte[videoHeader.framerate * (videoHeader.stride * videoHeader.width + sizeof(int) + sizeof(ushort) * 2)];
				}
			}
		}
		internal bool GetNextSecond(out byte[] audiobuffer, out int audiolen,  out byte[] videobuffer, out int videolen)
		{
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