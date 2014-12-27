using System;

namespace uNet2.Exceptions.Server
{
    public class ServerInitializationException : ServerException
    {
        public ServerInitializationException()
        {
        }

        public ServerInitializationException(string message)
            : base(message)
        {
        }

        public ServerInitializationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
