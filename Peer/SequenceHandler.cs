using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using uNet2.Extensions;
using uNet2.Network;
using uNet2.Packet;
using uNet2.Utils;

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
        public ISequencePacket[] CreateSequence(IDataPacket packet, byte[] completeBuff, int buffSize,
            out SequenceInitPacket initPacket, out Guid seqGuid)
        {
            //  if (!IsSequenceRequired(buffSize, completeBuff.Length))
            //TODO: do proper shit..
            // throw new Exception();

            var modRest = completeBuff.Length%(buffSize-33);
            var outList = CreateSequence(completeBuff, buffSize - 33, modRest, completeBuff.Length).ToArray();
            var curSeqGuid = seqGuid = SocketIdentity.GenerateGuid();
            initPacket = new SequenceInitPacket
            {
                PartsCount = outList.Length,
                SequenceGuid = seqGuid,
                FullSequenceSize = completeBuff.Length
            };
            for (var i = 0; i < outList.Length; i++)
                outList[i].SeqGuid = curSeqGuid;
            return outList.ToArray();
        }

        public static byte[] AssembleSequence(IList<ISequencePacket> packets)
        {
            var fullBuff = new byte[packets.Sum(p => p.SeqSize) - 4];

            Debug.Assert(packets.OrderBy(p => p.SeqIdx).ToList().SequenceEqual(packets));

            var offset = 0;
            foreach (var packet in packets)
            {
                Buffer.BlockCopy(packet.SeqBuffer, (packet.SeqIdx == 0 ? 4 : 0), fullBuff, offset,
                    (packet.IsLast ? packet.SeqSize - 4 : (packet.SeqIdx == 0 ? packet.SeqSize - 4 : packet.SeqSize)));
                offset += packet.SeqSize;
            }
            return fullBuff;
        }

        private static IEnumerable<ISequencePacket> CreateSequence(byte[] newBuff, int buffSize, int modRest, int origSize)
        {
            var idx = 0;
            for (var i = 0; i < newBuff.Length - modRest; i += buffSize, idx++)
            {
                var seqPacket = new SequencePacket
                {
                    SeqIdx = (i/buffSize),
                    SeqBuffer = FastBuffer.SliceBuffer(newBuff, i, buffSize),
                    SeqSize = buffSize,
                    IsLast = false
                };
                yield return seqPacket;
            }
            yield return new SequencePacket
            {
                SeqIdx = idx,
                SeqBuffer = FastBuffer.SliceBuffer(newBuff, idx*buffSize, modRest),
                SeqSize = modRest,
                IsLast = true
            };
        }

    }
}
