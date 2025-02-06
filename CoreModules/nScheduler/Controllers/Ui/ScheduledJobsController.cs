using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace nScheduler.Controllers
{
    [NTechAuthorizeAdmin]
    [RoutePrefix("Ui")]
    public class ScheduledJobsController : NController
    {
        [Route("ScheduledJobs")]
        [HttpGet]
        public ActionResult Index()
        {
            var model = NEnv.SchedulerModel;

            using (var context = new SchedulerContext())
            {
                var latestTimeslotStartDate = context
                    .ServiceRuns
                    .GroupBy(x => x.TimeSlotName ?? "Standalone")
                    .Select(x => new
                    {
                        TimeSlotName = x.Key,
                        StartDate = x.OrderByDescending(y => y.Timestamp).Select(y => y.StartDate).FirstOrDefault()
                    })
                    .ToDictionary(x => x.TimeSlotName, x => x.StartDate);

                var latestRuns = Api.ApiFetchServiceRunsController.GetLastJobRunStatus(context, this.GetUserDisplayNameByUserId);
                var mm = new
                {
                    getHistoryPageUrl = Url.Action("GetHistoricRunsPage", "ApiFetchServiceRuns"),
                    triggerManuallyUrl = Url.Action("TriggerServiceManually", "ApiTriggerTimeslot"),
                    timeSlots = model.Timeslots.Select(x => new
                    {
                        name = x.Name,
                        LatestStartDate = latestTimeslotStartDate.ContainsKey(x.Name) ? new DateTimeOffset?(latestTimeslotStartDate[x.Name]) : null,
                        jobs = x.Items.Select(y => new
                        {
                            jobName = y.ServiceCall.Name,
                            isManualTriggerAllowed = y.ServiceCall.IsManualTriggerAllowed,
                            triggerDescription = y.TriggerLimitation.GetDescription()
                        }).ToList()
                    }).ToList(),
                    latestRuns = latestRuns.Values.OrderBy(y => y.JobName).ToList()
                };

                ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.GetEncoding("iso-8859-1").GetBytes(JsonConvert.SerializeObject(mm)));
            }

            return View();
        }
    }
}