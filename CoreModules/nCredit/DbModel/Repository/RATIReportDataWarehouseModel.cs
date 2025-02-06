using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class RATIReportDataWarehouseModel
    {
        private static bool TryComputeRemainingRuntime(decimal annuityAmount, decimal balanceAmount, decimal interestRatePercent, string debugCreditNr, out int nrOfMonths, out string failedMessage)
        {
            try
            {
                Func<decimal> compute = () =>
                {
                    if (balanceAmount <= 0m)
                        return 0m;
                    if (interestRatePercent < 0m)
                        throw new Exception("Negative interest leads to no meaningful runtime");
                    else if (interestRatePercent == 0m)
                        return balanceAmount / annuityAmount;
                    else
                    {
                        double r = (double)(interestRatePercent / 100m / 12m);
                        var pv = (double)balanceAmount;
                        var p = (double)annuityAmount;

                        var n = (decimal)(Math.Log(Math.Pow(1d - (pv * r / p), -1d)) / Math.Log(1d + r));
                        return n;
                    }
                };

                var value = compute();

                failedMessage = null;
                nrOfMonths = (int)Math.Ceiling(value);

                return true;
            }
            catch
            {
                nrOfMonths = 0;
                failedMessage = $"Error in ComputeRemainingRuntime for credit: {debugCreditNr}";
                return false;
            }
        }

        private static decimal? ComputeEffectiveInterestRate(decimal annuityAmount, decimal balanceAmount, decimal interestRatePercent, decimal monthlyFee, decimal capitalizedInitialFeeAmount, string creditNr)
        {
            try
            {
                return PaymentPlanCalculation
                    .BeginCreateWithAnnuity(balanceAmount, annuityAmount, interestRatePercent, null, NEnv.CreditsUse360DayInterestYear)
                    .WithMonthlyFee(monthlyFee)
                    .WithInitialFeeCapitalized(capitalizedInitialFeeAmount)
                    .EndCreate()
                    .EffectiveInterestRatePercent;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"{creditNr}: annuity={annuityAmount}, balance={balanceAmount}, interest={interestRatePercent}, mfee={monthlyFee}, iFee={capitalizedInitialFeeAmount}");
                return null;
            }

        }

        public class RatiBasisData
        {
            public string CreditNr { get; set; }
            public DateTime StartDate { get; set; }
            public int InitialRuntimeInMonths { get; set; }
            public int CurrentRuntimeInMonths { get; set; }
            public decimal InitialCapitalDebt { get; set; }
            public decimal CurrentCapitalDebt { get; set; }
            public decimal? InitialInterestRate { get; set; }
            public decimal? CurrentInterestRate { get; set; }
            public decimal? InitialEffectiveInterest { get; set; }
            public DateTime? DebtCollectionDate { get; set; }
            public decimal DebtCollectionInterestDebt { get; set; }
            public decimal DebtCollectionCapitalDebt { get; set; }
            public decimal CurrentInterestDebt { get; set; }
            public int OverdueDays { get; set; }
        }

        public List<RatiBasisData> GetRatiModel(CreditContext context, DateTime fromDate, DateTime toDate, IList<string> onlyTheseCreditNrs = null, Action<string> obeserveCreditNrMissingRuntime = null)
        {
            var cl = CreditType.CompanyLoan.ToString();
            var queryBase = context
                .CreditHeaders
                .Where(x => x.CreatedByEvent.TransactionDate <= toDate && x.CreditType != cl);

            if (onlyTheseCreditNrs != null)
            {
                queryBase = queryBase.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
            }

            Func<decimal, decimal, decimal, string, int?> computeRemainingRuntimeLoggingFailed = (annuityAmount, balanceAmount, interestRatePercent, creditNr) =>
        {
            string _;
            int nrOfMonths;

            if (!TryComputeRemainingRuntime(annuityAmount, balanceAmount, interestRatePercent, creditNr, out nrOfMonths, out _))
            {
                obeserveCreditNrMissingRuntime?.Invoke(creditNr);
                return null;
            }
            else
                return nrOfMonths;
        };

            var result = queryBase
                    .Select(x => new
                    {
                        x.CreditNr,
                        StartDate = x.CreatedByEvent.TransactionDate,
                        InitialAnnuityAmount = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        InitialNotificationFeeAmount = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.NotificationFee.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        CurrentAnnuityAmount = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString() && y.TransactionDate <= toDate)
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        InitialCapitalDebt = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && (y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString() || y.BusinessEvent.EventType == BusinessEventType.CapitalizedInitialFee.ToString()))
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        InitialCapitalizedInitialFee = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && (y.BusinessEvent.EventType == BusinessEventType.CapitalizedInitialFee.ToString()))
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        CurrentCapitalDebt = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.TransactionDate <= toDate)
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        CurrentInterestDebt = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.InterestDebt.ToString() && y.TransactionDate <= toDate)
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        CreditStatusItem = x
                            .DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.TransactionDate <= toDate)
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .FirstOrDefault(),
                        DebtCollectionCapitalDebt = -x
                            .Transactions
                            .Where(y => y.WriteoffId.HasValue && y.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString() && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        DebtCollectionInterestDebt = -x
                            .Transactions
                            .Where(y => y.WriteoffId.HasValue && y.BusinessEvent.EventType == BusinessEventType.CreditDebtCollectionExport.ToString() && y.AccountCode == TransactionAccountType.InterestDebt.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m,
                        OldestUnpaidDueDate = x
                            .Notifications
                            .Where(y => y.TransactionDate <= toDate && (!y.ClosedTransactionDate.HasValue || y.ClosedTransactionDate.Value > toDate))
                            .Select(y => (DateTime?)y.DueDate)
                            .Min(),
                        InitialMarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        InitialReferenceInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString() && y.BusinessEventId == x.CreatedByBusinessEventId)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        CurrentMarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString() && y.TransactionDate <= toDate)
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        CurrentReferenceInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString() && y.TransactionDate <= toDate)
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                    })
                    .Where(x => x.CreditStatusItem.Value == CreditStatus.SentToDebtCollection.ToString() || x.CreditStatusItem.Value == CreditStatus.Normal.ToString())
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.StartDate,
                        x.InitialAnnuityAmount,
                        x.CurrentAnnuityAmount,
                        x.InitialCapitalDebt,
                        x.InitialCapitalizedInitialFee,
                        x.InitialNotificationFeeAmount,
                        x.CurrentCapitalDebt,
                        x.CurrentInterestDebt,
                        x.DebtCollectionCapitalDebt,
                        x.DebtCollectionInterestDebt,
                        InitialInterestRate = x.InitialMarginInterestRate + (x.InitialReferenceInterestRate ?? 0m),
                        CurrentInterestRate = x.CurrentMarginInterestRate + (x.CurrentReferenceInterestRate ?? 0m),
                        OverdueDays = !x.OldestUnpaidDueDate.HasValue ? 0 : (toDate < x.OldestUnpaidDueDate.Value ? 0 : DbFunctions.DiffDays(x.OldestUnpaidDueDate.Value, toDate) ?? 0),
                        DebtCollectionDate = x.CreditStatusItem.Value == CreditStatus.SentToDebtCollection.ToString()
                                        ? (DateTime?)x.CreditStatusItem.TransactionDate
                                        : null
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.StartDate,
                        x.InitialAnnuityAmount,
                        InitialRuntimeInMonths = x.InitialAnnuityAmount.HasValue && x.InitialInterestRate.HasValue ? computeRemainingRuntimeLoggingFailed(x.InitialAnnuityAmount.Value, x.InitialCapitalDebt, x.InitialInterestRate.Value, x.CreditNr) : new int?(),
                        CurrentRuntimeInMonths = x.CurrentAnnuityAmount.HasValue && x.CurrentInterestRate.HasValue ? computeRemainingRuntimeLoggingFailed(x.CurrentAnnuityAmount.Value, x.CurrentCapitalDebt, x.CurrentInterestRate.Value, x.CreditNr) : new int?(),
                        x.InitialCapitalDebt,
                        x.InitialInterestRate,
                        x.CurrentInterestRate,
                        InitialEffectiveInterest = x.InitialAnnuityAmount.HasValue ? ComputeEffectiveInterestRate(x.InitialAnnuityAmount.Value, x.InitialCapitalDebt - x.InitialCapitalizedInitialFee, x.InitialInterestRate ?? 0m, x.InitialNotificationFeeAmount ?? 0m, x.InitialCapitalizedInitialFee, x.CreditNr) : new decimal?(),
                        x.DebtCollectionDate,
                        x.DebtCollectionInterestDebt,
                        x.DebtCollectionCapitalDebt,
                        x.CurrentInterestDebt,
                        x.OverdueDays,
                        x.CurrentCapitalDebt
                    })
                    .Select(x => new RatiBasisData
                    {
                        CreditNr = x.CreditNr,
                        StartDate = x.StartDate,
                        InitialRuntimeInMonths = x.InitialRuntimeInMonths ?? 0,
                        CurrentRuntimeInMonths = x.CurrentRuntimeInMonths ?? 0,
                        InitialCapitalDebt = x.InitialCapitalDebt,
                        CurrentCapitalDebt = x.CurrentCapitalDebt,
                        InitialInterestRate = x.InitialInterestRate,
                        CurrentInterestRate = x.CurrentInterestRate,
                        InitialEffectiveInterest = x.InitialEffectiveInterest,
                        DebtCollectionDate = x.DebtCollectionDate,
                        DebtCollectionInterestDebt = x.DebtCollectionInterestDebt,
                        DebtCollectionCapitalDebt = x.DebtCollectionCapitalDebt,
                        CurrentInterestDebt = x.CurrentInterestDebt,
                        OverdueDays = x.OverdueDays
                    })
                    .ToList();

            return result;
        }

        public class BusinessEventRatiData
        {
            public string CreditNr { get; set; }
            public string EventType { get; set; }
            public int EventId { get; set; }
            public DateTime TransactionDate { get; set; }
            public int? AfterEventRuntimeInMonths { get; set; }
            public decimal AfterEventCapitalDebt { get; set; }
            public decimal? AfterEventInterestRate { get; set; }
            public decimal? AfterEventEffectiveInterest { get; set; }
            public decimal? ByEventAddedCapitalDebt { get; set; }
            public decimal? CurrentInterstDebtFraction { get; set; }
        }

        private class PreBusinessEventRatiData
        {
            public CreditHeader Credit { get; set; }
            public BusinessEvent Event { get; set; }
        }

        private IQueryable<PreBusinessEventRatiData> GetPreBusinessEventRatiData(CreditContext context, DateTime fromDate, DateTime toDate, BusinessEventType businessEventType)
        {
            var cl = CreditType.CompanyLoan.ToString();

            if (businessEventType == BusinessEventType.AcceptedCreditTermsChange)
            {
                return context
               .CreditTermsChangeHeaders
               .Where(x => x.CommitedByEvent != null && x.CommitedByEvent.TransactionDate >= fromDate && x.CommitedByEvent.TransactionDate <= toDate && x.Credit.CreditType != cl)
               .Select(x => new PreBusinessEventRatiData
               {
                   Event = x.CommitedByEvent,
                   Credit = x.Credit
               });
            }
            else if (businessEventType == BusinessEventType.NewAdditionalLoan)
            {
                return context
                    .BusinessEvents
                    .Where(x => x.EventType == businessEventType.ToString() && x.TransactionDate >= fromDate && x.TransactionDate <= toDate)
                    .Select(x => new
                    {
                        Event = x,
                        ByEventAddedCapitalTransactions = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                    })
                    .Select(x => new
                    {
                        Event = x.Event,
                        Credit = x.ByEventAddedCapitalTransactions.Select(y => y.Credit).FirstOrDefault(),
                        NrOfCreditsConnectToEvent = x.ByEventAddedCapitalTransactions.Select(y => y.CreditNr).Distinct().Count(),
                    })
                    .Where(x => x.NrOfCreditsConnectToEvent == 1 && x.Credit.CreditType != cl) //Mass term changes are not supported by this report (and dont exist at this time so this is just a safeguard to avoid data corruption if it's ever added which it shouldn't be since that should be a separate business event)
                    .Select(x => new PreBusinessEventRatiData
                    {
                        Event = x.Event,
                        Credit = x.Credit
                    });
            }
            else
                throw new NotImplementedException();
        }

        public List<BusinessEventRatiData> GetRatiBusinessEventRatiDataDataForPeriod(CreditContext context, DateTime fromDate, DateTime toDate, BusinessEventType businessEventType, Action<string> observeCreditMissingValidPaymentPlan, IList<string> onlyTheseCreditNrs = null)
        {
            var queryBase = GetPreBusinessEventRatiData(context, fromDate, toDate, businessEventType);
            if (onlyTheseCreditNrs != null)
                queryBase = queryBase.Where(x => onlyTheseCreditNrs.Contains(x.Credit.CreditNr));

            Func<decimal, decimal, decimal, string, int?> computeRemainingRuntimeLoggingFailed = (annuityAmount, balanceAmount, interestRatePercent, creditNr) =>
            {
                string _;
                int nrOfMonths;

                if (!TryComputeRemainingRuntime(annuityAmount, balanceAmount, interestRatePercent, creditNr, out nrOfMonths, out _))
                {
                    observeCreditMissingValidPaymentPlan?.Invoke(creditNr);
                    return null;
                }
                else
                    return nrOfMonths;
            };

            var basis = queryBase
                .Select(x => new
                {
                    x.Event,
                    x.Credit,
                    AfterEventOrderedDatedCreditValues = x.Credit
                        .DatedCreditValues
                        .Where(y => (y.TransactionDate < x.Event.TransactionDate || (y.TransactionDate == x.Event.TransactionDate && y.BusinessEventId <= x.Event.Id)))
                        .OrderByDescending(y => y.TransactionDate)
                        .ThenByDescending(y => y.Timestamp),
                })
                .Select(x => new
                {
                    CreditNr = x.Credit.CreditNr,
                    EventType = x.Event.EventType,
                    EventId = x.Event.Id,
                    TransactionDate = x.Event.TransactionDate,
                    AfterEventAnnuityAmount = x
                        .AfterEventOrderedDatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    AfterEventNotificationFeeAmount = x
                        .AfterEventOrderedDatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.NotificationFee.ToString())
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    AfterEventMarginInterestRate = x
                        .AfterEventOrderedDatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    AfterEventReferenceInterestRate = x
                        .AfterEventOrderedDatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    AfterEventCapitalDebt = x.Credit
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()
                            && (y.TransactionDate < x.Event.TransactionDate || (y.TransactionDate == x.Event.TransactionDate && y.BusinessEventId <= x.Event.Id)))
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                    ByEventAddedCapitalDebt = x
                        .Event
                        .Transactions
                        .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                        .Sum(y => (decimal?)y.Amount) ?? 0m,
                })
                .ToList();

            var newBasis = basis
                .Select(x => new
                {
                    x.CreditNr,
                    x.EventId,
                    x.TransactionDate,
                    x.EventType,
                    AfterEventCapitalDebt = x.AfterEventCapitalDebt,
                    x.AfterEventAnnuityAmount,
                    AfterEventInterestRate = x.AfterEventMarginInterestRate.HasValue ? (x.AfterEventMarginInterestRate.Value + (x.AfterEventReferenceInterestRate ?? 0m)) : new decimal?(),
                    ByEventAddedCapitalDebt = x.ByEventAddedCapitalDebt,
                    x.AfterEventNotificationFeeAmount
                });

            var finalBasis = newBasis
                .Select(x =>
                {
                    try
                    {
                        return new BusinessEventRatiData
                        {
                            CreditNr = x.CreditNr,
                            EventId = x.EventId,
                            TransactionDate = x.TransactionDate,
                            EventType = businessEventType.ToString(),
                            AfterEventCapitalDebt = x.AfterEventCapitalDebt,
                            AfterEventInterestRate = x.AfterEventInterestRate,
                            ByEventAddedCapitalDebt = x.ByEventAddedCapitalDebt,

                            //NOTE: This is basically nonsense but the whole concept makes no sense in the middle of an active loan
                            //      Here we pretend basically that a new loan starts after the change with just monthly fee
                            //      The difference from the initial value will be quite large for small loans (in relation to the initial fee) where this happens close to the start date
                            AfterEventEffectiveInterest = x.AfterEventAnnuityAmount.HasValue ? ComputeEffectiveInterestRate(x.AfterEventAnnuityAmount.Value, x.AfterEventCapitalDebt, x.AfterEventInterestRate ?? 0m, x.AfterEventNotificationFeeAmount ?? 0m, 0m, x.CreditNr) : new decimal?(),

                            AfterEventRuntimeInMonths = x.AfterEventAnnuityAmount.HasValue && x.AfterEventInterestRate.HasValue ? computeRemainingRuntimeLoggingFailed(x.AfterEventAnnuityAmount.Value, x.AfterEventCapitalDebt, x.AfterEventInterestRate.Value, x.CreditNr) : new int?(),
                            CurrentInterstDebtFraction =
                                 x.EventType == BusinessEventType.AcceptedCreditTermsChange.ToString()
                                 ? 1m
                                 : (x.AfterEventCapitalDebt == 0m ? 0m : x.ByEventAddedCapitalDebt / x.AfterEventCapitalDebt)
                        };
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error with credit '{x.CreditNr}': {ex.Message}");
                    }

                });

            return finalBasis.ToList();
        }

        public class CreditRatiData
        {
            public int InitialMaturity { get; set; }
            public int Count { get; set; }
            public string ExampleCreditNr { get; set; }
            public decimal InitialCapitalDebt { get; set; }
            public decimal WeightedInitialInterestRate { get; set; }
            public decimal WeightedInitialEffectiveInterestRate { get; set; }
            public decimal? InterestDebt { get; set; }
        }

        public class RenegotiatedCreditsRatiData
        {
            public int Maturity { get; set; }
            public int Count { get; set; }
            public string ExampleCreditNr { get; set; }
            public decimal? CapitalDebt { get; set; }
            public decimal? WeightedInterestRate { get; set; }
            public decimal? WeightedEffectiveInterestRate { get; set; }
            public decimal InterestDebt { get; set; }
        }

        public class RATIQuarter : Quarter
        {
            public static RATIQuarter FromRatiYearAndOrdinal(int year, int inYearOrdinalNr)
            {
                switch (inYearOrdinalNr)
                {
                    case 1:
                        return new RATIQuarter
                        {
                            InYearOrdinalNr = inYearOrdinalNr,
                            FromDate = new DateTime(year, 1, 1),
                            ToDate = new DateTime(year, 3, 1).AddMonths(1).AddDays(-1),
                            LastMonthFromDate = new DateTime(year, 3, 1),
                            LastMonthToDate = new DateTime(year, 3, 31),
                            Name = $"Q1_{year}",
                            LastMonthName = $"March {year}"
                        };
                    case 2:
                        return new RATIQuarter
                        {
                            InYearOrdinalNr = inYearOrdinalNr,
                            FromDate = new DateTime(year, 4, 1),
                            ToDate = new DateTime(year, 6, 1).AddMonths(1).AddDays(-1),
                            LastMonthFromDate = new DateTime(year, 6, 1),
                            LastMonthToDate = new DateTime(year, 6, 30),
                            Name = $"Q2_{year}",
                            LastMonthName = $"June {year}"
                        };
                    case 3:
                        return new RATIQuarter
                        {
                            InYearOrdinalNr = inYearOrdinalNr,
                            FromDate = new DateTime(year, 7, 1),
                            ToDate = new DateTime(year, 9, 1).AddMonths(1).AddDays(-1),
                            LastMonthFromDate = new DateTime(year, 9, 1),
                            LastMonthToDate = new DateTime(year, 9, 30),
                            Name = $"Q3_{year}",
                            LastMonthName = $"September {year}"
                        };
                    case 4:
                        return new RATIQuarter
                        {
                            InYearOrdinalNr = inYearOrdinalNr,
                            FromDate = new DateTime(year, 10, 1),
                            ToDate = new DateTime(year, 12, 1).AddMonths(1).AddDays(-1),
                            LastMonthFromDate = new DateTime(year, 12, 1),
                            LastMonthToDate = new DateTime(year, 12, 31),
                            Name = $"Q4_{year}",
                            LastMonthName = $"December {year}"
                        };
                    default:
                        throw new ArgumentException("inYearOrdinalNr < 1 || inYearOrdinalNr > 4", "inYearOrdinalNr");
                }
            }
            public static RATIQuarter ContainingRatiDate(DateTime d)
            {
                if (d.Month <= 3)
                    return FromRatiYearAndOrdinal(d.Year, 1);
                else if (d.Month <= 6)
                    return FromRatiYearAndOrdinal(d.Year, 2);
                else if (d.Month <= 9)
                    return FromRatiYearAndOrdinal(d.Year, 3);
                else
                    return FromRatiYearAndOrdinal(d.Year, 4);
            }

            public DateTime LastMonthFromDate { get; set; }
            public DateTime LastMonthToDate { get; set; }
            public string LastMonthName { get; set; }
        }

    }
}