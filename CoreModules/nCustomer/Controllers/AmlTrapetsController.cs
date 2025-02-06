using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nCustomer.Controllers
{
    [NTechAuthorize]
    public class AmlTrapetsController : NController
    {
        [Route("Kyc/FetchTrapetsAmlData")]
        [HttpPost]
        public ActionResult FetchTrapetsAmlData(string latestSeenTimestamp, IList<int> customerIds)
        {
            var latestSeenTs = latestSeenTimestamp == null ? null : Convert.FromBase64String(latestSeenTimestamp);
            var repo = new Code.Services.Aml.Trapets.TrapetsAmlDataRepository();
            var result = repo.FetchTrapetsAmlData(latestSeenTs, customerIds);
            return Json2(new
            {
                items = result.Item2,
                newLatestSeenTimestamp = result.Item1 == null ? null : Convert.ToBase64String(result.Item1)
            });
        }
    }
}