using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class MortgageLoansMiddleSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override void ExtendParameters(IDictionary<string, object> p)
        {
            p["workflowModel"] = NEnv.MortgageLoanWorkflow;
        }

        [Route("Ui/MortgageLoan/Application")]
        public ActionResult Application(string applicationNr)
        {
            if (Service.GetService<IMortgageLoanLeadsWorkListService>().IsLead(applicationNr))
                return RedirectToAction("Lead", new { applicationNr });

            var urlToHere = Url.ActionStrict("Application", "MortgageLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/MortgageLoan/Application",
                Tuple.Create("applicationNr", applicationNr)).ToString();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "mortgage-loan-application-dynamic", "Mortgage loan - application", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "navigationTargetCodeToHere", NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                        "MortgageLoanApplication",
                        new Dictionary<string, string> { { "applicationNr", applicationNr } }) }
                });
        }

        [Route("Ui/MortgageLoan/Lead")]
        public ActionResult Lead(string applicationNr, int? workListId)
        {
            var urlToHere = Url.ActionStrict("Lead", "MortgageLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/MortgageLoan/Lead",
                Tuple.Create("applicationNr", applicationNr)).ToString();

            var opts = new Dictionary<string, object>
            {
                { "navigationTargetCodeToHere", NTechNavigationTarget.CreateCrossModuleNavigationTargetCode("MortgageLoanLead", new Dictionary<string, string> {{"applicationNr", applicationNr}}) },
                { "rejectionReasonToDisplayNameMapping", NEnv.MortgageLoanScoringSetup.RejectionReasons?.ToDictionary(x => x.Name, x => x.DisplayName) }
            };

            if (workListId.HasValue)
            {
                opts["workListApplicationNr"] = applicationNr;
                opts["workListId"] = workListId.Value.ToString();
            }
            else
            {
                opts["leadOnlyApplicationNr"] = applicationNr;
            }

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "mortgage-loan-lead", "Mortgage loan - lead", opts);
        }

        [Route("Ui/MortgageLoan/Search")]
        public ActionResult Search(string tabName = null)
        {
            var urlToHere = Url.ActionStrict("Search", "MortgageLoansMiddleSharedUi", new { backTarget = this.initialDataHandler.Value.GetBack(this)?.GetBackTargetOrNull() });
            var urlToHereFromOtherModule = Url.Encode(NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/MortgageLoan/Search").ToString());

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "mortgage-application-search-wrapper", "Mortgage loan application - search", new Dictionary<string, object>
                {
                    { "applicationUrlPattern", Url.ActionStrict("Application", "MortgageLoansMiddleSharedUi", new { applicationNr = "NNNNNN" }) },
                    { "initialTabName", string.IsNullOrWhiteSpace(tabName) ? "workList" : tabName }
                });
        }

        [Route("Ui/MortgageLoan/NewCreditCheck")]
        public ActionResult NewCreditCheck(string applicationNr, string scoringWorkflowStepName)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");
            if (string.IsNullOrWhiteSpace(scoringWorkflowStepName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing scoringWorkflowStepName");

            var urlToHere = Url.ActionStrict("NewCreditCheck", "MortgageLoansMiddleSharedUi", new { applicationNr, scoringWorkflowStepName });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/MortgageLoan/NewCreditCheck",
                    Tuple.Create("applicationNr", applicationNr),
                    Tuple.Create("scoringWorkflowStepName", scoringWorkflowStepName)).ToString());

            var wf = NEnv.MortgageLoanWorkflow;
            var componentName = wf.GetCustomDataAsAnonymousType(scoringWorkflowStepName, new { NewCreditCheckComponentName = (string)null })?.NewCreditCheckComponentName;
            if (string.IsNullOrWhiteSpace(componentName))
                throw new Exception($"{scoringWorkflowStepName} is missing CustomData.NewCreditCheckComponentName in the workflow file");

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                componentName, "Mortgage loan - New Creditcheck", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "scoringWorkflowStepName", scoringWorkflowStepName },
                    { "creditUrlPattern", DependancyInjection.Services.Resolve<IServiceRegistryUrlService>().CreditUrl("NNN") },
                    { "rejectionReasonToDisplayNameMapping", NEnv.MortgageLoanScoringSetup.RejectionReasons?.ToDictionary(x => x.Name, x => x.DisplayName) }
                });
        }

        [Route("Ui/MortgageLoan/ViewCreditCheckDetails")]
        public ActionResult ViewCreditCheckDetails(string applicationNr, string scoringWorkflowStepName)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");
            if (string.IsNullOrWhiteSpace(scoringWorkflowStepName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing scoringWorkflowStepName");

            var urlToHere = Url.ActionStrict("ViewCreditCheckDetails", "MortgageLoansMiddleSharedUi", new { applicationNr, scoringWorkflowStepName });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/MortgageLoan/ViewCreditCheckDetails",
                    Tuple.Create("applicationNr", applicationNr),
                    Tuple.Create("scoringWorkflowStepName", scoringWorkflowStepName)).ToString());

            var wf = NEnv.MortgageLoanWorkflow;
            var componentName = wf.GetCustomDataAsAnonymousType(scoringWorkflowStepName, new { ViewCreditCheckComponentName = (string)null })?.ViewCreditCheckComponentName;
            if (string.IsNullOrWhiteSpace(componentName))
                throw new Exception($"{scoringWorkflowStepName} is missing CustomData.ViewCreditCheckComponentName in the workflow file");

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                componentName, "Mortgage loan - View Credit Decision", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "scoringWorkflowStepName", scoringWorkflowStepName },
                    { "creditUrlPattern", DependancyInjection.Services.Resolve<IServiceRegistryUrlService>().CreditUrl("NNN") },
                    { "rejectionReasonToDisplayNameMapping", NEnv.MortgageLoanScoringSetup.RejectionReasons?.ToDictionary(x => x.Name, x => x.DisplayName) }
                });
        }

        [Route("Ui/MortgageLoan/EditItem")]
        public ActionResult EditItem(string applicationNr, string dataSourceName, string itemName, bool? ro, string backTarget)
        {
            if (string.IsNullOrWhiteSpace(applicationNr))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing applicationNr");
            if (string.IsNullOrWhiteSpace(dataSourceName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing dataSourceName");
            if (string.IsNullOrWhiteSpace(itemName))
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing itemName");
            if (!ro.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Missing ro");

            var urlToHere = Url.ActionStrict("EditItem", "MortgageLoansMiddleSharedUi", new
            {
                applicationNr,
                dataSourceName,
                itemName,
                ro,
                backTarget = this.initialDataHandler.Value.GetBack(this)?.GetBackTargetOrNull()
            });
            var urlToHereFromOtherModule = Url.Encode(
                NEnv.ServiceRegistry.External.ServiceUrl("nPreCredit", "Ui/MortgageLoan/EditItem",
                    Tuple.Create("applicationNr", applicationNr),
                    Tuple.Create("dataSourceName", dataSourceName),
                    Tuple.Create("itemName", itemName),
                    Tuple.Create("ro", ro.Value.ToString())).ToString());

            var id = new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "dataSourceName", dataSourceName },
                    { "itemName", itemName },
                    { "isReadOnly", ro.Value },
                    { "applicationType", "mortgageLoan" },
                    { "backTarget", backTarget }
                };

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "application-data-editor", "Mortgage Loan - Edit Application Item", id);
        }

        [Route("Ui/MortgageLoan/Edit-Collateral")]
        public ActionResult EditCollateral(string applicationNr, int? listNr)
        {
            var urlToHere = Url.ActionStrict("EditCollateral", "MortgageLoansMiddleSharedUi", new { applicationNr, listNr });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/MortgageLoan/Edit-Collateral",
                Tuple.Create("applicationNr", applicationNr),
                Tuple.Create("listNr", listNr?.ToString())).ToString();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "mortgage-application-collateral-edit", "Mortgage loan - Collateral", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "listNr", listNr },
                    { "navigationTargetCodeToHere", NTechNavigationTarget.CreateCrossModuleNavigationTargetCode(
                        "MortgageLoanApplicationEditCollateral",
                        new Dictionary<string, string> { { "applicationNr", applicationNr }, { "listNr", listNr?.ToString() } }) }
                });
        }

        [Route("Ui/MortgageLoan/Handle-Settlement")]
        public ActionResult HandleSettlement(string applicationNr)
        {
            var urlToHere = Url.ActionStrict("HandleSettlement", "MortgageLoansMiddleSharedUi", new { applicationNr });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/MortgageLoan/Handle-Settlement",
                Tuple.Create("applicationNr", applicationNr)).ToString();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "mortgage-loan-application-dual-settlement-handle", "Mortgage loan - Handle settlement", new Dictionary<string, object>
                {
                    { "applicationNr", applicationNr },
                    { "navigationTargetCodeToHere", "MortgageLoanHandleSettlement" }
                });
        }
    }
}