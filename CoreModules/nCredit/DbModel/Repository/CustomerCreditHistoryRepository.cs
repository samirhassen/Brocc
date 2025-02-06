using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace nCredit.DbModel.Repository
{
    public class CustomerCreditHistoryRepository
    {
        private readonly CreditContextFactory contextFactory;

        public CustomerCreditHistoryRepository(CreditContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        private class CreditData
        {
            public int NrOfApplicants { get; set; }
            public string ProviderName { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public string Status { get; set; }
            public IEnumerable<int> CustomerIds { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentEver { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths { get; set; }
            public bool IsOrHasBeenOnDebtCollection { get; set; }
            public int NrOfClosedNotifications { get; set; }
            public decimal CapitalBalance { get; set; }
            public DateTime? CurrentlyOverdueSinceDate { get; set; }
            public string CreditType { get; set; }
        }

        public List<Credit> GetCustomerCreditHistory(List<int> customerIds, List<string> creditNrs)
        {
            var repo = new PartialCreditModelRepository();

            using (var context = contextFactory.CreateContext())
            {
                var sixMonthsBack = context.CoreClock.Today.AddMonths(-6);
                var today = context.CoreClock.Today;

                var pre = repo
                    .NewQuery(today)
                    .WithValues(DatedCreditValueCode.MarginInterestRate, DatedCreditValueCode.ReferenceInterestRate, DatedCreditValueCode.AnnuityAmount, DatedCreditValueCode.NotificationFee)
                    .WithStrings(DatedCreditStringCode.ApplicationNr)
                    .ExecuteExtended(context, baseQuery =>
                    {
                        var balanceAccountCode = TransactionAccountType.CapitalDebt.ToString();
                        if (creditNrs != null)
                        {
                            baseQuery = baseQuery.Where(x => creditNrs.Contains(x.Credit.CreditNr));
                        }
                        if (customerIds != null)
                        {
                            baseQuery = baseQuery.Where(x => x.Credit.CreditCustomers.Any(y => customerIds.Contains(y.CustomerId)));
                        }
                        return baseQuery.Select(y => new PartialCreditModelRepository.CreditFinalDataWrapper<CreditData>
                        {
                            BasicCreditData = y.BasicCreditData,
                            ExtraCreditData = new CreditData
                            {
                                NrOfApplicants = y.Credit.NrOfApplicants,
                                ProviderName = y.Credit.ProviderName,
                                StartDate = y.Credit.StartDate,
                                Status = y.Credit.Status,
                                CreditType = y.Credit.CreditType,
                                CustomerIds = y.Credit.CreditCustomers.Select(z => z.CustomerId),
                                MaxNrOfDaysBetweenDueDateAndPaymentEver = y
                                    .Credit
                                    .Notifications
                                    .Where(z => z.ClosedTransactionDate.HasValue)
                                    .Select(z => DbFunctions.DiffDays(z.DueDate, z.ClosedTransactionDate.Value))
                                    .Max(),
                                MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths = y
                                    .Credit
                                    .Notifications
                                    .Where(z => z.ClosedTransactionDate.HasValue && z.TransactionDate >= sixMonthsBack)
                                    .Select(z => DbFunctions.DiffDays(z.DueDate, z.ClosedTransactionDate.Value))
                                    .Max(),
                                IsOrHasBeenOnDebtCollection =
                                    (y.Credit.Status == CreditStatus.SentToDebtCollection.ToString())
                                    ||
                                    y.Credit.DatedCreditStrings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.SentToDebtCollection.ToString()),
                                NrOfClosedNotifications = y
                                    .Credit
                                    .Notifications
                                    .Count(z => z.ClosedTransactionDate.HasValue),
                                CapitalBalance = y
                                    .Credit
                                    .Transactions
                                    .Where(z => z.AccountCode == balanceAccountCode)
                                    .Sum(z => (decimal?)z.Amount) ?? 0m,
                                CurrentlyOverdueSinceDate =
                                    y.Credit.Status == CreditStatus.Normal.ToString()
                                    ? y
                                        .Credit
                                        .Notifications
                                        .Where(z => !z.ClosedTransactionDate.HasValue)
                                        .Min(z => (DateTime?)z.DueDate)
                                    : new DateTime?(),
                            }
                        });
                    });
                return pre.Select(x => new Credit
                {
                    CreditNr = x.CreditNr,
                    MarginInterestRatePercent = x.GetValue(DatedCreditValueCode.MarginInterestRate),
                    ReferenceInterestRatePercent = x.GetValue(DatedCreditValueCode.ReferenceInterestRate) ?? 0m,
                    AnnuityAmount = x.GetValue(DatedCreditValueCode.AnnuityAmount),
                    NotificationFeeAmount = x.GetValue(DatedCreditValueCode.NotificationFee),
                    ApplicationNr = x.GetString(DatedCreditStringCode.ApplicationNr),
                    ProviderName = x.ExtraData.ProviderName,
                    StartDate = x.ExtraData.StartDate,
                    CustomerIds = x.ExtraData.CustomerIds,
                    NrOfApplicants = x.ExtraData.NrOfApplicants,
                    Status = x.ExtraData.Status,
                    MaxNrOfDaysBetweenDueDateAndPaymentEver = x.ExtraData.MaxNrOfDaysBetweenDueDateAndPaymentEver,
                    MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths = x.ExtraData.MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths,
                    CapitalBalance = x.ExtraData.CapitalBalance,
                    IsOrHasBeenOnDebtCollection = x.ExtraData.IsOrHasBeenOnDebtCollection,
                    NrOfClosedNotifications = x.ExtraData.NrOfClosedNotifications,
                    CurrentlyOverdueSinceDate = x.ExtraData.CurrentlyOverdueSinceDate.HasValue && x.ExtraData.CurrentlyOverdueSinceDate.Value <= today
                        ? x.ExtraData.CurrentlyOverdueSinceDate.Value
                        : new DateTime?(),
                    IsMortgageLoan = x.ExtraData.CreditType == CreditType.MortgageLoan.ToString()
                }).ToList();
            }
        }

        public class Credit
        {
            public string CreditNr { get; set; }
            public int NrOfApplicants { get; set; }
            public string ProviderName { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public IEnumerable<int> CustomerIds { get; set; }
            public decimal CapitalBalance { get; set; }
            public decimal? MarginInterestRatePercent { get; set; }
            public decimal ReferenceInterestRatePercent { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? NotificationFeeAmount { get; set; }
            public DateTime? CurrentlyOverdueSinceDate { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentEver { get; set; }
            public int? MaxNrOfDaysBetweenDueDateAndPaymentLastSixMonths { get; set; }
            public bool IsOrHasBeenOnDebtCollection { get; set; }
            public int NrOfClosedNotifications { get; set; }
            public string ApplicationNr { get; set; }
            public string Status { get; set; }
            public bool IsMortgageLoan { get; set; }
        }
    }
}