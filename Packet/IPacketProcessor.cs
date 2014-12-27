using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace uNet2.Packet
{
    public interface IPacketProcessor
    {
        /// <summary>
        /// This method is called when a raw packet is received from a peer.
        /// Use this method to decrypt/decompress a raw packet
        /// </summary>
        /// <param name="rawData">The raw packet data</param>
        /// <returns>Returns the processed data</returns>
        byte[] ProcessRawData(byte[] rawData);
        IDataPacket ParsePacket(Stream data);
    }
}
