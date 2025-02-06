using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class LiquidityExposureReportDataWarehouseModel
    {
        private NotificationProcessSettings processSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.UnsecuredLoan);

        public class LiquidityExposureReportItemModel
        {
            public DateTime MonthFromDate { get; set; }
            public string CreditNr { get; set; }
            public int NrOfRemainingMonths { get; set; }
            public decimal CurrentCapitalDebt { get; set; }
            public decimal CurrentNotNotifiedCapitalDebt { get; set; }
            public decimal CapitalAmount_1_3 { get; set; }
            public decimal InterestAmount_1_3 { get; set; }
            public decimal CapitalAmount_4_12 { get; set; }
            public decimal InterestAmount_4_12 { get; set; }
            public decimal CapitalAmount_13_60 { get; set; }
            public decimal InterestAmount_13_60 { get; set; }
            public decimal CapitalAmount_61_end { get; set; }
            public decimal InterestAmount_61_end { get; set; }
        }

        private class ExtraCreditData
        {
            public byte[] Ts { get; set; }
            public DateTime CreationDate { get; set; }
            public decimal? CapitalDebt { get; set; }
            public decimal? NotNotifiedCapitalAmount { get; set; }
            public IEnumerable<DateTime> PendingFuturePaymentFreeMonths { get; set; }
        }

        public List<LiquidityExposureReportItemModel> GetLiquidityExposureModel(CreditContextExtended context, DateTime monthFromDate, IList<string> onlyTheseCreditNrs, bool isMortgageLoansEnabled)
        {
            var toDate = new DateTime(monthFromDate.Year, monthFromDate.Month, 1).AddMonths(1).AddDays(-1);

            var repo = new PartialCreditModelRepository();

            var models = repo
                    .NewQuery(toDate)
                    .WithValues(
                        DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate,
                        DatedCreditValueCode.AnnuityAmount, DatedCreditValueCode.MonthlyAmortizationAmount,
                        DatedCreditValueCode.NotificationFee)
                    .WithStrings(DatedCreditStringCode.AmortizationModel, DatedCreditStringCode.CreditStatus, DatedCreditStringCode.NextInterestFromDate)
                    .ExecuteExtended(context,
                        x => x.Select(y => new
                        {
                            y.Credit,
                            y.BasicCreditData
                        })
                            .Where(y =>
                                y.Credit.CreditType != CreditType.MortgageLoan.ToString() &&
                                y.Credit.CreatedByEvent.TransactionDate <= toDate &&
                                onlyTheseCreditNrs.Contains(y.Credit.CreditNr) &&
                                y.BasicCreditData.Strings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.Normal.ToString()))
                            .OrderBy(y => y.Credit.CreditNr)
                            .Select(y => new PartialCreditModelRepository.CreditFinalDataWrapper<ExtraCreditData>
                            {
                                BasicCreditData = y.BasicCreditData,
                                ExtraCreditData = new ExtraCreditData
                                {
                                    Ts = y.Credit.Timestamp,
                                    CreationDate = y.Credit.CreatedByEvent.TransactionDate,
                                    CapitalDebt = y
                                        .Credit
                                        .Transactions
                                        .Where(z => z.TransactionDate <= toDate && z.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                        .Sum(z => (decimal?)z.Amount),
                                    NotNotifiedCapitalAmount = y
                                        .Credit
                                        .Transactions
                                        .Where(z => z.TransactionDate <= toDate && z.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                                        .Sum(z => (decimal?)z.Amount),
                                    PendingFuturePaymentFreeMonths = y
                                        .Credit
                                        .CreditFuturePaymentFreeMonths
                                        .Where(z => z.CommitedByEvent == null && z.CancelledByEvent == null)
                                        .Select(z => z.ForMonth),
                                }
                            }));

            var credits = models
                .Select(x =>
                {
                    return new
                    {
                        x.ExtraData.NotNotifiedCapitalAmount,
                        x.ExtraData.CreationDate,
                        x.CreditNr,
                        x.ExtraData.PendingFuturePaymentFreeMonths,
                        x.ExtraData.CapitalDebt,
                        NotificationFee = x.GetValue(DatedCreditValueCode.NotificationFee) ?? 0m,
                        AmortizationModel = x.GetString(DatedCreditStringCode.AmortizationModel),
                        AnnuityAmount = x.GetValue(DatedCreditValueCode.AnnuityAmount),
                        MonthlyAmortizationAmount = x.GetValue(DatedCreditValueCode.MonthlyAmortizationAmount),
                        NextInterestFromDate = x.GetString(DatedCreditStringCode.NextInterestFromDate),
                        ReferenceInterestRate = x.GetValue(DatedCreditValueCode.ReferenceInterestRate),
                        MarginInterestRate = x.GetValue(DatedCreditValueCode.MarginInterestRate)
                    };
                })
                .ToList();

            var items = new List<LiquidityExposureReportItemModel>();
            var creditNrsWithNoAmortPlan = new List<string>();
            if (NEnv.HasPerLoanDueDay)
                throw new NotImplementedException();

            foreach (var credit in credits)
            {
                List<AmortizationPlan.Item> futureMonths;
                string failedMessage;
                //TODO: Possibly support payment free and exceptional here?
                var amortizationModel = CreditDomainModel.CreateAmortizationModel(credit.AmortizationModel, () => credit.AnnuityAmount.Value, () => credit.MonthlyAmortizationAmount.Value, null, null);
                if (!FixedDueDayAmortizationPlanCalculator.TrySimulateFutureMonths(
                    credit.NotNotifiedCapitalAmount.Value,
                    toDate.AddMonths(1), credit.NextInterestFromDate == null ? credit.CreationDate : DateTime.ParseExact(credit.NextInterestFromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    credit.MarginInterestRate.Value + (credit.ReferenceInterestRate ?? 0m),
                    amortizationModel,
                    credit.NotificationFee,
                    credit.PendingFuturePaymentFreeMonths.ToList(),
                    processSettings, null, out futureMonths, out failedMessage,
                    CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel)))
                {
                    creditNrsWithNoAmortPlan.Add(credit.CreditNr);
                }
                else
                {
                    var m1_3 = futureMonths.Take(3);
                    var m4_12 = futureMonths.Skip(3).Take(9);
                    var m13_60 = futureMonths.Skip(12).Take(48);
                    var m61_end = futureMonths.Skip(60);

                    items.Add(new LiquidityExposureReportItemModel
                    {
                        CreditNr = credit.CreditNr,
                        MonthFromDate = monthFromDate,
                        CurrentCapitalDebt = credit.CapitalDebt ?? 0m,
                        CurrentNotNotifiedCapitalDebt = credit.NotNotifiedCapitalAmount ?? 0m,
                        //beware: These are based on not notified capital even though the above is not.
                        //The correct way to fix this would be to include some kind of risk assessment. The idea that it's reasonable that the customer just follows the payment plan even though they haven't actually paid the last 3 notifications seems really bad
                        //The least bad quick fix I could imagine is to add the difference between not notified and actual capital to the 1-3 period (the hypothesis being that all customers pay everything they owe right now and then follow the plan basically)
                        NrOfRemainingMonths = futureMonths.Count,
                        CapitalAmount_1_3 = m1_3.Sum(y => (decimal?)y.CapitalTransaction) ?? 0m,
                        InterestAmount_1_3 = m1_3.Sum(y => y.InterestTransaction) ?? 0m,
                        CapitalAmount_4_12 = m4_12.Sum(y => (decimal?)y.CapitalTransaction) ?? 0m,
                        InterestAmount_4_12 = m4_12.Sum(y => y.InterestTransaction) ?? 0m,
                        CapitalAmount_13_60 = m13_60.Sum(y => (decimal?)y.CapitalTransaction) ?? 0m,
                        InterestAmount_13_60 = m13_60.Sum(y => y.InterestTransaction) ?? 0m,
                        CapitalAmount_61_end = m61_end.Sum(y => (decimal?)y.CapitalTransaction) ?? 0m,
                        InterestAmount_61_end = m61_end.Sum(y => y.InterestTransaction) ?? 0m,
                    });
                }
            }

            if (creditNrsWithNoAmortPlan.Any())
            {
                Log.Warning($"GetLiquidityExposureModel skipped {creditNrsWithNoAmortPlan.Count} credits whose terms mean they will never get paid. Examples: {string.Join(", ", creditNrsWithNoAmortPlan.Take(5))}");
            }

            return items;
        }
    }
}