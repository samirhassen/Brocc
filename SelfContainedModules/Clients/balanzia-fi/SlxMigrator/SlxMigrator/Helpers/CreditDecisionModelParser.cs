using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlxMigrator
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

        public class Application
        {
            public Applicant applicant1 { get; set; }
        }

        public class Applicant
        {
            public string approvedSat { get; set; }
            public string birthDate { get; set; }
            public decimal carOrBoatLoanAmount { get; set; }
            public decimal creditCardAmount { get; set; }
            public int customerId { get; set; }
            public string education { get; set; }
            public string employedSinceMonth { get; set; }
            public string employer { get; set; }
            public string employerPhone { get; set; }
            public string employment { get; set; }
            public string housing { get; set; }
            public decimal housingCostPerMonthAmount { get; set; }
            public decimal incomePerMonthAmount { get; set; }
            public string marriage { get; set; }
            public decimal mortgageLoanAmount { get; set; }
            public int nrOfChildren { get; set; }
            public decimal otherLoanAmount { get; set; }
            public decimal studentLoanAmount { get; set; }
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
            public bool? WasRejectedByMinimumDemands { get; set; }
            public List<string> Rejections { get; set; }
            public decimal? OfferedNotificationFeeAmount { get; set; }
            public string RiskGroup { get; set; }
            public decimal? LeftToLiveOn { get; set; }
            public decimal? Score { get; set; }
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

        public static Applicant ParseApplicant1(string model)
        {
            if (string.IsNullOrEmpty(model))
            {
                return null;
            }

            return JsonConvert.DeserializeAnonymousType(model, new { application = (Application)null })?.application?.applicant1;
        }

        public static UnsecuredLoanCreditDecisionModel ParseUnsecuredLoanCreditDecision(string model, bool isRejected)
        {
            if (model == null)
                return null;
            return new UnsecuredLoanCreditDecisionModel
            {
                WasAccepted = !isRejected,
                NewCreditOffer = isRejected ? null : ParseAcceptedNewCreditOffer(model),
                AdditionalLoanOffer = isRejected ? null : ParseAcceptedAdditionalLoanOffer(model),
                RejectionReasons = isRejected ? ParseRejectionReasons(model) : null
            };
        }

        public class UnsecuredLoanCreditDecisionModel
        {
            public bool WasAccepted { get; set; }
            public AcceptedNewCreditOffer NewCreditOffer { get; internal set; }
            public AcceptedAdditionalCreditOffer AdditionalLoanOffer { get; internal set; }
            public string[] RejectionReasons { get; internal set; }
        }
    }
}
