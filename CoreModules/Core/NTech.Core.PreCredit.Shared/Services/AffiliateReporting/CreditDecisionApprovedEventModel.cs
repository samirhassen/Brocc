namespace nPreCredit.Code.AffiliateReporting
{

    public class CreditDecisionApprovedEventModel : AffiliateReportingEventModelBase
    {
        public static string EventTypeName = "CreditDecisionApproved";

        public string ApplicationUrl { get; set; }
        public NewLoanOfferModel NewLoanOffer { get; set; }
        public AdditionalLoanOfferModel AdditionalLoanOffer { get; set; }

        public class NewLoanOfferModel
        {
            public decimal? Amount { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public decimal? ReferenceInterestRatePercent { get; set; }
            public decimal? InitialFeeAmount { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? EffectiveInterestRatePercent { get; set; }
            public decimal? TotalPaidAmount { get; set; }
            public decimal? InitialPaidToCustomerAmount { get; set; }

            public decimal? GetInterestRatePercent()
            {
                var p = MarginInterestRatePercent.GetValueOrDefault() + ReferenceInterestRatePercent.GetValueOrDefault();
                return p == 0m ? new decimal?() : p;
            }
        }

        public class AdditionalLoanOfferModel
        {
            public decimal? Amount { get; set; }
            public string CreditNr { get; set; }
            public decimal? NewAnnuityAmount { get; set; }
            public decimal? NewMarginInterestRatePercent { get; set; }
            public LoanStateModel LoanStateAfter { get; set; }

            public class LoanStateModel
            {
                public decimal? Balance { get; set; }
                public int? RepaymentTimeInMonths { get; set; }
                public decimal? AnnuityAmount { get; set; }
                public decimal? NotificationFeeAmount { get; set; }
                public decimal? MarginInterestRatePercent { get; set; }
                public decimal? ReferenceInterestRatePercent { get; set; }
                public decimal? EffectiveInterestRatePercent { get; set; }
                public decimal? GetInterestRatePercent()
                {
                    var p = MarginInterestRatePercent.GetValueOrDefault() + ReferenceInterestRatePercent.GetValueOrDefault();
                    return p == 0m ? new decimal?() : p;
                }
            }
        }

        public class SimplifiedOfferModel
        {
            public decimal? LoanAmount { get; set; }
            public int? RepaymentTimeInMonths { get; set; }
            public decimal? MontlyPaymentExcludingFees { get; set; }
            public decimal? MontlyPaymentIncludingFees { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
            public decimal? InterestRatePercent { get; set; }
            public decimal? EffectiveInterestRatePercent { get; set; }

            public decimal? InitialFeeAmount { get; set; }
        }

        public SimplifiedOfferModel GetSimplifiedOffer()
        {
            if (NewLoanOffer != null)
                return new SimplifiedOfferModel
                {
                    LoanAmount = NewLoanOffer.Amount,
                    NotificationFeeAmount = NewLoanOffer.NotificationFeeAmount,
                    EffectiveInterestRatePercent = NewLoanOffer.EffectiveInterestRatePercent,
                    InterestRatePercent = NewLoanOffer.GetInterestRatePercent(),
                    RepaymentTimeInMonths = NewLoanOffer.RepaymentTimeInMonths,
                    MontlyPaymentExcludingFees = NewLoanOffer.AnnuityAmount,
                    MontlyPaymentIncludingFees = NewLoanOffer.AnnuityAmount + NewLoanOffer.NotificationFeeAmount.GetValueOrDefault(),
                    InitialFeeAmount = NewLoanOffer.InitialFeeAmount
                };
            else if (AdditionalLoanOffer != null)
                return new SimplifiedOfferModel
                {
                    LoanAmount = AdditionalLoanOffer.Amount,
                    NotificationFeeAmount = AdditionalLoanOffer.LoanStateAfter?.NotificationFeeAmount,
                    EffectiveInterestRatePercent = AdditionalLoanOffer.LoanStateAfter?.EffectiveInterestRatePercent,
                    InterestRatePercent = AdditionalLoanOffer.LoanStateAfter?.GetInterestRatePercent(),
                    RepaymentTimeInMonths = AdditionalLoanOffer.LoanStateAfter?.RepaymentTimeInMonths,
                    MontlyPaymentExcludingFees = AdditionalLoanOffer.LoanStateAfter?.AnnuityAmount,
                    MontlyPaymentIncludingFees = AdditionalLoanOffer.LoanStateAfter?.AnnuityAmount + AdditionalLoanOffer.LoanStateAfter?.NotificationFeeAmount.GetValueOrDefault(),
                    InitialFeeAmount = 0m
                };
            else
                return null;
        }
    }
}