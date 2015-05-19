using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using uNet2.Channel;
using uNet2.Extensions;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.Peer.Events;
using uNet2.SocketOperation;
using uNet2.Utils;

namespace uNet2.Peer
{
    /// <summary>
    /// This class represents a connected client
    /// </summary>
    public sealed class Peer : IDisposable
    {
        internal class SendObject
        {
            public IChannel Channel { get; set; }
            public IDataPacket Packet { get; set; }
        }

        internal PacketEvents.OnSequenceFragmentReceived OnSequenceFragmentReceived;
        internal PeerEvents.OnPeerDisconnected OnPeerDisconnected;
        internal PeerEvents.OnPeerSynchronized OnPeerSynchronized;
        internal PeerEvents.OnPeerRelocationRequest OnPeerRelocationRequest;
        internal PacketEvents.OnRawServerPacketReceived OnRawPacketReceived;
        internal PacketEvents.OnPacketSent OnPacketSent;

        public IPEndPoint Endpoint { get; set; } 
        public SocketIdentity Identity { get; set; }
        /// <summary>
        /// The delay between the peer and client last check
        /// </summary>
        public double PingDelay { get; set; }

        private readonly Socket _sock;
        private readonly BufferObject _buffObj;
        private DateTime _initSyncDelay;
        private readonly int _bufferSize;
        private readonly Dictionary<Guid, SequenceSession> _activeSequenceSessions;
        private readonly NetworkWriter _netWriter;
        private readonly NetworkReader _netReader;
        private readonly object _lockObj;
        internal IServerChannel HostChannel;
        internal bool IsDisposed;

        private readonly Dictionary<int, Type> _internalPacketTbl = new Dictionary<int,Type>
        {
            {-10003, typeof(SocketOperationRequest)},
            {-10001, typeof(SequencePacket)},
            {-10000, typeof (SynchronizePacket)},
            {-9999, typeof(PeerRelocationRequestPacket)},
            {-9998, typeof(SequenceInitPacket)},
        };

        public Peer(SocketIdentity identity, Socket sock, uint bufferSize, IServerChannel hostChannel)
        {
            Identity = identity;
            _sock = sock;
            _sock.ReceiveBufferSize = _bufferSize = (int)bufferSize;
            _buffObj = new BufferObject((int)bufferSize);
            _activeSequenceSessions = new Dictionary<Guid, SequenceSession>();
            _netWriter = new NetworkWriter();
            _lockObj = new object();
            HostChannel = hostChannel;
            _netReader = new NetworkReader();
            Endpoint = (IPEndPoint)_sock.RemoteEndPoint;
        }

        public Peer(int id, Socket sock, uint bufferSize, Guid guid, IServerChannel hostChannel)
        {
            Identity = new SocketIdentity(id, guid);
            _sock = sock;
            _sock.ReceiveBufferSize = _bufferSize = (int) bufferSize;
            _buffObj = new BufferObject((int)bufferSize);
            _activeSequenceSessions = new Dictionary<Guid, SequenceSession>();
            _netWriter = new NetworkWriter();
            _lockObj = new object();
            HostChannel = hostChannel;
            _netReader = new NetworkReader();
            Endpoint = (IPEndPoint)_sock.RemoteEndPoint;
        }

        /// <summary>
        /// Disconnects this socket
        /// </summary>
        public void Disconnect()
        {
            _sock.Close();
            OnPeerDisconnected.Raise(this, new PeerEventArgs(this));
        }

        #region Internal operations
        internal void Receive()
        {
            var buffObj = new BufferObject(_bufferSize);
            _sock.BeginReceive(buffObj.RecBuff, 0, 4, 0, ReadLengthCallback, buffObj);
        }

        private void ReadLengthCallback(IAsyncResult res)
        {
            var buffObj = (BufferObject)res.AsyncState;
            try
            {
                var readLen = _sock.EndReceive(res);
                _netReader.ReadLength(ReadDataCallback, buffObj, _sock, readLen);
            }
            catch
            {
                Disconnect();
            }
        }

