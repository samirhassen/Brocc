using nCredit.DbModel;
using nCredit.DomainModel;
using NTech;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports.Shared
{
    public class LoanPerformanceReportModelService
    {
        private readonly CreditContextFactory creditContextFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly Func<List<AffiliateModel>> getAffiliateModels;
        private readonly CalendarDateService calendarDateService;
        private readonly PaymentOrderService paymentOrderService;

        public LoanPerformanceReportModelService(CreditContextFactory creditContextFactory, ICreditEnvSettings envSettings, Func<List<AffiliateModel>> getAffiliateModels,
            CalendarDateService calendarDateService, PaymentOrderService paymentOrderService)
        {
            this.creditContextFactory = creditContextFactory;
            this.envSettings = envSettings;
            this.getAffiliateModels = getAffiliateModels;
            this.calendarDateService = calendarDateService;
            this.paymentOrderService = paymentOrderService;
        }

        public class CreditModel
        {
            public string CreditNr { get; set; }
            public DateTime CreationDate { get; set; }
            public DateTime? MortgageLoanInitialSettlementDate { get; set; }
            public decimal? MarginInterestRate { get; set; }
            public decimal? ReferenceInterestRate { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? NotificationFee { get; set; }
            public int? NotificationDueDay { get; set; }
            public bool IsForNonPropertyUse { get; set; }
            public decimal? NotifiedInterestDebt { get; set; }
            public string ProviderDisplayName { get; set; }
            public string Status { get; set; }
            public DateTime? StatusDate { get; set; }
            public decimal? InitialCapitalBalance { get; set; }
            public decimal? CapitalBalance { get; set; }
            public decimal? BookKeepingCapitalBalance { get; set; }
            public int NrOfOverdueCount { get; set; }
            public decimal? PaidInterestAmountTotal { get; set; }
            public decimal? StoredInitialEffectiveInterestRatePercent { get; set; }
            public int NrOfOverdueDays { get; set; }
            public decimal NotNotifiedInterestBalance { get; set; }
            public decimal TotalInterestBalance { get; set; }
            public decimal NotfiedBalance { get; set; }
            public HashSet<string> ConnectedCreditNrs { get; set; }
            public string MortgageLoanOwner { get; set; }
            public DateTime? MortgageLoanNextInterestRebindDate { get; set; }
            public int? MortgageLoanInterestRebindMonthCount { get; set; }
        }

        public List<CreditModel> GetCredits(DateTime d, List<TransactionAccountType> paidFeesAccountTypes,
                    string onlyThisCreditNr = null, Action<List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem>> setInterestDetails = null,
                    Action<Dictionary<string, List<int>>> setCustomerIdsByCreditNr = null) =>
            GetCredits(d, paidFeesAccountTypes,
                onlyTheseCreditNrs: string.IsNullOrWhiteSpace(onlyThisCreditNr) ? null : new HashSet<string> { onlyThisCreditNr },
                setInterestDetails: setInterestDetails,
                setCustomerIdsByCreditNr: setCustomerIdsByCreditNr);

        public List<CreditModel> GetCredits(DateTime d, List<TransactionAccountType> paidFeesAccountTypes,
            HashSet<string> onlyTheseCreditNrs, Action<List<CreditNotNotifiedInterestRepository.CreditNotNotifiedInterestDetailItem>> setInterestDetails = null,
            Action<Dictionary<string, List<int>>> setCustomerIdsByCreditNr = null)
        {
            var paidFeesAccountTypesStrings = (paidFeesAccountTypes ?? new List<TransactionAccountType>()).Select(x => x.ToString()).ToList();

            using (var context = creditContextFactory.CreateContext())
            {
                var creditsBasis = context
                    .CreditHeadersQueryable.Where(x => x.CreatedByEvent.TransactionDate <= d);

                if (onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0)
                {
                    creditsBasis = creditsBasis.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr));
                }

                var paidInterestAccountTypes = new List<string>
                {
                    TransactionAccountType.InterestDebt.ToString(),
                    TransactionAccountType.SwedishRseDebt.ToString()
                };

                var creditsPre = creditsBasis
                    .OrderBy(x => x.CreatedByBusinessEventId)
                    .Select(x => new
                    {
                        x.CreditNr,
                        CreationDate = x.CreatedByEvent.TransactionDate,
                        MortgageLoanInitialSettlementDate = x
                            .DatedCreditDates
                            .Where(f => f.CreditNr == x.CreditNr && f.Name == DatedCreditDateCode.MortgageLoanInitialSettlementDate.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (DateTime?)y.Value)
                            .FirstOrDefault(),
                        MarginInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MarginInterestRate.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        ReferenceInterestRate = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        AnnuityAmount = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        NotificationFee = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.NotificationFee.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        NotificationDueDay = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.NotificationDueDay.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (int?)y.Value)
                            .FirstOrDefault(),
                        IsForNonPropertyUse = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.IsForNonPropertyUse.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault() == "true",
                        MainCreditCreditNr = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.MainCreditCreditNr.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        CapitalBalance = x
                            .Transactions
                            .Where(y => y.TransactionDate <= d && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        NotifiedInterestDebt = x
                            .Transactions
                            .Where(y => y.CreditNotificationId.HasValue && y.TransactionDate <= d && y.AccountCode == TransactionAccountType.InterestDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        x.ProviderName,
                        Status = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => y.Value)
                            .FirstOrDefault(),
                        StatusDate = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.CreditStatus.ToString())
                            .OrderByDescending(y => y.TransactionDate)
                            .ThenByDescending(y => y.Timestamp)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault(),
                        InitialCapitalBalance = x
                            .CreatedByEvent
                            .Transactions
                            .Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount),
                        BookKeepingCapitalBalance = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= d)
                            .Sum(y => (decimal?)y.Amount),
                        OldestOpenNotificationDueDate = x
                            .Notifications
                            .Where(y => y.DueDate < d && y.TransactionDate <= d && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > d))
                            .Min(y => (DateTime?)y.DueDate),
                        NrOfOverdueCount = x
                            .Notifications
                            .Where(y => y.DueDate < d && y.TransactionDate <= d && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > d))
                            .Count(),
                        PaidInterestAmountTotal = x.Transactions.Where(y =>
                                !y.WriteoffId.HasValue
                                && y.IncomingPaymentId.HasValue
                                && y.TransactionDate <= d
                                && paidInterestAccountTypes.Contains(y.AccountCode))
                            .Sum(y => -(decimal?)y.Amount),
                        StoredInitialEffectiveInterestRatePercent = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.InitialEffectiveInterestRatePercent.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (decimal?)y.Value)
                            .FirstOrDefault(),
                        CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                        MortgageLoanOwner = x
                            .DatedCreditStrings
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditStringCode.LoanOwner.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.Value)
                            .FirstOrDefault() ?? "[none]",
                        MortgageLoanNextInterestRebindDate = x
                            .DatedCreditDates
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditDateCode.MortgageLoanNextInterestRebindDate.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.RemovedByBusinessEventId == null ? (DateTime?)y.Value : null)
                            .FirstOrDefault(),
                        MortgageLoanInterestRebindMonthCount = x
                            .DatedCreditValues
                            .Where(y => y.TransactionDate <= d && y.Name == DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString())
                            .OrderByDescending(y => y.Id)
                            .Select(y => (int?)y.Value)
                            .FirstOrDefault(),
                    })
                    .ToList();

                if (setCustomerIdsByCreditNr != null)
                {
                    var customerIdsByCreditNr = creditsPre.ToDictionary(x => x.CreditNr, x => x.CustomerIds.ToList());
                    setCustomerIdsByCreditNr(customerIdsByCreditNr);
                }
                
                var notificationsByCreditNr = CreditNotificationDomainModel.CreateForSeveralCredits(creditsPre.Select(x => x.CreditNr).ToHashSetShared(), context, paymentOrderService.GetPaymentOrderItems(),
                    onlyFetchOpen: true);
                var connectedCreditsByCreditNr = creditsPre
                    .Where(x => x.MainCreditCreditNr != null)
                    .Select(x => new { x.CreditNr, x.MainCreditCreditNr })
                    .ToList()
                    .SelectMany(x => new[] { Tuple.Create(x.CreditNr, x.MainCreditCreditNr), Tuple.Create(x.MainCreditCreditNr, x.CreditNr) })
                    .GroupBy(x => x.Item1)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.Item2).ToHashSetShared());

                var notNotifiedInterestRepo = new CreditNotNotifiedInterestRepository(envSettings, creditContextFactory, calendarDateService);

                var notNotifiedInterestByCreditNr = notNotifiedInterestRepo.GetNotNotifiedInterestAmount(d, onlyTheseCreditNrs: onlyTheseCreditNrs, includeDetails: setInterestDetails);

                Func<string, string, decimal> getNotNotifiedInterestAmount = (cnr, status) =>
                {
                    if (!notNotifiedInterestByCreditNr.ContainsKey(cnr))
                        return 0m;

                    if (status != "Normal")
                        return 0m;

                    return notNotifiedInterestByCreditNr[cnr];
                };

                Func<decimal?, string, string, decimal> getTotalInterestBalance = (notifiedInterestDebt, cnr, creditStatus) =>
                    (notifiedInterestDebt ?? 0m) + getNotNotifiedInterestAmount(cnr, creditStatus);

                var affiliates = getAffiliateModels();
                var affiliateDisplayNames = affiliates.ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName);

                return creditsPre.Select(x => new CreditModel
                {
                    CreditNr = x.CreditNr,
                    IsForNonPropertyUse = x.IsForNonPropertyUse,
                    CreationDate = x.CreationDate,
                    MortgageLoanInitialSettlementDate = x.MortgageLoanInitialSettlementDate,
                    InitialCapitalBalance = x.InitialCapitalBalance,
                    MarginInterestRate = x.MarginInterestRate,
                    ReferenceInterestRate = x.ReferenceInterestRate,
                    BookKeepingCapitalBalance = x.BookKeepingCapitalBalance,
                    CapitalBalance = x.CapitalBalance,
                    NotifiedInterestDebt = x.NotifiedInterestDebt,
                    PaidInterestAmountTotal = x.PaidInterestAmountTotal,
                    ProviderDisplayName = affiliateDisplayNames?.Opt(x.ProviderName) ?? x.ProviderName,
                    Status = x.Status,
                    StatusDate = x.StatusDate,
                    NrOfOverdueCount = x.NrOfOverdueCount,
                    NrOfOverdueDays = x.OldestOpenNotificationDueDate.HasValue
                            ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(d, x.OldestOpenNotificationDueDate.Value).TotalDays)
                            : 0,
                    NotNotifiedInterestBalance = getNotNotifiedInterestAmount(x.CreditNr, x.Status),
                    TotalInterestBalance = getTotalInterestBalance(x.NotifiedInterestDebt, x.CreditNr, x.Status),
                    NotfiedBalance = notificationsByCreditNr.ContainsKey(x.CreditNr) ? notificationsByCreditNr[x.CreditNr].Values.Sum(y => y.GetRemainingBalance(d)) : 0m,
                    AnnuityAmount = x.AnnuityAmount,
                    NotificationFee = x.NotificationFee,
                    NotificationDueDay = x.NotificationDueDay,
                    ConnectedCreditNrs = connectedCreditsByCreditNr.ContainsKey(x.CreditNr) ? connectedCreditsByCreditNr[x.CreditNr] : null,
                    StoredInitialEffectiveInterestRatePercent = x.StoredInitialEffectiveInterestRatePercent,
                    MortgageLoanOwner = x.MortgageLoanOwner,
                    MortgageLoanNextInterestRebindDate = x.MortgageLoanNextInterestRebindDate,
                    MortgageLoanInterestRebindMonthCount = x.MortgageLoanInterestRebindMonthCount
                }).ToList();
            }
        }
    }
}