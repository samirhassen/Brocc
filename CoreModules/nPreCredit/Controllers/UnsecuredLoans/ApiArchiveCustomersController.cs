using nPreCredit.Code.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechApi]
    [NTechAuthorize]
    public class ApiArchiveCustomersController : NController
    {
        [HttpPost()]
        [Route("Api/Jobs/ArchiveCustomers")]
        public ActionResult ArchiveCustomers(IDictionary<string, string> schedulerData = null)
        {
            if (!NEnv.IsUnsecuredLoansEnabled)
                throw new NotImplementedException();

            Func<string, string> getSchedulerData = s => (schedulerData != null && schedulerData.ContainsKey(s)) ? schedulerData[s] : null;

            return PreCreditContext.RunWithExclusiveLock("ntech.scheduledjobs.precreditarchivecustomers",
                    ArchiveCustomersI,
                    () => new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Job is already running"));
        }

        private ActionResult ArchiveCustomersI()
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var w = Stopwatch.StartNew();
            var countArchived = 0;
            try
            {
                //Since this starts with a significant backlog we just let it catch up slowly over many days instead of murdering the system on day one.
                var batchCountArchived = 1;
                int? applicationsLeftAfterCount = null;
                while (w.Elapsed < NEnv.MaxCustomerArchiveJobRuntime && batchCountArchived > 0)
                {
                    batchCountArchived = ArchiveBatch(observeApplicationsLeftAfterCount: x => applicationsLeftAfterCount = x);
                    countArchived += batchCountArchived;
                }

                w.Stop();
                var logData = $"ArchiveCustomers finished TotalMilliseconds={w.ElapsedMilliseconds}, Count archived={countArchived}";
                if (applicationsLeftAfterCount.HasValue)
                    logData += $", ApplicationsLeftAfterCount={applicationsLeftAfterCount.Value}";
                NLog.Information(logData);
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"ArchiveCustomers crashed");
                errors.Add($"ArchiveCustomers crashed, see error log for details");
            }
            finally
            {
                w.Stop();
            }
            return Json2(new { errors, totalMilliseconds = w.ElapsedMilliseconds, warnings, countArchived });
        }


        private int ArchiveBatch(Action<int?> observeApplicationsLeftAfterCount = null)
        {
            using (var context = new PreCreditContextExtended(NTechUser, Clock))
            {
                var applications = context
                    .CreditApplicationHeaders
                    .Select(x => new
                    {
                        Application = x,
                        CustomerIds = x.Items.Where(y => y.GroupName.StartsWith("applicant") && y.Name == "customerId").Select(y => y.Value)
                    });

                var initialTs = KeyValueStoreService.GetValueComposable(context, "lastSeenTimestamp", "ArchiveCustomersJob");

                var source = applications.Where(x => x.Application.ArchivedDate.HasValue);

                Func<byte[], Tuple<byte[], int, ISet<string>>> getBatch = lastSeenTimestamp =>
                {
                    if (lastSeenTimestamp != null)
                        source = source.Where(x => BinaryComparer.Compare(x.Application.Timestamp, lastSeenTimestamp) > 0);

                    var candidateApplications = source
                        .OrderBy(x => x.Application.Timestamp)
                        .Take(100)
                        .Select(x => new { x.CustomerIds, x.Application.Timestamp })
                        .ToList();

                    var newLastSeenTimestamp = candidateApplications.Count == 0
                        ? lastSeenTimestamp
                        : candidateApplications[candidateApplications.Count - 1].Timestamp;

                    ISet<string> candidateCustomerIds = candidateApplications.SelectMany(x => x.CustomerIds).ToHashSet();

                    var customerIdsWithNonArchivedApplications = context
                        .CreditApplicationItems
                        .Where(y => y.GroupName.StartsWith("applicant") && y.Name == "customerId" && candidateCustomerIds.Contains(y.Value) && !y.CreditApplication.ArchivedDate.HasValue)
                        .Select(x => x.Value)
                        .ToHashSet();

                    candidateCustomerIds.ExceptWith(customerIdsWithNonArchivedApplications);

                    return Tuple.Create(newLastSeenTimestamp, candidateApplications.Count, candidateCustomerIds);
                };

                var customerIdsToArchive = new HashSet<int>();

                var lastCount = 1;
                byte[] ts = initialTs == null ? null : Convert.FromBase64String(initialTs);
                while (customerIdsToArchive.Count < NEnv.CustomerArchiveJobBatchSize && lastCount > 0)
                {
                    var batch = getBatch(ts);
                    customerIdsToArchive.AddRange(batch.Item3.Select(int.Parse));
                    lastCount = batch.Item2;
                    ts = batch.Item1;
                }

                int archiveCount = 0;
                if (customerIdsToArchive.Count > 0)
                {
                    var cc = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
                    archiveCount = cc.ArchiveCustomersBasedOnOurCandidates(customerIdsToArchive, NEnv.CurrentServiceName, NEnv.MaxCustomerArchiveJobRuntime) ?? 0;
                }


                if (ts != null)
                {
                    if (observeApplicationsLeftAfterCount != null)
                    {
                        observeApplicationsLeftAfterCount(source.Where(x => BinaryComparer.Compare(x.Application.Timestamp, ts) > 0).Count());
                    }

                    KeyValueStoreService.SetValueComposable(context, "lastSeenTimestamp", "ArchiveCustomersJob", Convert.ToBase64String(ts));
                }

                context.SaveChanges();

                return archiveCount;
            }
        }

        private static class BinaryComparer
        {
            public static int Compare(byte[] b1, byte[] b2)
            {
                throw new NotImplementedException();
            }
        }
    }
}