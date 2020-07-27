using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	interface iSEVideoEncoder
	{
		byte[] Encode(byte[] bytesToEncode);
		byte[] Decode(byte[] encodedBytes);
	}
}
