using nPreCredit.Code.Services;
using NTech.Services.Infrastructure;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditHigh]
    [RoutePrefix("Ui")]
    public class CompanyLoansHighSharedUiController : SharedUiControllerBase
    {
        private readonly IUserDisplayNameService userDisplayNameService;

        public CompanyLoansHighSharedUiController(IUserDisplayNameService userDisplayNameService)
        {
            this.userDisplayNameService = userDisplayNameService;
        }

        protected override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        [Route("CompanyLoan/ApproveApplications")]
        public ActionResult ApproveApplications()
        {
            var urlToHere = Url.ActionStrict("ApproveApplications", "CompanyLoansHighSharedUi", new { });
            var urlToHereFromOtherModule = Url.Encode(NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/Search").ToString());

            var userNameByUserId = userDisplayNameService.GetUserDisplayNamesByUserId();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "company-loan-approve-applications", "Company Loan - Approve applications", additionalParameters: new Dictionary<string, object>
                {
                    { "userNameByUserId", userNameByUserId }
                });
        }
    }
}