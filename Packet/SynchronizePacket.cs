using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    internal class SynchronizePacket : IDataPacket
    {
        public int PacketId { get { return -10000; } }
        public Guid Guid { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Synced { get; set; }

        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write(Guid.ToByteArray());
            bw.Write(BitConverter.GetBytes(Timestamp.ToBinary()));
            bw.Write(Synced);
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            br.ReadInt32();
            Guid = new Guid(br.ReadBytes(16));
            Timestamp = DateTime.FromBinary(br.ReadInt64());
            Synced = br.ReadBoolean();
        }
    }
}
