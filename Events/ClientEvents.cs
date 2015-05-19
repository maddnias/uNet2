using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Events
{
    public static class ClientEvents
    {
        public delegate void OnClientConnected(object sender, ClientEventArgs e);

        public static void Raise(this OnClientConnected @event, object sender, ClientEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }
    }
}
