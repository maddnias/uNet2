
using System;
using System.Collections.Generic;
using System.Net;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.SocketOperation;

namespace uNet2.Channel
{
    public interface IClientChannel : IChannel
    {
        event PacketEvents.OnClientPacketReceived OnPacketReceived;
        event PacketEvents.OnPacketSent OnPacketSent;
        UNetClient Client { get; set; }
        IPEndPoint EndPoint { get; set; }
        Dictionary<int, Type> OperationTable { get; set; }

        bool ConnectToChannel(string addr, int port);
        void Send(IDataPacket data);
        void SendSequence(SequenceContext seqCtx);
    }
}
