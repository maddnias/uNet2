using System;
using System.Runtime.Serialization;

namespace uNet2.Exceptions.Channel
{
    public class ChannelException : Exception, ISerializable
    {
        public ChannelException()
        {
        }

        public ChannelException(string message) 
            : base(message)
        {
        }

        public ChannelException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
