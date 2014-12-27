using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace uNet2.Exceptions.SocketOperation
{
    class SocketOperationException : Exception, ISerializable
    {
        public SocketOperationException()
        {
        }

        public SocketOperationException(string message)
            : base(message)
        {
        }

        public SocketOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
