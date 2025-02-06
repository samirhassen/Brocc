using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class CreditDocumentsController : NController
    {
        [HttpPost]
        [Route("Api/Credit/Documents/Fetch")]
        public ActionResult FetchCreditDocuments(string creditNr, bool? fetchFilenames, bool? includeExtraDocuments)
        {
            return Json2(this.Service.CreditDocuments.FetchCreditDocuments(
                creditNr,
                fetchFilenames.GetValueOrDefault(),
                includeExtraDocuments.GetValueOrDefault()));
        }
    }
}