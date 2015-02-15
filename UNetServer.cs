using System;
using System.Collections.Generic;
using uNet2.Channel;
using uNet2.Channel.Events;
using uNet2.Exceptions.Channel;
using uNet2.Exceptions.Server;
using uNet2.Network;
using uNet2.Packet;
using uNet2.SocketOperation;

namespace uNet2
{
    /// <summary>
    /// The server
    /// </summary>
    public sealed class UNetServer
    {
        /// <summary>
        /// This event is raised each time a channel is created successfully
        /// </summary>
        public event ChannelEvents.OnChannelCreated OnChannelCreated;
        /// <summary>
        /// This event is raised each time a channel is disposed successfully
        /// </summary>
        public event ChannelEvents.OnChannelCreated OnChannelDisposed;
        /// <summary>
        /// This event is raised each time a peer connected to any channel
        /// </summary>
        public event ChannelEvents.OnPeerConnected OnPeerConnected;

        private readonly ChannelManager _channelMgr;

        public List<Peer.Peer> ConnectedPeers
        {
            get { return GetMainChannel().ConnectedPeers; }
        }

        /// <summary>
        /// Use <see cref="Initialize(IServerChannel)"/> when initializing server
        /// </summary>
        public UNetServer()
        {
            _channelMgr = new ChannelManager();
        }

        /// <summary>
        /// Use <see cref="Initialize()"/> when initializing server
        /// </summary>
        /// <param name="mainChannel">The server main channel to use</param>
        public UNetServer(IServerChannel mainChannel)
        {
            _channelMgr = new ChannelManager();
            mainChannel.IsMainChannel = true;
            _channelMgr.UnsafeAddChannel(mainChannel);
            mainChannel.HostServer = this;
        }

        /// <summary>
        /// Initializes the server. 
        /// Only use this if you've set a main channel with <see cref="UNetServer(IServerChannel)"/>
        /// </summary>
        /// <remarks>
        /// May generate a <see cref="ServerInitializationException"/> if used incorrectly
        /// </remarks>
        public void Initialize()
        {
            if (_channelMgr.GetChannel<IServerChannel>(ch => ch is IServerChannel && ch.IsMainChannel) == null)
                throw new ServerInitializationException("Attempt to initialize server without a main channel");
            _channelMgr.GetChannel<IServerChannel>(ch => ch is IServerChannel && ch.IsMainChannel)
                .Start();
            _channelMgr.GetChannel<IServerChannel>(ch => ch is IServerChannel && ch.IsMainChannel)
                .OnPeerConnected +=
                (sender, e) => OnPeerConnected.Raise(sender, e);
        }

        /// <summary>
        /// Initializes the server.
        /// Only use this if you haven't set a main channel with <see cref="UNetServer()"/>
        /// </summary>
        /// <param name="mainChannel">The server main channel to use</param>
        public void Initialize(IServerChannel mainChannel)
        {
            if (_channelMgr.GetChannel<IServerChannel>(ch => ch.IsMainChannel) != null)
                return;
            mainChannel.IsMainChannel = true;
            mainChannel.HostServer = this;
            _channelMgr.UnsafeAddChannel(mainChannel);
            _channelMgr.GetChannel<IServerChannel>(mainChannel.Id).Start();
            _channelMgr.GetChannel<IServerChannel>(mainChannel.Id).OnPeerConnected +=
                (sender, e) => OnPeerConnected.Raise(sender, e);
        }

        /// <summary>
        /// Creates a channel and assigns a valid ID, port and manager to it
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method whenever you wish to create a new channel.
        /// The only reason not to use this method is if you wish to create a channel with a specific ID
        /// which may cause issues
        /// </remarks>
        /// <typeparam name="T">The type of channel to create</typeparam>
        /// <returns>Returns a newly created channel ready to be added</returns>
        public T CreateChannel<T>() where T : IServerChannel
        {
            return (T)_channelMgr.CreateChannel<T>();
        }

        /// <summary>
        /// Creates a channel and assigns a valid ID, port and manager to it
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method whenever you wish to create a new channel.
        /// The only reason not to use this method is if you wish to create a channel with a specific ID
        /// which may cause issues
        /// </remarks>
        /// <typeparam name="T">The type of channel to create</typeparam>
        /// <returns>Returns a newly created channel ready to be added</returns>
        public T CreateChannel<T>(IPacketProcessor packetProcessor) where T : IServerChannel
        {
            var channel = (T) _channelMgr.CreateChannel<T>();
            channel.PacketProcessor = packetProcessor;
            return channel;
        }

        /// <summary>
        /// Creates a channel and assigns a valid ID, port and manager to it
        /// </summary>
        /// <remarks>
        /// It is recommended to use this method whenever you wish to create a new channel.
        /// The only reason not to use this method is if you wish to create a channel with a specific ID
        /// which may cause issues
        /// </remarks>
        /// <typeparam name="T">The type of channel to create</typeparam>
        /// <param name="name">The name of the channel</param>
        /// <returns>Returns a newly created channel ready to be added</returns>
        public T CreateChannel<T>(string name) where T : IServerChannel
        {
            return (T)_channelMgr.CreateChannel<T>(name);
        }

    
        public T CreateChannel<T, TU>() where T : IServerChannel where TU : IPacketProcessor
        {
            return (T)_channelMgr.CreateChannel<T, TU>();
        }

        public IServerChannel GetMainChannel()
        {
            return _channelMgr.GetMainChannel() as IServerChannel;
        }

