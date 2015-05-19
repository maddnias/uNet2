using System;
using System.Collections.Generic;
using uNet2.Channel.Events;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.Peer.Events;


namespace uNet2.Channel
{
    public interface IServerChannel : IChannel
    {
        /// <summary>
        /// This event is raised each time a peer connects to this channel
        /// </summary>
        event ChannelEvents.OnPeerConnected OnPeerConnected;

        event ChannelEvents.OnPeerDisconnected OnPeerDisconnected;
        /// <summary>
        /// This event is raised each time a packet is received in this channel
        /// </summary>
        event PacketEvents.OnServerPacketReceived OnPacketReceived;

        /// <summary>
        /// The peers connected to this channel
        /// </summary>
        List<Peer.Peer> ConnectedPeers { get; set; }
        byte[] ChannelPrivateKey { get; set; }
        List<PendingPeerConnection> PendingConnections { get; set; }
        int PendingConnectionTimeout { get; set; }
        UNetServer HostServer { get; set; }
        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="id">The ID of the peer to disconnect</param>
        void DisconnectPeer(int id);
        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="peer">The peer to disconnect</param>
        void DisconnectPeer(Peer.Peer peer);
        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="pred">The predicate used to determine what peer to disconnect</param>
        void DisconnectPeer(Predicate<Peer.Peer> pred);
        /// <summary>
        /// Broadcasts a message to all peers in this channel
        /// </summary>
        /// <param name="data">The packet to broadcast</param>
        void Broadcast(IDataPacket data);
        /// <summary>
        /// Starts the channel socket
        /// </summary>
        void Start();

        void Send(IDataPacket data, Guid peerGuid);
        void Send(IDataPacket data, int peerId);
        void Send(IDataPacket data, Predicate<Peer.Peer> pred);
    }
}
