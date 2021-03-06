﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureStream
{
    class M0424VideoEncoder : iSEVideoCodec
    {

        public FrameControlFlags EncodingFlag
        {
            get
            {
                return FrameControlFlags.M0424Encoded;
            }
        }

        //if your making a new instance per frame then does a buffer help?
        public byte[] Decode(byte[] myFrame, int encodedBytesOffset, byte[] myPrevUnCompressedFrame, int stride, int width, int height)
        {
            int frameSize = (stride * height);
            int compressedSize = BitConverter.ToInt32(myFrame, encodedBytesOffset);
            byte[] buffer = new byte[frameSize];
            if (frameSize == compressedSize)
            {
                Buffer.BlockCopy(myFrame, encodedBytesOffset + sizeof(int), buffer, 0, compressedSize);
                return myFrame;
            }
            encodedBytesOffset += sizeof(int);
            bool flag = frameSize == (myPrevUnCompressedFrame?.Length ?? 0);

            //whats faster division or a variable?
            int timesRun = 0;
            for (int i = 0; i + 2 < myFrame.Length; i += 3)
            {
                byte count = myFrame[encodedBytesOffset + i];
                ushort myVal = BitConverter.ToUInt16(myFrame, encodedBytesOffset + i + 1);

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
