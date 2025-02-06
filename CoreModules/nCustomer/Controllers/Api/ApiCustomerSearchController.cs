using nCustomer.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nCustomer.Controllers.Api
{
    [NTechApi]
    public class ApiCustomerSearchController : NController
    {
        private CustomerCustomerSearchSourceService CreateService() =>
            new CustomerCustomerSearchSourceService(new CustomerSearchService(CreateSearchRepo, () => Service.CompanyLoanNameSearch));

        [HttpPost]
        [Route("Api/Customer/CustomerSearch/Find-Customers-Omni")]
        public ActionResult FindCustomersOmni(string searchQuery)
        {
            var service = CreateService();
            return Json2(service.FindCustomers(searchQuery));
        }

        [HttpPost]
        [Route("Api/Customer/CustomerSearch/Get-Customer-Entities")]
        public ActionResult GetCustomerEntities(int customerId)
        {
            var service = CreateService();
            return Json2(service.GetCustomerEntities(customerId));
        }
    }
}