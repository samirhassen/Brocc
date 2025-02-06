using nCustomerPages.Code;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.Credit
{
    [CustomerPagesAuthorize(Roles = "CreditCustomer")]
    public abstract class CreditBaseController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentPageProductGroup = "Credit";
            base.OnActionExecuting(filterContext);
        }

        protected CustomerLockedCreditClient CreateCustomerLockedCreditClient()
        {
            return new CustomerLockedCreditClient(CustomerId);
        }
    }
}