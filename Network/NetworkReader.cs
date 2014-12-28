using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace uNet2.Network
{
    internal class NetworkReader
    {
        public void ReadLength(AsyncCallback callback, BufferObject buffObj, Socket _sock, int readLen)
        {
            if (readLen >= 4)
            {
                buffObj.PacketSize = BitConverter.ToInt32(buffObj.RecBuff, 0);
                if (buffObj.PacketSize < buffObj.BufferSize)
                    _sock.BeginReceive(buffObj.RecBuff, 0, buffObj.PacketSize, 0, callback, buffObj);
                else
                    _sock.BeginReceive(buffObj.RecBuff, 0, buffObj.BufferSize, 0, callback, buffObj);
            }
        }
    }
}
