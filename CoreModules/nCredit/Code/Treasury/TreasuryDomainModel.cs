using nCredit.DbModel.DomainModel;
using nCredit.DbModel.Repository;
using nCredit.DomainModel;
using NTech;
using NTech.Banking.LoanModel;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace nCredit.Code.Treasury
{
    public class TreasuryDomainModel
    {
        private readonly IClock clock;

        public List<CashFlowItemModel> TransactionsConsumerLoanCashFlow { get; set; }

        public List<TransactionConsumerModel> TransactionsConsumerLoans { get; set; }

        public List<TransactionConsumerModel> TransactionsCorporateLoans { get; set; }

        public List<GurantorsModel> GurantorsCorporateLoans { get; set; }

        public List<CashFlowItemModel> TransactionsCompanyLoanCashFlow { get; set; }

        private TreasuryDomainModel(IClock clock)
        {
            this.clock = clock;
        }

        public static TreasuryDomainModel GetTreasuryDomainModel(IClock clock, INTechCurrentUserMetadata user)
        {
            var d = new TreasuryDomainModel(clock);

            using (var context = new CreditContextExtended(user, clock))
            {
                if (NEnv.IsUnsecuredLoansEnabled)
                {
                    d.TransactionsConsumerLoanCashFlow = d.GetTransactionsCashFlow(context, CreditType.UnsecuredLoan.ToString());
                    d.TransactionsConsumerLoans = d.GetTransactionsConsumers(context, d.TransactionsConsumerLoanCashFlow);
                }

                if (NEnv.IsCompanyLoansEnabled)
                {
                    d.TransactionsCorporateLoans = d.GetTransactionsCorporateLoans(context);
                    d.TransactionsCompanyLoanCashFlow = d.GetTransactionsCashFlow(context, CreditType.CompanyLoan.ToString());
                    d.GurantorsCorporateLoans = d.GetGurantorsCorporateLoans(context);
                }
            }

            return d;
        }

        public List<CashFlowItemModel> GetTransactionsCashFlow(CreditContextExtended context, string creditType)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (NEnv.IsUnsecuredLoansEnabled && string.IsNullOrWhiteSpace(f.Req("PrefixLoan")))
                throw new System.InvalidOperationException("PrefixLoan is missing in " + file.FullName);
            if (NEnv.IsCompanyLoansEnabled && string.IsNullOrWhiteSpace(f.Req("PrefixCompanyloanId")))
                throw new System.InvalidOperationException("PrefixCompanyloanId is missing in " + file.FullName);

            var model = new TreasuryDomainModel(clock);

            var allCreditNrs = context.CreditHeaders.Where(x => x.CreditType == creditType && x.Status == CreditStatus.Normal.ToString()).Select(x => x.CreditNr).ToArray();

            return model.GetCashFlowItems(context, allCreditNrs.ToList(), creditType);
        }

        private class InternalCreditModel
        {
            public int? CreditCustomer1Id { get; internal set; }
            public int? CreditCustomer2Id { get; internal set; }
            public string CreditNr { get; internal set; }
            public DateTimeOffset StartDate { get; internal set; }
            public decimal CurrentBalance { get; internal set; }
            public decimal AccruedIntrerest { get; internal set; }
            public bool IsDebtCollection { get; internal set; }
            public decimal BookKeepingCapitalDebtExceptNewLoans { get; internal set; }
            public decimal BookKeepingOutgoingPaymentFileAmount { get; internal set; }
            public decimal InitialPayoutAmountNewCredit { get; set; }
            public decimal InitialPayoutAmountNewAdditionalLoan { get; set; }
            public decimal InitialCapitalDebt { get; internal set; }
            public decimal InitialTermsReferenceInterestRate { get; internal set; }
            public decimal InitialTermsAnnuityAmount { get; internal set; }
            public decimal InitialTermsMarginInterestRate { get; internal set; }
            public DateTime? OldestOpenNotificationDueDate { get; internal set; }
            public string AmortizationModel { get; set; }
            public DateTime? CollectionDate { get; set; }
            public string ProviderName { get; set; }
            public string ApplicationNr { get; set; }
        }

        public List<TransactionConsumerModel> GetTransactionsConsumers(CreditContext context, List<CashFlowItemModel> monthlyItems)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            Lazy<int> dueDay = new Lazy<int>(() => NEnv.NotificationProcessSettings.GetByCreditType(CreditType.UnsecuredLoan).NotificationDueDay);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixLoan")))
                throw new System.InvalidOperationException("PrefixLoan is missing in " + file.FullName);

            var cl = CreditType.UnsecuredLoan.ToString();

            var includedStatuses = new List<string>();
            includedStatuses.Add(CreditStatus.Normal.ToString());
            if (!f.OptBool("ConsumerLoansExcludeDebtCollection"))
            {
                includedStatuses.Add(CreditStatus.SentToDebtCollection.ToString());
            }
            var creditsPre = context.CreditHeaders.Where(x => x.CreditType == cl && includedStatuses.Contains(x.Status));
            var creditsPerCustomers = creditsPre
            .Select(x => new
            {
                CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
            })
            .ToList();

            var allCustomerIds = creditsPerCustomers.SelectMany(x => x.CustomerIds).Distinct().ToList();
            //Get customerinfo
            var client = new CreditCustomerClient();
            var result = client.BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(allCustomerIds), "firstName", "lastName", "addressCountry", "companyName", "isCompany");

            string getValue(string n, int cid) =>
               result.Opt(cid)?.SingleOrDefault(x => x.Key.Equals(n, StringComparison.OrdinalIgnoreCase)).Value;

            var newLoanAmountEventTypes = new List<string> { BusinessEventType.NewCredit.ToString(), BusinessEventType.NewAdditionalLoan.ToString() };
            var d = clock.Today;

            var allCreditNrs = creditsPre.Select(x => x.CreditNr).ToArray();
            var credits = new List<InternalCreditModel>(allCreditNrs.Length);
            foreach (var creditNrGroup in allCreditNrs.SplitIntoGroupsOfN(500))
            {
                //Split up the lookups since it caused timeouts
                credits.AddRange(creditsPre
                    .Where(x => creditNrGroup.Contains(x.CreditNr))
                    .Select(x => new InternalCreditModel
                    {
                        CreditCustomer1Id = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                        CreditCustomer2Id = x.CreditCustomers.Where(y => y.ApplicantNr == 2).Select(y => (int?)y.CustomerId).FirstOrDefault(),
                        CreditNr = x.CreditNr,
                        StartDate = x.StartDate,
                        CurrentBalance = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => y.Amount),
                        AccruedIntrerest = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.InterestDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                        IsDebtCollection = x.Status == CreditStatus.SentToDebtCollection.ToString(),
                        BookKeepingCapitalDebtExceptNewLoans = (x
                                .Transactions
                                .Where(y => !newLoanAmountEventTypes.Contains(y.BusinessEvent.EventType) && y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= d)
                                .Sum(y => (decimal?)y.Amount) ?? 0m),
                        BookKeepingOutgoingPaymentFileAmount = -(x
                                .Transactions
                                .Where(y => y.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString() && y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.BookKeepingDate <= d)
                                .Sum(y => (decimal?)y.Amount) ?? 0m),
                        InitialCapitalDebt = x.CreatedByEvent.Transactions.Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => y.Amount),
                        InitialTermsReferenceInterestRate = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString()).Select(y => y.Value).FirstOrDefault(),
                        InitialTermsAnnuityAmount = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.AnnuityAmount.ToString()).Select(y => y.Value).FirstOrDefault(),
                        InitialTermsMarginInterestRate = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.MarginInterestRate.ToString()).Select(y => y.Value).FirstOrDefault(),
                        InitialPayoutAmountNewCredit = (x
                            .Transactions
                            .Where(y => y.BusinessEvent.EventType == BusinessEventType.NewCredit.ToString() && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                        InitialPayoutAmountNewAdditionalLoan = (x
                            .Transactions
                            .Where(y => y.BusinessEvent.EventType == BusinessEventType.NewAdditionalLoan.ToString() && y.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                        OldestOpenNotificationDueDate = x
                                .Notifications
                                .Where(y => y.TransactionDate <= d && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > d))
                                .Min(y => (DateTime?)y.DueDate),
                        CollectionDate = x.DatedCreditStrings
                            .Where(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() &&
                                        y.Value == CreditStatus.SentToDebtCollection.ToString())
                            .OrderByDescending(y => y.BusinessEventId)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault(),
                        ProviderName = x.ProviderName,
                        ApplicationNr = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.ApplicationNr.ToString()).Select(y => y.Value).FirstOrDefault()
                        //ApplicationNr = x.DatedCreditStrings.FirstOrDefault(y => y.Name == DatedCreditStringCode.ApplicationNr.ToString())?.Value
                    })
                .ToList());
            }

            var useActualBalance = f.OptBool("ConsumerLoansUseActualCurrentBalance");

            return credits
                .Select(x =>
                {
                    var currentBalance = x.CurrentBalance;
                    if (!useActualBalance)
                    {
                        //Same as the bookkeeping ledger. Intention is to push the initial balance from when the loan is created
                        //to when the outgoing payment file is created which is sometimes the day after.
                        currentBalance = x.BookKeepingCapitalDebtExceptNewLoans + x.BookKeepingOutgoingPaymentFileAmount;
                    }

                    DateTime endDate;
                    int? nrOfDaysOverdue;
                    if (!x.IsDebtCollection)
                    {
                        endDate = monthlyItems.Where(z => z.CreditNr == x.CreditNr).OrderByDescending(y => y.CashflowDate).Select(y => y.CashflowDate).FirstOrDefault();
                        nrOfDaysOverdue = x.OldestOpenNotificationDueDate.HasValue && x.OldestOpenNotificationDueDate.Value < d
                            ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(d, x.OldestOpenNotificationDueDate.Value).TotalDays)
                            : 0;
                    }
                    else
                    {
                        //Initial end date.
                        endDate = x
                            .StartDate
                            .DateTime
                            .AddMonths(PaymentPlanCalculation
                                .BeginCreateWithAnnuity(x.InitialCapitalDebt, x.InitialTermsAnnuityAmount, x.InitialTermsMarginInterestRate + x.InitialTermsReferenceInterestRate, null, NEnv.CreditsUse360DayInterestYear)
                                .EndCreate()
                                .Payments.Count);
                        endDate = new DateTime(endDate.Year, endDate.Month, dueDay.Value);

                        nrOfDaysOverdue = null;
                    }

                    return new TransactionConsumerModel
                    {
                        CustomerId1 = x.CreditCustomer1Id.Value,
                        CustomerId2 = x.CreditCustomer2Id,
                        CustomerFullName1 = $"{getValue("firstName", x.CreditCustomer1Id.Value)} {getValue("lastName", x.CreditCustomer1Id.Value)}",
                        CustomerCountry1 = getValue("addressCountry", x.CreditCustomer1Id.Value) ?? NEnv.ClientCfg.Country.BaseCountry,
                        CustomerFullName2 = x.CreditCustomer2Id.HasValue ? $"{getValue("firstName", x.CreditCustomer2Id.Value)} {getValue("lastName", x.CreditCustomer2Id.Value)}" : "",
                        CustomerCountry2 = x.CreditCustomer2Id.HasValue ? getValue("addressCountry", x.CreditCustomer2Id.Value) ?? NEnv.ClientCfg.Country.BaseCountry : "",
                        CreditNr = x.CreditNr,
                        CurrentBalance = currentBalance,
                        AccruedIntrerest = x.AccruedIntrerest,
                        StartDate = x.StartDate,
                        EndDate = endDate,
                        IsDebtCollection = x.IsDebtCollection,
                        DaysPastDue = nrOfDaysOverdue,
                        TotalInterestRate = x.InitialTermsMarginInterestRate + x.InitialTermsReferenceInterestRate,
                        CollectionDate = x.CollectionDate,
                        ProviderName = x.ProviderName,
                        ApplicationNr = x.ApplicationNr,
                        InitialPayoutAmount = x.InitialPayoutAmountNewCredit,
                        AdditionalLoanAmount = x.InitialPayoutAmountNewAdditionalLoan
                    };
                }).ToList();
        }

        public List<TransactionConsumerModel> GetTransactionsCorporateLoans(CreditContextExtended context)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            Lazy<int> dueDay = new Lazy<int>(() => NEnv.NotificationProcessSettings.GetByCreditType(CreditType.CompanyLoan).NotificationDueDay);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixLoan")))
                throw new System.InvalidOperationException("PrefixLoan is missing in " + file.FullName);

            var cl = CreditType.CompanyLoan.ToString();

            var includedStatuses = new List<string>();
            includedStatuses.Add(CreditStatus.Normal.ToString());
            if (!f.OptBool("CompanyLoansExcludeDebtCollection"))
            {
                includedStatuses.Add(CreditStatus.SentToDebtCollection.ToString());
            }

            var creditsPre = context.CreditHeaders.Where(x => x.CreditType == cl && includedStatuses.Contains(x.Status));

            var creditsPerCustomers = creditsPre
            .Select(x => new
            {
                CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
            })
            .ToList();

            var monthlyItems = GetCashFlowItems(
                context,
                creditsPre.Where(x => x.Status == CreditStatus.Normal.ToString()).Select(x => x.CreditNr).ToArray(),
                CreditType.CompanyLoan.ToString());

            var allCustomerIds = creditsPerCustomers.SelectMany(x => x.CustomerIds).Distinct().ToList();
            //Get customerinfo
            var client = new CreditCustomerClient();
            var result = client.BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(allCustomerIds), "addressCountry", "snikod", "companyName", "orgnr");

            string getValue(string n, int cid) =>
               result.Opt(cid)?.SingleOrDefault(x => x.Key.Equals(n, StringComparison.OrdinalIgnoreCase)).Value;

            var newLoanAmountEventTypes = new List<string> { BusinessEventType.NewCredit.ToString(), BusinessEventType.NewAdditionalLoan.ToString() };
            var d = clock.Today;

            var credits = creditsPre
                 .Select(x => new
                 {
                     CreditCustomer1 = x.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => new { y.CustomerId }).FirstOrDefault(),
                     x.CreditNr,
                     x.StartDate,
                     IsDebtCollection = x.Status == CreditStatus.SentToDebtCollection.ToString(),
                     CurrentBalance = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => y.Amount),
                     BookKeepingCapitalDebtExceptNewLoans = (x
                            .Transactions
                            .Where(y => !newLoanAmountEventTypes.Contains(y.BusinessEvent.EventType) && y.AccountCode == TransactionAccountType.CapitalDebt.ToString() && y.BookKeepingDate <= d)
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                     BookKeepingOutgoingPaymentFileAmount = -(x
                            .Transactions
                            .Where(y => y.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString() && y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.BookKeepingDate <= d)
                            .Sum(y => (decimal?)y.Amount) ?? 0m),
                     IsImportedLoan = x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.BeforeImportCreditNr.ToString()),
                     InitialCapitalDebt = x.CreatedByEvent.Transactions.Where(y => y.CreditNr == x.CreditNr && y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => y.Amount),
                     InitialPaymentTransactionDate = x
                            .Transactions
                            .Where(y => y.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && y.Amount < 0 && y.BusinessEvent.EventType == BusinessEventType.NewOutgoingPaymentFile.ToString())
                            .OrderBy(y => y.Id)
                            .Select(y => (DateTime?)y.TransactionDate)
                            .FirstOrDefault(),
                     AccruedIntrerest = x.Transactions.Where(y => y.AccountCode == TransactionAccountType.InterestDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                     CreditSniCode = x.DatedCreditStrings.Where(y => y.Name == DatedCreditStringCode.CompanyLoanSniKodSe.ToString()).OrderByDescending(y => y.BusinessEventId).Select(y => y.Value).FirstOrDefault(),
                     InitialTerms = new
                     {
                         ReferenceInterestRate = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.ReferenceInterestRate.ToString()).Select(y => y.Value).FirstOrDefault(),
                         AnnuityAmount = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.AnnuityAmount.ToString()).Select(y => y.Value).FirstOrDefault(),
                         MarginInterestRate = x.CreatedByEvent.DatedCreditValues.Where(y => y.CreditNr == x.CreditNr && y.Name == DatedCreditValueCode.MarginInterestRate.ToString()).Select(y => y.Value).FirstOrDefault()
                     },
                     OldestOpenNotificationDueDate = x
                            .Notifications
                            .Where(y => y.TransactionDate <= d && (y.ClosedTransactionDate == null || y.ClosedTransactionDate > d))
                            .Min(y => (DateTime?)y.DueDate)
                 })
                .ToList();

            var useActualBalance = f.OptBool("CompanyLoansUseActualCurrentBalance");

            return credits
                .Select(x =>
                {
                    var currentBalance = x.CurrentBalance;
                    if (!useActualBalance)
                    {
                        //Same as the bookkeeping ledger. Intention is to push the initial balance from when the loan is created
                        //to when the outgoing payment file is created which is sometimes the day after.
                        currentBalance = x.BookKeepingCapitalDebtExceptNewLoans + x.BookKeepingOutgoingPaymentFileAmount;
                        if (x.IsImportedLoan)
                        {
                            //Imported loans dont have an initial outgoing payment file at all so here we need to re-add the initial balance or it breaks
                            currentBalance += x.InitialCapitalDebt;
                        }
                    }

                    DateTime endDate;
                    int? nrOfDaysOverdue;
                    if (!x.IsDebtCollection)
                    {
                        endDate = monthlyItems.Where(z => z.CreditNr == x.CreditNr).OrderByDescending(y => y.CashflowDate).Select(y => y.CashflowDate).FirstOrDefault();
                        nrOfDaysOverdue = x.OldestOpenNotificationDueDate.HasValue && x.OldestOpenNotificationDueDate.Value < d
                            ? (int)Math.Round(Dates.GetAbsoluteTimeBetween(d, x.OldestOpenNotificationDueDate.Value).TotalDays)
                            : 0;
                    }
                    else
                    {
                        //Initial end date.
                        endDate = x
                            .StartDate
                            .DateTime
                            .AddMonths(PaymentPlanCalculation
                                .BeginCreateWithAnnuity(x.InitialCapitalDebt, x.InitialTerms.AnnuityAmount, x.InitialTerms.MarginInterestRate + x.InitialTerms.ReferenceInterestRate, null, NEnv.CreditsUse360DayInterestYear)
                                .EndCreate()
                                .Payments.Count);
                        endDate = new DateTime(endDate.Year, endDate.Month, dueDay.Value);

                        nrOfDaysOverdue = null;
                    }

                    return new TransactionConsumerModel
                    {
                        CustomerId1 = x.CreditCustomer1.CustomerId,
                        CustomerFullName1 = $"{getValue("companyName", x.CreditCustomer1.CustomerId)}",
                        CustomerCountry1 = getValue("addressCountry", x.CreditCustomer1.CustomerId) ?? NEnv.ClientCfg.Country.BaseCountry,
                        Sni = getValue("snikod", x.CreditCustomer1.CustomerId) ?? x.CreditSniCode,
                        Orgnr = getValue("orgnr", x.CreditCustomer1.CustomerId),
                        DaysPastDue = nrOfDaysOverdue,
                        CreditNr = x.CreditNr,
                        CurrentBalance = currentBalance,
                        AccruedIntrerest = x.AccruedIntrerest,
                        StartDate = x.StartDate,
                        EndDate = endDate,
                        IsDebtCollection = x.IsDebtCollection,
                        TotalInterestRate = x.InitialTerms.MarginInterestRate + x.InitialTerms.ReferenceInterestRate
                    };
                }).ToList();
        }

        public List<GurantorsModel> GetGurantorsCorporateLoans(CreditContext context)
        {
            var file = NTechEnvironment.Instance.StaticResourceFile("ntech.credit.Treasury.settingsfile", "Treasury-business-credit-settings.txt", true);

            var f = NTechSimpleSettings.ParseSimpleSettingsFile(file.FullName, forceFileExistance: true);

            if (string.IsNullOrWhiteSpace(f.Opt("PrefixLoan")))
                throw new System.InvalidOperationException("PrefixLoan is missing in " + file.FullName);

            var Credits = context
                .CreditCustomerListMembers
                .Where(x => x.ListName == "companyLoanCollateral" && x.Credit.Status == CreditStatus.Normal.ToString() && x.Credit.CreditType == CreditType.CompanyLoan.ToString())
                .Select(x => new
                {
                    CreditNr = x.CreditNr,
                    CollateralCustomerId = x.CustomerId,
                    CompanyCustomerId = x.Credit.CreditCustomers.Where(y => y.ApplicantNr == 1).Select(y => y.CustomerId).FirstOrDefault(),
                });

            return Credits
                .Select(x => new GurantorsModel
                {
                    CustomerId1 = x.CompanyCustomerId,
                    GuarantorCustPercent = 1,
                    CreditNr = x.CreditNr,
                    GuarantorCustomerId = x.CollateralCustomerId
                }).ToList();
        }

        public class CashFlowItemModel
        {
            public string CreditNr { get; set; }
            public string CashflowId { get; set; }
            public DateTime CashflowDate { get; set; }
            public decimal CapitalAmount { get; set; }
            public decimal? InterestAmount { get; set; }
            public decimal FeeAmount { get; set; }
        }

        public class GurantorsModel
        {
            public string CreditNr { get; set; }
            public int CustomerId1 { get; set; }
            public int? GuarantorCustPercent { get; set; }
            public int GuarantorCustomerId { get; set; }
        }

        public class TransactionConsumerModel
        {
            public string CreditNr { get; set; }
            public string CashflowId { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public decimal CapitalAmount { get; set; }
            public decimal? InterestAmount { get; set; }
            public decimal FeeAmount { get; set; }
            public int CustomerId1 { get; set; }
            public int? CustomerId2 { get; set; }
            public decimal CurrentBalance { get; set; }
            public decimal AccruedIntrerest { get; set; }
            public string CustomerFullName1 { get; set; }
            public string CustomerCountry1 { get; set; }
            public string CustomerFullName2 { get; set; }
            public string CustomerCountry2 { get; set; }
            public string Sni { get; set; }
            public string Orgnr { get; set; }
            public bool IsDebtCollection { get; internal set; }
            public int? DaysPastDue { get; internal set; }
            public decimal TotalInterestRate { get; set; }
            public DateTime? CollectionDate { get; set; }
            public string ProviderName { get; set; }
            public decimal? InitialPayoutAmount { get; set; }
            public decimal? AdditionalLoanAmount { get; set; }
            public string ApplicationNr { get; set; }
        }

        private class ExtraCreditData
        {
            public DateTime CreationDate { get; set; }
            public decimal? CapitalDebt { get; set; }
            public decimal? NotNotifiedCapitalAmount { get; set; }
            public DateTime? LatestNotificationDueDate { get; set; }
            public IEnumerable<DateTime> PendingFuturePaymentFreeMonths { get; set; }
        }

        public List<CashFlowItemModel> GetCashFlowItems(CreditContextExtended context, IList<string> onlyTheseCreditNrs, string creditType)
        {
            NotificationProcessSettings processSettings = null;
            processSettings = NEnv.NotificationProcessSettings.GetByCreditType(creditType);

            var repo = new PartialCreditModelRepository();

            var today = clock.Today;

            var models = repo
                    .NewQuery(clock.Today)
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
                                y.Credit.CreatedByEvent.TransactionDate <= today &&
                                onlyTheseCreditNrs.Contains(y.Credit.CreditNr) &&
                                y.BasicCreditData.Strings.Any(z => z.Name == DatedCreditStringCode.CreditStatus.ToString() && z.Value == CreditStatus.Normal.ToString()))
                            .OrderBy(y => y.Credit.CreditNr)
                            .Select(y => new PartialCreditModelRepository.CreditFinalDataWrapper<ExtraCreditData>
                            {
                                BasicCreditData = y.BasicCreditData,
                                ExtraCreditData = new ExtraCreditData
                                {
                                    LatestNotificationDueDate = y.Credit.Notifications.Max(z => (DateTime?)z.DueDate),
                                    CreationDate = y.Credit.CreatedByEvent.TransactionDate,
                                    CapitalDebt = y
                                        .Credit
                                        .Transactions
                                        .Where(z => z.TransactionDate <= today && z.AccountCode == TransactionAccountType.CapitalDebt.ToString())
                                        .Sum(z => (decimal?)z.Amount),
                                    NotNotifiedCapitalAmount = y
                                        .Credit
                                        .Transactions
                                        .Where(z => z.TransactionDate <= today && z.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString())
                                        .Sum(z => (decimal?)z.Amount),
                                    PendingFuturePaymentFreeMonths = y
                                        .Credit
                                        .CreditFuturePaymentFreeMonths
                                        .Where(z => z.CommitedByEvent == null && z.CancelledByEvent == null)
                                        .Select(z => z.ForMonth)
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
                        x.ExtraData.LatestNotificationDueDate,
                        NotificationFee = x.GetValue(DatedCreditValueCode.NotificationFee) ?? 0m,
                        AmortizationModel = x.GetString(DatedCreditStringCode.AmortizationModel),
                        AnnuityAmount = x.GetValue(DatedCreditValueCode.AnnuityAmount),
                        MonthlyAmortizationAmount = x.GetValue(DatedCreditValueCode.MonthlyAmortizationAmount),
                        NextInterestFromDate = x.GetString(DatedCreditStringCode.NextInterestFromDate),
                        ReferenceInterestRate = x.GetValue(DatedCreditValueCode.ReferenceInterestRate),
                        MarginInterestRate = x.GetValue(DatedCreditValueCode.MarginInterestRate),
                    };
                })
                .ToList();

            var items = new List<CashFlowItemModel>();
            var creditNrsWithNoAmortPlan = new List<string>();
            if (NEnv.HasPerLoanDueDay)
                throw new NotImplementedException();

            foreach (var credit in credits)
            {
                List<AmortizationPlan.Item> futureMonths;
                string failedMessage;
                var amortizationModel = CreditDomainModel.CreateAmortizationModel(
                    credit.AmortizationModel, () => credit.AnnuityAmount.Value,
                    () => credit.MonthlyAmortizationAmount.Value, null, null);

                //Start by assuming we can notify this month
                var nextNotificationDueDate = new DateTime(today.Year, today.Month, processSettings.NotificationDueDay);
                if (credit.LatestNotificationDueDate.HasValue && credit.LatestNotificationDueDate.Value > nextNotificationDueDate)
                {
                    //If this month or a future month has already been notified push it further
                    nextNotificationDueDate = credit.LatestNotificationDueDate.Value.AddMonths(1);
                }
                else if (today > new DateTime(today.Year, today.Month, processSettings.NotificationNotificationDay))
                {
                    //Notification day passed this month so move it forward by one month
                    nextNotificationDueDate = nextNotificationDueDate.AddMonths(1);
                }

                if (!FixedDueDayAmortizationPlanCalculator.TrySimulateFutureMonths(
                    credit.NotNotifiedCapitalAmount.Value, new DateTime(nextNotificationDueDate.Year, nextNotificationDueDate.Month, 1), credit.NextInterestFromDate == null ? credit.CreationDate : DateTime.ParseExact(credit.NextInterestFromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    credit.MarginInterestRate.Value + (credit.ReferenceInterestRate ?? 0m),
                    amortizationModel,
                    credit.NotificationFee,
                    credit.PendingFuturePaymentFreeMonths.ToList(),
                    processSettings, null, out futureMonths, out failedMessage, CreditDomainModel.GetInterestDividerOverrideByCode(NEnv.ClientInterestModel)))
                {
                    creditNrsWithNoAmortPlan.Add(credit.CreditNr);
                }
                else
                {
                    var rowNr = 1;
                    foreach (var item in futureMonths.Where(x => x.EventTypeCode != BusinessEventType.PaymentFreeMonth.ToString()))
                    {
                        items.Add(new CashFlowItemModel
                        {
                            CreditNr = credit.CreditNr,
                            CashflowId = credit.CreditNr + "_" + rowNr++.ToString(),
                            CapitalAmount = item.CapitalTransaction,
                            CashflowDate = item.EventTransactionDate,
                            FeeAmount = item.NotificationFeeTransaction,
                            InterestAmount = item.InterestTransaction
                        });
                    }
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