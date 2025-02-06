using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.Conversion;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCredit.HistoricalCreditModel;
using CreditDomainModelCreditType = nCredit.DomainModel.CreditType;

namespace nCredit
{
    public class AmortizationPlan
    {
        public AmortizationPlan()
        {
        }

        public int? SinglePaymentLoanRepaymentDays { get; set; }
        public CreditAmortizationModel AmortizationModel { get; set; }
        public decimal? CurrentCapitalDebt { get; set; }
        public decimal NotificationFee { get; set; }
        public int NrOfRemainingPayments { get; set; }
        public List<Item> Items { get; set; }
        public decimal FirstNotificationCostsAmount { get; set; }

        public class Item
        {
            public DateTime EventTransactionDate { get; set; }
            public decimal CapitalBefore { get; set; }
            public string EventTypeCode { get; set; }
            public decimal CapitalTransaction { get; set; }
            public decimal NotificationFeeTransaction { get; set; }
            public decimal? InterestTransaction { get; set; }
            public decimal TotalTransaction { get; set; }
            public bool IsWriteOff { get; set; }
            public bool IsFutureItem { get; set; }
            public bool IsTerminationLetterProcessSuspension { get; set; }
            public bool IsTerminationLetterProcessReActivation { get; set; }
            public string BusinessEventRoleCode { get; set; }
            public DateTime? FutureItemDueDate { get; set; }
            public decimal InitialFeeTransaction { get; set; }
        }

