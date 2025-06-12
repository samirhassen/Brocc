using System.Web.Mvc;
using nSavings.Code;
using nSavings.Code.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;

namespace nSavings.Controllers.Api
{
    [NTechApi]
    public class ApiCustomerSearchController : NController
    {
        [HttpPost]
        [Route("Api/Savings/CustomerSearch/Find-Customers-Omni")]
        public ActionResult FindCustomersOmni(string searchQuery)
        {
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                    NEnv.ServiceRegistry);
            var service = new SavingsCustomerSearchSourceService(new SavingsAccountSearchService(customerClient));
            return Json2(service.FindCustomers(searchQuery));
        }

        [HttpPost]
        [Route("Api/Savings/CustomerSearch/Get-Customer-Entities")]
        public ActionResult GetCustomerEntities(int customerId)
        {
            var customerClient =
                LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance,
                    NEnv.ServiceRegistry);
            var service = new SavingsCustomerSearchSourceService(new SavingsAccountSearchService(customerClient));
            return Json2(service.GetCustomerEntities(customerId));
        }
    }
}