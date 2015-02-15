using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Security
{
    public enum PacketIntegrityHash : byte
    {
        Sha256 = 0x0,
        Crc32 = 0x1,
        Elf32 = 0x2
    }
}
