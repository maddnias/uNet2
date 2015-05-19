using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uNet2.Channel;

namespace uNet2.Events
{
    public class ClientEventArgs : EventArgs
    {
        public UNetClient Client { get; set; }
        public IChannel Channel { get; set; }

        public ClientEventArgs(UNetClient client, IChannel channel)
        {
            Client = client;
            Channel = channel;
        }
    }
}
