using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public static class CommonReportingTableExpressions
    {
        /// <summary>
        /// Require a toDate and graceDays. Do not add fromDate to this. Create an extension with just that if needed.
        /// </summary>
        private static string ReportingExpressions =
@"with DimensionCredit
as
(
	select	h.CreditNr,
			(select c.CustomerId from CreditCustomer c where c.CreditNr = h.CreditNr and c.ApplicantNr = 1) as Applicant1CustomerId,
			e.TransactionDate as CreatedTransactionDate,
            h.CollateralHeaderId,
            (select count(*) from CreditCustomer c where c.CreditNr = h.CreditNr) as NrOfApplicants,
            h.ProviderName,
            h.StartDate,
            h.CreditType
	from	CreditHeader h
	join	BusinessEvent e on e.Id = h.CreatedByBusinessEventId
	where	e.TransactionDate <= @toDate
),
DatedCreditStringPeriodEndPre
as
(
	select	d.*,
			RANK() OVER (PARTITION BY d.CreditNr, d.[Name] ORDER BY d.BusinessEventId desc) as PeriodEndRank
	from	DatedCreditString d
	where	d.TransactionDate <= @toDate
),
DatedCreditStringPeriodEnd
as
(
	select	p.*
	from	DatedCreditStringPeriodEndPre p
	where	p.PeriodEndRank = 1
),
DatedCreditValuePeriodEndPre
as
(
	select	d.*,
			RANK() OVER (PARTITION BY d.CreditNr, d.[Name] ORDER BY d.BusinessEventId desc) as PeriodEndRank
	from	DatedCreditValue d
	where	d.TransactionDate <= @toDate
),
DatedCreditValuePeriodEnd
as
(
	select	p.*
	from	DatedCreditValuePeriodEndPre p
	where	p.PeriodEndRank = 1
),

DatedCreditDatePeriodEndPre
as
(
	select	d.*,
			RANK() OVER (PARTITION BY d.CreditNr, d.[Name] ORDER BY d.BusinessEventId desc) as PeriodEndRank
	from	DatedCreditDate d
	where	d.TransactionDate <= @toDate
),
DatedCreditDatePeriodEnd
as
(
	select	p.[Name],
			p.CreditNr,
			p.TransactionDate,
			case when p.RemovedByBusinessEventId is null then p.[Value] else null end as [Value]
	from	DatedCreditDatePeriodEndPre p
	where	p.PeriodEndRank = 1
),
AccountTransactionPeriodEnd
as
(
	select	t.*,
			case when t.AccountCode in ('ReminderFeeDebt', 'NotificationFeeDebt') then 1 else 0 end as IsFee,
            case when exists(
			    select 1 
			    from	CreditTerminationLetterHeader h 
			    left outer join BusinessEvent b on b.Id = h.InactivatedByBusinessEventId
			    where	h.CreditNr = t.CreditNr 
			    and		(h.InactivatedByBusinessEventId is null or b.TransactionDate >= t.TransactionDate)
			    and		h.DueDate < t.TransactionDate
            ) then 1 else 0 end as HasActiveOverdueTerminationLetter,
            b.EventType as BusinessEventType
	from	AccountTransaction t
    join	BusinessEvent b on b.Id = t.BusinessEventId
	where	t.TransactionDate <= @toDate
),
CreditCreatedDatedCreditValue
as
(
	select	v.*
	from	DatedCreditValue v	
	join	CreditHeader h on h.CreditNr = v.CreditNr and v.BusinessEventId = h.CreatedByBusinessEventId
),
CreditCreatedAccountTransaction
as
(
	select	c.*
	from	AccountTransaction c
	join	CreditHeader h on h.CreditNr = c.CreditNr and c.BusinessEventId = h.CreatedByBusinessEventId
),
CommitedByEventIdCreditTermChange
as
(
	select	t.CreditNr, b.TransactionDate, t.CommitedByEventId
	from	CreditTermsChangeHeader t
	join	CreditHeader c on c.CreditNr = t.CreditNr
	join	BusinessEvent b on b.Id = t.CommitedByEventId
    where	b.TransactionDate <= @toDate
),
CreditNotificationPeriodEnd
as
(
	select	h.*,
			case 
				when DATEDIFF(d, h.DueDate, coalesce(h.ClosedTransactionDate, @toDate)) > @graceDays then 1
				else 0
			end IsLateOrWasPaidLate,
			case
				when h.DueDate < @toDate and (h.ClosedTransactionDate is null or h.ClosedTransactionDate > @toDate) then DATEDIFF(d, h.DueDate, @toDate)
				else 0
			end as CurrentNrOfOverdueDays,
			case
				when h.DueDate < @toDate and (h.ClosedTransactionDate is null or h.ClosedTransactionDate > @toDate) then h.DueDate
				else null
			end as CurrentlyOverdueSinceDate,
            (isnull(case
                when h.DueDate < @toDate and h.ClosedTransactionDate <= @toDate and h.ClosedTransactionDate >= h.DueDate then DATEDIFF(d, h.DueDate, h.ClosedTransactionDate)
                else 0
            end, 0)) as NrOfDaysBetweenDueDateAndPayment
	from	CreditNotificationHeader h
	where	h.TransactionDate <= @toDate
),
CreditDebtCollectionExport
as
(
	select	t.CreditNr,
			max(b.TransactionDate) as ExportDate,
			-sum(t.Amount) as WrittenOffCapitalAmount
	from	AccountTransaction t
	join	BusinessEvent b on b.Id = t.BusinessEventId
	and		b.EventType = 'CreditDebtCollectionExport'
	and		t.AccountCode = 'CapitalDebt'
	and		t.WriteoffId is not null
	and		t.TransactionDate < @toDate
	group by t.CreditNr
),
CollateralItemPeriodEndPre
as
(
	select	c.* ,
			RANK() OVER (PARTITION BY c.CollateralHeaderId, c.ItemName ORDER BY c.CreatedByBusinessEventId desc) as PeriodEndRank
	from	CollateralItem c
	join	BusinessEvent b on b.Id = c.CreatedByBusinessEventId
	left outer join BusinessEvent r on r.Id = c.RemovedByBusinessEventId
	where	b.TransactionDate <= @toDate
	and		(c.RemovedByBusinessEventId is null or r.TransactionDate > @toDate)
),
CollateralItemPeriodEnd as (select * from CollateralItemPeriodEndPre where PeriodEndRank = 1),
CreditCreatedCollateralItem
as
(
	select	ii.*
	from	CollateralItem ii
	where	ii.Id in
	(
		select	i.Id		
		from	CollateralItem i
		join	CollateralHeader c1 on c1.Id = i.CollateralHeaderId
		join	CreditHeader c2 on c2.CollateralHeaderId = c1.Id
		where	c2.CreatedByBusinessEventId = i.CreatedByBusinessEventId
	)
)
";

        private static Dictionary<CommonReportingDataPointCode, string> commonDataPoints = new Dictionary<CommonReportingDataPointCode, string>
        {
            [CommonReportingDataPointCode.CurrentCapitalBalance] = "(select isnull(sum(t.Amount), 0) from AccountTransactionPeriodEnd t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr)",
            [CommonReportingDataPointCode.InitialCapitalBalance] = "(select isnull(sum(t.Amount), 0) from CreditCreatedAccountTransaction t where t.AccountCode = 'CapitalDebt' and t.CreditNr = c.CreditNr)",
            [CommonReportingDataPointCode.InitialCapitalizedInitialFee] = @"(select	isnull(sum(t.Amount), 0)
			    from	AccountTransactionPeriodEnd t
			    where	t.CreditNr = c.CreditNr 
			    and		t.BusinessEventType = 'CapitalizedInitialFee'
			    and		t.TransactionDate = c.CreatedTransactionDate
			    and		t.AccountCode = 'CapitalDebt')",
            [CommonReportingDataPointCode.CurrentMarginInterestRate] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'MarginInterestRate')",
            [CommonReportingDataPointCode.CurrentReferenceInterestRate] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'ReferenceInterestRate')",
            [CommonReportingDataPointCode.CurrentMonthlyAmortizationAmount] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'MonthlyAmortizationAmount')",
            [CommonReportingDataPointCode.CurrentAnnuityAmount] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'AnnuityAmount')",
            [CommonReportingDataPointCode.CurrentNotificationFee] = "(select d.[Value] from DatedCreditValuePeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'NotificationFee')",
            [CommonReportingDataPointCode.CurrentNrOfOverdueDays] = "(select isnull(max(n.CurrentNrOfOverdueDays), 0) from CreditNotificationPeriodEnd n where n.CreditNr = c.CreditNr)",
            [CommonReportingDataPointCode.CurrentOverdueSinceDate] = "(select top 1 n.CurrentlyOverdueSinceDate from CreditNotificationPeriodEnd n where n.CreditNr = c.CreditNr)",
            [CommonReportingDataPointCode.CurrentCreditStatus] = "(select d.[Value] from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'CreditStatus')",
            [CommonReportingDataPointCode.CurrentApplicationNr] = "(select d.[Value] from DatedCreditStringPeriodEnd d where d.CreditNr = c.CreditNr and d.[Name] = 'ApplicationNr')",
            [CommonReportingDataPointCode.InitialMarginInterestRate] = "(select d.[Value] from CreditCreatedDatedCreditValue d where d.CreditNr = c.CreditNr and d.[Name] = 'MarginInterestRate')",
            [CommonReportingDataPointCode.InitialReferenceInterestRate] = "(select d.[Value] from CreditCreatedDatedCreditValue d where d.CreditNr = c.CreditNr and d.[Name] = 'ReferenceInterestRate')",
            [CommonReportingDataPointCode.InitialAnnuityAmount] = "(select d.[Value] from CreditCreatedDatedCreditValue d where d.CreditNr = c.CreditNr and d.[Name] = 'AnnuityAmount')",
            [CommonReportingDataPointCode.InitialNotificationFee] = "(select d.[Value] from CreditCreatedDatedCreditValue d where d.CreditNr = c.CreditNr and d.[Name] = 'NotificationFee')",
        };
        public static string GetDataPointExpression(CommonReportingDataPointCode dataPoint) => commonDataPoints[dataPoint];

        //This is just to protect users from forgetting about required parameters of the CTE and allowing us to add new parameters in a type safe way
        public static List<TResult> Query<TResult>(IDbConnection connection, string query, DateTime toDate, int graceDays, 
            Action<IDictionary<string, object>> addExtraParameters = null, 
            IDbTransaction transaction = null,
            Action<string> observeSqlQuery = null)
        {
            var parameters = new ExpandoObject();
            parameters.SetValues(x =>
            {
                x["toDate"] = toDate;
                x["graceDays"] = graceDays;
                addExtraParameters?.Invoke(x);
            });
            var sqlQuery = ReportingExpressions + query;
            observeSqlQuery?.Invoke(sqlQuery);
            return connection.Query<TResult>(sqlQuery, param: parameters, transaction: transaction).ToList();
        }
    }

    public enum CommonReportingDataPointCode
    {
        CurrentCapitalBalance,
        InitialCapitalBalance,
        /// <summary>
        /// Capitalized initial fee that was not part of the loan creation transaction
        /// </summary>
        InitialCapitalizedInitialFee,
        CurrentMarginInterestRate,
        CurrentReferenceInterestRate,
        CurrentMonthlyAmortizationAmount,
        CurrentAnnuityAmount,
        CurrentNotificationFee,
        CurrentNrOfOverdueDays,
        CurrentOverdueSinceDate,
        CurrentCreditStatus,
        CurrentApplicationNr,
        InitialMarginInterestRate,
        InitialReferenceInterestRate,
        InitialAnnuityAmount,
        InitialNotificationFee
    }
}
