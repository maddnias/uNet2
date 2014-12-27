using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Peer;

namespace uNet2
{
    public class SequenceContext
    {
        public Guid SequenceGuid { get; set; }
        internal SequenceInitPacket InitPacket { get; set; }
        internal List<ISequencePacket> SequencePackets { get; set; }
        internal byte[] InitPacketBuffer { get; set; }
        internal List<byte[]> SequencePacketBuffers { get; set; }

        internal SequenceContext(byte[] initPacketBuffer, List<byte[]> sequencePacketBuffers)
        {
            InitPacketBuffer = initPacketBuffer;
            SequencePacketBuffers = sequencePacketBuffers;
        }

        internal SequenceContext(SequenceInitPacket initPacket, List<ISequencePacket> sequencePackets,
            byte[] initPacketBuffer, List<byte[]> sequencePacketBuffers)
        {
            InitPacket = initPacket;
            SequencePackets = sequencePackets;
            InitPacketBuffer = initPacketBuffer;
            SequencePacketBuffers = sequencePacketBuffers;
        }

        public static SequenceContext CreateFromPacket(IDataPacket data, int fragmentSize)
        {
            SequenceInitPacket initPacket;
            var ms = new MemoryStream();
            data.SerializeTo(ms);
            NetworkWriter.PrependStreamSize(ms);
            var completeBuff = ms.ToArray();

            Guid seqGuid;
            var sequence = new SequenceHandler().CreateSequence(data, completeBuff, fragmentSize, out initPacket, out seqGuid).ToList();
            var initPacketStream = new MemoryStream();
            var sequenceStreams = new MemoryStream[sequence.Count];
            var sequenceBuffs = new List<byte[]>();

            initPacket.SerializeTo(initPacketStream);

#if DEBUG
            for (var i = 0; i < sequence.Count; i++)
            {
                var seq = sequence[i];
                sequenceStreams[i] = new MemoryStream();
                seq.SerializeTo(sequenceStreams[i]);
                sequenceBuffs.Add(sequenceStreams[i].ToArray());
            }
#endif
            var seqCtx = new SequenceContext(initPacket, sequence, initPacketStream.ToArray(), sequenceBuffs)
            {
                SequenceGuid = seqGuid
            };
            return seqCtx;
        }
    }
}
