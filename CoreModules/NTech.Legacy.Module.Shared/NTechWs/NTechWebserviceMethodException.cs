using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    public class NTechWebserviceMethodException : Exception
    {
        public string ErrorCode { get; set; }
        public int? ErrorHttpStatusCode { get; set; }
        public bool IsUserFacing { get; set; }
        public NTechWebserviceMethodException() : base()
        {
        }

        public NTechWebserviceMethodException(string message) : base(message)
        {
        }

        public NTechWebserviceMethodException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