        private void ReadDataCallback(IAsyncResult res)
        {
            var buffObj = (BufferObject)res.AsyncState;
            buffObj.ReadLen = _sock.EndReceive(res);
            buffObj.TotalRead += buffObj.ReadLen;

            buffObj.CompleteBuff.AddRange(FastBuffer.SliceBuffer(buffObj.RecBuff, 0, buffObj.ReadLen));
            if (buffObj.CompleteBuff.Count < buffObj.PacketSize)
            {
                // keep reading
                if (buffObj.BufferSize < (buffObj.PacketSize - buffObj.CompleteBuff.Count))
                    _sock.BeginReceive(buffObj.RecBuff, 0, buffObj.BufferSize, 0, ReadDataCallback, buffObj);
                else
                    _sock.BeginReceive(buffObj.RecBuff, 0, (buffObj.PacketSize - buffObj.CompleteBuff.Count), 0, ReadDataCallback, buffObj);
            }
            else
            {
                // full message was received
                var dataBuff = buffObj.CompleteBuff.ToArray();
                var isOperation = BitConverter.ToBoolean(dataBuff, 0);
                int internalId;
                var operationGuid = Guid.Empty;
                var connectionGuid = Guid.Empty;
                if (isOperation)
                {
                    operationGuid = new Guid(dataBuff.Slice(2, 16));
                    connectionGuid = new Guid(dataBuff.Slice(18, 16));
                    internalId = BitConverter.ToInt32(dataBuff, 34);
                }
                else
                    internalId = BitConverter.ToInt32(dataBuff, 2);

                if (_internalPacketTbl.ContainsKey(internalId))
                {
                    var packet = (IDataPacket)Activator.CreateInstance(_internalPacketTbl[internalId]);
                    var ms = new MemoryStream(dataBuff) {Position = 2};
                    packet.DeserializeFrom(ms);

                    if (packet is SynchronizePacket)
                        HandleSynchronizePacket(packet as SynchronizePacket);
                    else if (packet is SequenceInitPacket)
                        HandleSequenceInitPacket(packet as SequenceInitPacket);
                    else if (packet is SequencePacket)
                        HandleSequencePacket(packet as SequencePacket);
                    else if (packet is PeerRelocationRequestPacket)
                        HandleRelocationPacket(packet as PeerRelocationRequestPacket);
                    else if (packet is SocketOperationRequest)
                        HandleSocketOperationPacket(packet as SocketOperationRequest);
                }
                else if (HostChannel.ActiveSocketOperations.ContainsKey(operationGuid))
                {
                    using (var ms = new MemoryStream(dataBuff) { Position = 34 })
                    {
                        var packet = HostChannel.PacketProcessor.ParsePacket(ms); 
                        ms.Position -= 4;
                        packet.DeserializeFrom(ms);
                        if (HostChannel.ActiveSocketOperations[operationGuid].ConnectionGuid == connectionGuid)
                        {
                            HostChannel.ActiveSocketOperations[operationGuid].PacketReceived(packet, HostChannel);
                            HostChannel.ActiveSocketOperations[operationGuid].OnPacketReceived.Raise(this,
                                new OperationPacketEventArgs(packet, HostChannel, 0,
                                    HostChannel.ActiveSocketOperations[operationGuid]));
                        }
                    }
                }
                else
                    OnRawPacketReceived.Raise(this, new RawServerPacketEventArgs(buffObj.CompleteBuff.ToArray(), this));

                buffObj.ReadLen = 0;
                buffObj.CompleteBuff.Clear();
                buffObj.PacketSize = 0;
                _sock.BeginReceive(buffObj.RecBuff, 0, 4, 0, ReadLengthCallback, buffObj);
            }
        }

        internal void SendData(IDataPacket data, IChannel senderChannel, SocketOperationContext operationCtx)
        {
            _netWriter.WritePacketToSocket(data, senderChannel, Identity.Guid, _buffObj, _sock, SendCallback, operationCtx);
        }

