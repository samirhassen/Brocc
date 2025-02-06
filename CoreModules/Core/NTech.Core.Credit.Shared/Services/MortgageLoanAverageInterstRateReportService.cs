using Dapper;
using NTech.Core.Credit.Shared.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class MortgageLoanAverageInterstRateReportService
    {
        private readonly CreditContextFactory creditContextFactory;

        public MortgageLoanAverageInterstRateReportService(CreditContextFactory creditContextFactory)
        {
            this.creditContextFactory = creditContextFactory;
        }

        public (List<AverageInterestRateItem> AverageRates, List<CreditItem> AllIncludedCredits) GetAverageInterestRates(DateTime forDate)
        {
            var lastDayOfMonth = new DateTime(forDate.Year, forDate.Month, 1).AddMonths(1).AddDays(-1);
            using (var context = creditContextFactory.CreateContext())
            {
                const string Query = @"
with CreditInterestChangeDate
as
(
	select	d.TransactionDate,
			d.CreditNr
	from	DatedCreditValue d
	where	d.[Name] in ('MarginInterestRate', 'ReferenceInterestRate')
	group by d.TransactionDate, d.CreditNr
),
CreditsWithChangesInMonth
as
(
	select	h.CreditNr,
			isnull((select top 1 d.Value from DatedCreditValue d where d.CreditNr = h.CreditNr and d.[Name] = 'MarginInterestRate' and d.TransactionDate <= @lastDayOfMonth order by d.BusinessEventId desc), 0)
			+ isnull((select top 1 d.Value from DatedCreditValue d where d.CreditNr = h.CreditNr and d.[Name] = 'ReferenceInterestRate' and d.TransactionDate <= @lastDayOfMonth order by d.BusinessEventId desc), 0) as InterestRatePercent,
			isnull((select sum(t.Amount) from AccountTransaction t where t.CreditNr = h.CreditNr and t.AccountCode = 'CapitalDebt' and t.TransactionDate <= @lastDayOfMonth), 0) as CapitalBalance,
			isnull((select top 1 d.Value from DatedCreditValue d where d.CreditNr = h.CreditNr and d.[Name] = 'MortgageLoanInterestRebindMonthCount' and d.TransactionDate <= @lastDayOfMonth order by d.BusinessEventId desc), 0) as RebindMonthCount
	from	CreditHeader h
	where	h.CreditNr in(select d.CreditNr from CreditInterestChangeDate d where YEAR(d.TransactionDate) = YEAR(@lastDayOfMonth) and MONTH(d.TransactionDate) = MONTH(@lastDayOfMonth))
)
select	c.RebindMonthCount,
        c.InterestRatePercent,
        c.CapitalBalance,
        c.CreditNr
from	CreditsWithChangesInMonth c
where   c.CapitalBalance > 0";

                var items = context
                    .GetConnection()
                    .Query<CreditItem>(Query,
                        param: new { lastDayOfMonth = lastDayOfMonth },
                        transaction: context.CurrentTransaction)
                    .ToList()
                    .OrderBy(x => x.RebindMonthCount)
                    .ThenBy(x => x.CreditNr)
                    .ToList();

                var averages = items
                    .GroupBy(x => x.RebindMonthCount)
                    .Select(x => new AverageInterestRateItem
                    {
                        RebindMonthCount = x.Key,
                        AverageInterestRatePercent = Math.Round(x.Sum(y => y.InterestRatePercent * y.CapitalBalance) / x.Sum(y => y.CapitalBalance), 2)
                    })
                    .OrderBy(x => x.RebindMonthCount)
                    .ToList();

                return (AverageRates: averages, AllIncludedCredits: items);
            }
        }

        public class AverageInterestRateItem
        {
            public int RebindMonthCount { get; set; }
            public decimal AverageInterestRatePercent { get; set; }
        }

        public class CreditItem
        {
            public int RebindMonthCount { get; set; }
            public decimal InterestRatePercent { get; set; }
            public decimal CapitalBalance { get; set; }
            public string CreditNr { get; set; }
        }
    }
}
