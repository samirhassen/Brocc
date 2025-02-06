using Dapper;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel
{
    public class CreditNotNotifiedInterestRepository
    {
        private readonly ICreditEnvSettings creditEnvSettings;
        private readonly CreditContextFactory creditContextFactory;
        private readonly CalendarDateService calendarDateService;

        public CreditNotNotifiedInterestRepository(ICreditEnvSettings creditEnvSettings, CreditContextFactory creditContextFactory, CalendarDateService calendarDateService)
        {
            this.creditEnvSettings = creditEnvSettings;
            this.creditContextFactory = creditContextFactory;
            this.calendarDateService = calendarDateService;
        }

        public class CreditNotNotifiedInterestDetailItem
        {
            public string CreditNr { get; set; }
            public DateTime CurrentCreditNextInterestFromDate { get; set; }
            public DateTime BlockFromDate { get; set; }
            public DateTime BlockToDate { get; set; }
            public decimal BlockInterestAmount { get; set; }
            public decimal CapitalDebtAmount { get; set; }
            public decimal InterestRate { get; set; }
            public decimal DayInterestAmount { get; set; }
            public int NrOfDaysInBlock { get; set; }
        }

        public class CreditNotNotifiedInterestGroupItem
        {
            public string CreditNr { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public Dictionary<string, decimal> GetNotNotifiedInterestAmount(DateTime forDate, string creditNr = null, Action<List<CreditNotNotifiedInterestDetailItem>> includeDetails = null) =>
            GetNotNotifiedInterestAmount(forDate, onlyTheseCreditNrs: string.IsNullOrWhiteSpace(creditNr) ? null : new HashSet<string> { creditNr },
                includeDetails: includeDetails);

        public Dictionary<string, decimal> GetNotNotifiedInterestAmount(DateTime forDate, HashSet<string> onlyTheseCreditNrs = null, Action<List<CreditNotNotifiedInterestDetailItem>> includeDetails = null)
        {
            calendarDateService.EnsureCalendarDates(forDate);

            var interestModelCode = creditEnvSettings.ClientInterestModel;
            string divider;
            if (interestModelCode == DomainModel.InterestModelCode.Actual_365_25)
            {
                divider = "365.25";
            }
            else if (interestModelCode == DomainModel.InterestModelCode.Actual_360)
            {
                divider = "360";
            }
            else
                throw new NotImplementedException();

            var hasCreditNrFilter = onlyTheseCreditNrs != null && onlyTheseCreditNrs.Count > 0;

            var QueryBasis =
string.Format(@"WITH Credits
as
(
	select	h.CreditNr,
			(select top 1 cast(v.Value as date)  from DatedCreditString v where v.CreditNr = h.CreditNr and v.Name = 'NextInterestFromDate' and v.TransactionDate <= @forDate order by v.[Timestamp] desc) as NextInterestFromDate,
			(select top 1 v.Value  from DatedCreditString v where v.CreditNr = h.CreditNr and v.Name = 'CreditStatus' and v.TransactionDate <= @forDate order by v.[Timestamp] desc) as CreditStatus
	from	CreditHeader h
),
Basis
as
(
	SELECT	d.TheDate,
			c.CreditNr,
			c.CreditStatus as CurrentCreditStatus,
			c.NextInterestFromDate as CurrentCreditNextInterestFromDate,
			(select SUM(t.Amount) from AccountTransaction t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr and t.TransactionDate <= d.TheDate) as CapitalDebtAmount,
			((select top 1 v.Value from DatedCreditValue v where v.CreditNr = c.CreditNr and v.Name = 'MarginInterestRate' and v.TransactionDate <= d.TheDate order by v.[Timestamp] desc)
			+
			(select top 1 v.Value from DatedCreditValue v where v.CreditNr = c.CreditNr and v.Name = 'ReferenceInterestRate' and v.TransactionDate <= d.TheDate order by v.[Timestamp] desc)) as InterestRate
	FROM	CalendarDate d
	cross join	Credits c
),
DailyInterestAmount
as
(
	select	b.*,
			b.CapitalDebtAmount * b.InterestRate / {0} / 100 as DayInterestAmount
	from	Basis b
	where	b.TheDate >= b.CurrentCreditNextInterestFromDate
	and		b.TheDate <= @forDate
	and		b.CurrentCreditStatus = 'Normal'
)", divider);

            var detailsQuery = string.Format(
    @"select	d.CreditNr,
		d.CurrentCreditNextInterestFromDate,
		MIN(d.TheDate) as BlockFromDate,
		MAX(d.TheDate) as BlockToDate,
		sum(d.DayInterestAmount) as BlockInterestAmount,
		d.CapitalDebtAmount,
		d.InterestRate,
		d.DayInterestAmount,
		COUNT(*) as NrOfDaysInBlock
from	DailyInterestAmount d
{0}
group by d.CreditNr, d.CurrentCreditNextInterestFromDate, d.CapitalDebtAmount, d.InterestRate, d.DayInterestAmount
order by d.CreditNr, MIN(d.TheDate)", hasCreditNrFilter ? "where d.CreditNr in @creditNrs" : "");

            var groupQuery = string.Format(
@"select	d.CreditNr,
		SUM(d.DayInterestAmount) as TotalAmount
from	DailyInterestAmount d
{0}
group by d.CreditNr", hasCreditNrFilter ? "where d.CreditNr in @creditNrs" : "");

            using (var context = creditContextFactory.CreateContext())
            {
                if (includeDetails != null)
                {
                    var details = context
                        .GetConnection()
                        .Query<CreditNotNotifiedInterestDetailItem>(QueryBasis + " " + detailsQuery, new
                        {
                            forDate = forDate,
                            creditNrs = onlyTheseCreditNrs
                        }).ToList();
                    if (hasCreditNrFilter)
                    {
                        details = details.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr)).ToList();
                    }
                    includeDetails(details);
                }

                var groupResult = context
                    .GetConnection()
                    .Query<CreditNotNotifiedInterestGroupItem>(QueryBasis + " " + groupQuery, new
                    {
                        forDate = forDate,
                        creditNrs = onlyTheseCreditNrs
                    })
                    .ToList();

                if (hasCreditNrFilter)
                {
                    groupResult = groupResult.Where(x => onlyTheseCreditNrs.Contains(x.CreditNr)).ToList();
                }

                return groupResult.ToDictionary(x => x.CreditNr, x => x.TotalAmount);
            }
        }
    }
}