using NTech.Banking.ScoringEngine;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanInitialCreditDecisionRecommendationModel
    {
        public bool WasAccepted { get; set; }
        public ScoringDataModel ScoringData { get; set; }
        public List<string> RejectionRuleNames { get; set; }
        public List<string> ManualAttentionRuleNames { get; set; }
        public Dictionary<string, decimal> ScorePointsByRuleName { get; set; }
        public Dictionary<string, string> DebugDataByRuleNames { get; set; }
        public string RiskClass { get; set; }
        public OfferModel Offer { get; set; }
        public string ApplicationNr { get; set; }
        public List<HistoricalCredit> HistoricalCredits { get; set; }
        public List<HistoricalApplication> HistoricalApplications { get; set; }

        public class OfferModel
        {
            [Required]
            public decimal? LoanAmount { get; set; }
            [Required]
            public decimal? NominalInterestRatePercent { get; set; }
            public decimal? ReferenceInterestRatePercent { get; set; }
            [Required]
            public decimal? MonthlyFeeAmount { get; set; }
            [Required]
            public decimal? InitialFeeAmount { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
        }
    }
}