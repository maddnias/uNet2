using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using uNet2.Channel.Events;
using uNet2.Exceptions.Server;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.Peer.Events;
using uNet2.SocketOperation;


namespace uNet2.Channel
{
    /// <summary>
    /// A standard server channel using a TCP protocol
    /// </summary>
    public class TcpServerChannel : IServerChannel
    {
        public event PacketEvents.OnSequenceFragmentReceived OnSequenceFragmentReceived;
        /// <summary>
        /// This event is raised each time a peer connects to this channel
        /// </summary>
        public event ChannelEvents.OnPeerConnected OnPeerConnected;
        /// <summary>
        /// This event is raised each time a packet is received from a peer in this channel
        /// </summary>
        public event PacketEvents.OnServerPacketReceived OnPacketReceived;

        /// <summary>
        /// The ID of this channel
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The name of this channel
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The port this channel listens to
        /// </summary>
        public uint Port { get; set; }
        /// <summary>
        /// Is this channel initialized
        /// </summary>
        public bool Initialized { get; set; }
        /// <summary>
        /// The buffersize for the channel socket
        /// </summary>
        public uint BufferSize { get; set; }
        /// <summary>
        /// The socket of this channel
        /// </summary>
        public Socket ChannelSocket { get; set; }
        /// <summary>
        /// Indicates if this is the main channel
        /// </summary>
        public bool IsMainChannel { get; set; }
        /// <summary>
        /// The peers connected to this channel
        /// </summary>
        public List<Peer.Peer> ConnectedPeers { get; set; }
        /// <summary>
        /// This channel's manager
        /// </summary>
        public ChannelManager Manager { get; set; }
        public int PendingConnectionTimeout { get; set; }

        public bool IsProtected { get; set; }
        public byte[] ChannelPrivateKey { get; set; }
        public List<PendingPeerConnection> PendingConnections { get; set; }
        public byte[] ChannelPublicKey { get; set; }

        /// <summary>
        /// The packet processor attached to this channel
        /// </summary>
        public IPacketProcessor PacketProcessor { get; set; }

        public Dictionary<Guid, ISocketOperation> ActiveSocketOperations { get; set; }

        private bool IsActive { get; set; }
        private bool IsDisposed { get; set; }
        private const string Localhost = "0.0.0.0";

        private readonly object _lockObj;

        public TcpServerChannel()
        {
            _lockObj = new object();
            ConnectedPeers = new List<Peer.Peer>();
            PendingConnections = new List<PendingPeerConnection>();
            PendingConnectionTimeout = 15;
            ActiveSocketOperations = new Dictionary<Guid, ISocketOperation>();
        }

        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="id">The ID of the peer to disconnect</param>
        public void DisconnectPeer(int id)
        {
            var peer = ConnectedPeers.First(p => p.Identity.Id == id);
            peer.Disconnect();
            lock (_lockObj)
                ConnectedPeers.Remove(peer);
        }

        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="peer">The peer to disconnect</param>
        public void DisconnectPeer(Peer.Peer peer)
        {
            peer.Disconnect();
            lock (_lockObj)
                ConnectedPeers.Remove(peer);
        }

        /// <summary>
        /// Disconnects a peer from this channel
        /// </summary>
        /// <param name="pred">The predicate used to determine what peer to disconnect</param>
        public void DisconnectPeer(Predicate<Peer.Peer> pred)
        {
            var peer = ConnectedPeers.First(new Func<Peer.Peer, bool>(pred));
            peer.Disconnect();
            lock (_lockObj)
                ConnectedPeers.Remove(peer);
        }

        /// <summary>
        /// Broadcasts a message to all peers in this channel
        /// </summary>
        /// <param name="data">The packet to broadcast</param>
        public void Broadcast(IDataPacket data)
        {
            ConnectedPeers.ForEach(cp => cp.SendData(data, this, null));
        }

        /// <summary>
        /// Starts the channel socket
        /// </summary>
        public void Start()
        {
            IsActive = true;
            ChannelSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ChannelSocket.Bind(new IPEndPoint(IPAddress.Parse(Localhost), (int)Port));
            ChannelSocket.Listen(100);

            ChannelSocket.BeginAccept(AcceptCallback, null);
            var t = new Thread(PendingConnectionsTimeoutPulse);
            t.Start();
        }

        public void Send(IDataPacket data, Guid peerGuid)
        {
            var peer = ConnectedPeers.FirstOrDefault(p => p.Identity.Guid == peerGuid);
            if (peer == null)
                throw new PeerNotFoundException();
            peer.SendData(data, this, null);
        }

        public void Send(IDataPacket data, int peerId)
        {
            var peer = ConnectedPeers.FirstOrDefault(p => p.Identity.Id == peerId);
            if (peer == null)
                throw new PeerNotFoundException();
            peer.SendData(data, this, null);
        }

        public void Send(IDataPacket data, Predicate<Peer.Peer> pred)
        {
            var peer = ConnectedPeers.FirstOrDefault(new Func<Peer.Peer, bool>(pred));
            if (peer == null)
                throw new PeerNotFoundException();
            peer.SendData(data, this, null);
        }

        internal void OperationSend(IDataPacket data, Guid peerGuid, Guid operationGuid)
        {
            var peer = ConnectedPeers.FirstOrDefault(p => p.Identity.Guid == peerGuid);
            if (peer == null)
                throw new PeerNotFoundException();
            peer.SendData(data, this, new SocketOperationContext(operationGuid));
        }

