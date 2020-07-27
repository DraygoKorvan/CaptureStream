using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public class NullVideoEncoder : iSEVideoEncoder
	{
		public byte[] Decode(byte[] encodedBytes)
		{
			return encodedBytes;

		}

		public byte[] Encode(byte[] bytesToEncode)
		{

			return bytesToEncode;
		}
	}
}
