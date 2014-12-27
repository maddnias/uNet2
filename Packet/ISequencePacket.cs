using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    public interface ISequencePacket : IDataPacket
    {
        int SeqIdx { get; set; }
        bool IsLast { get; set; }
        int SeqSize { get; set; }
        byte[] SeqBuffer { get; set; }
        Guid SeqGuid { get; set; }
    }
}
