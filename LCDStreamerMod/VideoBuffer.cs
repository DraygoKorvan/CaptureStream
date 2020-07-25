using LocalLCD;
using System;

namespace LCDText2
{
	public class VideoBuffer
	{
		public ulong steamid;
		public long nextFrame = 0;
		public VideoBuffer()
		{
			LCDWriterCore.instance.AddBuffer(this);
		}
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
				header.SampleRate = BitConverter.ToInt32(bytes, 0 + offset);
				header.Channels = BitConverter.ToInt32(bytes, sizeof(int) + offset);
				header.BitsPerSample = BitConverter.ToInt32(bytes, sizeof(int) * 2 + offset);
				header.AverageBytesPerSecond = BitConverter.ToInt32(bytes, sizeof(int) * 3 + offset);
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
				header.width = BitConverter.ToInt32(bytes, 0 + offset);
				header.height = BitConverter.ToInt32(bytes, sizeof(int) + offset);
				header.stride = BitConverter.ToInt32(bytes, sizeof(int) * 2 + offset);
				header.framerate = BitConverter.ToInt32(bytes, sizeof(int) * 3 + offset);
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
			this.steamid = steamid;
		}

		internal void AddToAudioBuffer(byte[] obj, int offset, int length)
		{
			
			if(!hasaudioheader)
			{
				
				audioHeader = AudioHeader.GetFromBytes(obj, offset);
				
				offset += AudioHeader.Length();
				length -= AudioHeader.Length();
				InitializeAudioBuffer();
			}
			if (length <= 0)
				return;
			int bytes = BitConverter.ToInt32(obj, offset);
			offset += sizeof(int);
			length -= sizeof(int);
			if(bytes + offset <= length)
			{
				int writepos = audioposition + audiosize % 10;
				if(aptr[writepos] + length < audioHeader.AverageBytesPerSecond)
				{
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], aptr[writepos], length);
					aptr[writepos] += length;
				}
				else
				{
					int remainder = audioHeader.AverageBytesPerSecond - aptr[writepos];
					
					length -= remainder;
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], aptr[writepos], remainder);
					aptr[writepos] = audioHeader.AverageBytesPerSecond;
					audiosize = audiosize + 1;
					
					if (audiosize >= 9)
					{
						audiosize--;
						if(videosize > 1)
						videosize--;
					}
					if (paused && audiosize >= 5 && videosize >= 5)
						paused = false;
					writepos = audioposition + audiosize % 10;
					aptr[writepos] = 0;
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], aptr[writepos], length);
					aptr[writepos] += length;
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

		internal void AddToVideoBuffer(byte[] obj, int offset, int length)
		{
			if (!hasvideoheader)
			{
				videoHeader = VideoHeader.GetFromBytes(obj, offset);

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

			if (control == 1)
			{
				closing = true;
				hasvideoheader = false;
				hasaudioheader = false;
				return;
			}

			if(stride * height > videoHeader.stride * videoHeader.height)
			{
				InitializeVideoBuffer();//remap
			}

			int rowsize = videoHeader.stride * videoHeader.height * videoHeader.framerate;
			if (stride * height <= length)
			{
				int writepos = videoposition + videosize % 10;
				if (aptr[writepos] + length < rowsize)
				{
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], vptr[writepos], length);
					vptr[writepos] += length;
				}
				else
				{
					int remainder = rowsize - vptr[writepos];
					length -= remainder;
					
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], vptr[writepos], remainder);
					vptr[writepos] = rowsize;
					videosize = videosize + 1;

					if (videosize >= 9)
					{
						videosize--;
						if (audiosize > 1)
							audiosize--;
					}
					writepos = videoposition + videosize % 10;
					vptr[writepos] = 0;
					if (paused && audiosize >= 5 && videosize >= 5)
						paused = false;
					Buffer.BlockCopy(obj, offset, audiostorage[writepos], aptr[writepos], length);
					vptr[writepos] += length;
				}
			}
		}

		private void InitializeVideoBuffer()
		{
			if (hasvideoheader)
			{
				var oldstorage = audiostorage;
				for (int i = 0; i < 10; i++)
				{
					videostorage[i] = new byte[videoHeader.framerate * videoHeader.stride * videoHeader.width];
					Buffer.BlockCopy(oldstorage[i], 0, videostorage[i], 0, vptr[i]);
				}
			}
			else
			{
				hasvideoheader = true;
				for (int i = 0; i < 10; i++)
				{
					videostorage[i] = new byte[videoHeader.framerate * videoHeader.stride * videoHeader.width];
				}
			}
		}

		internal int ReadAudio(out byte[] audiobuffer)
		{
			if (paused || audiosize <= 1 || videosize <= 1)
			{
				if(!paused && !closing)
					paused = true;
				audiobuffer = null;
				return 0;//wait
			}
			audiobuffer = audiostorage[audioposition];
			int length = aptr[audioposition];
			aptr[audioposition] = 0;
			audioposition = audioposition + 1 % 10;
			audiosize--;
			if (audiosize <= 1)
				paused = true;
			return length;
		}

		internal int ReadVideo(out byte[] videobuffer)
		{
			if (paused || audiosize <= 1 || videosize <= 1)
			{
				if(!paused && !closing)
					paused = true;
				videobuffer = null;
				return 0;//wait
			}

			videobuffer = videostorage[videoposition];
			int length = vptr[videoposition];
			vptr[videoposition] = 0;
			videoposition = videoposition + 1 % 10;
			videosize--;
			if (videosize <= 1)
				paused = true;
			return length;
		}
	}
}