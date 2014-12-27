using System;

namespace uNet2.Exceptions.Channel
{
    public class ChannelOperationException : ChannelException
    {
        public ChannelOperationException()
        {
        }

        public ChannelOperationException(string message)
            : base(message)
        {
        }

        public ChannelOperationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
