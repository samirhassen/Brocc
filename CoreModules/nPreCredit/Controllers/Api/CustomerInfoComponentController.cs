using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/CustomerInfoComponent")]
    public class CustomerInfoComponentController : NController
    {
        private readonly ICustomerOfficialDataService customerOfficialDataService;

        public CustomerInfoComponentController(ICustomerOfficialDataService customerOfficialDataService)
        {
            this.customerOfficialDataService = customerOfficialDataService;
        }

        [HttpPost]
        [Route("FetchInitial")]
        public ActionResult FetchInitial(string applicationNr, int applicantNr, string backTarget)
        {
            return Json2(customerOfficialDataService.GetInitialCustomerInfo(applicationNr, applicantNr, NTechNavigationTarget.CreateFromTargetCode(backTarget)));
        }

        [HttpPost]
        [Route("FetchInitialByItemName")]
        public ActionResult FetchInitialByItemName(string applicationNr, string customerIdApplicationItemCompoundName, string customerBirthDateApplicationItemCompoundName, string backTarget)
        {
            return Json2(customerOfficialDataService.GetInitialCustomerInfoByItemName(
                applicationNr, customerIdApplicationItemCompoundName,
                customerBirthDateApplicationItemCompoundName, NTechNavigationTarget.CreateFromTargetCode(backTarget)));
        }

        [HttpPost]
        [Route("FetchInitialByCustomerId")]
        public ActionResult FetchInitialByCustomerId(int customerId, string backTarget)
        {
            return Json2(customerOfficialDataService.GetInitialCustomerInfoByCustomerId(customerId, NTechNavigationTarget.CreateFromTargetCode(backTarget)));
        }
    }
}