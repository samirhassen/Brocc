using System;
using System.Runtime.Serialization;

namespace nPreCredit.Code
{
    public class SendAdditionalQuestionsEmailFailedException : Exception
    {
        public SendAdditionalQuestionsEmailFailedException()
        {
        }

        public SendAdditionalQuestionsEmailFailedException(string message) : base(message)
        {
        }

        public SendAdditionalQuestionsEmailFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SendAdditionalQuestionsEmailFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}