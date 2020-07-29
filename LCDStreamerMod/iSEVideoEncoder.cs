using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	interface iSEVideoEncoder
	{
		//you dont need encoding

		byte[] Decode(byte[] encodedBytes, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

	}
}
