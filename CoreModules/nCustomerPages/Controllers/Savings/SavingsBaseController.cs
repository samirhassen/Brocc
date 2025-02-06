using nCustomerPages.Code;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.Savings
{
    [RoutePrefix("savings")]
    [CustomerPagesAuthorize(Roles = "SavingsCustomer")]
    public abstract class SavingsBaseController : BaseController
    {
        public enum MessageTypeCode
        {
            alreadyhaveaccount,
            newaccountgreeting,
            accountbeingprocessed
        }

        protected string MakeSavingsOverviewMessageUrl(MessageTypeCode code, string newAccountNr = null)
        {
            return Url.Action("Index", "ProductOverview", new
            {
                MessageTypeCode = code.ToString()
            });
        }

        protected CustomerLockedSavingsClient CreateCustomerLockedSavingsClient()
        {
            return new CustomerLockedSavingsClient(CustomerId);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentPageProductGroup = "Savings";
            base.OnActionExecuting(filterContext);
        }
    }
}