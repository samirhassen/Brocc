using nCredit.Code;
using nCredit.Code.Email;
using nCredit.DbModel.BusinessEvents;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Legacy.Module.Shared.Services;
using NTech.Services.Infrastructure;
using System.Linq;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class ChangeTermsManagementController : NController
    {
        [HttpGet]
        [Route("Ui/ChangeTermsManagement")]
        public ActionResult Index()
        {
            var date = Clock.Today;
            var m = new CreditTermsChangeBusinessEventManager(GetCurrentUserMetadata(), Service.LegalInterestCeiling,
                CoreClock.SharedInstance, NEnv.ClientCfgCore, Service.ContextFactory, NEnv.EnvSettings, EmailServiceFactory.SharedInstance,
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry), 
                new SerilogLoggingService(), Service.ServiceRegistry, x => NEnv.GetAffiliateModel(x));

            SetInitialData(new
            {
                today = date,
                creditsWithPendingTermChanges = m.GetCreditNrsWithPendingTermChangesSignedByAllApplicants().Select(x => new
                {
                    creditNr = x,
                    link = Url.Action("Index", "Credit", new { creditNr = x })
                }).ToList()
            });

            return View();
        }
    }
}