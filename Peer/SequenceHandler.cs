using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using uNet2.Extensions;
using uNet2.Network;
using uNet2.Packet;

namespace uNet2.Peer
{
    internal class SequenceHandler
    {
        private Peer _peer;

        public SequenceHandler()
        {
            
        }

        public SequenceHandler(Peer peer)
        {
            _peer = peer;
        }

        public static bool IsSequenceRequired(int buffSize, int packetSize)
        {
            return buffSize < packetSize;
        }

        //TODO: optimize this
        public List<ISequencePacket> CreateSequence(IDataPacket packet, byte[] completeBuff, int buffSize,
            out SequenceInitPacket initPacket, out Guid seqGuid)
        {
            //  if (!IsSequenceRequired(buffSize, completeBuff.Length))
            //TODO: do proper shit..
            // throw new Exception();

            var padCounter = 0;
            // -29 for SequencePacket overhead size
            while ((completeBuff.Length + padCounter)%(buffSize-33) != 0)
                padCounter++;

            var newSize = completeBuff.Length + padCounter;
            var newBuff = new byte[newSize];
            Buffer.BlockCopy(completeBuff, 4, newBuff, 0, completeBuff.Length -4);

            var outList = CreateSequence(newBuff, (buffSize-33), completeBuff.Length).ToList();
            var curSeqGuid = seqGuid = SocketIdentity.GenerateGuid();
            initPacket = new SequenceInitPacket
            {
                PartsCount = outList.Count,
                SequenceGuid = seqGuid,
                FullSequenceSize = completeBuff.Length
            };
            outList.ForEach(seq => seq.SeqGuid = curSeqGuid);
            return outList;
        }

        public static byte[] AssembleSequence(IList<ISequencePacket> packets)
        {
            var fullBuff = new byte[packets.Sum(p => p.SeqSize) - 4];
            packets = packets.OrderBy(p => p.SeqIdx).ToList();
            var offset = 0;
            foreach (var packet in packets)
            {
                Buffer.BlockCopy(packet.SeqBuffer, 0, fullBuff, offset,
                    (packet.IsLast ? packet.SeqSize - 4 : packet.SeqSize));
                offset += packet.SeqSize;
            }
            return fullBuff;
        }

        private static IEnumerable<ISequencePacket> CreateSequence(byte[] newBuff, int buffSize, int origSize)
        {
            for (var i = 0; i < newBuff.Length; i += buffSize)
            {
                var seqPacket = new SequencePacket
                {
                    SeqIdx = (i/buffSize),
                    SeqBuffer = newBuff.Slice(i, buffSize),
                    IsLast = i == newBuff.Length - buffSize
                };
                if (!seqPacket.IsLast)
                    seqPacket.SeqSize = buffSize;
                else
                    seqPacket.SeqSize = origSize - i;
                yield return seqPacket;
            }
        }

    }
}
