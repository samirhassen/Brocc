using NTech.Services.Infrastructure;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api")]
    public partial class ApiApplicationNrByCreditNrController : NController
    {
        [Route("GetApplicationNrByCreditNr")]
        [HttpPost]
        public ActionResult GetApplicationNrByCreditNr(List<string> creditNrs)
        {
            using (var context = new PreCreditContext())
            {
                var cs = (creditNrs ?? new List<string>()).Distinct().ToList();
                var result = context
                    .CreditApplicationItems
                    .Where(x => !x.IsEncrypted && x.Name == "creditnr" && x.CreditApplication.IsFinalDecisionMade && cs.Contains(x.Value))
                    .Select(x => new
                    {
                        ApplicationNr = x.ApplicationNr,
                        CreditNr = x.Value
                    })
                    .ToList();

                var groups = result.GroupBy(x => x.CreditNr);

                if (groups.Any(x => x.Count() > 1))
                {
                    NLog.Error("There are several applications with the same creditnr");
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
                }
                var r = groups.Select(x => new { CreditNr = x.Key, ApplicationNr = x.Single().ApplicationNr });
                return Json2(new { hits = r });
            }
        }
    }
}