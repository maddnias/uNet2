using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Peer.Events
{
    public sealed class PeerEventArgs : EventArgs
    {
        public int Id { get; set; }
        public Peer Peer { get; set; }

        public PeerEventArgs(int id)
        {
            Id = id;
        }

        public PeerEventArgs(Peer peer)
        {
            Peer = peer;
            Id = peer.Identity.Id;
        }

        public PeerEventArgs(int id, Peer peer)
        {
            Id = id;
            Peer = peer;
        }
    }
}
