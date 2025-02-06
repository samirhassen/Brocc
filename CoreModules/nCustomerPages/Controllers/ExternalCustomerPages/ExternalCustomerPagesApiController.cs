using nCustomerPages.Code.Clients;
using System.Web.Mvc;

namespace nCustomerPages.Controllers.ExternalCustomerPages
{
    [HandleApiError]
    [ApiKeyAuthentication("ExternalCustomerPagesApi")]
    public class ExternalCustomerPagesApiController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.externalCustomerPagesApi"))
            {
                filterContext.Result = HttpNotFound();
            }

            base.OnActionExecuting(filterContext);
        }

        [Route("api/v1/external-customerpages/get-person-customer-id")]
        [HttpPost()]
        public ActionResult GetPersonCustomerId(string civicRegNr)
        {
            var client = new InternalServiceProxyClient();
            return client.Post("nCustomer", "Api/CustomerIdByCivicRegNr", new { civicRegNr });
        }

        [Route("api/v1/external-customerpages/unsecured-loan-standard/active-credits")]
        [HttpPost()]
        public ActionResult ActiveCredits(int? customerId)
        {
            if (!NEnv.IsStandardUnsecuredLoansEnabled)
                return HttpNotFound();

            var client = new InternalServiceProxyClient();
            return client.Post("nCredit", "Api/LoanStandard/CustomerPages/Fetch-Loans", new { customerId });
        }
    }
}