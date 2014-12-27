using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Packet.Events
{
    public class SequenceEventArgs : EventArgs
    {
        public Guid SequenceGuid { get; set; }
        public DateTime SessionStart { get; set; }
        public int ExpectedCompleteSize { get; set; }
        public int CurrentSequenceSize { get; set; }
        public int CurrentReceivedSize { get; set; }
        public int SequenceFragmentCount { get; set; }
        public int CurrentFragmentIndex { get; set; }
        public Peer.Peer Peer { get; set; }

        public SequenceEventArgs(Guid sequenceGuid, DateTime sessionStart, int expectedCompleteSize, int currentSequenceSize,
            int currentReceivedSize, int sequencePartCount, int currentFragmentIndex, Peer.Peer peer)
        {
            SequenceGuid = sequenceGuid;
            SessionStart = sessionStart;
            ExpectedCompleteSize = expectedCompleteSize;
            CurrentSequenceSize = currentSequenceSize;
            CurrentReceivedSize = currentReceivedSize;
            SequenceFragmentCount = sequencePartCount;
            CurrentFragmentIndex = currentFragmentIndex;
            Peer = peer;
        }
    }
}
