using System;

namespace nPreCredit.Code.Services
{
    public class ServiceException : Exception
    {
        public ServiceException() : base()
        {
        }

        public ServiceException(string message) : base(message)
        {
        }

        public ServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ServiceException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        public bool IsUserSafeException { get; set; }
        public string ErrorCode { get; set; }
    }
}