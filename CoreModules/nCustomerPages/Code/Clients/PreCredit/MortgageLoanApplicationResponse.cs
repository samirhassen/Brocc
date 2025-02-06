using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{

    public class MortgageLoanApplicationResponse
    {
        public bool IsError { get; set; }

        public SuccessResponse SuccessData { get; set; }
        public ErrorResponse ErrorData { get; set; }

        public class ErrorResponse
        {
            public string ErrorMessge { get; set; }
            public bool IsDuplicateProviderApplicationId { get; set; }
        }

        public class SuccessResponse
        {
            public string ApplicationNr { get; set; }
            public DirectScoringResultModel DirectScoringResult { get; set; }

            public class DirectScoringResultModel
            {
                public bool IsAccepted { get; set; }
                public AcceptedOfferModel AcceptedOffer { get; set; }
                public RejectedDetailsModel RejectedDetails { get; set; }
            }

            public class AcceptedOfferModel
            {
                public decimal LoanAmount { get; set; }
                public decimal MonthlyAmortizationAmount { get; set; }
                public decimal NominalInterestRatePercent { get; set; }
                public decimal MonthlyFeeAmount { get; set; }
                public decimal InitialFeeAmount { get; set; }
                public DateTime ValidUntilDate { get; set; }
            }

            public class RejectedDetailsModel
            {
                public List<string> RejectionReasons { get; set; }
            }
        }
    }
}