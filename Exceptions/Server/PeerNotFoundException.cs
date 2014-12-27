using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uNet2.Exceptions.Server
{
    public class PeerNotFoundException : ServerException
    {
        public PeerNotFoundException()
        {
        }

        public PeerNotFoundException(string message) 
            : base(message)
        {
        }

        public PeerNotFoundException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
