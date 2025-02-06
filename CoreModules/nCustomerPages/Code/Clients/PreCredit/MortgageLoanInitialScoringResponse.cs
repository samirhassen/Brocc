using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{

    public class MortgageLoanInitialScoringResponse
    {
        public Offer AcceptedOffer { get; set; }
        public RejectionDetails RejectedDetails { get; set; }

        public class Offer
        {
            public decimal LoanAmount { get; set; }
            public decimal MonthlyAmortizationAmount { get; set; }
            public decimal NominalInterestRatePercent { get; set; }
            public decimal MonthlyFeeAmount { get; set; }
            public decimal InitialFeeAmount { get; set; }
            public DateTime ValidUntilDate { get; set; }
        }

        public class RejectionDetails
        {
            public List<string> RejectionReasons { get; set; }
        }
    }
}