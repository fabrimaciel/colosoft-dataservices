using System;
using System.Runtime.Serialization;

namespace Colosoft.DataServices
{
    [Serializable]
    public class RemoteServiceException : Exception
    {
        public RemoteServiceException()
        {
        }

        public RemoteServiceException(string? message)
            : base(message)
        {
        }

        public RemoteServiceException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected RemoteServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
