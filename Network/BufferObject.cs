using System.Collections.Generic;

namespace uNet2.Network
{
    internal class BufferObject
    {
        public List<byte> CompleteBuff { get; set; }
        public byte[] RecBuff { get; set; }
        public int BufferSize { get; set; }
        public int ReadLen { get; set; }
        public int PacketSize { get; set; }
        public int TotalRead { get; set; }

        public BufferObject(int bufferSize)
        {
            CompleteBuff = new List<byte>();
            RecBuff = new byte[BufferSize = bufferSize];
            ReadLen = 0;
            PacketSize = 0;
            TotalRead = 0;
        }


        public void Clear()
        {
            CompleteBuff.Clear();
            RecBuff = new byte[BufferSize];
            PacketSize = 0;
            TotalRead = 0;
            ReadLen = 0;
        }
    }
}
