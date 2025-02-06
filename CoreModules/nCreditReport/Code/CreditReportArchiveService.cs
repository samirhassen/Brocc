using Dapper;
using nCreditReport;
using NTech;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Configuration;

namespace nCreditReport.Code
{
    public class CreditReportArchiveService : ICreditReportArchiveService
    {
        private readonly IClock clock;

        public CreditReportArchiveService(IClock clock)
        {
            this.clock = clock;
        }

        public int GetArchivableCreditReportsCount(int inactiveNrOfDaysCutoff)
        {
            using (var context = new CreditReportContext())
            {
                var minNrOfDaysSinceRequestTime = TimeSpan.FromDays(inactiveNrOfDaysCutoff);
                var minNrOfDaysSinceRequestDate = clock.Now.Subtract(minNrOfDaysSinceRequestTime);
                return GetCreditReportHeadersQuery(context, minNrOfDaysSinceRequestDate)
                   .Count();
            }
        }

        public IQueryable<CreditReportHeader> GetCreditReportHeadersQuery(CreditReportContext context, DateTimeOffset minNrOfDaysSinceRequestDate)
        {
            return context
                    .CreditApplicationHeaders
                    .Where(x => x.CustomerId.HasValue
                    && x.RequestDate < minNrOfDaysSinceRequestDate
                    && (x.TryArchiveAfterDate == null || x.TryArchiveAfterDate <= clock.Now));
        }

        public List<int> GetArchivableCreditReports(int batchSize, int inactiveNrOfDaysCutoff, out int totalAnalysedCreditReportsInBatch)
        {
            var minNrOfDaysSinceRequestTime = TimeSpan.FromDays(inactiveNrOfDaysCutoff);
            var minNrOfDaysSinceRequestDate = clock.Now.Subtract(minNrOfDaysSinceRequestTime);
            var creditReportsBatch = GetArchiveCreditReportsInBatchQuery(minNrOfDaysSinceRequestDate, batchSize);
            totalAnalysedCreditReportsInBatch = creditReportsBatch.Count;

            var creditReportsWithCustomersWithLoans = HandleCreditReportsForCustomersWithoutLoans(creditReportsBatch);
            var creditReportsWithCustomersWithInactiveLoans = HandleCreditReportsForCustomersWithInactiveApplications(creditReportsWithCustomersWithLoans, inactiveNrOfDaysCutoff);

            var result = creditReportsWithCustomersWithInactiveLoans
                .Select(x => x.Id)
                .ToList();

            return result;
        }
        public List<CreditReportHeaderItem> HandleCreditReportsForCustomersWithoutLoans(List<CreditReportHeaderItem> creditReports)
        {
            var client = new nCreditClient();
            var customerIds = creditReports
                .Select(x => x.CustomerId)
                .Cast<int>()
                .ToList();

            if (customerIds.Count < 1)
                return new List<CreditReportHeaderItem>();

            var customerIdsWithLoans = client.FilterOutCustomersWithLoans(customerIds);

            FlagVetoedCreditReportsTryLaterDate(
                creditReports
                .Where(x => !customerIdsWithLoans.Contains(x.CustomerId))
                .ToList());

            return creditReports
                .Where(x => customerIdsWithLoans.Contains(x.CustomerId))
                .ToList();
        }

        public void FlagVetoedCreditReportsTryLaterDate(List<CreditReportHeaderItem> vetoedCreditReports)
        {
            using (var context = new CreditReportContext())
            {
                var vetoedCreditReportIds = vetoedCreditReports
                    .Select(x => x.Id)
                    .ToList();

                var vetoedCreditReportHeaders = context
                    .CreditApplicationHeaders
                    .Where(x => vetoedCreditReportIds.Contains(x.Id))
                    .ToList();

                //TryLaterArchiveDate set to 30 days from now for vetoed credit reports 
                vetoedCreditReportHeaders
                    .ForEach(x => x.TryArchiveAfterDate = clock.Now.Add(TimeSpan.FromDays(30)));

                context.SaveChanges();
            }
        }

