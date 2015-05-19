using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Peer.Events
{
    public static class PeerEvents
    {
        internal delegate void OnPeerConnected(object sender, PeerEventArgs e);
        internal delegate void OnPeerDataReceived(object sender, PeerEventArgs e);
        internal delegate void OnPeerSynchronized(object sender, PeerEventArgs e);
        public delegate void OnPeerDisconnected(object sender, PeerEventArgs e);
        internal delegate void OnSocketOperationCreated(object sender, PeerEventArgs e);
        internal delegate void OnPeerRelocationRequest(object sender, PeerRelocationEventArgs e);

        internal static void Raise(this OnPeerConnected @event, object sender, PeerEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnPeerDataReceived @event, object sender, PeerEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnPeerSynchronized @event, object sender, PeerEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnPeerDisconnected @event, object sender, PeerEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnPeerRelocationRequest @event, object sender, PeerRelocationEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnSocketOperationCreated @event, object sender, PeerEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }
    }
}
