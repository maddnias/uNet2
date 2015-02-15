using System;
using System.Net.Sockets;
using uNet2.Packet;
using uNet2.Security;
using uNet2.SocketOperation;

namespace uNet2.Channel
{
    public interface IChannel : IDisposable, ISocketOperationHost
    {       
        /// <summary>
        /// The ID of this channel
        /// </summary>
        int Id { get; set; }
        /// <summary>
        /// The name of this channel
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The port this channel listens to
        /// </summary>
        uint Port { get; set; }
        /// <summary>
        /// Is this channel initialized
        /// </summary>
        bool Initialized { get; set; }
        /// <summary>
        /// The buffersize for the channel socket
        /// </summary>
        uint BufferSize { get; set; }
        /// <summary>
        /// The socket of this channel
        /// </summary>
        Socket ChannelSocket { get; set; }
        /// <summary>
        /// This channel's manager
        /// </summary>
        ChannelManager Manager { get; set; }
        /// <summary>
        /// Indicates if this is the main channel
        /// </summary>
        bool IsMainChannel { get; set; }
        bool IsProtected { get; set; }
        byte[] ChannelPublicKey { get; set; }
        IPacketProcessor PacketProcessor { get; set; }
        bool EnsurePacketIntegrity { get; set; }
        PacketIntegrityHash IntegrityHash { get; set; }
    }
}
