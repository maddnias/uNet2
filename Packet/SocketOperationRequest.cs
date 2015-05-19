using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    internal class SocketOperationRequest : IDataPacket
    {
        public enum OperationRequest : byte
        {
            Create = 0x0,
            Finish = 0x1,
            Close = 0x2
        }

        public OperationRequest Request { get; set; }
        public int OperationId { get; set; }
        public Guid OperationGuid { get; set; }
        public int PacketId { get { return -10003; } }

        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write((byte) Request);
            bw.Write(OperationId);
            bw.Write(OperationGuid.ToByteArray());
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            br.ReadInt32();
            Request = (OperationRequest) br.ReadByte();
            OperationId = br.ReadInt32();
            OperationGuid = new Guid(br.ReadBytes(16));
        }
    }
}
