using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Exceptions.SocketOperation
{
    class SocketOperationNotInitializedException : SocketOperationException
    {
        public SocketOperationNotInitializedException()
        {
        }

        public SocketOperationNotInitializedException(string message)
            : base(message)
        {
        }

        public SocketOperationNotInitializedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
