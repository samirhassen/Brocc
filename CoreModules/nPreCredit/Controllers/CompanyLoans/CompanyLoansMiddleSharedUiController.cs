using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    [RoutePrefix("Ui")]
    public class CompanyLoansMiddleSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override void ExtendParameters(IDictionary<string, object> p)
        {
            p["workflowModel"] = NEnv.CompanyLoanWorkflow;
        }

        [Route("CompanyLoan/Search")]
        public ActionResult CompanyLoanSearch()
        {
            var urlToHere = Url.ActionStrict("CompanyLoanSearch", "CompanyLoansMiddleSharedUi", new { });
            var urlToHereFromOtherModule = Url.Encode(NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/Search").ToString());

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "company-loan-application-search", "Company Loan - Application Search");
        }

        [Route("CompanyLoan/Application")]
        public ActionResult CompanyLoanApplication(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            var urlToHere = Url.ActionStrict("CompanyLoanApplication", "CompanyLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/Application", Tuple.Create("applicationNr", applicationNr)).ToString());

            var s = CompanyLoanRejectionScoringSetup.Instance;
            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "company-loan-application", "Company Loan - Application", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "rejectionReasonToDisplayNameMapping", s.GetRejectionReasonDisplayNameByReasonName() },
                    { "rejectionRuleToReasonNameMapping", s.GetRejectionReasonNameByRuleName() },
                    { "creditUrlPattern", DependancyInjection.Services.Resolve<IServiceRegistryUrlService>().CreditUrl("NNN") },
                    { "navigationTargetCodeToHere", NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                        "CompanyLoanApplication",
                        new Dictionary<string, string> { { "applicationNr", applicationNr } }) }
                });
        }

        [Route("CompanyLoan/NewCreditCheck")]
        public ActionResult NewCreditCheck(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            var urlToHere = Url.ActionStrict("NewCreditCheck", "CompanyLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/NewCreditCheck", Tuple.Create("applicationNr", applicationNr)).ToString());

            var s = CompanyLoanRejectionScoringSetup.Instance;
            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "company-loan-initial-credit-check-new", "Company Loan - New Creditcheck", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "rejectionReasonToDisplayNameMapping", s.GetRejectionReasonDisplayNameByReasonName() },
                    { "rejectionRuleToReasonNameMapping", s.GetRejectionReasonNameByRuleName() },
                    { "creditUrlPattern", DependancyInjection.Services.Resolve<IServiceRegistryUrlService>().CreditUrl("NNN") }
                });
        }

        [Route("CompanyLoan/ViewCreditCheckDetails")]
        public ActionResult ViewCreditCheckDetails(string applicationNr)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");

            var urlToHere = Url.ActionStrict("ViewCreditCheckDetails", "CompanyLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/ViewCreditCheckDetails", Tuple.Create("applicationNr", applicationNr)).ToString());

            var s = CompanyLoanRejectionScoringSetup.Instance;
            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "company-loan-initial-credit-check-view", "Company Loan - View CreditCheck Details", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "rejectionReasonToDisplayNameMapping", s.GetRejectionReasonDisplayNameByReasonName() },
                    { "rejectionRuleToReasonNameMapping", s.GetRejectionReasonNameByRuleName() },
                    { "creditUrlPattern", DependancyInjection.Services.Resolve<IServiceRegistryUrlService>().CreditUrl("NNN") }
                });
        }

        [Route("CompanyLoan/Application/EditItem")]
        public ActionResult EditItem(string applicationNr, string dataSourceName, string itemName, bool? ro)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing dataSourceName");
            if (string.IsNullOrWhiteSpace(itemName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing itemName");
            if (!ro.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing ro");

            var t = initialDataHandler.Value.GetBack(this);
            var urlToHere = Url.ActionStrict("EditItem", "CompanyLoansMiddleSharedUi", new
            {
                applicationNr,
                dataSourceName,
                itemName,
                ro,
                backTarget = t?.GetBackTargetOrNull()
            });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/CompanyLoan/Application/EditItem",
                    Tuple.Create("applicationNr", applicationNr),
                    Tuple.Create("dataSourceName", dataSourceName),
                    Tuple.Create("itemName", itemName),
                    Tuple.Create("ro", ro.Value.ToString())).ToString());

            var id = new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "applicationType", "companyLoan" },
                    { "dataSourceName", dataSourceName },
                    { "itemName", itemName },
                    { "isReadOnly", ro.Value }
                };
            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "application-data-editor", "Company Loan - Edit Application Item", id);
        }
    }
}