using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public static class CreditDecisionModelParser
    {
        public class AcceptedNewCreditOffer
        {
            public decimal? amount { get; set; }
            public decimal? annuityAmount { get; set; }
            public int? repaymentTimeInMonths { get; set; }
            public decimal? marginInterestRatePercent { get; set; }
            public decimal? referenceInterestRatePercent { get; set; }
            public decimal? initialFeeAmount { get; set; }
            public decimal? notificationFeeAmount { get; set; }
            public decimal? effectiveInterestRatePercent { get; set; }
            public decimal? totalPaidAmount { get; set; }
            public decimal? initialPaidToCustomerAmount { get; set; }
        }

        public class AcceptedAdditionalCreditOffer
        {
            public decimal? amount { get; set; }
            public string creditNr { get; set; }
            public decimal? newAnnuityAmount { get; set; }
            public decimal? newMarginInterestRatePercent { get; set; }
            public decimal? newNotificationFeeAmount { get; set; }
            public decimal? effectiveInterestRatePercent { get; set; }
        }

        public class Recommendation
        {
            public bool HasOffer { get; set; }
            public decimal? OfferedAmount { get; set; }
            public decimal? OfferedInitialFeeAmount { get; set; }
            public decimal? MaxOfferedAmount { get; set; }
            public int? OfferedRepaymentTimeInMonths { get; set; }
            public decimal? OfferedInterestRatePercent { get; set; }
            public string OfferedAdditionalLoanCreditNr { get; set; }
            public decimal? OfferedAdditionalLoanNewAnnuityAmount { get; set; }
            public decimal? OfferedAdditionalLoanNewMarginInterestPercent { get; set; }
            public decimal? OfferedNotificationFeeAmount { get; set; }
            public string RiskGroup { get; set; }
        }

        public static AcceptedNewCreditOffer ParseAcceptedNewCreditOffer(string model)
        {
            return JsonConvert.DeserializeAnonymousType(model, new
            {
                offer = new AcceptedNewCreditOffer
                {
                    amount = new decimal?(),
                    annuityAmount = new decimal?(),
                    repaymentTimeInMonths = new int?(),
                    marginInterestRatePercent = new decimal?(),
                    referenceInterestRatePercent = new decimal?(),
                    initialFeeAmount = new decimal?(),
                    notificationFeeAmount = new decimal?(),
                    effectiveInterestRatePercent = new decimal?(),
                    totalPaidAmount = new decimal?(),
                    initialPaidToCustomerAmount = new decimal?()
                }
            })?.offer;
        }

        public static AcceptedAdditionalCreditOffer ParseAcceptedAdditionalLoanOffer(string model)
        {
            return JsonConvert.DeserializeAnonymousType(model, new
            {
                additionalLoanOffer = new AcceptedAdditionalCreditOffer
                {
                    amount = new decimal?(),
                    creditNr = (string)null,
                    newAnnuityAmount = new decimal?(),
                    newMarginInterestRatePercent = new decimal?(),
                    effectiveInterestRatePercent = new decimal?(),
                    newNotificationFeeAmount = new decimal?()
                }
            })?.additionalLoanOffer;
        }

        public class HistoricalCredit
        {
            public string CreditNr { get; set; }
            public decimal? CapitalBalance { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public decimal? ReferenceInterestRatePercent { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
        }

        //Key = applicantnr, Value = credits
        public static IDictionary<int, List<HistoricalCredit>> ParseHistoricalCredits(string model)
        {
            var result = new Dictionary<int, List<HistoricalCredit>>();
            var a = JsonConvert.DeserializeAnonymousType(model, new
            {
                credits = new
                {
                    applicant1 = (List<HistoricalCredit>)null,
                    applicant2 = (List<HistoricalCredit>)null
                }
            });
            if ((a?.credits?.applicant1?.Count ?? 0m) > 0)
                result[1] = a.credits.applicant1;
            if ((a?.credits?.applicant2?.Count ?? 0m) > 0)
                result[2] = a.credits.applicant2;
            return result;
        }

        public static decimal? ParseOfferEffectiveInterestRate(string model, bool creditsUse360DayInterestYear)
        {
            if (model == null)
                return null;

            Func<AcceptedAdditionalCreditOffer, decimal?> parseAdditionalLoan = offer =>
            {
                if (offer.effectiveInterestRatePercent.HasValue)
                    return offer.effectiveInterestRatePercent;
                else
                {
                    var h = ParseHistoricalCredits(model);
                    var credit = h.Values.SelectMany(x => x).Where(x => x.CreditNr == offer.creditNr).FirstOrDefault();
                    if (credit == null)
                        return null;

                    var amount = (offer.amount ?? 0m) + (credit.CapitalBalance ?? 0m);
                    var interestRate = (offer.newMarginInterestRatePercent ?? credit.MarginInterestRatePercent ?? 0m) + (credit.ReferenceInterestRatePercent ?? 0m);
                    var annuityAmount = offer.newAnnuityAmount ?? credit.AnnuityAmount ?? 0m;
                    return PaymentPlanCalculation
                        .BeginCreateWithAnnuity(amount, annuityAmount, interestRate, null, creditsUse360DayInterestYear)
                        .WithMonthlyFee(credit.NotificationFeeAmount ?? 0m)
                        .EndCreate()
                        .EffectiveInterestRatePercent;
                }
            };

            Func<AcceptedNewCreditOffer, decimal?> parseNewLoan = offer =>
            {
                if (offer.effectiveInterestRatePercent.HasValue)
                    return offer.effectiveInterestRatePercent;
                else
                {
                    return PaymentPlanCalculation
                        .BeginCreateWithRepaymentTime(offer.amount ?? 0m, offer.repaymentTimeInMonths ?? 0, (offer.marginInterestRatePercent ?? 0m) + (offer.referenceInterestRatePercent ?? 0m), true, null, creditsUse360DayInterestYear)
                        .WithInitialFeeCapitalized(offer.initialFeeAmount ?? 0m)
                        .WithMonthlyFee(offer.notificationFeeAmount ?? 0m)
                        .EndCreate()
                        .EffectiveInterestRatePercent;
                }
            };

            var newLoanOfferParsed = ParseAcceptedNewCreditOffer(model);
            var additionalLoanOfferParsed = ParseAcceptedAdditionalLoanOffer(model);
            if (newLoanOfferParsed != null)
                return parseNewLoan(newLoanOfferParsed);
            else if (additionalLoanOfferParsed != null)
                return parseAdditionalLoan(additionalLoanOfferParsed);
            else
                return null;
        }

        public static string[] ParseRejectionReasons(string model)
        {
            var decisionModel = JsonConvert.DeserializeAnonymousType(model, new
            {
                rejectionReasons = new string[] { }
            });
            return decisionModel?.rejectionReasons;
        }

        public static Recommendation ParseRecommendation(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            return JsonConvert.DeserializeAnonymousType(model, new { recommendation = (Recommendation)null }).recommendation;
        }

        public static IList<ScoringBasisCreditReport> ParseCreditReportsUsed(string model)
        {
            return JsonConvert.DeserializeAnonymousType(model, new { creditReportsUsed = (IList<ScoringBasisCreditReport>)null })?.creditReportsUsed;
        }

        public static NTech.Banking.ScoringEngine.PluginScoringProcess.OfferModel ParseMortageInitialLoanOffer(string model)
        {
            var d = ParseMortgageLoanAcceptedDecision(model);
            if (d.ScoringPass != "Initial")
                throw new Exception("Scoring pass must be Initial");
            return d.MortgageLoanOffer;
        }

        public static MortgageLoanAcceptedDecision ParseMortgageLoanAcceptedDecision(string model)
        {
            var d = JsonConvert.DeserializeObject<MortgageLoanAcceptedDecision>(model);
            if (d?.ScoringPass == "Initial" || d?.ScoringPass == "Final")
            {
                if (d?.MortgageLoanOffer == null)
                    throw new Exception($"Scoring pass without an offer");
            }
            else
                throw new Exception($"ScoringPass must be either 'Initial' or 'Final' but was '{d?.ScoringPass}'");

            return d;
        }

        public static MortgageLoanRejectedDecision ParseMortgageLoanRejectedDecision(string model)
        {
            return JsonConvert.DeserializeObject<MortgageLoanRejectedDecision>(model);
        }

        public static Services.CompanyLoans.CompanyLoanCreditDecisionModel ParseCompanyLoanCreditDecision(string model)
        {
            //Accepted/Rejected share a model here
            return JsonConvert.DeserializeObject<Services.CompanyLoans.CompanyLoanCreditDecisionModel>(model);
        }

        public static UnsecuredLoanCreditDecisionModel ParseUnsecuredLoanCreditDecision(string model)
        {
            if (model == null)
                return null;
            var newCreditOffer = ParseAcceptedNewCreditOffer(model);
            var additionalLoanOffer = ParseAcceptedAdditionalLoanOffer(model);
            var rejectionReason = ParseRejectionReasons(model);
            return new UnsecuredLoanCreditDecisionModel
            {
                WasAccepted = newCreditOffer != null || additionalLoanOffer != null,
                NewCreditOffer = newCreditOffer,
                AdditionalLoanOffer = additionalLoanOffer,
                RejectionReasons = rejectionReason
            };
        }

        public class UnsecuredLoanCreditDecisionModel
        {
            public bool WasAccepted { get; set; }
            public AcceptedNewCreditOffer NewCreditOffer { get; internal set; }
            public AcceptedAdditionalCreditOffer AdditionalLoanOffer { get; internal set; }
            public string[] RejectionReasons { get; internal set; }
        }

        public class MortgageLoanAcceptedDecision
        {
            public string ScoringPass { get; set; }
            public NTech.Banking.ScoringEngine.PluginScoringProcess.OfferModel MortgageLoanOffer { get; set; }
        }

        public class MortgageLoanRejectedDecision
        {
            public string ScoringPass { get; set; }
            public List<string> RejectionReasons { get; set; }
        }
    }
}