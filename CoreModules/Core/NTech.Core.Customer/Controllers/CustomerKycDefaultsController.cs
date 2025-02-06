using Microsoft.AspNetCore.Mvc;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Customer.Controllers
{
    [ApiController]
    public class CustomerKycDefaultsController : Controller
    {
        private readonly CustomerKycDefaultsService service;

        public CustomerKycDefaultsController(CustomerKycDefaultsService service)
        {
            this.service = service;
        }

        [HttpPost]
        [Route("Api/Customer/SetupCustomerKycDefaults")]
        public SetupCustomerKycDefaultsResponse SetupCustomerKycDefaults(SetupCustomerKycDefaultsRequest request) =>
            service.SetupCustomerKycDefaults(request);
    }
}
