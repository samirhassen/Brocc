using nCredit.Code;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports.Shared
{
    public class Credits
    {

        public class Credit
        {
            public string CompanyName { get; internal set; }
            public string CreditNr { get; internal set; }
            public DateTime CreationDate { get; internal set; }
            public decimal? MarginInterestRate { get; internal set; }
            public decimal? ReferenceInterestRate { get; internal set; }
            public string ProviderName { get; internal set; }
            public string Status { get; internal set; }
            public DateTime? StatusDate { get; internal set; }
            public string CompanyLoanSniKodSe { get; internal set; }
            public DateTime? InitialPaymentFileDate { get; internal set; }
            public decimal? TotalAdditionalLoanPaymentAmount { get; internal set; }
            public decimal? InitialPaymentFileAmount { get; internal set; }
            public decimal? InitialEffectiveInterestRatePercent { get; internal set; }
            public int? NrOfOverdueCount { get; internal set; }
            public int? NrOfDaysOverdue { get; internal set; }
            public decimal? TotalNotifiedUnpaidBalance { get; internal set; }
            public int? ReservationOverDueCount { get; internal set; }
            public int? ReservationNrOfDaysOverdue { get; internal set; }
            public decimal? CapitalDebt { get; internal set; }
            public decimal? TotalNotifiedCapital { get; internal set; }
            public decimal? TotalNotifiedFees { get; internal set; }
            public decimal? TotalNotifiedInterest { get; internal set; }
            public decimal TotalPaidFees { get; internal set; }
            public decimal? TotalPaidInterest { get; internal set; }
            public decimal? InitalCapitalDebt { get; internal set; }
            public RemainingPaymentsCalculation.RemainingPaymentsModel CurrentRemainingModel { get; internal set; }
            public RemainingPaymentsCalculation.RemainingPaymentsModel InitialRemainingModel { get; internal set; }
            public string OrganisationNumber { get; internal set; }
            public int NrOfClosedNotifications { get; internal set; }
            public DateTime? LatestNotificationDueDate { get; set; }
            public int? LatestNotificationId { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public decimal? CurrentNotNotifiedCapitalBalance { get; set; }
            public DateTime? NextInterestFromDate { get; set; }
            public decimal? CurrentNotificationFeeAmount { get; set; }
        }

        public static Dictionary<int, Dictionary<string, string>> GetCustomerData(ISet<int> customerIds, params string[] propertyNames)
        {
            var customerClient = new CreditCustomerClient();

            Dictionary<int, Dictionary<string, string>> customerData = new Dictionary<int, Dictionary<string, string>>(customerIds.Count);
            IEnumerable<IEnumerable<int>> groups = customerIds.ToArray().SplitIntoGroupsOfN(500);
            foreach (IEnumerable<int> customerIdsGroup in groups)
            {
                var resultCustomer = customerClient.BulkFetchPropertiesByCustomerIdsD(
                    customerIdsGroup.ToHashSet(),
                    propertyNames);
                foreach (var b in resultCustomer)
                    customerData[b.Key] = b.Value;
            }

            return customerData;
        }

        public static List<Credit> GetCredits(Request request)
        {
            var c = new DataWarehouseClient();
            var p = new ExpandoObject();
            p.SetValues(d => d["forDate"] = request.Date.Value);

            var supportItems = c.FetchReportData<CompanyLoanLedgerReportPartialDataModel>("LoanPerformanceReportPartialData1", p)?.ToDictionary(x => x.CreditNr);
            var notificationSettings = NEnv.NotificationProcessSettings.GetByCreditType(CreditType.CompanyLoan);

            using (var context = new CreditContext())
            {
                var d = request.Date.Value.Date;
                var creditsBasis = CurrentCreditState.GetCreditsQueryable(context, request.Date.Value);

                if (!string.IsNullOrWhiteSpace(request.CreditNr))
                {
                    creditsBasis = creditsBasis.Where(x => x.CreditNr == request.CreditNr);
                }

                var customerIds = context.CreditCustomers.Select(x => x.CustomerId).ToHashSet();

                var customerData = GetCustomerData(customerIds, "companyName", "orgnr");

                Func<int, string, string> getP = (customerId, propertyName) => customerData.Opt(customerId).Opt(propertyName);

                var remainingCalculation = new RemainingPaymentsCalculation();

                var credits = creditsBasis
                    .OrderBy(x => x.CreationDate)
                    .ThenBy(x => x.Timestamp)
                    .Select(x => new
                    {
                        Base = x,
                        InitialPaymentFile = (context.OutgoingPaymentHeaders.Where(y => y.Transactions.Any(z => z.CreditNr == x.CreditNr)))
                            .Select(y => new
                            {
                                Event = y.CreatedByEvent,
                                PaymentFile = y.OutgoingPaymentFile,
                                Amount = y.Transactions.Where(z => z.AccountCode == TransactionAccountType.ShouldBePaidToCustomer.ToString() && z.BusinessEventId == y.CreatedByBusinessEventId).Sum(z => (decimal?)z.Amount) ?? 0m
                            })
                            .Where(y => y.Event.TransactionDate <= d)
                            .OrderBy(y => y.Event.Id)
                            .Select(y => new
                            {
                                y.Event.EventType,
                                PaymentDate = y.PaymentFile == null ? null : (DateTime?)y.PaymentFile.TransactionDate,
                                y.Amount,
                                EventId = y.Event.Id
                            })
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Select(data =>
                    {
                        var x = data.Base;

                        var m = supportItems?.Opt(x.CreditNr);

                        var currentInterestRate = x.MarginInterestRate.GetValueOrDefault() + x.ReferenceInterestRate.GetValueOrDefault();
                        var creationInterestRate = x.CreationMarginInterestRate.GetValueOrDefault() + x.CreationReferenceInterestRate.GetValueOrDefault();

                        return new Credit
                        {
                            CreditNr = x.CreditNr,
                            CompanyName = customerData.Opt(x.CustomerId).Opt("companyName"),
                            OrganisationNumber = customerData.Opt(x.CustomerId).Opt("orgnr"),
                            CreationDate = x.CreationDate,
                            MarginInterestRate = x.MarginInterestRate,
                            ReferenceInterestRate = x.ReferenceInterestRate,
                            ProviderName = x.ProviderName,
                            Status = x.Status,
                            StatusDate = x.StatusDate,
                            CompanyLoanSniKodSe = x.CompanyLoanSniKodSe,
                            InitialPaymentFileDate = data.InitialPaymentFile?.PaymentDate,
                            TotalAdditionalLoanPaymentAmount = m?.TotalNewAdditionalLoanCapitalAmount,
                            InitialPaymentFileAmount = data.InitialPaymentFile?.Amount,
                            InitialEffectiveInterestRatePercent = m?.InitialEffectiveInterestRatePercent,
                            NrOfOverdueCount = m?.OverDueCount,
                            NrOfDaysOverdue = m?.NrOfDaysOverdue,
                            TotalNotifiedUnpaidBalance = m?.TotalNotifiedUnpaidBalance,
                            ReservationOverDueCount = m?.ReservationOverDueCount,
                            ReservationNrOfDaysOverdue = m?.ReservationNrOfDaysOverdue,
                            CapitalDebt = x.CapitalDebt,
                            TotalNotifiedCapital = x.TotalNotifiedCapital,
                            TotalNotifiedFees = x.TotalNotifiedFees,
                            TotalNotifiedInterest = x.TotalNotifiedInterest,
                            TotalPaidFees = x.TotalPaidInitialFees.GetValueOrDefault() + x.TotalPaidNotifiedFees.GetValueOrDefault(),// Total paid fees = totalt betalda avgifter; avi+påminnelse+uppläggningsavgift
                            TotalPaidInterest = x.TotalPaidInterest,
                            InitalCapitalDebt = x.InitalCapitalDebt,
                            CurrentRemainingModel = x.GetCurrentRemainingPayments(d, notificationSettings),
                            InitialRemainingModel = x.GetInitialRemainingPayments(notificationSettings),
                            NrOfClosedNotifications = x.NrOfClosedNotifications,
                            LatestNotificationDueDate = x.LatestNotificationDueDate,
                            LatestNotificationId = x.LatestNotificationId,
                            AnnuityAmount = x.AnnuityAmount,
                            CurrentNotNotifiedCapitalBalance = x.CurrentNotNotifiedCapitalBalance,
                            NextInterestFromDate = x.GetParsedNextInterestFromDate(),
                            CurrentNotificationFeeAmount = x.CurrentNotificationFeeAmount
                        };
                    })
                    .ToList();

                return credits;
            }
        }

        public class CompanyLoanLedgerReportPartialDataModel
        {
            public string CreditNr { get; set; }
            public decimal CurrentCapitalDebt { get; set; }
            public DateTime? NewCreditTransactionDate { get; set; }
            public decimal InitialNewCreditCapitalAmount { get; set; }
            public decimal TotalNewAdditionalLoanCapitalAmount { get; set; }
            public int OverDueCount { get; set; }
            public int? NrOfDaysOverdue { get; set; }
            public decimal? InitialEffectiveInterestRatePercent { get; set; }
            public decimal? TotalNotifiedUnpaidBalance { get; set; }
            public int? ReservationOverDueCount { get; set; }
            public int? ReservationNrOfDaysOverdue { get; set; }
        }

        public class Request
        {
            public DateTime? Date { get; set; }
            public string CreditNr { get; set; }
            public bool? IncludeActualOverdue { get; set; }
        }
    }
}