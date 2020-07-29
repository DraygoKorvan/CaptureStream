using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class NullVideoEncoder : iSEVideoEncoder
	{


		public byte[] Decode(byte[] encodedBytes, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln)
		{
			return encodedBytes;
		}

		public byte[] Encode(byte[] bytesToEncode, int stride, int width, int height, int imageln)
		{
			var encodedbytes = new byte[imageln];
			Buffer.BlockCopy(bytesToEncode, 0, encodedbytes, 0, imageln);
			return bytesToEncode;
		}

		public byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln, int keyframeln)
		{
			return Encode(bytesToEncode, stride, width, height, imageln);
		}
	}
}
