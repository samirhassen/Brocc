using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace nScheduler.Controllers.Api
{
    [NTechAuthorizeAdmin]
    [RoutePrefix("Api")]
    public class ApiFetchServiceRunsController : NController
    {
        [HttpPost]
        [Route("Api/ServiceRun/GetHistoricRunsPage")]
        public ActionResult GetHistoricRunsPage(int pageSize, bool includeSkipped = false, int pageNr = 0)
        {
            using (var context = new SchedulerContext())
            {
                var baseResult = context
                    .ServiceRuns.AsQueryable();

                if (!includeSkipped)
                    baseResult = baseResult.Where(x => x.EndStatus != JobRunStatus.Skipped.ToString());

                var totalCount = baseResult.Count();
                var currentPage = baseResult
                    .OrderByDescending(x => x.Timestamp)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize)
                    .ToList()
                    .Select(x => new
                    {
                        x.JobName,
                        x.TimeSlotName,
                        x.StartDate,
                        x.EndDate,
                        x.RuntimeInMs,
                        x.EndStatusData,
                        x.EndStatus,
                        x.TriggeredById
                    })
                    .ToList()
                    .Select(x => new
                    {
                        x.JobName,
                        x.TimeSlotName,
                        x.StartDate,
                        x.EndDate,
                        x.RuntimeInMs,
                        EndStatusData = x.EndStatusData == null ? null : JsonConvert.DeserializeObject(x.EndStatusData),
                        x.EndStatus,
                        x.TriggeredById,
                        UserDisplayName = GetUserDisplayNameByUserId(x.TriggeredById.ToString())
                    })
                    .ToList();

                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return Json2(new
                {
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage.ToList()
                });
            }
        }

        public class LastJobRunStatus
        {
            public string JobName { get; set; }
            public string TriggeredInTimeSlotName { get; set; }
            public DateTimeOffset? StartDate { get; set; }
            public DateTimeOffset? EndDate { get; set; }
            public string EndStatus { get; set; }
            public object EndStatusData { get; set; }
            public long? RuntimeInMs { get; set; }
            public int? TriggeredById { get; set; }
            public string TriggeredByDisplayName { get; set; }
            public bool IsManualTriggerAllowed { get; set; }
        }

        public static LastJobRunStatus CreateLastJobRunStatus(ServiceCallModel serviceCall, ServiceRun lr, Func<string, string> getUserNameById)
        {
            return new LastJobRunStatus
            {
                JobName = serviceCall.Name,
                IsManualTriggerAllowed = serviceCall.IsManualTriggerAllowed,
                TriggeredInTimeSlotName = lr?.JobName,
                EndDate = lr?.EndDate,
                EndStatus = lr?.EndStatus,
                EndStatusData = lr?.EndStatusData == null ? (object)null : JsonConvert.DeserializeObject(lr.EndStatusData),
                RuntimeInMs = lr?.RuntimeInMs,
                StartDate = lr?.StartDate,
                TriggeredById = lr?.TriggeredById,
                TriggeredByDisplayName = (lr?.TriggeredById).HasValue ? getUserNameById(lr.TriggeredById.ToString()) : null,
            };
        }

        public static IDictionary<string, LastJobRunStatus> GetLastJobRunStatus(SchedulerContext context, Func<string, string> getUserNameById)
        {
            var latestsRuns = context
                .ServiceRuns
                .Where(x => x.EndStatus != JobRunStatus.Skipped.ToString())
                .GroupBy(x => x.JobName)
                .Select(x => x.OrderByDescending(y => y.Timestamp).FirstOrDefault())
                .ToDictionary(x => x.JobName, x => x, StringComparer.InvariantCultureIgnoreCase);

            var result = new Dictionary<string, LastJobRunStatus>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var serviceCall in NEnv.SchedulerModel.ServiceCalls.Values)
            {
                result[serviceCall.Name] = CreateLastJobRunStatus(
                    serviceCall,
                    latestsRuns.ContainsKey(serviceCall.Name) ? latestsRuns[serviceCall.Name] : null,
                    getUserNameById);
            }

            return result;
        }

        [HttpPost]
        [Route("Api/ServiceRun/GetLastJobRunStatus")]
        public ActionResult GetLastJobRunStatus()
        {
            using (var context = new SchedulerContext())
            {
                var result = GetLastJobRunStatus(context, this.GetUserDisplayNameByUserId);
                return Json2(result.Values.OrderBy(x => x.JobName).ToList());
            }
        }
    }
}