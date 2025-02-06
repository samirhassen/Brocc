using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{
    public class CreateCompanyLoanApplicationRequest
    {
        public decimal? RequestedAmount { get; set; }

        public int? RequestedRepaymentTimeInMonths { get; set; }

        public string ProviderName { get; set; }

        public string CustomerIpAddress { get; set; }

        public ApplicantModel Applicant { get; set; }

        public CompanyModel Customer { get; set; }

        public bool? SkipHideFromManualUserLists { get; set; }

        public bool? SkipInitialScoring { get; set; }

        public class ApplicantModel
        {
            public string CivicRegNr { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Phone { get; set; }

            public string Email { get; set; }

            public DateTime? BirthDate { get; set; }
        }

        public Dictionary<string, string> AdditionalApplicationProperties { get; set; }

        public Dictionary<string, string> ExternalVariables { get; set; }

        public class CompanyModel
        {
            public string Orgnr { get; set; }
            public string CompanyName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
        }
    }
}