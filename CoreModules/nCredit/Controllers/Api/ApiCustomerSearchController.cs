using nCredit.Code.Services;
using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechApi]
    public class ApiCustomerSearchController : NController
    {
        private CreditCustomerSearchSourceService CreateService()
        {
            var clock = new CoreClock();
            var contextFactory = new NTech.Core.Credit.Shared.Database.CreditContextFactory(() => new CreditContextExtended(GetCurrentUserMetadata(), clock));
            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var searchService = new CreditSearchService(
                customerClient,
                NEnv.ClientCfgCore, contextFactory, NEnv.EnvSettings);
            return new CreditCustomerSearchSourceService(searchService, contextFactory, clock);
        }

        [HttpPost]
        [Route("Api/Credit/CustomerSearch/Find-Customers-Omni")]
        public ActionResult FindCustomersOmni(string searchQuery)
        {

            var service = CreateService();
            return Json2(service.FindCustomers(searchQuery));
        }

        [HttpPost]
        [Route("Api/Credit/CustomerSearch/Get-Customer-Entities")]
        public ActionResult GetCustomerEntities(int customerId)
        {
            var service = CreateService();
            return Json2(service.GetCustomerEntities(customerId));
        }
    }
}