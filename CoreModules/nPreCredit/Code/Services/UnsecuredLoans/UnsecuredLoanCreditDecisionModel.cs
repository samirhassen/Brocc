using NTech.Banking.LoanModel;

namespace nPreCredit.Code.Services.UnsecuredLoans
{
    public class UnsecuredLoanExtendedOfferModel : UnsecuredLoanInitialCreditDecisionRecommendationModel.OfferModel
    {
        public decimal? TotalInterestRatePercent { get; set; }
        public decimal? TotalPaidAmount { get; set; }
        public decimal? EffectiveInterestRatePercent { get; set; }

        public static UnsecuredLoanExtendedOfferModel CreateFromOffer(UnsecuredLoanInitialCreditDecisionRecommendationModel.OfferModel offer)
        {
            if (offer == null)
                return null;

            var f = offer;
            var i = f.NominalInterestRatePercent.Value + f.ReferenceInterestRatePercent.GetValueOrDefault();
            var p = PaymentPlanCalculation
                .BeginCreateWithAnnuity(f.LoanAmount.Value, f.AnnuityAmount.Value, i, null, NEnv.CreditsUse360DayInterestYear)
                .WithMonthlyFee(f.MonthlyFeeAmount.GetValueOrDefault())
                .WithInitialFeeDrawnFromLoanAmount(f.InitialFeeAmount.GetValueOrDefault())
                .EndCreate();

            return new UnsecuredLoanExtendedOfferModel
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