using System;
using uNet2.Channel;
using uNet2.Packet;
using uNet2.Packet.Events;

namespace uNet2.SocketOperation
{
    public interface ISocketOperation
    {
        PacketEvents.OnOperationPacketReceived OnPacketReceived { get; set; }

        int OperationId { get; }
        Guid OperationGuid { get; set; }
        Guid ConnectionGuid { get; set; }
        IChannel HostChannel { get; set; }

        void Initialize();
        void SendPacket(IDataPacket data);
        void SendSequence(SequenceContext seqCtx);
        void PacketReceived(IDataPacket data, IChannel sender);
        void PacketSent(IDataPacket data, IChannel targetChannel);
        void SequenceFragmentReceived(SequenceFragmentInfo fragmentInfo);
        void Disconnected();
        void CloseOperation();
    }
}
