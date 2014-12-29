using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using uNet2.Extensions;
using uNet2.Utils;

namespace uNet2.Packet
{
    internal class SequencePacket : ISequencePacket
    {
        public int PacketId { get { return -10001; } }
        public int SeqIdx { get; set; }
        public bool IsLast { get; set; }
        public int SeqSize { get; set; }
        public byte[] SeqBuffer { get; set; }
        public Guid SeqGuid { get; set; }

        public void SerializeTo(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            bw.Write(PacketId);
            bw.Write(SeqGuid.ToByteArray());
            bw.Write(SeqIdx);
            bw.Write(IsLast);
            bw.Write(SeqSize);
            bw.Write(FastBuffer.SliceBuffer(SeqBuffer, 0, SeqSize));
        }

        public void DeserializeFrom(Stream stream)
        {
            var br = new BinaryReader(stream);
            br.ReadInt32();
            SeqGuid = new Guid(br.ReadBytes(16));
            SeqIdx = br.ReadInt32();
            IsLast = br.ReadBoolean();
            SeqSize = br.ReadInt32();
            SeqBuffer = br.ReadBytes(SeqSize);
        }
    }
}
