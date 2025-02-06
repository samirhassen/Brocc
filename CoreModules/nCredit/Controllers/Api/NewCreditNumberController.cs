using Dapper;
using Newtonsoft.Json.Serialization;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class NewCreditNumberController : NController
    {
        [HttpPost]
        [Route("Api/NewCreditNumber")]
        public ActionResult NewCreditNumber()
        {
            var gen = new CreditNrGenerator(Service.ContextFactory);
            return Json2(new { nr = gen.GenerateNewCreditNr() });
        }

        [HttpPost]
        [Route("Api/NewCreditNumbers")]
        public ActionResult NewCreditNumbers(int? count)
        {
            var actualCount = count ?? 1;
            var gen = new CreditNrGenerator(Service.ContextFactory);            
            return Json2(new { nrs = gen.GenerateNewCreditNrs(actualCount) });
        }

        [HttpPost]
        [Route("Api/RecentlyGeneratedCreditNrs")]
        public ActionResult CreditNumberHistory(int? maxCount)
        {
            var actualMaxCount = maxCount ?? 500;
            using(var context = new CreditContext())
            {
                var result = context.GetConnection().Query<RecentCreditNrEntry>(
@"select top [[[MAX_COUNT]]] k.Id,
		@prefix + cast(k.Id as nvarchar(128)) as CreditNr,
		(select h.StartDate from CreditHeader h where h.CreditNr = (@prefix + cast(k.Id as nvarchar(128)))) as CreditStartDate
from	CreditKeySequence k
order by k.Id desc".Replace("[[[MAX_COUNT]]]", actualMaxCount.ToString()), param: new { prefix = CreditNrGenerator.CreditNrPrefix });

                var actionResult = new JsonNetActionResult
                {
                    Data = new { RecentCreditNrs = result }
                };
                actionResult.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                return actionResult;
            }
        }

        private class RecentCreditNrEntry
        {
            public long Id { get; set; }
            public string CreditNr { get; set; }
            public DateTimeOffset? CreditStartDate { get; set; }
        }
    }
}