        public List<CreditReportHeaderItem> HandleCreditReportsForCustomersWithInactiveApplications(List<CreditReportHeaderItem> creditReports, int minNrOfDaysCutoff)
        {
            var client = new nPreCreditClient();
            var customerIds = creditReports
                .Select(x => x.CustomerId)
                .Distinct()
                .Cast<int>()
                .ToList();

            var customerIdsWithInactiveApplications = client.FilterOutCustomersWithInactiveApplications(customerIds, minNrOfDaysCutoff);

            FlagVetoedCreditReportsTryLaterDate(
                creditReports
                .Where(x => !customerIdsWithInactiveApplications.Contains(x.CustomerId))
                .ToList());

            return creditReports
                .Where(x => customerIdsWithInactiveApplications.Contains(x.CustomerId))
                .ToList();
        }

        public List<CreditReportHeaderItem> GetArchiveCreditReportsInBatchQuery(DateTimeOffset minNrOfDaysSinceRequestDate, int batchSize)
        {
            using (var context = new CreditReportContext())
            {
                return GetCreditReportHeadersQuery(context, minNrOfDaysSinceRequestDate)
                   .Select(x => new CreditReportHeaderItem
                   {
                       Id = x.Id,
                       CustomerId = x.CustomerId ?? 0
                   })
                   .Take(batchSize)
                   .ToList();
            }
        }

        public void ArchiveCreditReports(List<int> creditReportNrs)
        {
            if (creditReportNrs.Count == 0)
                return;

            using (var context = new CreditReportContext())
            {
                var timeout = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
                using (var connection = new SqlConnection(WebConfigurationManager.ConnectionStrings[context.ConnectionStringName].ConnectionString))
                {
                    connection.Open();
                    var tr = connection.BeginTransaction();

                    Func<DynamicParameters> getStandardParameters = () =>
                    {
                        var p = new DynamicParameters();
                        p.AddDynamicParams(new
                        {
                            creditReportNrs
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
                            throw new Exception($"Execute failed. Example credit report nrs: [{string.Join(", ", creditReportNrs.Take(5))}]. Statement:{Environment.NewLine}{query}", ex);
                        }
                    }

                    try
                    {
                        var queryBase =
                        @"with CreditReportToArchive
                    as (
	                    select c.*
	                    from CreditReportHeader c
	                    where c.id in @creditReportNrs
                    )";

                        //Remove from CreditReportHeader 
                        Execute(queryBase + "delete from CreditReportHeader where id in(select c.Id from CreditReportToArchive c)");


                        //Remove from CreditReportSearchTerm
                        Execute(queryBase + "delete from CreditReportSearchTerm where CreditReportHeaderId in(select c.Id from CreditReportToArchive c)");

                        //Remove from EncryptedCreditReportItem
                        Execute(queryBase + "delete from EncryptedCreditReportItem where CreditReportHeaderId in(select c.Id from CreditReportToArchive c)");

                        tr.Commit();
                    }
                    catch
                    {
                        tr.Rollback();
                        throw;
                    }
                }
                context.SaveChanges();
            }
        }
    }
}


public interface ICreditReportArchiveService
{
    int GetArchivableCreditReportsCount(int inactiveNrOfDaysCutoff);
    List<int> GetArchivableCreditReports(int batchSize, int inactiveNrOfDaysCutoff, out int totalNrOfAnalysedCreditReports);
    List<CreditReportHeaderItem> HandleCreditReportsForCustomersWithoutLoans(List<CreditReportHeaderItem> creditReports);
    List<CreditReportHeaderItem> HandleCreditReportsForCustomersWithInactiveApplications(List<CreditReportHeaderItem> creditReports, int days);
    List<CreditReportHeaderItem> GetArchiveCreditReportsInBatchQuery(DateTimeOffset minNrOfDaysSinceRequestDate, int batchSize);
    void ArchiveCreditReports(List<int> creditReportNrs);
    IQueryable<CreditReportHeader> GetCreditReportHeadersQuery(CreditReportContext context, DateTimeOffset minNrOfDaysSinceRequestDate);
}

public class CreditReportHeaderItem
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
}