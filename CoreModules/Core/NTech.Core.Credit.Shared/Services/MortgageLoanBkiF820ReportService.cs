using nCredit;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class MortgageLoanBkiF820ReportService
    {
        private readonly ICoreClock clock;
        private readonly CreditContextFactory creditContextFactory;

        public MortgageLoanBkiF820ReportService(ICoreClock clock, CreditContextFactory creditContextFactory)
        {
            this.clock = clock;
            this.creditContextFactory = creditContextFactory;
        }

        public ReportData CreateReportData(Quarter quarter)
        {
            var lastQuarterData = CreateReportDataInternal(quarter.GetPrevious());
            var reportData = CreateReportDataInternal(quarter);

            var impairedLastQuarter = lastQuarterData.Loans.Where(x => x.IsImpaired).Select(x => x.CreditNr).ToHashSetShared();
            reportData.Loans.ForEach(x => x.WasImpairedDuringLastQuarter = impairedLastQuarter.Contains(x.CreditNr));
            reportData.NrOfNewImpairedCreditsThisQuarter = reportData.Loans.Count(x => x.IsImpaired && !x.WasImpairedDuringLastQuarter);

            return reportData;
        }

        private ReportData CreateReportDataInternal(Quarter quarter)
        {
            var fd = quarter.FromDate;
            var td = quarter.ToDate;

            if (td > clock.Today)
                td = clock.Today;

            var feeAccountTypes = new List<string>
                {
                    TransactionAccountType.NotificationFeeDebt.ToString(),
                    TransactionAccountType.ReminderFeeDebt.ToString()
                };
            var paidInterestAccountTypes = new List<string>
                {
                    TransactionAccountType.InterestDebt.ToString(),
                    TransactionAccountType.SwedishRseDebt.ToString()
                };

            List<QuarterlyBKIReportItem> loans;

            using (var context = creditContextFactory.CreateContext())
            {
                loans = context
                    .CreditHeadersQueryable
                    .Where(x => x.CreatedByEvent.TransactionDate <= td)
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.CreditNr,
                        StartDate = x.CreatedByEvent.TransactionDate,
                        NrOfCustomers = x.CreditCustomers.Count(),
                        CustomerIds = x.CreditCustomers.Select(y => y.CustomerId).ToList(),
                        OldestOpenNotificationDueDate = x
                            .Notifications
                            .Where(y => y.TransactionDate <= td && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > td))
                            .Min(y => (DateTime?)y.DueDate),
                        FinalStatusItem = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= td && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .FirstOrDefault(),
                        PaidFeesAmount =
                            x.Transactions.Where(y =>
                                !y.WriteoffId.HasValue
                                && y.IncomingPaymentId.HasValue
                                && y.TransactionDate >= fd
                                && y.TransactionDate <= td
                                && feeAccountTypes.Contains(y.AccountCode))
                            .Sum(y => -(decimal?)y.Amount),
                        PaidInterestAmount = x.Transactions.Where(y =>
                                !y.WriteoffId.HasValue
                                && y.IncomingPaymentId.HasValue
                                && y.TransactionDate >= fd
                                && y.TransactionDate <= td
                                && paidInterestAccountTypes.Contains(y.AccountCode))
                            .Sum(y => (decimal?)y.Amount),
                        CapitalBalance = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= td)
                            .Sum(y => (decimal?)y.Amount),
                        InitialPaidToCustomerAmount = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        InitialFeeDrawnFromLoanAmount = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.InitialFeeDrawnFromLoanAmount.ToString())
                            .Sum(y => (decimal?)y.Amount),
                    })
                    .Select(x => new
                    {
                        x.CreditNr,
                        x.StartDate,
                        x.NrOfCustomers,
                        x.CustomerIds,
                        x.CapitalBalance,
                        x.PaidFeesAmount,
                        x.PaidInterestAmount,
                        x.InitialPaidToCustomerAmount,
                        FinalStatus = x.FinalStatusItem.Value,
                        x.OldestOpenNotificationDueDate,
                        x.InitialFeeDrawnFromLoanAmount
                    })
                    .ToList()
                    .Select(x =>
                    {
                        var nrOfOverdueDays = x.OldestOpenNotificationDueDate.HasValue
                            ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(td, x.OldestOpenNotificationDueDate.Value).TotalDays)
                            : 0;
                        var isNewInQuarter = quarter.ContainsDate(x.StartDate);
                        return new QuarterlyBKIReportItem
                        {
                            CreditNr = x.CreditNr,
                            StartDate = x.StartDate,
                            NrOfCustomers = x.NrOfCustomers,
                            CustomerIds = x.CustomerIds,
                            CapitalBalance = x.CapitalBalance ?? 0m,
                            FeeRevenue = (x.PaidFeesAmount ?? 0m)
                                + (isNewInQuarter ? (x.InitialFeeDrawnFromLoanAmount ?? 0m) : 0m),
                            InterestRevenue = -(x.PaidInterestAmount ?? 0m),
                            FinalStatus = x.FinalStatus,
                            NrOfOverdueDays = x.OldestOpenNotificationDueDate.HasValue
                            ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(td, x.OldestOpenNotificationDueDate.Value).TotalDays)
                            : 0,
                            InitialPaidOutAmount = (x.InitialPaidToCustomerAmount ?? 0m) - (x.InitialFeeDrawnFromLoanAmount ?? 0m),
                            IsNewInQuarter = quarter.ContainsDate(x.StartDate),
                            IsImpaired = nrOfOverdueDays > 90
                        };
                    })
                    .ToList();
            }

            var finallyActiveLoans = loans.Where(x => x.FinalStatus == CreditStatus.Normal.ToString()).ToList();

            var customerIdsByCreditNr = finallyActiveLoans.ToDictionary(x => x.CreditNr, x => x.CustomerIds.ToList());
            var activeCreditCustomerIds = finallyActiveLoans
                .SelectMany(x => customerIdsByCreditNr[x.CreditNr].Select(y => new { x.CreditNr, CustomerId = y }))
                .ToList();
            var uniqueCustomerIds = activeCreditCustomerIds.Select(x => x.CustomerId).Distinct();

            return new ReportData
            {
                Quarter = quarter,
                CapitalBalance = finallyActiveLoans.Sum(x => x.CapitalBalance),
                InterestRevenue = loans.Sum(x => x.InterestRevenue),
                FeeRevenue = loans.Sum(x => x.FeeRevenue),
                PaidOutAmountInQuarter = loans.Where(x => x.IsNewInQuarter).Sum(x => x.InitialPaidOutAmount),
                NrOfCredits = finallyActiveLoans.Count(),
                NrOfNewCreditsInQuarter = loans.Where(x => x.IsNewInQuarter).Count(),
                NrOfCustomers = uniqueCustomerIds.Count(),
                NrOfOverdueCredits = finallyActiveLoans.Where(x => x.NrOfOverdueDays > 90).Count(),
                ActiveCreditCustomers = activeCreditCustomerIds.Select(x => (x.CustomerId, x.CreditNr)).ToList(),
                Loans = loans,
                NrOfImpairedCredits = finallyActiveLoans.Where(x => x.IsImpaired).Count()
            };
        }

        public class ReportData
        {
            public Quarter Quarter { get; set; }
            public int NrOfCredits { get; set; }
            public int NrOfNewCreditsInQuarter { get; set; }
            public int NrOfCustomers { get; set; }
            public int NrOfOverdueCredits { get; set; }
            public int NrOfImpairedCredits { get; set; }
            public int NrOfNewImpairedCreditsThisQuarter { get; set; }
            public List<QuarterlyBKIReportItem> Loans { get; set; }
            public decimal CapitalBalance { get; set; }
            public decimal PaidOutAmountInQuarter { get; set; }
            public decimal InterestRevenue { get; set; }
            public decimal FeeRevenue { get; set; }
            public List<(int CustomerId, string CreditNr)> ActiveCreditCustomers { get; set; }
        }

        public class QuarterlyBKIReportItem
        {
            public string CreditNr { get; set; }
            public DateTime StartDate { get; set; }
            public int NrOfCustomers { get; set; }
            public List<int> CustomerIds { get; set; }
            public decimal CapitalBalance { get; set; }
            public decimal FeeRevenue { get; set; }
            public decimal InterestRevenue { get; set; }
            public string FinalStatus { get; set; }
            public int NrOfOverdueDays { get; set; }
            public decimal InitialPaidOutAmount { get; internal set; }
            public bool IsNewInQuarter { get; set; }
            public bool IsImpaired { get; set; }
            public bool WasImpairedDuringLastQuarter { get; set; }
        }
    }
}