using System;
using System.Collections.Generic;
using System.Linq;
using uNet2.Channel;
using uNet2.Exceptions.Channel;
using uNet2.Network;
using uNet2.Packet;

namespace uNet2
{
    /// <summary>
    /// This class manages multiple <see cref="IChannel"/>
    /// </summary>
    public sealed class ChannelManager
    {
        private readonly HashSet<IChannel> _channels;
        private int _idCounter, _peerIdCounter, _portCounter;
        private readonly object _channelLockObj;

        public ChannelManager()
        {
            _channels = new HashSet<IChannel>();
            _idCounter = _peerIdCounter = 0;
            _portCounter = 1000;
            _channelLockObj = new object();
        }
        
        /// <summary>
        /// Generates a valid channel ID
        /// </summary>
        /// <returns>Returns the generated channel ID</returns>
        public int GenerateChannelId()
        {
            return _idCounter++;
        }

        /// <summary>
        /// Generates a valid peer ID
        /// </summary>
        /// <returns>Returns the generated peer ID</returns>
        public int GeneratePeerId()
        {
            return _peerIdCounter++;
        }

        /// <summary>
        /// Generates a valid port for a channel
        /// </summary>
        /// <returns>Returns the generated channel port</returns>
        public uint GeneratePort()
        {
            return (uint)_portCounter++;
        }

        /// <summary>
        /// Creates a channel and assigns a valid ID, port and manager to it
        /// </summary>
        /// <typeparam name="T">Type of channel to create</typeparam>
        /// <returns>Returns a newly created channel ready to be added</returns>
        public IChannel CreateChannel<T>() where T : IChannel
        {
            var channel = Activator.CreateInstance<T>();
            channel.Id = GenerateChannelId();
            channel.Port = GeneratePort();
            channel.Manager = this;
            channel.BufferSize = 8192;
            channel.Name = "ch:\\channel" + channel.Id;
            channel.IsProtected = false;
            return channel;
        }

        /// <summary>
        /// Creates a channel and assigns a valid ID, port and manager to it
        /// </summary>
        /// <typeparam name="T">Type of channel to create</typeparam>
        /// <param name="name">The name of the channel</param>
        /// <returns>Returns a newly created channel ready to be added</returns>
        public IChannel CreateChannel<T>(string name) where T : IChannel
        {
            var channel = Activator.CreateInstance<T>();
            channel.Id = GenerateChannelId();
            channel.Port = GeneratePort();
            channel.Manager = this;
            channel.BufferSize = 8192;
            channel.Name = "ch:\\" + name;
            channel.IsProtected = false;
            return channel;
        }

        public IChannel CreateChannel<T, TU>() where T : IChannel where TU : IPacketProcessor
        {
            var channel = Activator.CreateInstance<T>();
            channel.Id = GenerateChannelId();
            channel.Port = GeneratePort();
            channel.Manager = this;
            channel.BufferSize = 8192;
            channel.Name = "ch:\\channel" + channel.Id;
            channel.IsProtected = false;
            channel.PacketProcessor = Activator.CreateInstance<TU>();
            return channel;
        }

        /// <summary>
        /// Used to add a main channel
        /// </summary>
        /// <param name="channel">Main channel</param>
        /// <returns>Returns true if added successfully</returns>
        internal bool UnsafeAddChannel(IChannel channel)
        {
            channel.Id = _channels.Count;
            lock (_channelLockObj)
                _channels.Add(channel);
            return true;
        }

        /// <summary>
        /// Adds a channel to this manager
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <param name="channel">The channel to add</param>
        /// <returns>Returns true if channel was added successfully</returns>
        public bool AddChannel(IChannel channel)
        {
            if (channel is IServerChannel)
                if (channel.IsMainChannel)
                    throw new ChannelOperationException("Attempt to add additional main channel in server");
            if (channel is IServerChannel)
                ((IServerChannel) channel).Start();
            if (_channels.Contains(channel))
                return false;
            if (_channels.FirstOrDefault(ch => ch.Id == channel.Id) != null)
                throw new ChannelOperationException("Attempt to add a channel with non-unique ID");
            if (channel.Manager == null)
                channel.Manager = this;
            lock (_channelLockObj)
                _channels.Add(channel);
            return true;
        }

