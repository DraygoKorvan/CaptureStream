namespace CaptureStream
{
	[System.Flags]
	public enum FrameControlFlags : uint
	{
		None = 0,
		IsKeyFrame = 1, //0001
		M0424Encoded = 2, //0010
		D8x8Encoded = 4 //0100
	}
	public interface iSEVideoCodec
	{
		int Threshold
		{
			get;
			set;
		}
		FrameControlFlags EncodingFlag
		{
			get;
		}
		byte[] Encode(byte[] bytesToEncode, byte[] myPrevUnCompressedFrame, int stride, int width, int height, int imageln, int keyframeln);

		byte[] Encode(byte[] bytesToEncode, int stride, int width, int height, int imageln);

		byte[] Decode(byte[] encodedBytes, int encodedBytesOffset, byte[] myPrevUnCompressedFrame, int stride, int width, int height);

	}
}
