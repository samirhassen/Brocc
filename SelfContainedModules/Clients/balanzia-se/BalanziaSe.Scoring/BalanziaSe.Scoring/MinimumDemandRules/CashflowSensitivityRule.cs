using NTech.Banking.ScoringEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BalanziaSe.Scoring
{
    public class CashflowSensitivityRule : MinimumDemandScoringRule
    {
        protected override RuleContext.MimumDemandsResultCode CheckMinimumDemand(RuleContext context)
        {
            var applicationAmount = context.RequireDecimal("applicationAmount", null);
            var applicationRepaymentTimeInMonths = context.RequireInt("applicationRepaymentTimeInMonths", null);
            var nrOfMonthsLeftCurrentYear = context.RequireInt("nrOfMonthsLeftCurrentYear", null);
            var applicationCompanyYearlyRevenue = HandleDecimalOrMissing(context, "applicationCompanyYearlyRevenue");
            var applicationCompanyYearlyResult = HandleDecimalOrMissing(context, "applicationCompanyYearlyResult");
            var applicationCompanyCurrentDebtAmount = HandleDecimalOrMissing(context, "applicationCompanyCurrentDebtAmount"); 

            //Rejection at this stage prevents error when dividing by zero.
            if (applicationCompanyYearlyRevenue == 0)
                return RuleContext.MimumDemandsResultCode.Rejected;

            var result = ComputeNormalAndStressedCashflowEstimate(
                applicationAmount, applicationCompanyCurrentDebtAmount,
                applicationCompanyYearlyRevenue, applicationCompanyYearlyResult, applicationRepaymentTimeInMonths, nrOfMonthsLeftCurrentYear, observeDebugData: context.SetDebugData);

            var minNormal = result.Item1;
            var minStressed = result.Item2;

            context.InjectScoringVariables(m =>
            {
                m.Set("normalCashFlowEstimateAmount", Math.Round(minNormal), null);
                m.Set("stressedCashFlowEstimateAmount", Math.Round(minStressed), null);
            });

            if (minNormal < 0m)
                return RuleContext.MimumDemandsResultCode.Rejected;
            else if (minStressed < 0m)
                return RuleContext.MimumDemandsResultCode.AcceptedWithManualAttention;
            else
                return RuleContext.MimumDemandsResultCode.Accepted;
        }

        protected override ISet<string> DeclareRequiredApplicantItems()
        {
            return ToSet();
        }

        protected override ISet<string> DeclareRequiredApplicationItems()
        {
            return ToSet("applicationAmount", "applicationRepaymentTimeInMonths", "nrOfMonthsLeftCurrentYear", "applicationCompanyYearlyRevenue", "applicationCompanyYearlyResult", "applicationCompanyCurrentDebtAmount");
        }

        private static decimal HandleDecimalOrMissing(RuleContext context, string name)
        {
            if (context.RequireString(name, null) == "missing")
                return 0;
            else
                return context.RequireDecimal(name, null);
        }

        public static Tuple<decimal, decimal> ComputeNormalAndStressedCashflowEstimate(decimal loanAmount,
           decimal otherloansAmount,
           decimal revenueAmount,
           decimal resultAmount,
           int repaymentTimeInMonths,
           int nrOfMonthsLeftOnCurrentYear,
           Action<string> observeDebugData = null)
        {
            //Assume fixed amortization rather than annuites to simplify the model, use 4 fixed simulated future years always where the first one is this year
            //The revenue and result are assumed to be for last year and the year before and are the same

            var paymentMonthsLeft = repaymentTimeInMonths;
            var paymentMonthsPerYear = new List<decimal>();
            for (var yearNr = 1; yearNr <= 4; yearNr++)
            {
                var paymentMonths = Math.Min(paymentMonthsLeft, yearNr == 1 ? nrOfMonthsLeftOnCurrentYear : 12);
                paymentMonthsLeft = paymentMonthsLeft - paymentMonths;
                paymentMonthsPerYear.Add(paymentMonths);
            }
            var revenueGrowthPercent = 5m;
            var loanInterestPercent = 7m;
            var stressFactor = 0.8m;

            var normal = ComputeAccumulatedCashflowEstimate(loanAmount, otherloansAmount, paymentMonthsPerYear, revenueAmount, resultAmount, revenueGrowthPercent, loanInterestPercent);
            var stressed = ComputeAccumulatedCashflowEstimate(loanAmount, otherloansAmount, paymentMonthsPerYear, revenueAmount, resultAmount, revenueGrowthPercent, loanInterestPercent, stressBasisValues: normal, stressFactor: stressFactor);

            var minNormal = normal.Values.Where(x => x.AkkumuleratKassaflode.HasValue && x.IsSimulated).Min(x => x.AkkumuleratKassaflode.Value);
            var minStressed = stressed.Values.Where(x => x.AkkumuleratKassaflode.HasValue && x.IsSimulated).Min(x => x.AkkumuleratKassaflode.Value);

            if (observeDebugData != null)
            {
                var b = new StringBuilder();
                b.AppendLine($"Ränta = {loanInterestPercent}%, Tillväxt = {revenueGrowthPercent}%, Stress = {stressFactor * 100m}%, Löptider = {string.Join(", ", paymentMonthsPerYear.Select(x => x.ToString()))} månader");
                b.AppendLine("--normal--");
                b.AppendLine(YearModel.GetDescHeader());
                foreach (var ar in normal.Keys)
                {
                    b.AppendLine(normal[ar].GetDesc());
                }
                b.AppendLine($"Min={YearModel.StringRoundKilo(minNormal)}");
                b.AppendLine("--stressad--");
                b.AppendLine(YearModel.GetDescHeader());
                foreach (var ar in stressed.Keys)
                {
                    b.AppendLine(stressed[ar].GetDesc());
                }
                b.AppendLine($"Min={YearModel.StringRoundKilo(minStressed)}");
                observeDebugData(b.ToString());
            }

            return Tuple.Create(minNormal, minStressed);
        }

        private static Dictionary<int, YearModel> ComputeAccumulatedCashflowEstimate(
            decimal loanAmount,
            decimal otherloansAmount,
            List<decimal> paymentMonthsPerYear,
            decimal revenueAmount,
            decimal resultAmount,
            decimal revenueGrowthPercent,
            decimal loanInterestPercent,
            Dictionary<int, YearModel> stressBasisValues = null,
            decimal stressFactor = 1m)
        {
            var ar = new Dictionary<int, YearModel>();

            stressBasisValues = stressBasisValues ?? new Dictionary<int, YearModel>();

            ar[-1] = new YearModel
            {
                Ar = -1,
                Omsattning = revenueAmount,
                Resultat = resultAmount,
                EbitProcent = 100m * (resultAmount / revenueAmount), //no stressfactor here for some reason
                Skulder = loanAmount + otherloansAmount,
                AmortProcent = 0m,
                Kassaflode = ComputeKassaflode(resultAmount, loanAmount + otherloansAmount, 0m, loanInterestPercent)
            };

            ar[0] = new YearModel
            {
                Ar = 0,
                Omsattning = revenueAmount,
                Resultat = resultAmount,
                EbitProcent = 100m * (resultAmount / revenueAmount),
                Skulder = loanAmount + otherloansAmount,
                AmortProcent = 0m,
                Kassaflode = ar[-1].Kassaflode,
                AkkumuleratKassaflode = ar[-1].Kassaflode
            };

            var year = 0;
            var totalMonths = paymentMonthsPerYear.Sum(x => x);
            foreach (var paymentMonths in paymentMonthsPerYear)
            {
                var yearAmortizationPercent = 100m * paymentMonths / totalMonths;

                year++;
                var yff = ar[year - 2];
                var yf = ar[year - 1];
                var ebitProcent = stressBasisValues.ContainsKey(year) ? stressBasisValues[year].EbitProcent * stressFactor : (yff.EbitProcent + yf.EbitProcent) / 2m;
                var omsattning = stressBasisValues.ContainsKey(year) ? stressBasisValues[year].Omsattning * stressFactor : yf.Omsattning * (100m + revenueGrowthPercent) / 100m;
                var skulder = yf.Skulder - yf.Skulder * yf.AmortProcent / 100m;
                var resultat = ebitProcent * omsattning / 100m;

                var kassaflode = ComputeKassaflode(resultat, skulder, yearAmortizationPercent, loanInterestPercent);

                ar[year] = new YearModel
                {
                    Ar = year,
                    Omsattning = omsattning,
                    EbitProcent = ebitProcent,
                    Resultat = resultat,
                    Skulder = skulder,
                    Kassaflode = kassaflode,
                    AmortProcent = yearAmortizationPercent,
                    AkkumuleratKassaflode = yf.Kassaflode + kassaflode,
                    IsSimulated = true
                };
            }
            return ar;
        }

        private static decimal ComputeKassaflode(decimal resultat, decimal skulder, decimal yearAmortizationPercent, decimal loanInterestPercent)
        {
            return resultat - skulder * ((yearAmortizationPercent + loanInterestPercent) / 100m) - (0.28m * 0.5m * resultat);
        }

        private class YearModel
        {
            public int Ar { get; set; }
            public decimal Omsattning { get; set; }
            public decimal Resultat { get; set; }
            public decimal EbitProcent { get; set; }
            public decimal Skulder { get; set; }
            public decimal? Kassaflode { get; set; }
            public decimal? AkkumuleratKassaflode { get; set; }
            public decimal AmortProcent { get; set; }
            public bool IsSimulated { get; set; }

            public override string ToString()
            {
                return GetDescHeader() + Environment.NewLine + GetDesc();
            }

            public static string GetDescHeader()
            {
                return "År\tOmsättning\tResultat\tSkulder\tKassaflöde\tAkk. kassaflöde";
            }

            public string GetDesc()
            {
                return $"{Ar}\t{StringRoundKilo(Omsattning)}\t{StringRoundKilo(Resultat)}\t{StringRoundKilo(Skulder)}\t{(StringRoundKilo(Kassaflode))}\t{(StringRoundKilo(AkkumuleratKassaflode))}";
            }

            public static string StringRoundKilo(decimal? d)
            {
                if (!d.HasValue)
                    return "-";

                return Math.Round(d.Value / 1000m).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
