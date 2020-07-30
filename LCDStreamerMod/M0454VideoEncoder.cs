using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
    class M0454VideoEncoder : iSEVideoEncoder
    {

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

            bool flag = frameSize == (myPrevUnCompressedFrame?.Length ?? 0);

            //whats faster division or a variable?
            int timesRun = 0;
            for (int i = 0; i < myFrame.Length; i += 3)
            {
                byte count = myFrame[i];
                ushort myVal = BitConverter.ToUInt16(myFrame, i + 1);

                if (flag && myVal == 65535)
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
