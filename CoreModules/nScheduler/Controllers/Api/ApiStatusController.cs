using System;
using System.Linq;
using System.Web.Mvc;

namespace nScheduler.Controllers.Api
{
    public class ApiStatusController : NController
    {
        [HttpPost]
        [Route("Api/ServiceRun/FetchLastSuccessAgeInDaysByTag")]
        public ActionResult FetchLastSuccessAgeInDaysByTag(string tag)
        {
            var model = NEnv.SchedulerModel;

            var serviceCalls = model
                .ServiceCalls
                .Values
                .Where(x => x.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var serviceCallNames = serviceCalls.Select(x => x.Name).ToList();

            using (var context = new SchedulerContext())
            {
                var maxAge = context
                    .ServiceRuns
                    .Where(x => serviceCallNames.Contains(x.JobName) && x.EndStatus == "Success")
                    .GroupBy(x => x.JobName)
                    .Select(x => x.OrderByDescending(y => y.Id).FirstOrDefault())
                    .Select(x => new
                    {
                        x.JobName,
                        x.StartDate
                    })
                    .ToList()
                    .Max(x => (int?)Clock.Now.Date.Subtract(x.StartDate.Date).TotalDays);

                return Json2(maxAge);
            }
        }
    }
}