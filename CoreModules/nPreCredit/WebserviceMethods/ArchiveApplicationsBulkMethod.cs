using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Diagnostics;

namespace nPreCredit.WebserviceMethods
{
    public class ArchiveApplicationsBulkMethod : TypedWebserviceMethod<ArchiveApplicationsBulkMethod.Request, ArchiveApplicationsBulkMethod.Response>
    {
        public override string Path => "Application/ArchiveBulk";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var s = requestContext.Resolver().Resolve<IApplicationArchiveService>();
            try
            {
                var batchSize = Math.Min(100, request.MaxCountArchived ?? 100);
                var pauseCutoffTime = TimeSpan.FromDays(request.MinAgeInDaysBeforeArchivedIfPauseRejected ?? (30));
                var inactiveCutoffTime = TimeSpan.FromDays(request.MinAgeInDaysBeforeArchivedIfInactive ?? (90));
                var maxAllowedArchiveLevel = request.MaxAllowedArchiveLevel ?? NEnv.MaxAllowedArchiveLevel;
                var maxArchivedCountPerRun = request.MaxCountArchived ?? NEnv.MaxArchivedCountPerRun;

                if (maxAllowedArchiveLevel != ApplicationArchiveService.ArchiveLevel)
                    return Error("Invalid archiveLevel", errorCode: "invalidArchiveLevel");

                var w = Stopwatch.StartNew();
                var totalArchivedCount = 0;
                int remainingNrOfArchivableApplications = 0;
                s.WithArchivableApplicatioNrBatches(batchSize, pauseCutoffTime, inactiveCutoffTime, getBatch =>
                {
                    while (w.Elapsed < TimeSpan.FromMinutes(request.MaxRuntimeInMinutes ?? 15) && (!maxArchivedCountPerRun.HasValue || totalArchivedCount < maxArchivedCountPerRun.Value))
                    {
                        var nextBatchSize = maxArchivedCountPerRun.HasValue
                            ? Math.Min(Math.Max(maxArchivedCountPerRun.Value - totalArchivedCount, 0), batchSize)
                            : batchSize;
                        var getBatchResult = getBatch();
                        remainingNrOfArchivableApplications = getBatchResult.RemainingNrOfArchivableApplications;
                        var applicationNrs = getBatchResult.ApplicationNrsBatch;
                        if (applicationNrs.Count == 0)
                            break;
                        s.ArchiveApplications(applicationNrs);
                        totalArchivedCount += applicationNrs.Count;
                    }
                });

                return new Response
                {
                    NrOfArchivedApplications = totalArchivedCount,
                    RemainingNrOfArchivableApplications = remainingNrOfArchivableApplications
                };
            }
            catch (ServiceException ex)
            {
                if (!ex.IsUserSafeException)
                    throw;

                return Error(ex.Message, httpStatusCode: 400, errorCode: ex.ErrorCode);
            }
        }

        public class Response
        {
            public int NrOfArchivedApplications { get; set; }
            public int RemainingNrOfArchivableApplications { get; set; }
        }

        public class Request
        {
            public int? MaxAllowedArchiveLevel { get; set; }
            public int? MinAgeInDaysBeforeArchived { get; set; }
            public int? MinAgeInDaysBeforeArchivedIfPauseRejected { get; set; }
            public int? MinAgeInDaysBeforeArchivedIfInactive { get; set; }
            public int? MaxRuntimeInMinutes { get; set; }
            public int? MaxCountArchived { get; set; }
        }
    }
}