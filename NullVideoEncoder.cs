using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class NullVideoEncoder : iSEVideoEncoder
	{


		public byte[] Decode(byte[] encodedBytes, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
		{
			return encodedBytes;
		}

		public byte[] Encode(byte[] bytesToEncode, int stride, int width, int height)
		{
			return bytesToEncode;
		}

		public byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
		{
			return bytesToEncode;
		}
	}
}
