using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class D4x4VideoCodec : iSEVideoCodec
	{
		public int Threshold
		{
			get
			{
				return 0;
			}
			set
			{
				return;
			}
		}

		public FrameControlFlags EncodingFlag
		{
			get
			{
				return FrameControlFlags.D4x4Encoded;
			}
		}



		public byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln, int keyframeln)
		{
			int uncompressedln = stride * height;
			if (uncompressedln > buffer.Length)
				buffer = new byte[uncompressedln];
			//we dont actually know the length of our encoded byte list.
			int hptr = 0;
			int hmax = height / 4 + (height % 4 != 0 ? 1 : 0);
			int wptr = 0;
			int wmax = stride / 8 + (stride % 8 != 0 ? 1 : 0);
			int bufferpos = 0;
			int first = 0;
			int second = 1;
			if (BitConverter.IsLittleEndian)
			{
				first = 1;
				second = 0;
			}
			for (hptr = 0; hptr < hmax; hptr++)
			{

				for (wptr = 0; wptr < wmax; wptr++)
				{
					int ptr = hptr * 4 * stride + wptr * 8;

					int h = height - hptr * 4;
					if (h > 4)
						h = 4;

					int w = stride - wptr * 8;
					if (w > 8)
						w = 8;
					//get mask
					ushort onMask = 0b0_11111_11111_11111;
					ushort offMask = 0b0_11111_11111_11111;

					
					ushort prevOnMask = 0b0_11111_11111_11111; // for encoding with keyframe
					ushort prevOffMask = 0b0_11111_11111_11111;

					if (myPrevUnCompressedFrame == null)
					{
						prevOnMask = 0b0_00000_00000_00000;
						prevOffMask = 0b0_00000_00000_00000;
					}

						//ushort flip = 0b1_00000_00000_00000;//dont flip the first bit.
					for (int y = 0; y < 4 && y < h; y++)
					{
						for (int x = 0; x + 1 < 8 && x < w; x += 2)
						{
							
							ushort ontest = (ushort)(((ushort)bytesToEncode[ptr + y * stride + x + first] << 8) + (ushort)bytesToEncode[ptr + y * stride + x + second]);
							ushort offtest = (ushort)(~ontest);//change 1's to 0's and 0's to 1's. 

							onMask &= ontest;
							offMask &= offtest;

							if(myPrevUnCompressedFrame != null)
							{

								byte firstbyte = (byte)(bytesToEncode[ptr + y * stride + x + first] ^ myPrevUnCompressedFrame[ptr + y * stride + x + first]);
								byte secondbyte = (byte)(bytesToEncode[ptr + y * stride + x + second] ^ myPrevUnCompressedFrame[ptr + y * stride + x + second]);
								ushort prevOnTest = (ushort)((firstbyte << 8) + secondbyte);

								ushort prevOffTest = (ushort)(~prevOnTest);

								prevOnMask &= prevOnTest;
								prevOffMask &= prevOffTest;
							}	
						}
					}
					ushort ComputedMask = (ushort)(onMask | offMask);
					ushort PrevComputedMask = (ushort)(prevOnMask | prevOffMask);





					if (onMask == 0 && offMask == 0 && prevOffMask == 0 && prevOnMask == 0)
					{
						//cannot compress at all copy block into buffer uncompressed. 
						//DONE prevent blockcopy from going outside the bounds of the array. 
						for (int yc = 0; (yc * stride) + ptr < uncompressedln && yc < h; yc++)
						{
							//var move = uncompressedln - (16 + (yc * stride) + ptr);
							//if (move > 16)
							//	move = 16;
							Buffer.BlockCopy(bytesToEncode, ptr + (yc * stride), buffer, bufferpos, w);
							bufferpos += w;
						}
						continue;
					}


					int compressedSize = 0;
					int pcompressedSize = 0;
					ushort mask = (ushort)(ComputedMask | FLIP);  
					ushort pmask = (ushort)(PrevComputedMask | FLIP);

					for (int i = 0; i < 16; i++)
					{
						compressedSize += mask & 1;
						pcompressedSize += pmask & 1;
						mask >>= 1;
						pmask >>= 1;
					}

					bool maskWithPreviousFrame = pcompressedSize > compressedSize;
					if(maskWithPreviousFrame)
					{
						compressedSize = pcompressedSize;
						ComputedMask = PrevComputedMask;
						onMask = prevOnMask;
					}
					//write our masks to the buffer, flip the first bit to 1 for both the mask and computed mask. 
					Buffer.BlockCopy(BitConverter.GetBytes((ushort)(ComputedMask | FLIP)), 0, buffer, bufferpos, 2);
					bufferpos += 2;
					Buffer.BlockCopy(BitConverter.GetBytes((ushort)(onMask) | (maskWithPreviousFrame ? FLIP : 0)), 0, buffer, bufferpos, 2);
					bufferpos += 2;
					//count set bits

					//now compressedsize is the amount of bits were saving per pixel in the 8x8 block. 
					if (compressedSize == 16)
					{
						continue;//we dont have to do anything further, all colors are the same for the block. We just saved 126 bytes :)
					}


					int bitpos = 0;//our bit position. 
					int resultbufferptr = 0;
					resultbuffer[0] = 0b0000_0000;//set first item to 0. 
					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x + 1 < w; x += 2)
						{
							byte firstuncompressedbyte = bytesToEncode[ptr + y * stride + x + first];
							byte seconduncompressedbyte = bytesToEncode[ptr + y * stride + x + second];
							if (maskWithPreviousFrame)
							{
								firstuncompressedbyte ^= myPrevUnCompressedFrame[ptr + y * stride + x + first];
								seconduncompressedbyte ^= myPrevUnCompressedFrame[ptr + y * stride + x + second];
							}
							ushort uncompressedUshort = (ushort)((firstuncompressedbyte << 8) + seconduncompressedbyte);
							//compress out each ushort and store unpacked.
							for (ushort bit = 0b0100_0000_0000_0000; bit != 0; bit >>= 1)
							{
								//ok we have a shifting 'read' mask. 
								//if the mask bit is set, skip this value.
								if ((ushort)(ComputedMask & bit) == bit)
								{
									continue;
								}
								//if the uncompressed value is set in this bit position, add it to the resultbuffer, otherwise we can skip as zero is default.
								if ((uncompressedUshort & bit) == bit)
								{
									resultbuffer[resultbufferptr] |= (byte)(0b1000_0000 >> bitpos);
								}
								bitpos++;
								bitpos %= 8;
								if (bitpos == 0)
								{
									resultbufferptr++;
									resultbuffer[resultbufferptr] = 0b0000_0000;//prep the next byte by setting to zero. 
								}
							}
						}
					}
					if (bitpos == 0)
						resultbufferptr--;
					resultbufferptr++;
					Buffer.BlockCopy(resultbuffer, 0, buffer, bufferpos, resultbufferptr);
					bufferpos += resultbufferptr;//move the ptr.  
				}
			}
			var returned = new byte[bufferpos];
			Buffer.BlockCopy(buffer, 0, returned, 0, bufferpos);
			//Decode(returned, 0, myPrevUnCompressedFrame, stride, width, height);
			return returned;
			//return Decode(returned, 0, myPrevUnCompressedFrame, stride, width, height);//testing. 
		}
		byte[] buffer = new byte[0];
		ushort[] copyblock = new ushort[4 * 8];
		byte[] resultbuffer = new byte[4 * 16 + 2];
		public byte[] Encode(byte[] bytesToEncode, int stride, int width, int height, int imageln)
		{
			return Encode(bytesToEncode,  null, stride, width, height, imageln, 0);
		}
		public static ushort FLIP = 0b1_00000_00000_00000;
		public byte[] Decode(byte[] encodedBytes, int encodedBytesOffset, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
		{
			int uncompressedln = stride * height;
			if (uncompressedln != buffer.Length)
				buffer = new byte[uncompressedln];

			//dostuff

			int hmax = height / 4 + (height % 4 != 0 ? 1 : 0);
			int wmax = width / 4 + (width % 4 != 0 ? 1 : 0);

			int encodedbyteptr = encodedBytesOffset;

			for (int hptr = 0; hptr < hmax; hptr++)
			{
				for (int wptr = 0; wptr < wmax; wptr++)
				{
					var ptr = hptr * 4 * stride + wptr * 8;

					//read compressedmask and mask ushorts. 
					var CompressedMask = BitConverter.ToUInt16(encodedBytes, encodedbyteptr);
					encodedbyteptr += 2;
					var MaskBytes = BitConverter.ToUInt16(encodedBytes, encodedbyteptr);
					encodedbyteptr += 2;
					bool usePrevFrame = false;
					if ((ushort)(MaskBytes & 0b1000_0000_0000_0000) == (ushort)0b1000_0000_0000_0000)
					{
						MaskBytes -= 0b1000_0000_0000_0000;
						usePrevFrame = true;
					}

					int h = height - hptr * 4;  

					if (h > 4)
						h = 4;

					int w = stride - wptr * 8;
					if (w > 8)
						w = 8;

					if ((ushort)(CompressedMask & 0b1000_0000_0000_0000) == (ushort)0b0000_0000_0000_0000)
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
					else if(CompressedMask == 0b1111_1111_1111_1111)
					{

						var writebytes = BitConverter.GetBytes(MaskBytes); //copy it back in so we can drop the prev frame flag. 
						//data is 100% compressed into 2 bytes! write the maskbytes in
						for (int yc = 0; (yc * stride) + ptr < uncompressedln && yc < h; yc++)
						{
							//var move = uncompressedln - (16 + (yc * stride) + ptr);
							//if (move > 16)
							//	move = 16;
							for(int xc = 0; xc < w; xc += 2)
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
								ushort resultBuffer = 0b0000_0000_0000_0000;
								//compress out each ushort and store unpacked.
								for (ushort bit = 0b0100_0000_0000_0000; bit != 0; bit >>= 1)
								{
									//ok we have a shifting 'read' mask. 
									//if the mask bit is set, skip this value.
									if ((ushort)(CompressedMask & bit) == bit)
									{
										continue;
									}
									//if the uncompressed value is set in this bit position, add it to the resultbuffer, otherwise we can skip as zero is default.
									var maskbit = (0b1000_0000 >> bitpos);
									if ((searchBuffer & maskbit)  == maskbit)
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
