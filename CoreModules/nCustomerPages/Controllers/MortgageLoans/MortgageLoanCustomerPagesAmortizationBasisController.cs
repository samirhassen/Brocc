using nCustomerPages.Controllers.Credit;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers.MortgageLoans
{
    [CustomerPagesAuthorize(Roles = LoginProvider.EmbeddedCustomerPagesStandardRoleName)]
    public class MortgageLoanCustomerPagesAmortizationBasisController : CreditBaseController
    {
        [Route("mortgageloans/api/credit/fetch-amortizationbasis")]
        [HttpGet]
        public ActionResult GetAmortizationBasisPdf(string creditNr)
        {
            var c = CreateCustomerLockedCreditClient();
            var result = c.GetAmortizationBasisPdf(creditNr);

            return File(result, "application/pdf");
        }

    }
}