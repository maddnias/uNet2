using System;

namespace uNet2.Channel
{
    public class PendingPeerConnection
    {
        public Guid Guid { get; set; }
        public Peer.Peer Peer { get; set; }
        public int TimeoutPeriod { get; set; }
        internal bool IsCancelled { get; set; }
        internal DateTime ConnectionTimestamp { get; set; }

        public PendingPeerConnection(Guid guid, Peer.Peer peer)
        {
            Guid = guid;
            Peer = peer;
        }

        public PendingPeerConnection(Guid guid, Peer.Peer peer, int timeoutPeriod, DateTime connectionTimestamp)
        {
            Guid = guid;
            Peer = peer;
            TimeoutPeriod = timeoutPeriod;
            ConnectionTimestamp = connectionTimestamp;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }
}
