using Dapper;
using NTech;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace nDataWarehouse.Code.Services
{
    public class VintageReportService
    {
        private readonly Func<DateTimeOffset> getNow;

        public VintageReportService(Func<DateTimeOffset> getNow)
        {
            this.getNow = getNow;
        }

        public VintageReportResult FetchVintageReportData(VintageReportRequest request)
        {
            int? treatNotificationsAsClosedMaxBalance = string.IsNullOrWhiteSpace(request.TreatNotificationsAsClosedMaxBalance)
                ? new int?()
                : int.Parse(request.TreatNotificationsAsClosedMaxBalance);

            Func<string, bool> hasValue = x => !string.IsNullOrWhiteSpace(x);

            if ((hasValue(request.OverdueMonthsFrom) || hasValue(request.OverdueMonthsTo)) && (hasValue(request.OverdueDaysFrom) || hasValue(request.OverdueDaysTo)))
            {
                throw new ServiceException("OverdueMonths and OverdueDays cannot be combined")
                {
                    IsUserSafeException = true,
                    ErrorCode = "overdueMonthsAndDaysCannotBeCombined"
                };
            }

            request.SetDefaults();

            Func<string> actualInitialValue = () =>
            {
                var v = "b.InitialBalance + b.InitialAdditionalLoanBalance";

                return v;
            };
            Func<string> initialValue = () =>
                {
                    if (request.CellValueIsCount == "true")
                    {
                        return $"case when {actualInitialValue()} <> 0 then 1.0 else 0.0 end";
                    }
                    else
                        return actualInitialValue();
                };

            Func<string> actualValue = () =>
                {
                    string v;
                    if (request.ExcludeCapitalBalance == "true")
                    {
                        v = "0.0";
                    }
                    else if (hasValue(request.OverdueDaysFrom) || hasValue(request.OverdueDaysTo))
                    {
                        var f = "";

                        if (hasValue(request.OverdueDaysFrom))
                            f += $"b.OldestOpenNotificationOverdueDays >= {int.Parse(request.OverdueDaysFrom)}";

                        if (hasValue(request.OverdueDaysTo))
                        {
                            if (f.Length > 0)
                                f += " and ";
                            f += $"b.OldestOpenNotificationOverdueDays <= {int.Parse(request.OverdueDaysTo)}";
                        }

                        v = $"case when {f} then b.CapitalBalance else 0.0 end";
                    }
                    else if (hasValue(request.OverdueMonthsFrom) || hasValue(request.OverdueMonthsTo))
                    {
                        var f = "";

                        if (hasValue(request.OverdueMonthsFrom))
                            f += $"b.OldestOpenNotificationOverdueMonths >= {int.Parse(request.OverdueMonthsFrom)}";

                        if (hasValue(request.OverdueMonthsTo))
                        {
                            if (f.Length > 0)
                                f += " and ";
                            f += $"b.OldestOpenNotificationOverdueMonths <= {int.Parse(request.OverdueMonthsTo)}";
                        }

                        v = $"case when {f} then b.CapitalBalance else 0.0 end";
                    }
                    else
                    {
                        v = "b.CapitalBalance";
                    }
                    if (request.AccumulateDebtCollectionBalance == "true")
                    {
                        v += " + case when b.xDueDate >= b.DebtCollectionVintageMonth then b.ExportedToDebtCollectionBalance else 0.0 end";
                    }
                    else if (request.IncludeDebtCollectionBalance == "true")
                    {
                        v += " + case when b.xDueDate = b.DebtCollectionVintageMonth then b.ExportedToDebtCollectionBalance else 0.0 end";
                    }
                    return v;
                };
            Func<string> value = () =>
                {
                    if (request.CellValueIsCount == "true")
                    {
                        return $"case when {actualValue()} <> 0 then 1.0 else 0.0 end";
                    }
                    else
                        return actualValue();
                };

            var vintageBasisQuery =
$@"select    b.CreditNr,
		b.yDueDate as RowId,
		CONVERT(nvarchar(30), b.xDueDate, 112) as ColumnId,
        case when b.yDueDate = b.xDueDate then ({initialValue()}) else 0 end as InitialValue,
		({value()}) as Value,
        b.OldestOpenNotificationOverdueDays as OverdueDays,
        b.OldestOpenNotificationOverdueMonths as OverdueMonths,
        b.ProviderName,
        b.RiskGroup
from	ReportBasis b
where   1=1 ";

            QueryParams parameters = new QueryParams();

            string vintageMonthFilter = "";
            if (hasValue(request.AxisYFrom))
            {
                var yFromMonth = Dates.ParseDateTimeExactOrNull((request.AxisYFrom.Length > 7 ? request.AxisYFrom.Substring(0, 7) : request.AxisYFrom) + "-01", "yyyy-MM-dd");
                if (!yFromMonth.HasValue)
                    throw new ServiceException("Invalid AxisYFrom. Format should be YYYY-MM or YYYY-MM-DD") { IsUserSafeException = true, ErrorCode = "invalidAxisYFrom" };
                request.AxisYFrom = yFromMonth.Value.ToString("yyyy-MM");
                parameters.YFromMonth = yFromMonth.Value;
                vintageBasisQuery += " and DATEADD(month, DATEDIFF(month, 0, b.yDueDate), 0) >= @YFromMonth";
                vintageMonthFilter += " and DATEADD(month, DATEDIFF(month, 0, t.DueDate), 0) >= @YFromMonth";
            }

            if (hasValue(request.AxisYTo))
            {
                var yToMonth = Dates.ParseDateTimeExactOrNull((request.AxisYTo.Length > 7 ? request.AxisYTo.Substring(0, 7) : request.AxisYTo) + "-01", "yyyy-MM-dd");
                if (!yToMonth.HasValue)
                    throw new ServiceException("Invalid AxisYFrom. Format should be YYYY-MM or YYYY-MM-DD") { IsUserSafeException = true, ErrorCode = "invalidAxisYTo" };
                request.AxisYTo = yToMonth.Value.ToString("yyyy-MM");
                parameters.YToMonth = yToMonth.Value;
                vintageBasisQuery += " and DATEADD(month, DATEDIFF(month, 0, b.yDueDate), 0) <= @YToMonth";
                vintageMonthFilter += " and DATEADD(month, DATEDIFF(month, 0, t.DueDate), 0) <= @YToMonth";
            }

            if (hasValue(request.ProviderName))
            {
                vintageBasisQuery += " and b.ProviderName = @ProviderName";
                parameters.ProviderName = request.ProviderName;
            }
            if (hasValue(request.RiskGroup))
            {
                vintageBasisQuery += " and b.RiskGroup = @RiskGroup";
                parameters.RiskGroup = request.RiskGroup;
            }
            if (hasValue(request.CreditNr))
            {
                vintageBasisQuery += " and b.CreditNr = @CreditNr";
                parameters.CreditNr = request.CreditNr;
            }

            return WithSqlConnection(conn =>
            {
                conn.Execute($"{CreditApplicationDataQuery} select * into #tmp_CreditApplicationData from CreditApplicationData");
                conn.Execute("create index idx1 on #tmp_CreditApplicationData(CreditNr)");

                var vintageMonthTempTableName = $"[##DkVintageM_{Guid.NewGuid().ToString()}]"; //Since we use parameters the temp table will be instantly dropped if its not global since its created inside sp_executesql hence the ## global madness
                conn.Execute(GetVintageReportBasisQueryPrePattern(vintageMonthFilter, treatNotificationsAsClosedMaxBalance) + $" select * into {vintageMonthTempTableName} from DebtCollectionsWithVintageMonth", param: parameters);
                conn.Execute($"create index idx2 on {vintageMonthTempTableName}(CreditNr)");

                var q = GetVintageReportBasisQueryPattern(vintageMonthFilter, vintageMonthTempTableName, treatNotificationsAsClosedMaxBalance) + $", ReportData as ({vintageBasisQuery}) ";
                var columnIds = conn.Query<string>(q + "select CONVERT(nvarchar(30), DueDate, 112) from VintageMonths order by DueDate", param: parameters).ToList();

                var pivotedBasis = q + $", PivotedReportData as (select	c.* from ReportData b pivot (sum (b.Value) for b.ColumnId in ({string.Join(", ", columnIds.Select(x => $"[{x}]"))})) as c)";

                List<Detailsrow> detailedData = null;
                if (request.IncludeDetails == "true")
                {
                    detailedData = conn.Query<Detailsrow>(pivotedBasis + " select p.RowId as RowId, p.InitialValue, CONVERT(datetime, p.ColumnId, 112) as ColumnId, p.CreditNr as ItemId, p.Value, p.OverdueDays, p.OverdueMonths, p.ProviderName, p.RiskGroup from ReportData p order by p.RowId, p.ColumnId, p.CreditNr", param: parameters).ToList();
                }

                var vintageRows = new List<VintageRow>();

                Func<string, int, string> aggregateValue = (x, i) =>
                    {
                        var v = $"SUM(p.[{x}])";
                        if (request.ShowPercent == "true")
                        {
                            return $"case when SUM(p.InitialValue) = 0 then 0 else {v}/SUM(p.InitialValue) end";
                        }
                        else
                            return v;
                    };
                using (var reader = conn.ExecuteReader(pivotedBasis +
                    $@" select CONVERT(datetime, v.DueDate, 112) as RowId,
		                    isnull(SUM(p.InitialValue), 0) as InitialValue,
                            {string.Join(",", columnIds.Select((x, i) => $"({aggregateValue(x, i)}) as [c{i + 1}]"))}
                    from    VintageMonths v
                    left outer join PivotedReportData p on p.RowId = v.DueDate
                    group by v.DueDate
                    order by v.DueDate
                    ", param: parameters))
                {
                    var rowNr = 0;
                    while (reader.Read())
                    {
                        rowNr++;
                        var r = new VintageRow();
                        r.ColumnValues = new List<decimal?>();
                        r.InitialValue = reader.IsDBNull(reader.GetOrdinal("InitialValue")) ? new decimal?() : reader.GetDecimal(reader.GetOrdinal("InitialValue"));
                        r.RowId = reader.IsDBNull(reader.GetOrdinal("RowId")) ? new DateTime?() : reader.GetDateTime(reader.GetOrdinal("RowId"));
                        for (var i = 1; i <= columnIds.Count; i++)
                        {
                            var o = reader.GetOrdinal($"c{i}");
                            if (i >= rowNr) //Skip rows
                            {
                                r.ColumnValues.Add(reader.IsDBNull(o) ? 0m : reader.GetDecimal(o));
                            }
                            else if (!reader.IsDBNull(o))
                                throw new Exception("Logical error. This should no be possible given the model");
                        }
                        for (var j = 1; j < rowNr; j++)
                        {
                            r.ColumnValues.Add(null); //Add add the empty values at the end instead
                        }
                        vintageRows.Add(r);
                    }
                }

                conn.Execute($"drop table {vintageMonthTempTableName}");

                return new VintageReportResult
                {
                    ColumnCount = columnIds.Count,
                    DataRows = vintageRows,
                    DetailRows = detailedData,
                    ReportRequest = request,
                    CreationDate = this.getNow()
                };
            });
        }

        public List<DateTime> FetchVintageMonths()
        {
            return WithSqlConnection(conn => conn.Query<DateTime>($"{GetVintageReportBasisQueryPrePattern("", null)} select DueDate from VintageMonths order by DueDate").ToList());
        }

        private class QueryParams
        {
            public string ProviderName { get; set; }
            public string RiskGroup { get; set; }
            public DateTime? YFromMonth { get; set; }
            public DateTime? YToMonth { get; set; }
            public string CreditNr { get; set; }
        }

        private T WithSqlConnection<T>(Func<SqlConnection, T> f)
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DataWarehouse"].ConnectionString;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                return f(conn);
            }
        }

        private static string GetVintageReportBasisQueryPrePattern(string dateFilter, int? treatNotificationsAsClosedMaxBalance)
        {
            return $@"with
{GetVintageNotificationModelCte(treatNotificationsAsClosedMaxBalance)},
VintageMonths
as
(
	select t.DueDate,
		   ROW_NUMBER() over(order by DueDate) as Nr
	from	(select distinct DueDate from VintageModelNotificationState) t
    where   1=1
    {dateFilter}
),
Notifications
as
(
	select	h.CreditNr,
			c.StartDate,
			h.DueDate,
			ROW_NUMBER() over(partition by h.CreditNr order by h.DueDate) as NotificationNr
	from	VintageModelNotificationState h
	join	Dimension_Credit c on c.CreditNr = h.CreditNr
),
CreditsWithVintageMonth
as
(
	select	h.CreditNr,
            h.ProviderName,
			cast(h.StartDate as date) as StartDate,
			fn.DueDate as FirstNotificationDueDate,
			v.Nr as VintageMonth,
			v.DueDate as VintageDueDate,            
			(select	isnull(SUM(t.Amount), 0) from Fact_CreditCapitalBalanceEvent t where t.CreditNr = h.CreditNr and t.EventType in('NewCredit', 'CapitalizedInitialFee')) as InitialBalance,
			(select	isnull(SUM(t.Amount), 0) from Fact_CreditCapitalBalanceEvent t where t.CreditNr = h.CreditNr and t.EventType in('NewAdditionalLoan')) as InitialAdditionalLoanBalance
	from	Dimension_Credit h
	left outer join Notifications fn on fn.CreditNr = h.CreditNr and fn.NotificationNr = 1
	left outer join VintageMonths v on v.DueDate = fn.DueDate
),
DebtCollectionsWithVintageMonth
as
(
	select	f.TransactionDate as DebtColletionDate, 
			f.CreditNr, 
			isnull(-SUM(f.Amount), 0) as Amount,
			(select min(v.DueDate) from VintageMonths v where v.DueDate >= f.TransactionDate) as DebtCollectionVintageMonth
	from	Fact_CreditCapitalBalanceEvent f
	where	f.EventType = 'CreditDebtCollectionExport'
	group by f.TransactionDate, f.CreditNr
)";
        }

        private static string GetVintageReportBasisQueryPattern(string dateFilter, string vintageMonthTempTableName, int? treatNotificationsAsClosedMaxBalance)
        {
            return $@"{GetVintageReportBasisQueryPrePattern(dateFilter, treatNotificationsAsClosedMaxBalance)},
VintageMonthCreditSnapshotPre
as
(
	select	c.CreditNr,
            c.ProviderName,
			c.VintageDueDate as yDueDate,
			x.DueDate as xDueDate,
			c.FirstNotificationDueDate,			
			c.InitialBalance,
			c.InitialAdditionalLoanBalance,
			(select isnull(SUM(t.Amount), 0) from Fact_CreditCapitalBalanceEvent t where t.CreditNr = c.CreditNr and t.TransactionDate <= x.DueDate) as CapitalBalance,
			(select MIN(n.DueDate) from VintageModelNotificationState n where n.CreditNr = c.CreditNr and n.DueDate <= x.DueDate and (n.ReservationClosedDate is null or n.ReservationClosedDate > x.DueDate)) as OldestOpenNotificationDueDate,
            (select top 1 d.Amount from {vintageMonthTempTableName} d where d.CreditNr = c.CreditNr and d.DebtCollectionVintageMonth <= x.DueDate) as ExportedToDebtCollectionBalance,
            (select top 1 d.DebtCollectionVintageMonth from {vintageMonthTempTableName} d where d.CreditNr = c.CreditNr and d.DebtCollectionVintageMonth <= x.DueDate) as DebtCollectionVintageMonth			
	from	CreditsWithVintageMonth c
	join	VintageMonths y on y.Nr = c.VintageMonth
	join	VintageMonths x on x.Nr >= c.VintageMonth
),
VintageMonthCreditSnapshot
as
(
	select	c.*,
			isnull(DATEDIFF(d, c.OldestOpenNotificationDueDate, c.xDueDate), 0) as OldestOpenNotificationOverdueDays,
            isnull(DATEDIFF(m, c.OldestOpenNotificationDueDate, c.xDueDate), 0) as OldestOpenNotificationOverdueMonths
	from	VintageMonthCreditSnapshotPre c
),
ReportBasis
as
(
	select	c.CreditNr,
			c.yDueDate,
			c.xDueDate,
			c.InitialBalance,
			c.InitialAdditionalLoanBalance,
			c.CapitalBalance,
			c.OldestOpenNotificationOverdueDays,
            c.OldestOpenNotificationOverdueMonths,
			c.ExportedToDebtCollectionBalance,
            c.DebtCollectionVintageMonth,
            c.ProviderName,
            (select top 1 a.ScoreGroup from #tmp_CreditApplicationData a where a.CreditNr = c.CreditNr) as RiskGroup
	from	VintageMonthCreditSnapshot c
)
";
        }

        private static string GetVintageNotificationModelCte(int? treatNotificationsAsClosedMaxBalance)
        {
            if (treatNotificationsAsClosedMaxBalance.HasValue)
            {
                return string.Format(@"VintageModelNotificationStatePre
as
(
	select	s.CreditNr,
			s.DueDate,
			s.DueMonth,
			s.DwUpdatedDate,
			s.NotificationId,
			s.[Timestamp],
			s.IsOpen,
			s.ClosedDate,
			(select min(b.TransactionDate) from Fact_CreditNotificationBalanceSnapshot b where b.NotificationId = s.NotificationId and b.TotalBalance <= {0}) as EarliestLowBalanceDate
	from	Fact_CreditNotificationState s
),
VintageModelNotificationState
as
(
	select	s.CreditNr,
			s.DueDate,
			s.DueMonth,
			s.DwUpdatedDate,
			s.NotificationId,
			s.[Timestamp],
			case 
				when s.ClosedDate is not null and s.EarliestLowBalanceDate is not null and s.ClosedDate > s.EarliestLowBalanceDate then s.EarliestLowBalanceDate
				when s.ClosedDate is not null and s.EarliestLowBalanceDate is not null then s.ClosedDate
				when s.ClosedDate is not null then s.ClosedDate
				else s.EarliestLowBalanceDate
			end as ReservationClosedDate
	from	VintageModelNotificationStatePre s
)", treatNotificationsAsClosedMaxBalance.Value.ToString());
            }
            else
            {
                return @"VintageModelNotificationState
as
(
	select	s.CreditNr,
			s.DueDate,
			s.DueMonth,
			s.DwUpdatedDate,
			s.NotificationId,
			s.[Timestamp],
			s.ClosedDate as ReservationClosedDate
	from	Fact_CreditNotificationState s
)";
            }
        }

        private const string CreditApplicationDataQuery =
@"with 
Latest_Fact_CreditApplicationSnapshot
as
(
    select  fc.ApplicationNr, max(fc.[Date]) as LatestDate
    from    Fact_CreditApplicationSnapshot fc
    group by fc.ApplicationNr
),
CreditApplicationData
as
(
	select	fa.CreditNr,
			fa.ScoreGroup
	from	Fact_CreditApplicationSnapshot fa
	join    Latest_Fact_CreditApplicationSnapshot lf on fa.ApplicationNr = lf.ApplicationNr and fa.[Date] = lf.LatestDate
	where	fa.CreditNr is not null
)";
    }

    public interface IVintageReportService
    {
        VintageReportResult FetchVintageReportData(VintageReportRequest request);
        List<DateTime> FetchVintageMonths();
    }

    public class VintageRow
    {
        public DateTime? RowId { get; set; }
        public decimal? InitialValue { get; set; }
        public List<decimal?> ColumnValues { get; set; }
    }

    public class Detailsrow
    {
        public DateTime? RowId { get; set; }
        public decimal? InitialValue { get; set; }
        public DateTime? ColumnId { get; set; }
        public string ItemId { get; set; }
        public decimal? Value { get; set; }
        public int? OverdueDays { get; set; }
        public int? OverdueMonths { get; set; }
        public string ProviderName { get; set; }
        public string RiskGroup { get; set; }
    }

    public class VintageReportRequest
    {
        public string IncludeDebtCollectionBalance { get; set; } //Included on the month it's sent to debt collection
        public string AccumulateDebtCollectionBalance { get; set; } //Included on the month it's sent to debt collection and onwards
        public string OverdueDaysFrom { get; set; }
        public string OverdueDaysTo { get; set; }
        public string OverdueMonthsFrom { get; set; }
        public string OverdueMonthsTo { get; set; }
        public string ExcludeCapitalBalance { get; set; }
        public string CellValueIsCount { get; set; }
        public string ShowPercent { get; set; }
        public string AxisScaleY { get; set; } //Month, Quarter or Year. Default is Month
        public string AxisScaleX { get; set; } //Month, Quarter or Year. Default is Month
        public string IncludeDetails { get; set; }//Include the rows broken down per credit to allow drilling down to figure out why it looks like it does
        public string ProviderName { get; set; }
        public string RiskGroup { get; set; }
        public string AxisYFrom { get; set; } //YYYY-MM
        public string AxisYTo { get; set; } //YYYY-MM
        public string CreditNr { get; set; }
        public string TreatNotificationsAsClosedMaxBalance { get; set; }
        public void SetDefaults()
        {
            AxisScaleY = AxisScaleY ?? "Month";
            AxisScaleX = AxisScaleX ?? "Month";
        }
    }

    public class VintageReportResult
    {
        public int ColumnCount { get; set; }
        public List<VintageRow> DataRows { get; set; }
        public List<Detailsrow> DetailRows { get; set; }
        public VintageReportRequest ReportRequest { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}