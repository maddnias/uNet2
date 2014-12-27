using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using uNet2.Channel;
using uNet2.Extensions;
using uNet2.Packet;
using uNet2.Peer;
using uNet2.SocketOperation;

namespace uNet2.Network
{
    internal class NetworkWriter
    {
        public NetworkWriter()
        {
            

        }

        public void WritePacketToSocket(IDataPacket data, IChannel senderChannel, Guid guid, BufferObject buffObj,
            Socket sock, AsyncCallback sendCallback, SocketOperationContext operationCtx)
        {
            var ms = new MemoryStream();
            data.SerializeTo(ms);
            var sendBuff = new List<byte> {operationCtx == null ? ((byte) 0x0) : ((byte) 0x1)};
            if (operationCtx != null)
            {
                sendBuff.AddRange(operationCtx.OperationGuid.ToByteArray());
                sendBuff.AddRange(guid.ToByteArray());
            }
            sendBuff.AddRange(ms.ToArray());
            sendBuff.InsertRange(0, BitConverter.GetBytes(sendBuff.Count));
            var buff = sendBuff.ToArray();

            var sendObj = new Peer.Peer.SendObject {Channel = senderChannel, Packet = data};
            if (sock.Connected)
                sock.BeginSend(buff, 0, buff.Length, 0, sendCallback, sendObj);
        }

        public void WriteSequenceToSocket(SequenceContext seqCtx, IChannel senderChannel, Guid guid,
            BufferObject buffObj,
            Socket sock, AsyncCallback sendCallback)
        {
            WritePacketToSocket(seqCtx.InitPacket, senderChannel, guid, buffObj, sock, sendCallback, null);
            seqCtx.SequencePackets.ForEach(seqPacket => WritePacketToSocket(seqPacket, senderChannel, guid, buffObj, sock, sendCallback, null));
        }

        internal static void PrependStreamSize(MemoryStream stream)
        {
            var size = stream.Length;
            InsertData(stream, 0, BitConverter.GetBytes((int) size));
        }

        private static void InsertData(MemoryStream stream, int idx, byte[] data)
        {
            var curBuff = stream.ToArray();
            stream.Position = data.Length +idx;
            stream.Write(curBuff, 0, curBuff.Length);
            stream.Position = idx;
            stream.Write(data, 0, data.Length);
            stream.Position += curBuff.Length;
        }
    }
}
