using System;
using System.IO;

namespace uNet2.Packet
{
    internal class SequenceInitPacket : IDataPacket
    {
        public int PacketId { get { return -9998; } }
        public int PartsCount { get; set; }
        public int FullSequenceSize { get; set; }
        public Guid SequenceGuid { get; set; }
        public bool IsOperation { get; set; }
        public Guid OperationGuid { get; set; }

        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write(FullSequenceSize);
            bw.Write(PartsCount);
            bw.Write(SequenceGuid.ToByteArray());
            bw.Write(IsOperation);
            bw.Write(OperationGuid.ToByteArray());
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            br.ReadInt32();
            FullSequenceSize = br.ReadInt32();
            PartsCount = br.ReadInt32();
            SequenceGuid = new Guid(br.ReadBytes(16));
            IsOperation = br.ReadBoolean();
            if(IsOperation)
                OperationGuid = new Guid(br.ReadBytes(16));
        }
    }
}
