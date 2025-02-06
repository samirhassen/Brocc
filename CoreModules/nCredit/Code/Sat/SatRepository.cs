using nCredit.Code;
using nCredit.Code.Sat;
using nCredit.DbModel.DomainModel;
using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.Controllers
{
    public class SatRepository
    {
        private readonly Func<ICustomerClient> createCustomerClient;
        public SatRepository(Func<ICustomerClient> createCustomerClient)
        {
            this.createCustomerClient = createCustomerClient;
        }

        static int MonthsBetween(DateTimeOffset d1, DateTime d2)
        {
            var d = NTech.Dates.GetAbsoluteNrOfMonthsBetweenDates(d1.Date, d2);
            return d;
        }

        public class ActiveCredit
        {
            public string CreditNr { get; set; }
            public decimal? AnnuityAmount { get; set; }
            public IEnumerable<int> CustomerIds { get; set; }
            public int NrOfDaysOverdue { get; set; }
            public decimal TotalOpenNotificationInterestBalance { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public decimal CapitalDebtCaptialBalance { get; set; }
        }

        public static Dictionary<int, SatExportItem> GetSatExportItems(List<ActiveCredit> activeCredits, Dictionary<int, Tuple<decimal, DateTime>> monthlyIncomeAndIncomeDateByCustomerId, Dictionary<int, string> civicRegNrByCustomerId, DateTime now)
        {
            var lastestIncomeByCustomerId = monthlyIncomeAndIncomeDateByCustomerId
                //Multiplying by 12.5 and NOT simply 12 because SAT says so. Guessing it might be due to vacation and such being factored into annual salary.
                .ToDictionary(x => x.Key, x => new { YearlySatIncome = 12.5m * x.Value.Item1, IncomeDate = x.Value.Item2 });

            Func<DateTime?, DateTime?, DateTime?> maxDate = (d1, d2) =>
            {
                if (d1.HasValue && d2.HasValue)
                    return d1.Value > d2.Value ? d1.Value : d2.Value;
                else if (!d2.HasValue)
                    return d1;
                else
                    return d2;
            };
            Func<DateTime?, DateTime?, DateTime?> minDate = (d1, d2) =>
            {
                if (d1.HasValue && d2.HasValue)
                    return d1.Value < d2.Value ? d1.Value : d2.Value;
                else if (!d2.HasValue)
                    return d1;
                else
                    return d2;
            };

            var export = new Dictionary<int, SatExportItem>();
            foreach (var creditCustomerItem in activeCredits.SelectMany(x => x.CustomerIds.Select(y => new { CustomerId = y, Credit = x })))
            {
                if (!export.ContainsKey(creditCustomerItem.CustomerId))
                    export[creditCustomerItem.CustomerId] = new SatExportItem { CustomerId = creditCustomerItem.CustomerId };

                var d = export[creditCustomerItem.CustomerId];

                d.Count += 1;
                var balance = (int)Math.Round(creditCustomerItem.Credit.CapitalDebtCaptialBalance);
                d.ItemC01 += balance;
                if (creditCustomerItem.Credit.NrOfDaysOverdue > 60)
                {
                    d.ItemC03 += ((int)Math.Round(creditCustomerItem.Credit.CapitalDebtCaptialBalance))
                        + ((int)Math.Round(creditCustomerItem.Credit.TotalOpenNotificationInterestBalance));
                }
                var monthlyPayments = (int)Math.Round(creditCustomerItem.Credit.AnnuityAmount ?? 0m);
                d.ItemC04 += monthlyPayments;
                d.ItemH14 += monthlyPayments;
                d.ItemD11 += 1;
                d.ItemD12 += balance;
                d.ItemE11 = 0;
                d.ItemE12 = 0;
                d.ItemF11 = 0;
                d.ItemF12 = 0;
                d.ItemF13 = 0;
                if (MonthsBetween(creditCustomerItem.Credit.StartDate, now) <= 12)
                {
                    d.ItemH15 += 1;
                }
                if (creditCustomerItem.Credit.CustomerIds.Count() > 1)
                {
                    d.ItemH16 += 1;
                }
                d.ItemK11 = maxDate(d.ItemK11, creditCustomerItem.Credit.StartDate.Date);
                d.ItemK12 = minDate(d.ItemK12, creditCustomerItem.Credit.StartDate.Date);
            }

            foreach (var e in export)
            {
                e.Value.CivicRegNr = civicRegNrByCustomerId[e.Key];
                if (lastestIncomeByCustomerId.ContainsKey(e.Key))
                {
                    var income = lastestIncomeByCustomerId[e.Key];
                    e.Value.ItemT11 = (int)Math.Round(income.YearlySatIncome);
                    e.Value.ItemT12 = income.IncomeDate;
                }
            }

            return export;
        }

        public Dictionary<int, SatExportItem> GetSatExportItems(CreditContext context, DateTime today)
        {
            var openNotifications = CurrentNotificationStateServiceLegacy.GetCurrentOpenNotificationsStateQuery(context, today);

            var credits = context
                .CreditHeaders
                .Where(x => x.Status == CreditStatus.Normal.ToString())
                .Select(x => new
                {
                    Credit = x,
                    AnnuityAmount = x
                        .DatedCreditValues
                        .Where(y => y.Name == DatedCreditValueCode.AnnuityAmount.ToString())
                        .OrderByDescending(y => y.TransactionDate)
                        .ThenByDescending(y => y.Timestamp)
                        .Select(y => (decimal?)y.Value)
                        .FirstOrDefault(),
                    CustomerIds = x.CreditCustomers.Select(y => y.CustomerId),
                    OpenNotifications = openNotifications.Where(y => y.CreditNr == x.CreditNr),
                    x.StartDate
                })
                .Select(x => new
                {
                    CreditNr = x.Credit.CreditNr,
                    x.AnnuityAmount,
                    x.CustomerIds,
                    TotalOpenNotificationInterestBalance = x.OpenNotifications.Sum(y => (decimal?)y.RemainingInterestAmount) ?? 0m,
                    OldestOpenNotification = x.OpenNotifications.OrderBy(y => y.DueDate).FirstOrDefault(),
                    CapitalDebtCaptialBalance = x.Credit.Transactions.Where(y => y.AccountCode == TransactionAccountType.CapitalDebt.ToString()).Sum(y => (decimal?)y.Amount) ?? 0m,
                    x.StartDate
                })
                .Select(x => new
                {
                    CreditNr = x.CreditNr,
                    x.AnnuityAmount,
                    x.CustomerIds,
                    x.CapitalDebtCaptialBalance,
                    x.TotalOpenNotificationInterestBalance,
                    NrOfDaysOverdue = x.OldestOpenNotification == null ? 0 : x.OldestOpenNotification.NrOfDaysOverdue,
                    x.StartDate
                })
                .Select(x => new ActiveCredit
                {
                    CreditNr = x.CreditNr,
                    AnnuityAmount = x.AnnuityAmount,
                    CustomerIds = x.CustomerIds,
                    NrOfDaysOverdue = x.NrOfDaysOverdue,
                    CapitalDebtCaptialBalance = x.CapitalDebtCaptialBalance,
                    TotalOpenNotificationInterestBalance = x.TotalOpenNotificationInterestBalance,
                    StartDate = x.StartDate
                })
                .ToList();

            var dc = new DataWarehouseClient();

            var dwItems = dc.FetchReportData<DwSatCreditInfoItem>("satCustomerCreditInfo", null);

            var lastestIncomeByCustomerId = dwItems
                .Where(x => x.CustomerId.HasValue && x.IncomePerMonth.HasValue && x.ApplicationDate.HasValue).Select(x => new { CustomerId = x.CustomerId.Value, IncomePerMonth = x.IncomePerMonth.Value, ApplicationDate = x.ApplicationDate.Value })
                .GroupBy(x => x.CustomerId)
                .Select(x => new
                {
                    CustomerId = x.Key,
                    LatestItem = x.OrderByDescending(y => y.ApplicationDate).First()
                })
                .ToDictionary(x => x.CustomerId, x => Tuple.Create(x.LatestItem.IncomePerMonth, x.LatestItem.ApplicationDate));

            var allCustomerIds = credits.SelectMany(x => x.CustomerIds).Distinct().ToList();

            var civicRegNrByCustomerId = new Dictionary<int, string>();
            var cc = createCustomerClient();
            var parser = NEnv.BaseCivicRegNumberParser;
            foreach (var i in cc.BulkFetchPropertiesByCustomerIdsD(new HashSet<int>(allCustomerIds), "civicRegNr"))
                civicRegNrByCustomerId[i.Key] = parser.Parse(i.Value["civicRegNr"]).NormalizedValue;

            return GetSatExportItems(credits, lastestIncomeByCustomerId, civicRegNrByCustomerId, DateTime.Now);
        }
    }
}