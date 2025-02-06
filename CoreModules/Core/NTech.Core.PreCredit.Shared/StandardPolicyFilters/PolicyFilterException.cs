using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace nPreCredit.Code.StandardPolicyFilters
{
    public class PolicyFilterException : Exception
    {
        public bool IsMissingApplicationLevelVariable { get; set; }
        public bool IsMissingApplicantLevelVariable { get; set; }
        public string MissingVariableOrParameterName { get; set; }
        public ISet<int> MissingApplicantLevelApplicantNrs { get; set; }
        public bool IsMissingStaticParameter { get; set; }

        public PolicyFilterException()
        {
        }

        public PolicyFilterException(string message) : base(message)
        {
        }

        public PolicyFilterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PolicyFilterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}