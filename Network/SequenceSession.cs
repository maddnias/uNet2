using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uNet2.Packet;

namespace uNet2.Network
{
    internal class SequenceSession
    {
        public Guid Guid { get; set; }
        public SequenceInitPacket InitPacket { get; set; }
        public List<ISequencePacket> Sequence { get; set; }
        public int CurrentReceivedSize { get; set; }
        public DateTime SessionStart { get; set; }
        public bool IsCancelled { get; set; }

        public SequenceSession()
        {
            Sequence = new List<ISequencePacket>();
        }

        public SequenceSession(SequenceInitPacket initPacket)
        {
            InitPacket = initPacket;
            Sequence = new List<ISequencePacket>();
            SessionStart = DateTime.Now;
        }
    }
}