        /// <summary>
        /// Adds a channel to this server
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <param name="channel">The channel to add</param>
        /// <returns>Returns true if channel was added successfully</returns>
        public bool AddChannel(IServerChannel channel)
        {
            if (!_channelMgr.AddChannel(channel))
                return false;
            OnChannelCreated.Raise(this, new ChannelEventArgs(channel));
            channel.HostServer = this;
            channel.OnPeerConnected += (sender, e) => OnPeerConnected.Raise(this, e);
            channel.PacketProcessor = _channelMgr.GetMainChannel().PacketProcessor;
            return true;
        }

        /// <summary>
        /// Creates a channel using <see cref="ChannelManager.CreateChannel{T}()"/> and adds it to this server
        /// </summary>
        /// <remarks>
        /// May throw a <see cref="ChannelOperationException"/> if used incorrectly
        /// </remarks>
        /// <code>
        /// CreateAndAddChannel{T}(ch => {
        ///     ch.Name = "Example";
        ///     ch.Port = 1000;
        ///     ...
        /// });
        /// </code>
        /// <typeparam name="T">The type of channel to create</typeparam>
        /// <param name="channelAction">An action to set channel data</param>
        /// <returns>Returns true if channel was created and added successfully</returns>
        public bool CreateAndAddChannel<T>(Action<T> channelAction) where T : IServerChannel
        {
            var channel = (IServerChannel)_channelMgr.CreateChannel<T>();
            channelAction((T)channel);
            if (!_channelMgr.AddChannel(channel))
                return false;
            channel.HostServer = this;
            OnChannelCreated.Raise(this, new ChannelEventArgs(channel));
            channel.OnPeerConnected += OnPeerConnected;
            return true;
        }

        /// <summary>
        /// Disposes and removes a channel from this server
        /// </summary>
        /// <param name="id">The ID of the channel to remove</param>
        public void DisposeChannel(int id)
        {
            _channelMgr.RemoveChannel(id);
            OnChannelDisposed.Raise(this, new ChannelEventArgs(id));
        }

        /// <summary>
        /// Disposes and removes a channel from this server
        /// </summary>
        /// <param name="channel">The channel to remove</param>
        public void DisposeChannel(IServerChannel channel)
        {
            var id = _channelMgr.RemoveChannel(channel);
            OnChannelDisposed.Raise(this, new ChannelEventArgs(id));
        }

        /// <summary>
        /// Disposes and removes a channel from this server
        /// </summary>
        /// <param name="pred">The predicate used to determine what channel to dispose and remove</param>
        public void DisposeChannel(Predicate<IChannel> pred)
        {
            var id = _channelMgr.RemoveChannel(pred);
            OnChannelDisposed.Raise(this, new ChannelEventArgs(id));
        }

        /// <summary>
        /// Retrieves a channel from this server
        /// </summary>
        /// <typeparam name="T">The type of channel to retrieve</typeparam>
        /// <param name="id">The ID of the channel to retrieve</param>
        /// <returns>Returns the channel requested</returns>
        public T GetChannel<T>(int id) where T : IServerChannel
        {
            return _channelMgr.GetChannel<T>(id);
        }

        /// <summary>
        /// Retrieves a channel from this server
        /// </summary>
        /// <typeparam name="T">The type of channel to retrieve</typeparam>
        /// <param name="channel">The channel to retrieve</param>
        /// <returns>Returns the channel requested</returns>
        public T GetChannel<T>(IChannel channel) where T : IServerChannel
        {
            return _channelMgr.GetChannel<T>(channel.Id);
        }

        /// <summary>
        /// Retrieves a channel from this server
        /// </summary>
        /// <typeparam name="T">The type of channel to retrieve</typeparam>
        /// <param name="pred">The predicate used to determine what channel to retrieve</param>
        /// <returns>Returns the channel requested</returns>
        public T GetChannel<T>(Predicate<IChannel> pred) where T : IServerChannel
        {
            return _channelMgr.GetChannel<T>(pred);
        }

        public void AddPeerToChannel(IServerChannel channel, Predicate<Peer.Peer> pred)
        {
            _channelMgr.AddPeerToChannel(channel, pred);
        }

        public void AddPeerToChannel(IServerChannel channel, SocketIdentity identity)
        {
            _channelMgr.AddPeerToChannel(channel, identity);
        }

        /// <summary>
        /// Broadcasts a packet to all connected peers in all channels
        /// </summary>
        /// <param name="data">The data to broadcast</param>
        public void GlobalBroadcast(IDataPacket data)
        {
            
        }

        /// <summary>
        /// Broadcasts a packet to all connected peers in a specific channel
        /// </summary>
        /// <param name="channel">The channel to broadcast to</param>
        /// <param name="data">The data to broadcast</param>
        public void BroadcastToChannel(IServerChannel channel, IDataPacket data)
        {
            _channelMgr.GetChannel<IServerChannel>(channel.Id).Broadcast(data);
        }

        /// <summary>
        /// Shuts down the server
        /// </summary>
        /// <remarks>
        /// You will have to call <see cref="Initialize()"/> or <see cref="Initialize(IServerChannel)"/> to use it again/>
        /// </remarks>
        public void Shutdown()
        {
            _channelMgr.Reset();
        }

        public T CreateOperation<T, TU>(TU hostChannel) where T : ISocketOperation where TU : IChannel
        {
            return _channelMgr.GetChannel<TU>(hostChannel.Id).CreateOperation<T>();
        }
    }
}
