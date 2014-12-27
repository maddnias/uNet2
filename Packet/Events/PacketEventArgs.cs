using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uNet2.Channel;

namespace uNet2.Packet.Events
{
    public class ServerPacketEventArgs : EventArgs
    {
        public IDataPacket Packet { get; set; }
        public Peer.Peer Peer { get; set; }
        public IChannel Channel { get; set; }
        public int RawSize { get; set; }

        public ServerPacketEventArgs(IDataPacket packet, Peer.Peer peer, IChannel channel, int rawSize)
        {
            Packet = packet;
            Peer = peer;
            Channel = channel;
            RawSize = rawSize;
        }
    }

    public class ClientPacketEventArgs : EventArgs
    {
        public IDataPacket Packet { get; set; }
        public IChannel Channel { get; set; }
        public int RawSize { get; set; }

        public ClientPacketEventArgs(IDataPacket packet, IChannel channel, int rawSize)
        {
            Packet = packet;
            Channel = channel;
            RawSize = rawSize;
        }
    }

    internal class RawServerPacketEventArgs : EventArgs
    {
        public byte[] RawData { get; set; }
        public Peer.Peer Peer { get; set; }

        public RawServerPacketEventArgs(byte[] rawData, Peer.Peer peer)
        {
            RawData = rawData;
            Peer = peer;
        }
    }

    public class RawClientPacketEventArgs : EventArgs
    {
        public byte[] RawData { get; set; }
        public IClientChannel Channel { get; set; }

        public RawClientPacketEventArgs(byte[] rawData, IClientChannel channel)
        {
            RawData = rawData;
            Channel = channel;
        }
    }
}
