using nCredit.Code.Services;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCreditSearchController : NController
    {
        [HttpPost]
        [Route("Api/Credit/Search")]
        public ActionResult Search(SearchCreditRequest request)
        {
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var searchService = new CreditSearchService(customerClient, NEnv.ClientCfgCore,
                new CreditContextFactory(() => new CreditContextExtended(GetCurrentUserMetadata(), Clock)), NEnv.EnvSettings);

            try
            {
                var result = searchService.Search(request);
                return Json2(new { hits = result });
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (ex.IsUserFacing)
                {
                    return new HttpStatusCodeResult(ex.ErrorHttpStatusCode ?? 400, ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}