        private void SendCallback(IAsyncResult res)
        {
            var sendObj = (SendObject) res.AsyncState;
            var datLen = _sock.EndSend(res);
            OnPacketSent.Raise(null, new ServerPacketEventArgs(sendObj.Packet, this, sendObj.Channel, datLen));
        }

        private void HandleSocketOperationPacket(SocketOperationRequest socketOperationRequest)
        {
            if (socketOperationRequest.Request == SocketOperationRequest.OperationRequest.Finish)
            {
                if (!HostChannel.ActiveSocketOperations.ContainsKey(socketOperationRequest.OperationGuid))
                    //TODO: real exception
                    Debug.Print("Could not locate socket operation");
                HostChannel.ActiveSocketOperations[socketOperationRequest.OperationGuid].Initialize();
            }
        }

        private void HandleSynchronizePacket(SynchronizePacket packet)
        {
            if (!packet.Synced)
            {
                // when peer doesn't have a channel this is needed
                if (!Identity.IsSet)
                    Identity.Guid = packet.Guid;
                var syncPacketOut = new SynchronizePacket
                {
                    Guid = Identity.Guid,
                    Timestamp = _initSyncDelay = DateTime.Now,
                    Synced = false
                };
                SendData(syncPacketOut, null, null);
            }
            else
            {
                PingDelay = (packet.Timestamp - _initSyncDelay).TotalMilliseconds;
                var syncPacketOut = new SynchronizePacket
                {
                    Guid = Identity.Guid,
                    Synced = true
                };
                SendData(syncPacketOut, null, null);
                OnPeerSynchronized.Raise(this, new PeerEventArgs(this));
            }
        }

        private void HandleSequencePacket(ISequencePacket packet)
        {
            if (!_activeSequenceSessions.ContainsKey(packet.SeqGuid))
            {
                //TODO: exception
                Debug.Print("Received sequence packet without an active session");
                return;
            }

            lock (_lockObj)
            {
                _activeSequenceSessions[packet.SeqGuid].Sequence.Add(packet);
                _activeSequenceSessions[packet.SeqGuid].CurrentReceivedSize += packet.SeqSize;
            }
            var seqSession = _activeSequenceSessions[packet.SeqGuid];
            if (seqSession.IsOperation)
            {
                HostChannel.ActiveSocketOperations[seqSession.OperationGuid].SequenceFragmentReceived(
                    new SequenceFragmentInfo(packet.SeqGuid, seqSession.SessionStart,
                        seqSession.InitPacket.FullSequenceSize,
                        packet.SeqSize, seqSession.CurrentReceivedSize, seqSession.InitPacket.PartsCount, packet.SeqIdx));
            }
            else
            {
                OnSequenceFragmentReceived.Raise(this,
                    new SequenceEventArgs(packet.SeqGuid, seqSession.SessionStart,
                        seqSession.InitPacket.FullSequenceSize, packet.SeqSize,
                        seqSession.CurrentReceivedSize,
                        seqSession.InitPacket.PartsCount, packet.SeqIdx, this));
            }
            if (packet.IsLast)
            {
                var fullPacketBuff = SequenceHandler.AssembleSequence(_activeSequenceSessions[packet.SeqGuid].Sequence);
                OnRawPacketReceived.Raise(this, new RawServerPacketEventArgs(fullPacketBuff, this));
                lock (_lockObj)
                    _activeSequenceSessions.Remove(packet.SeqGuid);
            }
        }

        private void HandleSequenceInitPacket(SequenceInitPacket packet)
        {
            var seqSession = new SequenceSession(packet);
            if (packet.IsOperation)
            {
                seqSession.IsOperation = true;
                seqSession.OperationGuid = packet.OperationGuid;
            }
            lock(_lockObj)
                _activeSequenceSessions.Add(packet.SequenceGuid, seqSession);
        }

        private void HandleRelocationPacket(PeerRelocationRequestPacket packet)
        {
            OnPeerRelocationRequest.Raise(this, new PeerRelocationEventArgs(packet.Operation, this, packet.ChannelId, packet.PeerGuid));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            if (disposing)
            {
                _sock.Close();
            }
            IsDisposed = true;
        }
    }
        #endregion
}
