using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace CaptureStream
{
	public class D8x8VideoCodec : iSEVideoCodec
	{


		public FrameControlFlags EncodingFlag
		{
			get
			{
				return FrameControlFlags.D8x8Encoded;
			}
		}
	
		byte[] buffer = new byte[0];
	//	ushort[] copyblock = new ushort[8 * 8];
	//	byte[] resultbuffer = new byte[8 * 16 + 2];

	//	const ushort FLIP = 32768;
		const ushort U0b0000_0000_0000_0000 = 0;
		const ushort U0b0100_0000_0000_0000 = 16384;
		const ushort U0b1000_0000_0000_0000 = 32768;
		const ushort U0b1111_1111_1111_1111 = 65535;

		const byte B0b1000_0000 = 128;

		//public static ushort FLIP = 0b1_00000_00000_00000;
		public byte[] Decode(byte[] encodedBytes, int encodedBytesOffset, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
		{
			//MyLog.Default.WriteLine("D8x8Decode");
			int uncompressedln = stride * height;
			if (uncompressedln != buffer.Length)
				buffer = new byte[uncompressedln];

			//dostuff

			int hmax = height / 8 + (height % 8 != 0 ? 1 : 0);
			int wmax = width / 8 + (width % 8 != 0 ? 1 : 0);

			int encodedbyteptr = encodedBytesOffset;

			for (int hptr = 0; hptr < hmax; hptr++)
			{
				//MyLog.Default.WriteLine("hptr " + hptr.ToString());
				for (int wptr = 0; wptr < wmax; wptr++)
				{
					var ptr = hptr * 8 * stride + wptr * 16;

					//read compressedmask and mask ushorts. 
					var CompressedMask = BitConverter.ToUInt16(encodedBytes, encodedbyteptr);
					encodedbyteptr += 2;
					var MaskBytes = BitConverter.ToUInt16(encodedBytes, encodedbyteptr);
					encodedbyteptr += 2;
					bool usePrevFrame = false;
					if ((ushort)(MaskBytes & U0b1000_0000_0000_0000) == (ushort)U0b1000_0000_0000_0000)
					{
						MaskBytes -= U0b1000_0000_0000_0000;
						usePrevFrame = true;
						//MyLog.Default.WriteLine("UsePrevFrame");
					}

					int h = height - hptr * 8;

					if (h > 8)
						h = 8;

					int w = stride - wptr * 16;
					if (w > 16)
						w = 16;

					if ((ushort)(CompressedMask & U0b1000_0000_0000_0000) == (ushort)U0b0000_0000_0000_0000)
					{

						encodedbyteptr -= 4; //back step
											 //data has no compression, copy directly into the array. 
						for (int yc = 0; (yc * stride) + ptr < uncompressedln && yc < h; yc++)
						{
							//var move = uncompressedln - (16 + (yc * stride) + ptr);
							//if (move > 16)
							//	move = 16;
							Buffer.BlockCopy(encodedBytes, encodedbyteptr, buffer, ptr + (yc * stride), w);
							encodedbyteptr += w;
						}
					}
					else if (CompressedMask == U0b1111_1111_1111_1111)
					{
						//data is 100% compressed into 2 bytes! write the maskbytes in
						var writebytes = BitConverter.GetBytes(MaskBytes); //copy it back in so we can drop the prev frame flag. 
																		  
						for (int yc = 0; (yc * stride) + ptr < uncompressedln && yc < h; yc++)
						{
							//var move = uncompressedln - (16 + (yc * stride) + ptr);
							//if (move > 16)
							//	move = 16;
							for (int xc = 0; xc < w; xc += 2)
							{
								Buffer.BlockCopy(writebytes, 0, buffer, ptr + (yc * stride) + xc, 2);

							}
						}
					}
					else
					{
						//decompress.
						int bitpos = 0;//our bit position. 
									   //int resultbufferptr = 0;

						byte searchBuffer = encodedBytes[encodedbyteptr];
						encodedbyteptr++;
						//resultbuffer[0] = 0b0000_0000;//set first item to 0. 
						for (int y = 0; y < h; y++)
						{
							for (int x = 0; x + 1 < w; x += 2)
							{
								ushort resultBuffer = U0b0000_0000_0000_0000;
								//compress out each ushort and store unpacked.
								for (ushort bit = U0b0100_0000_0000_0000; bit != 0; bit >>= 1)
								{
									//ok we have a shifting 'read' mask. 
									//if the mask bit is set, skip this value.
									if ((ushort)(CompressedMask & bit) == bit)
									{
										continue;
									}
									//if the uncompressed value is set in this bit position, add it to the resultbuffer, otherwise we can skip as zero is default.
									var maskbit = (B0b1000_0000 >> bitpos);
									if ((searchBuffer & maskbit) == maskbit)
									{
										resultBuffer += bit;
									}

									bitpos++;
									bitpos %= 8;
									if (bitpos == 0)
									{
										if (encodedbyteptr < encodedBytes.Length)
											searchBuffer = encodedBytes[encodedbyteptr];
										encodedbyteptr++;
									}
								}

								Buffer.BlockCopy(BitConverter.GetBytes(resultBuffer + MaskBytes), 0, buffer, ptr + y * stride + x, 2);
							}
						}
						if (bitpos == 0)
						{
							encodedbyteptr--;
						}
					}
					if (usePrevFrame)
					{
						// ^xor with previous frame. 
						for (int y = 0; y < h; y++)
						{
							int x = 0;
							for (; x + 3 < w; x += 4)
							{
								buffer[ptr + (y * stride) + x + 0] ^= myPrevUnCompressedFrame[ptr + (y * stride) + x + 0];
								buffer[ptr + (y * stride) + x + 1] ^= myPrevUnCompressedFrame[ptr + (y * stride) + x + 1];
								buffer[ptr + (y * stride) + x + 2] ^= myPrevUnCompressedFrame[ptr + (y * stride) + x + 2];
								buffer[ptr + (y * stride) + x + 3] ^= myPrevUnCompressedFrame[ptr + (y * stride) + x + 3];
							}
							for (; x < w; x += 1)
							{
								buffer[ptr + (y * stride) + x] ^= myPrevUnCompressedFrame[ptr + (y * stride) + x];
							}
						}
					}
				}
			}


			return buffer;
		}
	}
}
