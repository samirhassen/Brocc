using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    public class CompanyLoansHighSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override void ExtendParameters(IDictionary<string, object> p)
        {

        }

        [Route("Ui/CompanyLoan/Import")]
        public ActionResult Import()
        {
            return RenderComponent("company-loan-import", "Company loan - import");
        }
    }
}