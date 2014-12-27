using System;

namespace uNet2.Channel.Events
{
    /// <summary>
    /// Event arguments for channel events
    /// </summary>
    public sealed class ChannelEventArgs : EventArgs
    {
        /// <summary>
        /// The ID of the channel
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The associated channel
        /// </summary>
        public IChannel Channel { get; set; }
        /// <summary>
        /// The associated peer
        /// </summary>
        public Peer.Peer Peer { get; set; }

        public ChannelEventArgs(IChannel channel)
        {
            Channel = channel;
            Id = channel.Id;
        }

        public ChannelEventArgs(int id)
        {
            Id = id;
        }

        public ChannelEventArgs(int id, IChannel channel)
        {
            Id = id;
            Channel = channel;
        }

        public ChannelEventArgs(IChannel channel, Peer.Peer peer)
        {
            Channel = channel; 
            Peer = peer;
        }
    }
}
