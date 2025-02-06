using NTech.Banking.ScoringEngine;
using System.Collections.Generic;

namespace nPreCredit.Code
{
    public class ScoringResult
    {
        public bool HasOffer { get; set; }
        public decimal? OfferedAmount { get; set; }
        public decimal? MaxOfferedAmount { get; set; }
        public int? OfferedRepaymentTimeInMonths { get; set; }
        public decimal? OfferedInterestRatePercent { get; set; }
        public decimal? OfferedNotificationFeeAmount { get; set; }
        public decimal? OfferedInitialFeeAmount { get; set; }

        public string OfferedAdditionalLoanCreditNr { get; set; }
        public decimal? OfferedAdditionalLoanNewAnnuityAmount { get; set; }
        public decimal? OfferedAdditionalLoanNewMarginInterestPercent { get; set; }
        
        public ScoringDataModelFlat ScoringData { get; set; }
        public int? PetrusVersion { get; set; }
        public string PetrusApplicationId { get; set; }

        //Like score or paymentRemark
        public List<string> RejectionReasons { get; set; }
    }
}
