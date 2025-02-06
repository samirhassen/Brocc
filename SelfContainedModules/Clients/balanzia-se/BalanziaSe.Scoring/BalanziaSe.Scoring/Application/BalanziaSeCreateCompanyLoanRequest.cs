using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BalanziaSe.Scoring
{
    public class BalanziaSeCreateCompanyLoanRequest
    {
        [Required]
        public decimal? RequestedAmount { get; set; }

        public int? RequestedRepaymentTimeInMonths { get; set; }

        [Required]
        public string ProviderName { get; set; }

        public string CustomerIpAddress { get; set; }

        [Required]
        public ApplicantModel Applicant { get; set; }

        [Required]
        public CompanyModel Customer { get; set; }

        public bool? SkipHideFromManualUserLists { get; set; }

        public bool? SkipInitialScoring { get; set; }

        public bool? SupressUserNotification { get; set; }

        public class ApplicantModel
        {
            [Required]
            public string CivicRegNr { get; set; }

            [Required]
            public string FirstName { get; set; }

            [Required]
            public string LastName { get; set; }

            [Required]
            public string Email { get; set; }

            [Required]
            public string Phone { get; set; }

            public DateTime? BirthDate { get; set; }
        }

        public Dictionary<string, string> AdditionalApplicationProperties { get; set; }

        public Dictionary<string, string> ExternalVariables { get; set; }

        public class CompanyModel
        {
            [Required]
            public string Orgnr { get; set; }
            public string CompanyName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }
    }
}
