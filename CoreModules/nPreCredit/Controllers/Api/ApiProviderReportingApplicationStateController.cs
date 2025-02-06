using nPreCredit.Code.AffiliateReporting;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/providerreporting")]
    public class ApiProviderReportingApplicationStateController : NController
    {
        [Route("applicationstate")]
        [HttpPost]
        public ActionResult ApplicationState(string applicationNr)
        {
            var s = Service.Resolve<IAffiliateReportingService>();
            var u = Service.Resolve<INTechCurrentUserMetadata>();
            var result = s.GetCurrentApplicationState(applicationNr, u);
            return Json2(result);
        }

        [Route("applicationidsbycreditnrs")]
        [HttpPost()]
        public ActionResult FetchApplicationIdsByCreditNrs(List<string> creditNrs)
        {
            using (var context = new PreCreditContext())
            {
                var result = context
                    .CreditApplicationHeaders
                    .Select(x => new
                    {
                        ApplicationNr = x.ApplicationNr,
                        CreditNr = x.Items.Where(y => y.GroupName == "application" && y.Name == "creditnr" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault(),
                        ProviderApplicationId = x.Items.Where(y => y.GroupName == "application" && y.Name == "providerApplicationId" && !y.IsEncrypted).Select(y => y.Value).FirstOrDefault()
                    })
                    .Where(x => creditNrs.Contains(x.CreditNr))
                    .ToList();

                var missingCreditNrs = creditNrs.Where(x => !result.Any(y => y.CreditNr == x)).ToList();
                if (missingCreditNrs.Any())
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, $"These creditNrs have no matching application: {string.Join(", ", missingCreditNrs)}");
                }

                return Json2(new { applications = result });
            }
        }
    }
}
