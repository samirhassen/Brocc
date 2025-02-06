using NTech.Banking.LoanModel;
using System.Collections.Generic;

namespace nPreCredit.Code.Services.CompanyLoans
{
    public class CompanyLoanCreditDecisionModel
    {
        public string ScoringPass { get; set; }
        public bool WasAccepted { get; set; }
        public CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel CompanyLoanOffer { get; set; }
        public List<string> RejectionReasons { get; set; }
        public CompanyLoanInitialCreditDecisionRecommendationModel Recommendation { get; set; }

        public CompanyLoanExtendedOfferModel GetExtendedOfferModel(IPreCreditEnvSettings env)
        {
            return CompanyLoanExtendedOfferModel.CreateFromOffer(this.CompanyLoanOffer, env);
        }
    }

    public class CompanyLoanExtendedOfferModel : CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel
    {
        public decimal? TotalInterestRatePercent { get; set; }
        public decimal? TotalPaidAmount { get; set; }
        public decimal? EffectiveInterestRatePercent { get; set; }

        public static CompanyLoanExtendedOfferModel CreateFromOffer(CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel offer, IPreCreditEnvSettings env)
        {
            if (offer == null)
                return null;

            var f = offer;
            var i = f.NominalInterestRatePercent.Value + f.ReferenceInterestRatePercent.GetValueOrDefault();
            var p = PaymentPlanCalculation
                .BeginCreateWithAnnuity(f.LoanAmount.Value, f.AnnuityAmount.Value, i, null, env.CreditsUse360DayInterestYear)
                .WithMonthlyFee(f.MonthlyFeeAmount.GetValueOrDefault())
                .WithInitialFeeDrawnFromLoanAmount(f.InitialFeeAmount.GetValueOrDefault())
                .EndCreate();

            return new CompanyLoanExtendedOfferModel
            {
                AnnuityAmount = f.AnnuityAmount,
                InitialFeeAmount = f.InitialFeeAmount,
                LoanAmount = f.LoanAmount,
                MonthlyFeeAmount = f.MonthlyFeeAmount,
                NominalInterestRatePercent = f.NominalInterestRatePercent,
                ReferenceInterestRatePercent = f.ReferenceInterestRatePercent,
                RepaymentTimeInMonths = f.RepaymentTimeInMonths ?? p.Payments.Count,
                TotalPaidAmount = p.TotalPaidAmount,
                TotalInterestRatePercent = i,
                EffectiveInterestRatePercent = p.EffectiveInterestRatePercent.Value
            };
        }
    }
}