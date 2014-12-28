using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using uNet2.Extensions;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Packet.Events;
using uNet2.Peer;
using uNet2.SocketOperation;

namespace uNet2.Channel
{
    public class TcpClientChannel : IClientChannel
    {
        public event PacketEvents.OnClientPacketReceived OnPacketReceived;
        public event PacketEvents.OnPacketSent OnPacketSent;

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
        /// This channel's manager
        /// </summary>
        public ChannelManager Manager { get; set; }
        public UNetClient Client { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public Dictionary<int, Type> OperationTable { get; set; }
        public bool IsSynchronized { get; set; }

        public bool IsMainChannel { get; set; }

        public bool IsProtected { get; set; }
        public byte[] ChannelPublicKey { get; set; }
        public IPacketProcessor PacketProcessor { get; set; }

        public Dictionary<Guid, ISocketOperation> ActiveSocketOperations { get; set; }

        private readonly int _bufferSize;
        private readonly Dictionary<Guid, List<ISequencePacket>> _activeSequenceSessions;
        private readonly AutoResetEvent _synchronizeHandle;
        private readonly Dictionary<int, Type> _internalPacketTbl = new Dictionary<int, Type>
        {
            {-10003, typeof(SocketOperationRequest)},
            {-10001, typeof(SequencePacket)},
            {-10000, typeof (SynchronizePacket)},
            {-9999, typeof(PeerRelocationRequestPacket)},
            {-9998, typeof(SequenceInitPacket)},
        };
        private readonly BufferObject _buffObj;
        private readonly NetworkWriter _netWriter;
        private readonly object _lockObj;

        public TcpClientChannel()
        {
            ChannelSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _synchronizeHandle = new AutoResetEvent(false);
            _buffObj = new BufferObject(_bufferSize = 8192);
            _netWriter = new NetworkWriter();
            ActiveSocketOperations = new Dictionary<Guid, ISocketOperation>();
            OperationTable = new Dictionary<int, Type>();
            _lockObj = new object();
        }

        public TcpClientChannel(UNetClient client)
        {
            ChannelSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _synchronizeHandle = new AutoResetEvent(false);
            _buffObj = new BufferObject(_bufferSize = 8192);
            Client = client;
            _netWriter = new NetworkWriter();
            ActiveSocketOperations = new Dictionary<Guid, ISocketOperation>();
            OperationTable = new Dictionary<int, Type>();
            _lockObj = new object();
        }

        public bool ConnectToChannel(string addr, int port)
        {
            EndPoint = new IPEndPoint(IPAddress.Parse(addr), port);
            _synchronizeHandle.Reset();
            ChannelSocket.BeginConnect(addr, port, ConnectCallback, null);
            _synchronizeHandle.WaitOne();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return ChannelSocket.Connected && Client.Identity.Guid != null;
        }

        private void ConnectCallback(IAsyncResult res)
        {
            ChannelSocket.EndConnect(res);
            Receive();

            var syncPacket = new SynchronizePacket()
            {
                Guid = Client.Identity.Guid
            };

            Send(syncPacket);
        }

        internal void OperationSend(IDataPacket data, Guid operationGuid)
        {
            _netWriter.WritePacketToSocket(data, this, Client.Identity.Guid, _buffObj, ChannelSocket, SendCallback,
                new SocketOperationContext(operationGuid));
        }

        public void Send(IDataPacket data)
        {
            _netWriter.WritePacketToSocket(data, null, Client.Identity.Guid, _buffObj, ChannelSocket, SendCallback, null);
        }

        public void SendSequence(SequenceContext seqCtx)
        {
            _netWriter.WriteSequenceToSocket(seqCtx, null, Client.Identity.Guid, _buffObj, ChannelSocket, SendCallback);
        }

        internal void OperationSend(IDataPacket data, Guid peerGuid, Guid operationGuid)
        {
            _netWriter.WritePacketToSocket(data, this, peerGuid, _buffObj, ChannelSocket, SendCallback,
                new SocketOperationContext(operationGuid));
        }

        private void SendCallback(IAsyncResult res)
        {
            var sent = ChannelSocket.EndSend(res);
            Debug.Print(sent.ToString());
        }

        private void Receive()
        {
            var buffObj = new BufferObject(_bufferSize);
            ChannelSocket.BeginReceive(buffObj.RecBuff, 0, 4, 0, ReadLengthCallback, buffObj);
        }

        private void ReadLengthCallback(IAsyncResult res)
        {
            try
            {
                var buffObj = (BufferObject) res.AsyncState;
                var readLen = ChannelSocket.EndReceive(res);

                if (readLen >= 4)
                {
                    buffObj.PacketSize = BitConverter.ToInt32(buffObj.RecBuff, 0);
                    if (buffObj.PacketSize < buffObj.BufferSize)
                        ChannelSocket.BeginReceive(buffObj.RecBuff, 0, buffObj.PacketSize, 0, ReadDataCallback, buffObj);
                    else
                        ChannelSocket.BeginReceive(buffObj.RecBuff, 0, buffObj.BufferSize, 0, ReadDataCallback, buffObj);
                }
            }
            catch
            {
                
            }
        }

        private void ReadDataCallback(IAsyncResult res)
        {
            var buffObj = (BufferObject) res.AsyncState;
            buffObj.ReadLen = ChannelSocket.EndReceive(res);
            buffObj.TotalRead += buffObj.ReadLen;

            buffObj.CompleteBuff.AddRange(buffObj.RecBuff.FastSlice(0, buffObj.ReadLen));
            if (buffObj.CompleteBuff.Count < buffObj.PacketSize)
            {
                // keep reading
                if (buffObj.BufferSize < (buffObj.PacketSize - buffObj.CompleteBuff.Count))
                    ChannelSocket.BeginReceive(buffObj.RecBuff, 0, buffObj.BufferSize, 0, ReadDataCallback, buffObj);
                else
                    ChannelSocket.BeginReceive(buffObj.RecBuff, 0, (buffObj.PacketSize - buffObj.CompleteBuff.Count),
                        0, ReadDataCallback, buffObj);
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
                    operationGuid = new Guid(dataBuff.Slice(1, 16));
                    Debug.Print("Received operation guid: " + operationGuid);
                    connectionGuid = new Guid(dataBuff.Slice(17, 16));
                    internalId = BitConverter.ToInt32(dataBuff, 33);
                }
                else
                    internalId = BitConverter.ToInt32(dataBuff, 1);

                if (_internalPacketTbl.ContainsKey(internalId))
                {
                    var packet = (IDataPacket) Activator.CreateInstance(_internalPacketTbl[internalId]);
                    var ms = new MemoryStream(dataBuff) {Position = 1};
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
                else if (ActiveSocketOperations.ContainsKey(operationGuid))
                {
                    using (var ms = new MemoryStream(dataBuff) {Position = 33})
                    {
                        var packet = PacketProcessor.ParsePacket(ms);
                        packet.DeserializeFrom(ms);
                        ActiveSocketOperations[operationGuid].PacketReceived(packet, this);
                    }
                }
                else
                    RawPacketReceived(dataBuff);
                buffObj.ReadLen = 0;
                buffObj.CompleteBuff.Clear();
                buffObj.PacketSize = 0;
                ChannelSocket.BeginReceive(buffObj.RecBuff, 0, 4, 0, ReadLengthCallback, buffObj);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void HandleSocketOperationPacket(SocketOperationRequest socketOperationRequest)
        {
            if (socketOperationRequest.Request == SocketOperationRequest.OperationRequest.Create)
            {
                var operation =
                    (ISocketOperation) Activator.CreateInstance(OperationTable[socketOperationRequest.OperationId]);
                operation.OperationGuid = socketOperationRequest.OperationGuid;
                operation.HostChannel = this;
                operation.ConnectionGuid = Client.Identity.Guid;
                ActiveSocketOperations.Add(socketOperationRequest.OperationGuid, operation);
                var operationRequest = new SocketOperationRequest
                {
                    OperationGuid = socketOperationRequest.OperationGuid,
                    Request = SocketOperationRequest.OperationRequest.Finish
                };
                Send(operationRequest);
                Debug.Print("New SocketOperation created with GUID: " + socketOperationRequest.OperationGuid);
            }
        }

        private void HandleSynchronizePacket(SynchronizePacket packet)
        {
            if (!packet.Synced)
            {
                Client.Identity.Guid = packet.Guid;
                var syncPacket = new SynchronizePacket
                {
                    Guid = Client.Identity.Guid,
                    Timestamp = DateTime.Now,
                    Synced = true
                };
                Send(syncPacket);
            }
            else
            {
                IsSynchronized = true;
                _synchronizeHandle.Set();
            }
        }

        private void RawPacketReceived(byte[] data)
        {
            IDataPacket parsedPacket;
            using (var ms = new MemoryStream(data) { Position = 1})
            {
                parsedPacket = PacketProcessor.ParsePacket(ms);
            }
            OnPacketReceived.Raise(this, new ClientPacketEventArgs(parsedPacket, this, data.Length));
        }

        private void HandleRelocationPacket(PeerRelocationRequestPacket packet)
        {
            if (packet.PeerGuid != Client.Identity.Guid)
                return;
            var newChannel = Client.CreateChannel<TcpClientChannel>();
            newChannel.Client = Client;
            newChannel.IsSynchronized = true;
            foreach (var op in OperationTable)
                newChannel.OperationTable.Add(op.Key, op.Value);
            newChannel.PacketProcessor = PacketProcessor;
            newChannel.ConnectToChannel(EndPoint.Address.ToString(), packet.Port);
            newChannel.OnPacketReceived += OnPacketReceived;
            newChannel.OnPacketSent += OnPacketSent;
            Client.ActiveChannels.Add(newChannel);

            newChannel.Send(new PeerRelocationRequestPacket
            {
                ChannelId = packet.ChannelId,
                PeerGuid = Client.Identity.Guid,
                Operation = PeerRelocationRequestPacket.RelocateOperation.AcceptRequest
            });
        }

        private void HandleSequencePacket(ISequencePacket packet)
        {
            if (!_activeSequenceSessions.ContainsKey(packet.SeqGuid))
            {
                Debug.Print("Received sequence packet without a session");
                return;
            }

            _activeSequenceSessions[packet.SeqGuid].Add(packet);
        }

        private void HandleSequenceInitPacket(SequenceInitPacket packet)
        {
            _activeSequenceSessions.Add(packet.SequenceGuid, new List<ISequencePacket>());
        }

        public T CreateOperation<T>() where T : ISocketOperation
        {
            var operation = (ISocketOperation)Activator.CreateInstance<T>();
            operation.HostChannel = this;
            return (T)operation;
        }

        public T RegisterOperation<T>(Guid connectionGuid) where T : ISocketOperation
        {
            throw new NotImplementedException();
        }

        public void RegisterOperation(ISocketOperation operation)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

    }
}
