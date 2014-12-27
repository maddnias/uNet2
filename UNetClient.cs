using System;
using System.Collections.Generic;
using System.Linq;
using uNet2.Channel;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.SocketOperation;

namespace uNet2
{
    public sealed class UNetClient
    {
        public event PacketEvents.OnClientPacketReceived OnPacketReceived;

        public SocketIdentity Identity { get; set; }
        public IPacketProcessor PacketProcessor { get; set; }
        public List<IClientChannel> ActiveChannels { get; set; }
        private Dictionary<int, ISocketOperation> _operationTable { get; set; }

        private readonly ChannelManager _channelManager;

        public UNetClient(IPacketProcessor packetProcessor)
        {
            _channelManager = new ChannelManager();
            PacketProcessor = packetProcessor;
            ActiveChannels = new List<IClientChannel> {CreateMainChannel()};
            Identity = new SocketIdentity(0);
            _operationTable = new Dictionary<int, ISocketOperation>();
        }

        public bool Connect(string addr, int port)
        {
            return GetMainChannel().ConnectToChannel(addr, port);
        }

        public void SendData(IDataPacket packet)
        {
            GetMainChannel().Send(packet);
        }

        public void SendSequence(SequenceContext seqCtx)
        {
            GetMainChannel().SendSequence(seqCtx);
        }

        internal TcpClientChannel GetMainChannel()
        {
            return (TcpClientChannel)ActiveChannels.First(ch => ch.IsMainChannel);
        }

        internal T CreateChannel<T>() where T : IClientChannel
        {
            var channel = (T)_channelManager.CreateChannel<T>();
            channel.Client = this;
            return channel;
        }

        private IClientChannel CreateMainChannel()
        {
            var mainChannel = (IClientChannel)_channelManager.CreateChannel<TcpClientChannel>();
            mainChannel.BufferSize = 8192;
            mainChannel.Client = this;
            mainChannel.Name = "ch:\\main";
            mainChannel.IsMainChannel = true;
            mainChannel.OnPacketReceived += (sender, e) => OnPacketReceived.Raise(sender, e);
            mainChannel.PacketProcessor = PacketProcessor;
            return mainChannel;
        }

        public void RegisterOperation<T>() where T : ISocketOperation
        {
            var socketOperation = Activator.CreateInstance<T>();
            _operationTable.Add(socketOperation.OperationId, socketOperation);
            ActiveChannels.ForEach(ch =>
            {
                var operation = ch.CreateOperation<T>();
                ch.OperationTable.Add(operation.OperationId, operation.GetType());
            });
        }

        void mainChannel_OnPacketReceived(object sender, ClientPacketEventArgs e)
        {

        }
    }
}
