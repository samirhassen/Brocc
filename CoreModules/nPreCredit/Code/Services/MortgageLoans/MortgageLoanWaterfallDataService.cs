using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services.MortgageLoans
{
    public class MortgageLoanWaterfallDataService
    {
        private const string WaterfallQuery = @"with 
LeadItem
as
(
	select	a.ApplicationNr,			
			a.ItemName,
			a.ItemValue
	from	ComplexApplicationListItem a 
	where	a.ListName = 'Lead'
	and		a.Nr = 1
	and		a.IsRepeatable = 0
),
SignedApplication
as
(
	select	a.ApplicationNr
	from	ComplexApplicationListItem a
	where	a.ListName = 'ApplicationCategory'
	and		a.ItemName =  'IsApplicationSigned'
	and		a.ItemValue = 'true'
	and		a.IsRepeatable = 0
),
AgreementApprovedApplication
as
(
	select	a.ApplicationNr
	from	ComplexApplicationListItem a
	where	a.ListName = 'ApplicationCategory'
	and		a.ItemName =  'IsAgreementApproved'
	and		a.ItemValue = 'true'
	and		a.IsRepeatable = 0
),
WaterfallApplicationPre1
as
(
	select	h.ApplicationNr,
			h.IsActive,
			h.IsRejected,
			h.IsCancelled,
			h.ProviderName,
			DATEADD(month, DATEDIFF(month, 0, h.ApplicationDate), 0) as PeriodMonthDate,
			DATEADD(qq, DATEDIFF(qq, 0, h.ApplicationDate), 0) as PeriodQuarterDate,
			DATEADD(yy, DATEDIFF(yy, 0, h.ApplicationDate), 0) as PeriodYearDate,
			case 
				when exists(select 1 from LeadItem a where a.ApplicationNr = h.ApplicationNr and a.ItemName = 'IsLead' and a.ItemValue = 'false') 
					  and exists(select 1 from LeadItem a where a.ApplicationNr = h.ApplicationNr and a.ItemName = 'WasAccepted' and a.ItemValue = 'true') 
				then 1 else 0 
			end as WasChangedToQualifiedLead,
			case 
				when exists(select 1 from LeadItem a where a.ApplicationNr = h.ApplicationNr and a.ItemName = 'IsLead' and a.ItemValue = 'true') then 1 else 0 
			end as IsLead,
			case when exists(select 1 from SignedApplication a where a.ApplicationNr = h.ApplicationNr) then 1 else 0 end as IsApplicationSigned,
			case when exists(select 1 from AgreementApprovedApplication a where a.ApplicationNr = h.ApplicationNr) then 1 else 0 end as IsAgreementApproved,
			(select top 1 PARSE(a.ItemValue AS decimal(18,2) USING 'en-US') from ComplexApplicationListItem a where a.ListName = 'Settlement' and a.ItemName = 'TotalInitialCapitalDebt' and a.ApplicationNr = h.ApplicationNr order by Id desc) as InitialCapitalDebt,
			h.IsFinalDecisionMade
	from	CreditApplicationHeader h
),
WaterfallApplicationPre2
as
(
	select	p.*,
			case when (p.IsLead = 1 or p.WasChangedToQualifiedLead = 1) then 1 else 0 end as IsOrWasLead,
			case when p.IsActive = 1 and p.IsLead = 1 then 1 else 0 end as IsCurrentLead,
			case when (p.WasChangedToQualifiedLead = 1 or not exists(select 1 from LeadItem a where a.ApplicationNr = p.ApplicationNr and a.ItemName = 'IsLead')) then 1 else 0 end IsQualifiedLead
	from	WaterfallApplicationPre1 p
),
WaterfallApplication
as
(
	select	p.*,
			case when p.IsFinalDecisionMade = 0 and p.IsQualifiedLead = 1 and p.IsApplicationSigned = 0 and p.IsAgreementApproved = 0 then 1 else 0 end as IsLastCategoryQualifiedLead,
			case when p.IsFinalDecisionMade = 0 and p.IsQualifiedLead = 1 and p.IsApplicationSigned = 1 and p.IsAgreementApproved = 0 then 1 else 0 end as IsLastCategorySignedApplication,
			case when p.IsFinalDecisionMade = 0 and p.IsQualifiedLead = 1 and p.IsApplicationSigned = 1 and p.IsAgreementApproved = 1 then 1 else 0 end as IsLastCategoryAgreementSent,
            case when p.IsFinalDecisionMade = 1 then 1 else 0 end as IsLastCategoryPaidOut
	from	WaterfallApplicationPre2 p
)";

        public List<MortgageLoanWaterfallApplicationModel> GetApplicationModels(
            string filterByProviderName = null,
            (DateTime From, DateTime To)? filterByMonthDates = null,
            (DateTime From, DateTime To)? filterByQuarterDates = null,
            (DateTime From, DateTime To)? filterByYearDates = null,
            (string ParameterName, string ParameterValue)? filterByCampaignParameter = null
            )
        {
            DateTime fromDate;
            DateTime toDate;
            string dateColumn;
            (DateTime From, DateTime To) CreateDateFilter((DateTime From, DateTime To)? dates, Func<DateTime, DateTime> transform) =>
                (transform(dates.Value.From), transform(dates.Value.To));

            if (filterByMonthDates.HasValue)
            {
                (fromDate, toDate) = CreateDateFilter(filterByMonthDates,
                    d => new DateTime(d.Year, d.Month, 1));
                dateColumn = "PeriodMonthDate";
            }
            else if (filterByQuarterDates.HasValue)
            {
                (fromDate, toDate) = CreateDateFilter(filterByQuarterDates,
                    d => Quarter.ContainingDate(d).FromDate);
                dateColumn = "PeriodQuarterDate";
            }
            else if (filterByYearDates.HasValue)
            {
                (fromDate, toDate) = CreateDateFilter(filterByYearDates,
                    d => new DateTime(d.Year, 1, 1));
                dateColumn = "PeriodYearDate";
            }
            else
                throw new Exception("Must provide a date filter");

            var parameters = new DynamicParameters(new { });

            var q = $" select p.* from WaterfallApplication p where 1=1 ";

            q += $" and p.{dateColumn} >= @fromDate";
            parameters.Add("@fromDate", fromDate);

            q += $" and p.{dateColumn} <= @toDate";
            parameters.Add("@toDate", toDate);

            if (!string.IsNullOrWhiteSpace(filterByProviderName))
            {
                q += $" and p.ProviderName = @providerName";
                parameters.Add("@providerName", filterByProviderName.Trim());
            }

            if (filterByCampaignParameter != null)
            {
                var n = filterByCampaignParameter.Value.ParameterName;
                var v = filterByCampaignParameter.Value.ParameterValue;
                if (!string.IsNullOrWhiteSpace(n) && !string.IsNullOrWhiteSpace(v))
                {
                    q += string.Format(
                        @" and exists (select 1
                        from	ComplexApplicationListItem c
                        where	c.ListName = '{0}'
                        and		c.Nr = 1
                        and		c.ItemName = @campaignParameterName
                        and		c.ItemValue = @campaignParameterValue
                        and		c.ApplicationNr = p.ApplicationNr)", CampaignCodeService.ParameterComplexListName);
                    parameters.Add("@campaignParameterName", n);
                    parameters.Add("@campaignParameterValue", v);
                }
            }

            using (var context = new PreCreditContext())
            {
                return context.Database.Connection.Query<MortgageLoanWaterfallApplicationModel>(WaterfallQuery + " " + q,
                    param: parameters, commandTimeout: 60).ToList();
            }
        }
    }

    public class MortgageLoanWaterfallApplicationModel
    {
        public string ApplicationNr { get; set; }
        public string ProviderName { get; set; }
        public DateTime? PeriodMonthDate { get; set; }
        public DateTime? PeriodQuarterDate { get; set; }
        public DateTime? PeriodYearDate { get; set; }
        public bool WasChangedToQualifiedLead { get; set; }
        public bool IsOrWasLead { get; set; }
        public bool IsLead { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrentLead { get; set; }
        public bool IsQualifiedLead { get; set; }
        public bool IsRejected { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsApplicationSigned { get; set; }
        public bool IsAgreementApproved { get; set; }
        public bool IsLastCategoryPaidOut { get; set; }
        public decimal? InitialCapitalDebt { get; set; }
        public bool IsLastCategoryQualifiedLead { get; set; }
        public bool IsLastCategorySignedApplication { get; set; }
        public bool IsLastCategoryAgreementSent { get; set; }
    }
}