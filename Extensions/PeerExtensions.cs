using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uNet2.Channel;

namespace uNet2.Extensions
{
    public static class PeerExtensions
    {
        public static void AddToChannel(this Peer.Peer peer, IServerChannel targetChannel)
        {
            peer.HostChannel.HostServer.AddPeerToChannel(targetChannel, peer.Identity);
        }
    }
}
