using System;
using System.Runtime.Serialization;

namespace Colosoft.DataServices
{
    [Serializable]
    public class RemoteServiceException : Exception
    {
        public ErrorMessage? ErrorMessage { get; }

        private static string GetMessage(ErrorMessage errorMessage) =>
            errorMessage.Message!;

        public RemoteServiceException(ErrorMessage? errorMessage)
            : base(
                  GetMessage(errorMessage ?? throw new ArgumentNullException(nameof(errorMessage))),
                  errorMessage.Inner != null ? new RemoteServiceException(errorMessage.Inner) : null)
        {
            this.ErrorMessage = errorMessage;
        }

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
