using System;

namespace NTech.Core.Module.Shared.Infrastructure
{
    /// <summary>
    /// This gets trapped by FeatureToggleActionFilter
    /// </summary>
    public class NTechCoreWebserviceException : Exception
    {
        public string ErrorCode { get; set; }

        public int? ErrorHttpStatusCode { get; set; }

        public bool IsUserFacing { get; set; }

        public NTechCoreWebserviceException()
        {
        }

        public NTechCoreWebserviceException(string message)
            : base(message)
        {
        }

        public NTechCoreWebserviceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
