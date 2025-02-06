using Dapper;
using NTech;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace nPreCredit.Code.Services
{
    public class ApplicationArchiveService : IApplicationArchiveService
    {
        public const int ArchiveLevel = 3;
        private readonly IClock clock;

        public ApplicationArchiveService(IClock clock)
        {
            this.clock = clock;
        }

        public void ArchiveApplications(List<string> applicationNrs)
        {
            if (applicationNrs.Count == 0)
                return;

            var now = clock.Now;

            var timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
            using (var connection = new SqlConnection(WebConfigurationManager.ConnectionStrings["PreCreditContext"].ConnectionString))
            {
                connection.Open();
                var tr = connection.BeginTransaction();

                var examplesAlreadyArchived = connection.Query<string>("select top 5 h.ApplicationNr from CreditApplicationHeader h where h.ApplicationNr in @applicationNrs and (h.ArchivedDate is not null or h.ArchiveLevel is not null)", commandTimeout: timeout, transaction: tr, param: new { applicationNrs }).ToList();
                if (examplesAlreadyArchived.Count > 0)
                    throw new Exception($"Some applications were already archived. Examples: {string.Join(", ", examplesAlreadyArchived)}");

                Func<DynamicParameters> getStandardParameters = () =>
                {
                    var p = new DynamicParameters();
                    p.AddDynamicParams(new
                    {
                        now,
                        applicationNrs,
                        archiveLevel = ArchiveLevel
                    });
                    return p;
                };

                void Execute(string query, object extraParameters = null)
                {
                    var parameters = getStandardParameters();
                    if (extraParameters != null)
                        parameters.AddDynamicParams(extraParameters);
                    try
                    {
                        connection.Execute(query, param: parameters, transaction: tr, commandTimeout: timeout);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Execute failed. Example application nrs: [{string.Join(", ", applicationNrs.Take(5))}]. Statement:{Environment.NewLine}{query}", ex);
                    }
                }

                try
                {
                    var queryBase =
@"with ApplicationToArchive 
as 
(
    select a.* 
    from   CreditApplicationHeader a 
    where  a.ApplicationNr in @applicationNrs
),
DecisionToArchive
as
(
    select  d.*
    from	ApplicationToArchive a
    join	CreditDecision d on d.ApplicationNr = a.ApplicationNr
),
FraudControlToArchive
as
(
    select  f.*
    from	ApplicationToArchive a
    join	FraudControl f on f.ApplicationNr = a.ApplicationNr
),
ApplicationEventToArchive
as
(
    select  e.*
    from	ApplicationToArchive a
    join	CreditApplicationEvent e on e.ApplicationNr = a.ApplicationNr
)
";

                    //Copy any pause item from credit decisions and then remove them
                    Execute(queryBase + @"INSERT INTO [CreditApplicationPauseItem] ([PauseReasonName],[CustomerId],[PausedUntilDate],[ApplicationNr],[ChangedById],[ChangedDate],[InformationMetaData])
select	p.RejectionReasonName, p.CustomerId,p. PausedUntilDate, d.ApplicationNr, p.ChangedById, p.ChangedDate, p.InformationMetaData 
from	DecisionToArchive d
join	CreditDecisionPauseItem p on p.CreditDecisionId = d.Id
where	p.PausedUntilDate > @now");

                    //Remove decisions
                    Execute(queryBase + "update CreditApplicationHeader set CurrentCreditDecisionId = null where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + "delete from CreditDecisionPauseItem where CreditDecisionId in(select d.Id from DecisionToArchive d)");
                    Execute(queryBase + "delete from CreditDecisionItem where CreditDecisionId in(select d.Id from DecisionToArchive d)");
                    Execute(queryBase + "delete from CreditDecisionSearchTerm where CreditDecisionId in(select d.Id from DecisionToArchive d)");
                    Execute(queryBase + "delete from CreditDecision where Id in(select d.Id from DecisionToArchive d)");

                    //Remove comments
                    Execute(queryBase + "delete from CreditApplicationComment where ApplicationNr in(select a.ApplicationNr from ApplicationToArchive a)");

                    //Remove items except preserved                    
                    var preservedItemNames = new List<string> { "customerId", "creditnr", "providerApplicationId" };
                    Execute(queryBase + " delete from CreditApplicationItem where Name not in @preservedItemNames and ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)", new { preservedItemNames });

                    Execute(queryBase + " delete from ComplexApplicationListItem where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + " delete from CreditApplicationCancellation where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + " delete from CreditApplicationChangeLogItem where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + " delete from CreditApplicationOneTimeToken where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");

                    //Remove fraud control
                    Execute(queryBase + "delete from FraudControlItem where FraudControl_Id in(select f.Id from FraudControlToArchive f)");
                    Execute(queryBase + "delete from FraudControl where Id in(select f.Id from FraudControlToArchive f)");
                    //TODO: FraudControlProperty? (not in the old code but should it really remain?)

                    //CreditApplicationLists
                    Execute(queryBase + "delete from CreditApplicationListMember where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + "delete from CreditApplicationListOperation where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");

                    //HComplexApplicationListItem
                    Execute(queryBase + "delete from HComplexApplicationListItem where ApplicationNr in (select a.ApplicationNr from ApplicationToArchive a)");
                    Execute(queryBase + "delete from HComplexApplicationListItem where ChangeEventId is not null and ChangeEventId in (select e.Id from ApplicationEventToArchive e)");

                    //Events
                    Execute(queryBase + "delete from CreditApplicationEvent where Id in (select e.Id from ApplicationEventToArchive e)");

                    //Flag as archived
                    Execute(queryBase +
                        @"update CreditApplicationHeader set ArchiveLevel = @archiveLevel, ArchivedDate = @now 
                            where ApplicationNr in(select a.ApplicationNr from ApplicationToArchive a)");

                    tr.Commit();
                }
                catch
                {
                    tr.Rollback();
                    throw;
                }
            }
        }

        public void WithArchivableApplicatioNrBatches(int batchSize, TimeSpan pauseCutoffTime, TimeSpan inactiveCutoffTime, Action<Func<(List<string> ApplicationNrsBatch, int RemainingNrOfArchivableApplications)>> withBatching)
        {
            Func<SqlConnection> createConnection = () => new SqlConnection(WebConfigurationManager.ConnectionStrings["PreCreditContext"].ConnectionString);
            using (var tempTableConnection = createConnection())
            {
                tempTableConnection.Open();
                try
                {
                    tempTableConnection.Execute(@"SELECT distinct h.ApplicationNr into ##TempRejectedOnPauseApplicationNrs
                        FROM	[CreditDecisionSearchTerm] t
                        join	[CreditDecision] d ON d.[Id] = t.[CreditDecisionId]
                        join	CreditApplicationHeader h on h.ApplicationNr = d.ApplicationNr and h.CurrentCreditDecisionId = d.Id
                        WHERE	t.[TermName] = 'RejectionReason' 
                        AND		t.[TermValue] = 'paused' 
                        and		h.ArchivedDate is null");
                    tempTableConnection.Execute("create index TempRejectedOnPauseApplicationNrsIdx1 on ##TempRejectedOnPauseApplicationNrs(ApplicationNr)");
                    withBatching(() =>
                    {
                        var now = clock.Now;
                        var pauseCutoffDate = now.Subtract(pauseCutoffTime);
                        var inactiveCutoffDate = now.Subtract(inactiveCutoffTime);

                        using (var connection = createConnection())
                        {
                            var sqlBase = @"with ApplicationCandidatePre
                                AS
                                (
                                    select
	                                h.ApplicationNr,
                                    h.[ApplicationDate] AS [ApplicationDate],
                                    h.[ArchivedDate] AS [ArchivedDate],
                                    h.[ArchiveLevel] AS [ArchiveLevel],
                                    h.[IsActive] AS [IsActive],
                                    h.[IsFinalDecisionMade] AS [IsFinalDecisionMade],
                                    CASE WHEN ( EXISTS (SELECT 1 from ##TempRejectedOnPauseApplicationNrs t where t.ApplicationNr = h.ApplicationNr)) THEN 1 ELSE 0 END AS IsRejectedOnPause
                                    FROM [CreditApplicationHeader] AS h
                                ),
                                ApplicationCandidate
                                as
                                (
	                                select	p.*
	                                from	ApplicationCandidatePre p
	                                where	( (p.IsRejectedOnPause = 1 and p.ApplicationDate < @pauseCutoffDate)
	                                            or
			                                  (p.ApplicationDate < @inactiveCutoffDate)
			                                )
	                                and		p.ArchivedDate is null
                                    and     p.IsActive = 0 
                                    and     p.IsFinalDecisionMade = 0
                                )";

                            var parameters = new { pauseCutoffDate, inactiveCutoffDate };

                            var totalNrOfArchivableApplications = connection.QueryFirst<int>(sqlBase + " select count(*) from ApplicationCandidate", parameters);

                            var result = connection.Query<string>(sqlBase + $"select top {batchSize} a.ApplicationNr from ApplicationCandidate a order by a.ApplicationDate", parameters).ToList();

                            var remainingNrOfArchivableApplications = Math.Max(totalNrOfArchivableApplications - result.Count, 0);

                            return (ApplicationNrsBatch: result, RemainingNrOfArchivableApplications: remainingNrOfArchivableApplications);
                        }
                    });
                }
                finally
                {
                    tempTableConnection.Execute("drop table ##TempRejectedOnPauseApplicationNrs");
                }
            }
        }
    }
    public interface IApplicationArchiveService
    {
        void ArchiveApplications(List<string> applicationNrs);
        void WithArchivableApplicatioNrBatches(int batchSize, TimeSpan pauseCutoffTime, TimeSpan inactiveCutoffTime, Action<Func<(List<string> ApplicationNrsBatch, int RemainingNrOfArchivableApplications)>> withBatching);
    }
}