        /// <summary>
        /// Disposes the channel.
        /// Call this when you're done using it
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            if (disposing)
            {
                OnPeerConnected = null;
                ConnectedPeers.ForEach(p => p.Disconnect());
                IsActive = false;
                ChannelSocket.Close();
            }
            IsDisposed = true;
        }

        #region Internal operations

        private void AcceptCallback(IAsyncResult res)
        {
            if (!IsActive)
                return;
            var newSock = ChannelSocket.EndAccept(res);
            Peer.Peer newPeer;
            if (IsMainChannel)
            {
                newPeer = new Peer.Peer(new SocketIdentity(Manager.GeneratePeerId(), SocketIdentity.GenerateGuid()), newSock, BufferSize, this);
                newPeer.Receive();
                AcceptPeer(newPeer);
            }
            else
            {
                newPeer = new Peer.Peer(new SocketIdentity(Manager.GeneratePeerId()), newSock, BufferSize, this);
                PendingConnections.Add(new PendingPeerConnection(newPeer.Identity.Guid, newPeer));
                newPeer.Receive();
                newPeer.OnPeerRelocationRequest += (sender, e) =>
                {
                    if (PendingConnections.FirstOrDefault(pc => pc.Guid == e.PeerGuid && !pc.IsCancelled) != null)
                    {
                        AcceptPeer(newPeer);
                        OnPeerConnected.Raise(this, new ChannelEventArgs(this, e.Peer));
                        lock (_lockObj)
                            PendingConnections.Remove(PendingConnections.First(pc => pc.Guid == e.PeerGuid));
                    }
                };
            }
            ChannelSocket.BeginAccept(AcceptCallback, null);
        }

        private void AcceptPeer(Peer.Peer newPeer)
        {
            newPeer.OnPeerSynchronized +=
                (sender, e) => OnPeerConnected.Raise(this, new ChannelEventArgs(this, e.Peer));
            newPeer.OnPeerDisconnected +=
                (sender, e) =>
                {
                    e.Peer.Dispose();
                    lock (_lockObj)
                        ConnectedPeers.Remove(e.Peer);
                };
            newPeer.OnRawPacketReceived += RawPacketReceived;
            newPeer.OnSequenceFragmentReceived += (sender, e) => OnSequenceFragmentReceived.Raise(sender, e);
            lock (_lockObj)
                ConnectedPeers.Add(newPeer);
        }

        private void RawPacketReceived(object sender, RawServerPacketEventArgs e)
        {
            IDataPacket parsedPacket;
            var rawDat = e.RawData;
            rawDat = PacketProcessor.ProcessRawData(rawDat);
            using (var ms = new MemoryStream(rawDat) { Position = 1})
            {
                parsedPacket = PacketProcessor.ParsePacket(ms);
            }
            OnPacketReceived.Raise(this, new ServerPacketEventArgs(parsedPacket, e.Peer, this, e.RawData.Length));
        }

        private void PendingConnectionsTimeoutPulse()
        {
            while (IsActive)
            {
                for (var i = 0; i < PendingConnections.Count; i++)
                    if ((DateTime.Now - PendingConnections[i].ConnectionTimestamp).TotalSeconds >=
                        PendingConnectionTimeout)
                        lock (_lockObj)
                            PendingConnections.RemoveAt(i);
                Thread.Sleep(5000);
            }
        }

        #endregion

        public T CreateOperation<T>() where T : ISocketOperation
        {
            var operation = (ISocketOperation)Activator.CreateInstance<T>();
            operation.HostChannel = this;
            return (T)operation;
        }

        public T RegisterOperation<T>(Guid connectionGuid) where T : ISocketOperation
        {
            var operation = CreateOperation<T>();
            operation.ConnectionGuid = connectionGuid;
            lock (_lockObj)
                ActiveSocketOperations.Add(operation.OperationGuid, operation);
            var operationRequest = new SocketOperationRequest
            {
                OperationGuid = operation.OperationGuid,
                Request = SocketOperationRequest.OperationRequest.Create,
                OperationId = operation.OperationId
            };
            Send(operationRequest, operation.ConnectionGuid);
            return operation;
        }

        public void RegisterOperation(ISocketOperation operation)
        {
            if (operation.ConnectionGuid == Guid.Empty)
                //TODO: real exception
                Debug.Print("Socket operation not attached");
            lock(_lockObj)
                ActiveSocketOperations.Add(operation.OperationGuid, operation);
            var operationRequest = new SocketOperationRequest
            {
                OperationGuid = operation.OperationGuid,
                Request = SocketOperationRequest.OperationRequest.Create,
                OperationId = operation.OperationId
            };
            Send(operationRequest, operation.ConnectionGuid);
        }

        public void UnregisterOperation(ISocketOperation operation)
        {
            if (!ActiveSocketOperations.ContainsKey(operation.OperationGuid))
                return;
            lock (_lockObj)
                ActiveSocketOperations.Remove(operation.OperationGuid);
            Debug.Print("Closed operation with GUID: " + operation.OperationGuid);
        }

        public void UnregisterOperation(Guid operationGuid)
        {
            if (!ActiveSocketOperations.ContainsKey(operationGuid))
                return;
            lock (_lockObj)
                ActiveSocketOperations.Remove(operationGuid);
            Debug.Print("Closed operation with GUID: " + operationGuid);
        }

        public void AttachOperation(ISocketOperation operation, Guid connectionGuid)
        {
            operation.ConnectionGuid = connectionGuid;
        }

    }
}
