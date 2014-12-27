using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    internal class PeerRelocationRequestPacket : IDataPacket
    {
        public enum RelocateOperation : byte
        {
            Join = 0x1,
            Leave = 0x2,
            AcceptRequest = 0x3
        }

        public int PacketId { get { return -9999; } }
        public Guid PeerGuid { get; set; }
        public int ChannelId { get; set; }
        public byte[] ChannelKey { get; set; }
        public RelocateOperation Operation { get; set; }
        public int Port { get; set; }

        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write(PeerGuid.ToByteArray());
            bw.Write(ChannelId);
            bw.Write((byte)Operation);
            bw.Write(Port);
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            br.ReadInt32();
            PeerGuid = new Guid(br.ReadBytes(16));
            ChannelId = br.ReadInt32();
            Operation = (RelocateOperation) br.ReadByte();
            Port = br.ReadInt32();
        }
    }
}
