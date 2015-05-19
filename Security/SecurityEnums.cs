using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Security
{
    public enum PacketIntegrityHash : byte
    {
        None = 0x0,
        Sha256 = 0x1,
        Crc32 = 0x2,
        Elf32 = 0x4
    }
}
