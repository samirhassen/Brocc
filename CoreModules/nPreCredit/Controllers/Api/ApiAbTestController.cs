using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public class ApiAbTestController : NController
    {
        [Route("GetAllAbTestExperiments")]
        [HttpPost]
        public ActionResult GetAllAbTestExperiments()
        {
            using (var context = new PreCreditContext())
            {
                var experiments = context
                    .AbTestingExperiments
                   .Select(y => new AbTestExperimentModel
                   {
                       ExperimentId = y.Id,
                       ExperimentName = y.ExperimentName,
                       CreatedDate = y.CreatedDate
                   })
                   .OrderByDescending(x => x.CreatedDate)
                   .ToList();

                return Json2(experiments);
            }
        }
    }

    public class AbTestExperimentModel
    {
        public int ExperimentId { get; set; }
        public string ExperimentName { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
    }
}