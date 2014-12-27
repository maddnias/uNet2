using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uNet2.Packet;

namespace uNet2.Peer.Events
{
    internal class PeerRelocationEventArgs : EventArgs
    {
        public PeerRelocationRequestPacket.RelocateOperation Operation { get; set; }
        public Peer Peer { get; set; }
        public Guid PeerGuid { get; set; }
        public int TargetChannelId { get; set; }

        public PeerRelocationEventArgs(PeerRelocationRequestPacket.RelocateOperation operation, Peer peer,
            int targetChannelId, Guid peerGuid)
        {
            Operation = operation;
            Peer = peer;
            TargetChannelId = targetChannelId;
            PeerGuid = peerGuid;
        }
    }
}
