using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Packet.Events
{
    public static class PacketEvents
    {
        public delegate void OnServerPacketReceived(object sender, ServerPacketEventArgs e);
        public delegate void OnClientPacketReceived(object sender, ClientPacketEventArgs e);
        public delegate void OnSequenceFragmentReceived(object sender, SequenceEventArgs e);
        public delegate void OnPacketSent(object sender, ServerPacketEventArgs e);
        internal delegate void OnRawServerPacketReceived(object sender, RawServerPacketEventArgs e);

        public delegate void OnRawClientPacketReceived(object sender, RawClientPacketEventArgs e);

        internal static void Raise(this OnRawServerPacketReceived @event, object sender, RawServerPacketEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        internal static void Raise(this OnRawClientPacketReceived @event, object sender, RawClientPacketEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnServerPacketReceived @event, object sender, ServerPacketEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnClientPacketReceived @event, object sender, ClientPacketEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnPacketSent @event, object sender, ServerPacketEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        public static void Raise(this OnSequenceFragmentReceived @event, object sender, SequenceEventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }
    }
}
