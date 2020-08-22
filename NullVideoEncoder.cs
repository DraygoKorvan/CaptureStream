using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class NullVideoEncoder : iSEVideoCodec
	{
		public FrameControlFlags EncodingFlag 
		{
			get
			{
				return FrameControlFlags.None;
			}
		}

		int iSEVideoCodec.Threshold
		{
			get
			{
				return 0;
			}
			set
			{

			}

		}
		public byte[] Decode(byte[] encodedBytes, int offset, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
		{
			return encodedBytes;
		}

		public byte[] Encode(byte[] bytesToEncode, int stride, int width, int height, int imageln)
		{
			var outbuffer = new byte[imageln];
			Buffer.BlockCopy(bytesToEncode, 0, outbuffer, 0, imageln);
			return outbuffer;
		}

		public byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln, int keyframeln)
		{
			return Encode(bytesToEncode, stride, width, height, imageln);
			
		}
	}
}
