namespace CaptureStream
{
	interface iSEVideoEncoder
	{
		byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

		byte[] Encode(byte[] bytesToEncode, int stride, int width, int height);

		byte[] Decode(byte[] encodedBytes, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

	}
}
