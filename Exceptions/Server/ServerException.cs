using System;
using System.Runtime.Serialization;

namespace uNet2.Exceptions.Server
{
    public class ServerException : Exception, ISerializable
    {
        public ServerException()
        {
        }

        public ServerException(string message) 
            : base(message)
        {
        }

        public ServerException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
