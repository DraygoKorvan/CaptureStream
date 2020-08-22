using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
	public enum FrameControlFlags : uint
	{
		None = 0,
		IsKeyFrame = 1,
		M0424Encoded = 2, //001
		D8x8Encoded = 4 //010
	}
	interface iSEVideoCodec
	{
		FrameControlFlags EncodingFlag
		{
			get;
		}
		byte[] Decode(byte[] encodedBytes, int encodedoffset, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

	}
}
