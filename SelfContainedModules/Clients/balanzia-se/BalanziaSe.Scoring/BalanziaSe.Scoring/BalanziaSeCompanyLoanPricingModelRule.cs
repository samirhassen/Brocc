using NTech.Banking.LoanModel;
using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BalanziaSe.Scoring
{
    public class BalanziaSeCompanyLoanPricingModelRule : PricingModelScoringRule
    {
        public override string RuleName => "PricingModel";

        protected override Tuple<string, ScoringProcess.OfferModel, bool> ComputeRiskClassAndPossibleOffer(decimal scorePoints, RuleContext context)
        {
            var pdRiskClass = (int)Math.Round(scorePoints);

            if (pdRiskClass <= 3)
                return Reject(pdRiskClass.ToString());

            var requestedLoanAmount = context.RequireDecimal("applicationAmount", null);
            var actualLoanAmount = Capped(requestedLoanAmount, 50000m, 1000000m);

            var requestedRepaymentTimeInMonths = context.RequireInt("applicationRepaymentTimeInMonths", null);
            var actualRepaymentTimeInMonths = (int)Capped(requestedRepaymentTimeInMonths, 6, 60);
            var ucRiskClass = context.RequireString("creditReportRiskklassForetag");

            var interestRate = GetInterestRate(ucRiskClass, pdRiskClass);
            if (!interestRate.HasValue)
                return Reject(pdRiskClass.ToString());

            var currentReferenceInterestRatePercent = context.RequireDecimal("currentReferenceInterestRatePercent", null);

            var offer = new ScoringProcess.OfferModel
            {
                LoanAmount = actualLoanAmount,
                InitialFeeAmount = Math.Round(0.01m * actualLoanAmount),
                MonthlyFeeAmount = 50m,
                NominalInterestRatePercent = interestRate.Value
            };

            var p = PaymentPlanCalculation
                .BeginCreateWithRepaymentTime(offer.LoanAmount, actualRepaymentTimeInMonths, offer.NominalInterestRatePercent + currentReferenceInterestRatePercent, true, null, false)
                .WithInitialFeeDrawnFromLoanAmount(offer.InitialFeeAmount)
                .WithMonthlyFee(offer.MonthlyFeeAmount)
                .EndCreate();

            offer.AnnuityAmount = p.AnnuityAmount;

            return AcceptWithOffer(pdRiskClass.ToString(), offer, requiresManualAttention: pdRiskClass <= 9);
        }

        private decimal Capped(decimal computedValue, decimal minAllowedValue, decimal maxAllowedValue)
        {
            return computedValue > maxAllowedValue ? maxAllowedValue : (computedValue < minAllowedValue ? minAllowedValue : computedValue);
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("applicationAmount", "applicationRepaymentTimeInMonths", "creditReportRiskklassForetag", "currentReferenceInterestRatePercent");
        }

        private decimal? GetInterestRate(string ucRiskClass, int pdRiskClass)
        {
            return Rates
                .Value
                .Where(x => x.PdRiskClass == pdRiskClass && x.UcRiskClass == ucRiskClass)
                .FirstOrDefault()
                ?.InterestRatePercent;
        }

        private class Rate
        {
            public int PdRiskClass { get; set; }
            public string UcRiskClass { get; set; }
            public decimal InterestRatePercent { get; set; }
        }

        private static Lazy<List<Rate>> Rates = new Lazy<List<Rate>>(() =>
        {
            return NTech.Banking.Conversion.EmbeddedResources.WithEmbeddedStream("BalanziaSe.Scoring.Resources", "InterestRates.txt", s =>
            {
                var rates = new List<Rate>();
                using (var r = new StreamReader(s))
                {
                    string line;
                    var isFirst = true;
                    string[] ucRiskClasses = null;
                    while ((line = r.ReadLine()) != null)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            ucRiskClasses = line.Split('\t').Skip(1).ToArray();
                        }
                        else
                        {
                            var cells = line.Split('\t');
                            if (ucRiskClasses == null || cells.Length != (ucRiskClasses.Length + 1))
                                throw new Exception("Invalid interest rate table. Should be tab separated entries.");

                            var pd = cells[0];
                            for (var i = 1; i < cells.Length; i++)
                            {
                                rates.Add(new Rate
                                {
                                    PdRiskClass = int.Parse(pd),
                                    UcRiskClass = ucRiskClasses[i - 1],
                                    InterestRatePercent = decimal.Parse(cells[i].Replace("%", "").Replace(",", "."), CultureInfo.InvariantCulture)
                                });
                            }
                        }
                    }
                }
                return rates;
            });
        });
    }
}