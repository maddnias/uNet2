using System;
using uNet2.Channel;
using uNet2.Exceptions.SocketOperation;
using uNet2.Network;
using uNet2.Packet;

namespace uNet2.SocketOperation
{
    public abstract class SocketOperationBase : ISocketOperation
    {
        public abstract int OperationId { get; }
        public Guid OperationGuid { get; set; }
        public Guid ConnectionGuid { get; set; }
        public IChannel HostChannel { get; set; }
        internal bool IsReady { get; set; }

        protected SocketOperationBase()
        {
            OperationGuid = SocketIdentity.GenerateGuid();
        }

        public virtual void Initialize()
        {
            IsReady = true;
        }

        public void SendPacket(IDataPacket data)
        {
          //  if (!IsReady)
          //      throw new SocketOperationNotInitializedException();
            if (HostChannel is TcpServerChannel)
                ((TcpServerChannel)HostChannel).OperationSend(data, ConnectionGuid, OperationGuid);
            else if (HostChannel is TcpClientChannel)
                ((TcpClientChannel) HostChannel).OperationSend(data, ConnectionGuid, OperationGuid);
        }   

        public void SendSequence(SequenceContext seqCtx)
        {
            if (!IsReady)
                throw new SocketOperationNotInitializedException();
        }

        public abstract void PacketReceived(IDataPacket packet, IChannel sender);
        public abstract void PacketSent(IDataPacket packet, IChannel targetChannel);
        public abstract void SequenceFragmentReceived(SequenceFragmentInfo fragmentInfo);
        public abstract void Disconnected();

        public virtual void CloseOperation()
        {
            HostChannel.UnregisterOperation(this);
        }
    }
}
