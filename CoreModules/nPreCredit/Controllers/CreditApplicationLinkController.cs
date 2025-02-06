using System;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public class CreditApplicationLinkController : NController
    {
        [HttpGet]
        [Route("Ui/Application/{applicationNr}")]
        public ActionResult ApplicationGateway(string applicationNr, string backTarget)
        {
            var url = GetApplicationUrl(applicationNr, backTarget);
            if (url == null)
                return HttpNotFound();
            return Redirect(url.ToString());
        }

        private Uri GetApplicationUrl(string applicationNr, string backTarget)
        {
            var sr = NEnv.ServiceRegistry.Internal;
            Uri GetUrl(string moduleName, string relativeUrl, Tuple<string, string> extraParameter = null)
            {
                var backTargetParam = Tuple.Create("backTarget", backTarget);
                return sr.ServiceUrl(moduleName, relativeUrl, extraParameter == null ? new[] { backTargetParam } : new[] { backTargetParam, extraParameter }); ;
            }

            if (NEnv.IsStandardUnsecuredLoansEnabled)
                return GetUrl("nBackoffice", $"s/unsecured-loan-application/application/{applicationNr}");
            else if (NEnv.IsStandardMortgageLoansEnabled)
                return GetUrl("nBackoffice", $"s/mortgage-loan-application/application/{applicationNr}");
            else if (NEnv.IsMortgageLoansEnabled)
                return GetUrl("nPreCredit", "Ui/MortgageLoan/Application", Tuple.Create("applicationNr", applicationNr));
            else if (NEnv.IsUnsecuredLoansEnabled)
                return GetUrl("nPreCredit", "CreditManagement/CreditApplication", Tuple.Create("applicationNr", applicationNr));
            else if (NEnv.IsCompanyLoansEnabled)
                return GetUrl("nPreCredit", "Ui/CompanyLoan/Application", Tuple.Create("applicationNr", applicationNr));
            else
                return null;
        }
    }
}