        /// <summary>
        /// Removes a channel from this manager
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <param name="channel">The channel to remove</param>
        /// <returns>Returns the ID of the removed channel</returns>
        public int RemoveChannel(IChannel channel)
        {
            if(channel is IServerChannel)
                if (_channels.Count == 1 && channel.IsMainChannel)
                    throw new ChannelOperationException("Can not remove main channel when no other channels are present");
            lock (_channelLockObj)
                _channels.Remove(channel);
            return channel.Id;
        }

        /// <summary>
        /// Removes a channel from this manager
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <param name="id">The ID of the channel to remove</param>
        /// <returns>Returns the ID of the removed channel</returns>
        public int RemoveChannel(int id)
        {
            var channel = GetChannel<IChannel>(id);
            if (channel is IServerChannel)
                if (_channels.Count == 1 && channel.IsMainChannel)
                    throw new ChannelOperationException("Can not remove main channel when no other channels are present");
            lock (_channelLockObj)
                _channels.RemoveWhere(c => c.Id == id);
            return id;
        }

        /// <summary>
        /// Removes a channel from this manager
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <param name="pred">The predicate used to determine what channel to remove</param>
        /// <returns>Returns the ID of the removed channel</returns>
        public int RemoveChannel(Predicate<IChannel> pred)
        {
            var channel = GetChannel<IChannel>(pred);
            if (channel is IServerChannel)
                if (_channels.Count == 1 && channel.IsMainChannel)
                    throw new ChannelOperationException("Can not remove main channel when no other channels are present");
            var id = _channels.First(new Func<IChannel, bool>(pred)).Id;
            lock (_channelLockObj)
                _channels.RemoveWhere(pred);
            return id;
        }

        /// <summary>
        /// Retrieves a channel from this manager
        /// </summary>
        /// <typeparam name="T">The type of channel to retrieve</typeparam>
        /// <param name="id">The ID of the channel to retrieve</param>
        /// <returns>Returns the channel requested</returns>
        public T GetChannel<T>(int id) where T : IChannel
        {
            return (T)_channels.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Retrieves a channel from this manager
        /// </summary>
        /// <typeparam name="T">The type of channel to retrieve</typeparam>
        /// <param name="pred">The predicate used to determine what channel to retrieve</param>
        /// <returns>Returns the channel requested</returns>
        public T GetChannel<T>(Predicate<IChannel> pred) where T : IChannel
        {
            return (T) _channels.FirstOrDefault(new Func<IChannel, bool>(pred));
        }

        public void AddPeerToChannel(IServerChannel channel, Predicate<Peer.Peer> pred)
        {
            var peer = ((IServerChannel)GetMainChannel()).ConnectedPeers.FirstOrDefault(new Func<Peer.Peer, bool>(pred));
            if (peer == null)
                throw new ChannelOperationException("Could not locate peer");

            var relocationPacket = new PeerRelocationRequestPacket
            {
                ChannelId = channel.Id,
                PeerGuid = peer.Identity.Guid,
                Operation = PeerRelocationRequestPacket.RelocateOperation.Join,
                Port = (int)channel.Port
            };
            lock(_channelLockObj)
                channel.PendingConnections.Add(new PendingPeerConnection(peer.Identity.Guid, peer));
            peer.SendData(relocationPacket, channel, null);
        }

        public void AddPeerToChannel(IServerChannel channel, SocketIdentity identity)
        {
            var peer = ((IServerChannel)GetMainChannel()).ConnectedPeers.FirstOrDefault(p => p.Identity.Equals(identity));
            if (peer == null)
                throw new ChannelOperationException("Could not locate peer");

            var relocationPacket = new PeerRelocationRequestPacket
            {
                ChannelId = channel.Id,
                PeerGuid = peer.Identity.Guid,
                Operation = PeerRelocationRequestPacket.RelocateOperation.Join,
                Port = (int)channel.Port
            };
            lock (_channelLockObj)
                channel.PendingConnections.Add(new PendingPeerConnection(peer.Identity.Guid, peer));
            peer.SendData(relocationPacket, channel, null);
        }

        public List<IChannel> GetAllChannles()
        {
            return _channels.ToList();
        }

        internal void Reset()
        {
            _channels.ToList().ForEach(ch => ch.Dispose());
            _channels.Clear();
        }

        internal IChannel GetMainChannel()
        {
            return _channels.First(ch => ch.IsMainChannel);
        }
    }
}
