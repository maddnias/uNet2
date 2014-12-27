using System;
using System.IO;

namespace uNet2.Packet
{
    public interface IDataPacket : IPacket
    {
        /// <summary>
        /// Before a packet is sent this method is called to serialize the packet
        /// </summary>
        /// <param name="stream">The output stream</param>
        void SerializeTo(Stream stream);
        /// <summary>
        /// When a packet is received this method is called to deserialize the packet
        /// </summary>
        /// <param name="stream">The input stream</param>
        void DeserializeFrom(Stream stream);
    }
}
