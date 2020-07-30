namespace CaptureStream
{
	public interface iSEVideoEncoder
	{
		int Threshold
		{
			get;
			set;
		}
		byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln, int keyframeln);

		byte[] Encode(byte[] bytesToEncode, int stride, int width, int height, int imageln);

		byte[] Decode(byte[] encodedBytes, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

	}
}
