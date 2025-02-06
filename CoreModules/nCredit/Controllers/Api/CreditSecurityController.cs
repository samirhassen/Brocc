using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class CreditSecurityController : NController
    {
        [HttpPost]
        [Route("Api/Credit/Security/FetchItems")]
        public ActionResult FetchCreditSecurityItems(string creditNr, int? lastIncludedBusinessEventId = null)
        {
            return Json2(this.Service.CreditSecurity.FetchSecurityItems(creditNr, lastIncludedBusinessEventId: lastIncludedBusinessEventId));
        }
    }
}