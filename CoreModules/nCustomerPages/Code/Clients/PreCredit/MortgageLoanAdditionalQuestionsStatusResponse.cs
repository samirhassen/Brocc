using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{

    public class MortgageLoanAdditionalQuestionsStatusResponse
    {
        public int? CurrentLoanAmount { get; set; }
        public string ApplicationNr { get; set; }
        public bool IsPossibleToAnswer { get; set; }
        public bool HasAlreadyAnswered { get; set; }
        public bool IsTokenExpired { get; set; }

        public List<ApplicantInfo> Applicants { get; set; }

        public class ApplicantInfo
        {
            public int ApplicantNr { get; set; }
            public string FirstName { get; set; }

            public DateTime? BirthDate { get; set; }
        }
    }
}