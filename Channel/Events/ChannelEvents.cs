namespace uNet2.Channel.Events
{
    public static class ChannelEvents
    {
        public delegate void OnChannelCreated(object sender, ChannelEventArgs e);
        public delegate void OnChannelDisposed(object sender, ChannelEventArgs e);
        public delegate void OnPeerConnected(object sender, ChannelEventArgs e);
        public delegate void OnPeerDisconnected(object sender, ChannelEventArgs e);

        public static void Raise(this OnChannelCreated @event, object sender, ChannelEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnChannelDisposed @event, object sender, ChannelEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnPeerConnected @event, object sender, ChannelEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnPeerDisconnected @event, object sender, ChannelEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }
    }
}
