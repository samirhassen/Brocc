using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NTech.Banking.ScoringEngine
{
    
    public class MissingRequiredScoringDataException : Exception
    {
        public string ItemName { get; set; }
        public int? ApplicantNr { get; set; }

        public static MissingRequiredScoringDataException Create(string name, int? applicantNr)
        {
            var e = new MissingRequiredScoringDataException($"Missing required item {name}{(applicantNr.HasValue ? $" for applicant {applicantNr.Value}" : "")}");
            e.ItemName = name;
            e.ApplicantNr = applicantNr;
            return e;
        }

        public MissingRequiredScoringDataException() : base()
        {
        }

        public MissingRequiredScoringDataException(string message) : base(message)
        {
        }

        public MissingRequiredScoringDataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingRequiredScoringDataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}