        public static Dictionary<string, HistoricalCreditModel> GetHistoricalCreditModels(string[] creditNrs, ICreditContextExtended context, bool isMortgageLoansEnabled, DateTime? now)
        {
            var allItems = new List<HistoricalCreditModel>(creditNrs.Length);

            foreach (var creditNrGroup in creditNrs.SplitIntoGroupsOfN(250))
            {
                var items = context
                    .CreditHeadersQueryable
                    .Where(x => creditNrGroup.Contains(x.CreditNr))
                    .Select(x => new
                    {
                        CreditNr = x.CreditNr,
                        Status = x.Status,
                        x.CreditType,
                        PendingFuturePaymentFreeMonths = x
                            .CreditFuturePaymentFreeMonths
                            .Where(y => y.CommitedByEvent == null && y.CancelledByEvent == null)
                            .Select(y => new HistoricalCreditModel.PendingFuturePaymentFreeMonthModel { ForMonth = y.ForMonth, Id = y.Id }),
                        CreatedByEvent = new HistoricalCreditModel.ModelBusinessEvent
                        {
                            Id = x.CreatedByEvent.Id,
                            EventType = x.CreatedByEvent.EventType,
                            TransactionDate = x.CreatedByEvent.TransactionDate,
                            Timestamp = x.CreatedByEvent.Timestamp
                        },
                        AnnuityAmount = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        MonthlyAmortizationAmount = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.MonthlyAmortizationAmount.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        AmortizationModel = x
                            .DatedCreditStrings
                            .Where(z => z.Name == DatedCreditStringCode.AmortizationModel.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => z.Value)
                            .FirstOrDefault(),
                        ReferenceInterestRatePercent = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault() ?? 0m,
                        NotificationFee = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.NotificationFee.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault() ?? 0m,
                        MarginInterestRatePercent = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        MortgageLoanEndDate = x
                            .DatedCreditDates
                            .Where(z => z.Name == DatedCreditDateCode.MortgageLoanEndDate.ToString() && !z.RemovedByBusinessEventId.HasValue)
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (DateTime?)z.Value)
                            .FirstOrDefault(),
                        MortgageLoanInterestRebindMonthCount = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        MortgageLoanNextInterestRebindDate = x
                            .DatedCreditDates
                            .Where(z => z.Name == DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (DateTime?)z.Value)
                            .FirstOrDefault(),
                        CurrentCapitalBalance = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= (now ?? DateTime.Now))
                            .Sum(y => (decimal?)y.Amount),
                        AmortizationExceptionUntilDate = x
                            .DatedCreditDates
                            .Where(z => z.Name == DatedCreditDateCode.AmortizationExceptionUntilDate.ToString() && !z.RemovedByBusinessEventId.HasValue)
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (DateTime?)z.Value)
                            .FirstOrDefault(),
                        NotificationDueDay = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.NotificationDueDay.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        ExceptionAmortizationAmount = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.ExceptionAmortizationAmount.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (decimal?)z.Value)
                            .FirstOrDefault(),
                        SinglePaymentLoanRepaymentDays = x
                            .DatedCreditValues
                            .Where(z => z.Name == DatedCreditValueCode.SinglePaymentLoanRepaymentDays.ToString())
                            .OrderByDescending(z => z.TransactionDate)
                            .ThenByDescending(z => z.Id)
                            .Select(z => (int?)z.Value)
                            .FirstOrDefault(),
                        Transactions = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString() || y.AccountCode == TransactionAccountType.InterestDebt.ToString() || y.AccountCode == TransactionAccountType.NotificationFeeDebt.ToString())
                            .Select(y => new HistoricalCreditModel.ModelTransaction
                            {
                                Id = y.Id,
                                AccountCode = y.AccountCode,
                                Amount = y.Amount,
                                IsIncomingPayment = y.IncomingPaymentId.HasValue,
                                IsWriteOff = y.WriteoffId.HasValue,
                                BusinessEvent = new HistoricalCreditModel.ModelBusinessEvent
                                {
                                    Id = y.BusinessEvent.Id,
                                    EventType = y.BusinessEvent.EventType,
                                    TransactionDate = y.BusinessEvent.TransactionDate,
                                    Timestamp = y.BusinessEvent.Timestamp
                                },
                                CreditNotificationDueDate = y.CreditNotification.DueDate,
                                PaymentFreeMonthDueDate = y.PaymentFreeMonth.DueDate,
                                BusinessEventRoleCode = y.BusinessEventRoleCode
                            }),
                        NrOfPaidNotifications = x
                            .Notifications
                            .Count(y => y.ClosedTransactionDate.HasValue),
                        ProcessSuspendingTerminationLetters = x
                            .TerminationLetters
                            .Where(y => y.SuspendsCreditProcess == true)
                            .Select(y => new ProcessSuspendingTerminationLetter
                            {
                                SuspendedFromDate = y.TransactionDate,
                                SuspendedToDate = y.InactivatedByBusinessEvent.TransactionDate
                            }),
                        FirstNotificationCostsAmount = (x
                            .CreatedByEvent.Transactions
                            .Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.NotNotifiedNotificationCost.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m)
                    })
                    .ToList();

                items.ForEach(x =>
                    allItems.Add(new HistoricalCreditModel
                    {
                        IsMortgageLoan = x.CreditType == CreditDomainModelCreditType.MortgageLoan.ToString(),
                        CreditType = x.CreditType,
                        EndDate = x.CreditType == CreditDomainModelCreditType.MortgageLoan.ToString() ? x.MortgageLoanEndDate : new DateTime?(),
                        InterestRebindMonthCount = x.CreditType == CreditDomainModelCreditType.MortgageLoan.ToString() ? x.MortgageLoanInterestRebindMonthCount : null,
                        NextInterestRebindDate = x.CreditType == CreditDomainModelCreditType.MortgageLoan.ToString() ? x.MortgageLoanNextInterestRebindDate : null,
                        CurrentCapitalBalance = x.CreditType == CreditDomainModelCreditType.MortgageLoan.ToString() ? x.CurrentCapitalBalance : null,
                        CreatedByEvent = x.CreatedByEvent,
                        CreditNr = x.CreditNr,
                        MarginInterestRatePercent = x.MarginInterestRatePercent,
                        NotificationFee = x.NotificationFee,
                        NrOfPaidNotifications = x.NrOfPaidNotifications,
                        PendingFuturePaymentFreeMonths = x.PendingFuturePaymentFreeMonths?.ToList(),
                        ReferenceInterestRatePercent = x.ReferenceInterestRatePercent,
                        Status = x.Status,
                        Transactions = x.Transactions?.ToList(),
                        AmortizationModel = CreditDomainModel.CreateAmortizationModel(x.AmortizationModel, () => x.AnnuityAmount.Value, () => x.MonthlyAmortizationAmount.Value, x.AmortizationExceptionUntilDate, x.ExceptionAmortizationAmount),
                        NotificationDueDay = x.NotificationDueDay.HasValue ? (int)Math.Round(x.NotificationDueDay.Value) : new int?(),
                        ProcessSuspendingTerminationLetters = x.ProcessSuspendingTerminationLetters.ToList(),
                        SinglePaymentLoanRepaymentDays = x.SinglePaymentLoanRepaymentDays,
                        FirstNotificationCostsAmount = x.FirstNotificationCostsAmount
                    }));
            }

            return allItems.ToDictionary(x => x.CreditNr, x => x);
        }

        public static HistoricalCreditModel GetHistoricalCreditModel(string creditNr, ICreditContextExtended context, bool isMortgageLoansEnabled, DateTime? now = null)
        {
            return GetHistoricalCreditModels(new[] { creditNr }, context, isMortgageLoansEnabled, now).Opt(creditNr);
        }

        public class PossiblePaymentFreeItem
        {
            public bool? IsPossible { get; set; }
            public Item Item { get; set; }
        }

        public List<PossiblePaymentFreeItem> GetPossibleFuturePaymentFreeMonths(int nrOfPaidNotifications, NotificationProcessSettings processSettings)
        {
            var pItems = Items.Select(x => new PossiblePaymentFreeItem { Item = x }).ToList();

            if (!processSettings.PaymentFreeMonthMinNrOfPaidNotifications.HasValue || nrOfPaidNotifications < processSettings.PaymentFreeMonthMinNrOfPaidNotifications.Value)
                return pItems;

            var nrOfPaymentFreeMonthsPerYear = Items
                .Where(x => x.EventTypeCode == BusinessEventType.PaymentFreeMonth.ToString())
                .GroupBy(x => x.EventTransactionDate.Year)
                .ToDictionary(x => x.Key, x => x.Count());
            Func<Item, int> getNrOfHistoricalOrFuturePaymentFreeMonthsInYear = i =>
            {
                if (!nrOfPaymentFreeMonthsPerYear.ContainsKey(i.EventTransactionDate.Year))
                    return 0;
                else
                    return nrOfPaymentFreeMonthsPerYear[i.EventTransactionDate.Year];
            };

            var filteredItems = pItems
                .Where(x => x.Item.EventTypeCode.IsOneOf(BusinessEventType.NewNotification.ToString(), BusinessEventType.PaymentFreeMonth.ToString()))
                .ToList();

            var minMonthsBetweenPaymentFreeMonths = processSettings.MinMonthsBetweenPaymentFreeMonths;
            if (!processSettings.AreBackToBackPaymentFreeMonthsAllowed && minMonthsBetweenPaymentFreeMonths == 0)
                minMonthsBetweenPaymentFreeMonths = 1;

            var indexedItems = filteredItems.Select((x, i) => new { Item = x, Index = i, IsPaymentFreeMonth = x.Item.EventTypeCode == BusinessEventType.PaymentFreeMonth.ToString() }).ToList();

            //Get the nr of months between the current month and latest payment free month before or the first payment free month after.
            int? GetSurroundingPaymentFreeMonthMonthsBetweenCount(int index, bool isNext)
            {
                var pre = (isNext ? indexedItems.Where(x => x.Index > index) : indexedItems.Where(x => x.Index < index)).Where(x => x.IsPaymentFreeMonth);
                var surroundingMonthDate = (isNext ? pre.OrderBy(x => x.Index) : pre.OrderByDescending(x => x.Index)).FirstOrDefault()?.Item?.Item?.EventTransactionDate;
                if (!surroundingMonthDate.HasValue)
                    return null;
                var surroundingMonth = Month.ContainingDate(surroundingMonthDate.Value);
                var currentMonth = Month.ContainingDate(indexedItems[index].Item.Item.EventTransactionDate);
                return Month.NrOfMonthsBetween(surroundingMonth, currentMonth);
            }

            foreach (var iterItem in indexedItems)
            {
                var pItem = iterItem.Item;
                var itemIndex = iterItem.Index;
                if (pItem.Item.EventTypeCode == BusinessEventType.NewNotification.ToString() && pItem.Item.IsFutureItem)
                {
                    var isBannedByMonthSpacing = false;
                    if(minMonthsBetweenPaymentFreeMonths > 0)
                    {
                        var previousMonthsBetweenCount = GetSurroundingPaymentFreeMonthMonthsBetweenCount(itemIndex, isNext: false);
                        var nextMonthsBetweenCount = GetSurroundingPaymentFreeMonthMonthsBetweenCount(itemIndex, isNext: true);
                        if (previousMonthsBetweenCount.HasValue && previousMonthsBetweenCount <= minMonthsBetweenPaymentFreeMonths || nextMonthsBetweenCount.HasValue && nextMonthsBetweenCount <= minMonthsBetweenPaymentFreeMonths)
                            isBannedByMonthSpacing = true;
                    }

                    pItem.IsPossible = !isBannedByMonthSpacing && getNrOfHistoricalOrFuturePaymentFreeMonthsInYear(pItem.Item) < processSettings.PaymentFreeMonthMaxNrPerYear;
                }
            }

            return pItems;
        }

        public static bool TryGetAmortizationPlan(HistoricalCreditModel c, NotificationProcessSettings processSettings, out AmortizationPlan p, out string failedMessage, ICoreClock clock, IClientConfigurationCore clientConfiguration, Func<DateTime, decimal> getDividerOverride, Action<DateTime, decimal> observeDailyInterestAmount = null)
        {
            if (c.SinglePaymentLoanRepaymentDays.HasValue)
                return TryGetAmortizationWhenSinglePaymentLoanRepaymentDays(c, clock, out p, out failedMessage);
            else if (c.NotificationDueDay.HasValue)
                return PerLoanDueDayAmortizationPlanCalculator.TryGetAmortizationPlan(c, processSettings, out p, out failedMessage, clock, clientConfiguration, getDividerOverride, observeDailyInterestAmount: observeDailyInterestAmount);
            else
                return FixedDueDayAmortizationPlanCalculator.TryGetAmortizationPlan(c, processSettings, out p, out failedMessage, clock, getDividerOverride, observeDailyInterestAmount: observeDailyInterestAmount);
        }

        private static bool TryGetAmortizationWhenSinglePaymentLoanRepaymentDays(HistoricalCreditModel model, ICoreClock clock, out AmortizationPlan p, out string failedMessage)
        {
            if (model == null)
            {
                p = null;
                failedMessage = "No such credit";
                return false;
            }

            if (!model.SinglePaymentLoanRepaymentDays.HasValue)
                throw new Exception("Needs a SinglePaymentLoanRepaymentDays-loan");

            if (model.Status != CreditStatus.Normal.ToString())
            {
                p = null;
                failedMessage = $"Credit is {model.Status}";
                return false;
            }
                                    
            var historicResult = PerLoanDueDayAmortizationPlanCalculator.GetHistoricMonths(model);
            var initialCapitalAmount = -historicResult.Items.Single(x => x.EventTypeCode == model.CreatedByEvent.EventType).CapitalTransaction;
            p = new AmortizationPlan
            {
                SinglePaymentLoanRepaymentDays = model.SinglePaymentLoanRepaymentDays,
                FirstNotificationCostsAmount = model.FirstNotificationCostsAmount,
                AmortizationModel = CreditAmortizationModel.CreateMonthlyFixedCapitalAmount(initialCapitalAmount, null, null, null),
                CurrentCapitalDebt = historicResult.CapitalDebt,
                Items = historicResult.Items,
                NotificationFee = model.NotificationFee,
                NrOfRemainingPayments = 0
            };
            
            if (!historicResult.Items.Any(x => x.EventTypeCode == BusinessEventType.NewNotification.ToString()))
            {
                var currentCapitalDebt = -historicResult.Items.Sum(x => x.CapitalTransaction);
                var plan = PaymentPlanCalculation
                    .CaclculateSinglePaymentWithRepaymentTimeInDays(currentCapitalDebt, model.SinglePaymentLoanRepaymentDays.Value,
                    model.ReferenceInterestRatePercent + (model.MarginInterestRatePercent ?? 0m),
                    initialFeeOnNotification: model.FirstNotificationCostsAmount,
                    notificationFee: model.NotificationFee);
                
                p.NrOfRemainingPayments = plan.Payments.Count;
                
                var capitalDebt = plan.InitialCapitalDebtAmount;
                foreach (var payment in plan.Payments)
                {
                    p.Items.Add(new AmortizationPlan.Item
                    {
                        CapitalBefore = capitalDebt,
                        CapitalTransaction = payment.Capital,
                        InterestTransaction = payment.Interest,
                        InitialFeeTransaction = payment.InitialFee,
                        NotificationFeeTransaction = payment.MonthlyFee,
                        TotalTransaction = payment.Capital + payment.Interest,
                        EventTransactionDate = clock.Today,
                        EventTypeCode = BusinessEventType.NewNotification.ToString(),
                        IsFutureItem = true,
                        FutureItemDueDate = clock.Today.AddDays(payment.NonStandardPaymentDays.Value)
                    });
                }
            }

            failedMessage = null;
            return true;
        }
    }

    public class NaturalOrderByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            // Shortcuts: If both are null, they are the same.
            if (x == null && y == null) return 0;

            // If one is null and the other isn't, then the
            // one that is null is "lesser".
            if (x == null && y != null) return -1;
            if (x != null && y == null) return 1;

            // Both arrays are non-null.  Find the shorter
            // of the two lengths.
            int bytesToCompare = Math.Min(x.Length, y.Length);

            // Compare the bytes.
            for (int index = 0; index < bytesToCompare; ++index)
            {
                // The x and y bytes.
                byte xByte = x[index];
                byte yByte = y[index];

                // Compare result.
                int compareResult = Comparer<byte>.Default.Compare(xByte, yByte);

                // If not the same, then return the result of the
                // comparison of the bytes, as they were the same
                // up until now.
                if (compareResult != 0) return compareResult;

                // They are the same, continue.
            }

            // The first n bytes are the same.  Compare lengths.
            // If the lengths are the same, the arrays
            // are the same.
            if (x.Length == y.Length) return 0;

            // Compare lengths.
            return x.Length < y.Length ? -1 : 1;
        }
    }

    public class HistoricalCreditModel
    {
        public string CreditNr { get; set; }
        public string Status { get; set; }
        public bool IsMortgageLoan { get; set; }
        public string CreditType { get; set; }
        public int? NotificationDueDay { get; set; }
        public int? SinglePaymentLoanRepaymentDays { get; set; }
        public decimal FirstNotificationCostsAmount { get; set; }

        public CreditDomainModelCreditType GetCreditType()
        {
            if (CreditType == null)
                return CreditDomainModelCreditType.UnsecuredLoan;
            else
                return Enums.Parse<CreditDomainModelCreditType>(this.CreditType).Value;
        }

        public DateTime? EndDate { get; set; }
        public decimal? InterestRebindMonthCount { get; set; }
        public DateTime? NextInterestRebindDate { get; set; }
        public decimal? CurrentCapitalBalance { get; set; }
        public ModelBusinessEvent CreatedByEvent { get; set; }
        public decimal ReferenceInterestRatePercent { get; set; }
        public decimal NotificationFee { get; set; }
        public decimal? MarginInterestRatePercent { get; set; }
        public List<ModelTransaction> Transactions { get; set; }
        public List<PendingFuturePaymentFreeMonthModel> PendingFuturePaymentFreeMonths { get; set; }
        public List<ProcessSuspendingTerminationLetter> ProcessSuspendingTerminationLetters { get; set; }
        public int NrOfPaidNotifications { get; set; }
        public CreditAmortizationModel AmortizationModel { get; set; }

        public class PendingFuturePaymentFreeMonthModel
        {
            public int Id { get; set; }
            public DateTime ForMonth { get; set; }
        }

        public class ProcessSuspendingTerminationLetter
        {
            public DateTime SuspendedFromDate { get; set; }
            public DateTime? SuspendedToDate { get; set; }
        }

        public class ModelBusinessEvent
        {
            public int Id { get; set; }
            public string EventType { get; set; }
            public DateTime TransactionDate { get; set; }
            public byte[] Timestamp { get; set; }
        }

        public class ModelTransaction
        {
            public long Id { get; set; }
            public string AccountCode { get; set; }
            public decimal Amount { get; set; }
            public bool IsWriteOff { get; set; }
            public bool IsIncomingPayment { get; set; }
            public ModelBusinessEvent BusinessEvent { get; set; }
            public DateTime? CreditNotificationDueDate { get; set; }
            public DateTime? PaymentFreeMonthDueDate { get; set; }
            public string BusinessEventRoleCode { get; set; }
        }
    }
}