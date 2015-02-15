using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace uNet2.Security
{
    public static class Sha256Helper
    {
        public static byte[] GetHash(byte[] data)
        {
            return new SHA256Managed().ComputeHash(data);
        }

        public static byte[] GetHash(string data)
        {
            var buff = Encoding.UTF8.GetBytes(data);
            return new SHA256Managed().ComputeHash(buff);
        }
    }
}
