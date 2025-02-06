using Microsoft.AspNetCore.Mvc;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.PreCredit.Apis
{
    [ApiController]
    public class CustomerSearchController : Controller
    {
        private readonly CreditCustomerSearchSourceService creditCustomerSearchSourceService;

        public CustomerSearchController(CreditCustomerSearchSourceService creditCustomerSearchSourceService)
        {
            this.creditCustomerSearchSourceService = creditCustomerSearchSourceService;
        }

        /// <summary>
        /// Find customers by free text search. Credit nr, customer name, ocr number, email.
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/CustomerSearch/Find-Customers-Omni")]
        public ISet<int> FindCustomersOmni(CustomerSearchFindCustomersOmniRequest request) =>
            creditCustomerSearchSourceService.FindCustomers(request?.SearchQuery);

        /// <summary>
        /// Get credits for the customer
        /// </summary>
        [HttpPost]
        [Route("Api/Credit/CustomerSearch/Get-Customer-Entities")]
        public List<CustomerSearchEntity> GetCustomerEntities(CustomerSearchGetCustomerEntitiesRequest request) =>
            creditCustomerSearchSourceService.GetCustomerEntities((request ?? new CustomerSearchGetCustomerEntitiesRequest()).CustomerId);
    }

    public class CustomerSearchFindCustomersOmniRequest
    {
        public string SearchQuery { get; set; }
    }

    public class CustomerSearchGetCustomerEntitiesRequest
    {
        public int CustomerId { get; set; }
    }
}
