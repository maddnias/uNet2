using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace uNet2.Utils
{
    public static class FastBuffer
    {
        private unsafe delegate void memcpyimpl(byte* src, byte* dest, int len);
        private static readonly memcpyimpl _memcpyimpl = (memcpyimpl)Delegate.CreateDelegate(
                typeof(memcpyimpl), typeof(Buffer).GetMethod("memcpyimpl",
                    BindingFlags.Static | BindingFlags.NonPublic));

        public unsafe static void MemCpy(byte[] src, int srcIndex, byte[] dest, int destIndex, int count)
        {
            fixed (byte* pSrc = src, pDest = dest)
                _memcpyimpl(pSrc + srcIndex, pDest + destIndex, count);
        }

        public unsafe static byte[] SliceBuffer(byte[] buff, int startIndex, int count)
        {
            var outBuff = new byte[count];
            fixed (byte* pSrc = buff, pDest = outBuff)
                _memcpyimpl(pSrc + startIndex, pDest, count);
            return outBuff;
        }
    }
}
