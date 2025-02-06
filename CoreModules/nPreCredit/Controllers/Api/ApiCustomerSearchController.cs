using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    public class ApiCustomerSearchController : NController
    {
        [HttpPost]
        [Route("Api/PreCredit/CustomerSearch/Find-Customers-Omni")]
        public ActionResult FindCustomersOmni(string searchQuery)
        {
            var service = new PreCreditCustomerSearchService();
            return Json2(service.FindCustomers(searchQuery));
        }

        [HttpPost]
        [Route("Api/PreCredit/CustomerSearch/Get-Customer-Entities")]
        public ActionResult GetCustomerEntities(int customerId)
        {
            var service = new PreCreditCustomerSearchService();
            return Json2(service.GetCustomerEntities(customerId));
        }
    }
}