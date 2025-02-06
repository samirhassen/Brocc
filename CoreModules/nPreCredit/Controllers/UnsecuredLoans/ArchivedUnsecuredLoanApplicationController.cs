using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using System;
using System.Linq;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class ArchivedUnsecuredLoanApplicationController : NController
    {
        [Route("Application/Archived")]
        public ActionResult Index(string applicationNr)
        {
            UrlHelper h = new UrlHelper();
            using (var context = new PreCreditContextExtended(Service.Resolve<INTechCurrentUserMetadata>(), this.Clock))
            {
                var model = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr && x.ArchivedDate.HasValue)
                    .Select(x => new
                    {
                        x.ArchivedDate,
                        Comment = x.Comments.FirstOrDefault().CommentText
                    })
                    .Single();

                SetInitialData(new
                {
                    ApplicationNr = applicationNr,
                    IsTest = !NEnv.IsProduction,
                    UrlToHereFromOtherModule = h.Encode(NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Application/Archived", Tuple.Create("applicationNr", applicationNr)).ToString()),
                    translation = GetTranslations()
                });
                return View();
            }
        }
    }
}