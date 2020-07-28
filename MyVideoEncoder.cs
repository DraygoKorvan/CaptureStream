using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
    class MyVideoEncoder : iSEVideoEncoder
    {
        public static int Threshold = 10;

        byte[] buffer = new byte[0];

        public byte[] Encode(byte[] myFrame, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
        {
            //change in size, so send a new image (keyframe)
            if ((width * height * 2) != myPrevUnCompressedFrame.Length)
            {
                return Encode(myFrame, stride, width, height);
            }

            //make the buffer the size of the frame because we shouldnt make *more* data
            if (buffer.Length < myFrame.Length)
                buffer = new byte[myFrame.Length];

            //how big is the final frame?
            int compressedSize = 0;

            //how many times does this short repeat
            byte count = 1;

            //previous value for RLE encoding
            ushort myPrevVal = BitConverter.ToUInt16(myFrame, 0);

            //I could continue chunking data but this is fast
            for (int i = 2; i < myFrame.Length; i += 2)
            {
                ushort myOtherVal = BitConverter.ToUInt16(myPrevUnCompressedFrame, i);
                ushort myVal = BitConverter.ToUInt16(myFrame, i);

                //make a comparison to the other frame, 0 means use previous frame
                if (Math.Abs(myOtherVal - myVal) <= Threshold)
                {
                    myVal = 0;
                }

                if (i != myFrame.Length - 2 && count < 255 && Math.Abs(myPrevVal - myVal) <= Threshold)
                {
                    count++;
                }
                else
                {
                    buffer[compressedSize] = count;
                    BitConverter.GetBytes(myPrevVal).CopyTo(buffer, compressedSize + 1);
                    myPrevVal = myVal;
                    count = 1;
                    compressedSize += 3;
                    if (compressedSize + 3 > buffer.Length)
                    {
                        //Compression failed, we'll get em' next time
                        return myFrame;
                    }
                }
            }


            byte[] returned = new byte[compressedSize];
            Buffer.BlockCopy(buffer, 0, returned, 0, returned.Length);
            return returned;
        }


        //clone from above minus the previous frame
        public byte[] Encode(byte[] myFrame, int stride, int width, int height)
        {
            if (buffer.Length < myFrame.Length)
                buffer = new byte[myFrame.Length];
            int compressedSize = 0;
            byte count = 1;
            ushort myPrevVal = BitConverter.ToUInt16(myFrame, 0);
            for (int i = 2; i < myFrame.Length; i += 2)
            {
                ushort myVal = BitConverter.ToUInt16(myFrame, i);
                if (i != myFrame.Length - 2 && count < 255 && Math.Abs(myPrevVal - myVal) <= Threshold)
                {
                    count++;
                }
                else
                {
                    buffer[compressedSize] = count;
                    BitConverter.GetBytes(myPrevVal).CopyTo(buffer, compressedSize + 1);
                    myPrevVal = myVal;
                    count = 1;
                    compressedSize += 3;
                    if (compressedSize + 3 > buffer.Length)
                    {
                        return myFrame;
                    }
                }
            }
            byte[] returned = new byte[compressedSize];
            Buffer.BlockCopy(buffer, 0, returned, 0, returned.Length);
            return returned;
        }


        //if your making a new instance per frame then does a buffer help?
        public byte[] Decode(byte[] myFrame, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
        {
            int frameSize = (width * height * 2);
            //I wasnt compressed so return the frame
            if (frameSize == myFrame.Length)
            {
                return myFrame;
            }

            byte[] buffer = new byte[frameSize];

            bool flag = frameSize == myPrevUnCompressedFrame.Length;

            //whats faster division or a variable?
            int timesRun = 0;
            for (int i = 0; i < myFrame.Length; i += 3)
            {
                byte count = myFrame[i];
                ushort myVal = BitConverter.ToUInt16(myFrame, i + 1);

                if (flag && myVal == 0)
                {
                    Buffer.BlockCopy(myPrevUnCompressedFrame, timesRun * 2, buffer, timesRun * 2, count * 2);
                }
                else
                {
                    for (int c = 0; c < count; c++)
                    {
                        BitConverter.GetBytes(myVal).CopyTo(buffer, (timesRun + c) * 2);
                    }
                }
                timesRun += count;
            }
            return buffer;
        }



    }
}
