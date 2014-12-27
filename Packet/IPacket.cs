using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    public interface IPacket
    {
        /// <summary>
        /// The ID of this packet
        /// </summary>
        /// <remarks>
        /// Only use positive integers as ID
        /// </remarks>
        int PacketId { get; }
    }
}
