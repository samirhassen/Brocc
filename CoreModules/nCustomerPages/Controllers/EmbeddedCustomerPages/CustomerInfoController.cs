using nCustomerPages.Code;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.EmbeddedCustomerPages
{
    // Shared between all products. 
    public class CustomerInfoController : EmbeddedCustomerPagesControllerBase
    {
        [Route("Api/FetchCustomerInfo")]
        [HttpPost]
        public ActionResult FetchCustomerInfo()
        {
            var customerClient = new CustomerLockedCustomerClient(this.CustomerId);
            var result = customerClient.GetContactInfo();

            return Json2(result);
        }
    }
}