using NTech.Services.Infrastructure.NTechWs;
using NTech.Services.Infrastructure;
using System;
using System.IO;
using System.Web.Mvc;
using System.Web.Routing;
using NTech.Services.Infrastructure.CreditStandard;
using NTech;
using System.Linq;
using System.Security.Claims;
using System.Net;
using nGccCustomerApplication.Code;

namespace nGccCustomerApplication.Controllers.EmbeddedCustomerApplication
{
    public class EmbeddedCustomerApplicationController : EmbeddedCustomerApplicationControllerBase
    {
        /*        
       Given a angular app started with 
       ng build --base-href='/s/' --watch  
       That has at least the routes: '/' and '/app'
       You can surf to /s/ and /s/app/
       The reason this controller is needed is so /s/app doesnt 404 since mvc will by default expect that to be a request for the static file
       /s/app/index.html (which is why /s just works since it loads /s/index.html ... which is also why we replace with that)

       This should probably be dynamic so we can have several plugins but that can be added as needed. The s prefix would then be from config         
        */

        [AllowAnonymous]
        public ActionResult Content()
        {
            this.Response.Headers.Remove("ETag");
            this.Response.Headers["Cache-Control"] = "max-age=0, no-cache, no-store, must-revalidate";
            this.Response.Headers["Pragma"] = "no-cache";
            this.Response.Headers["Expires"] = "Wed, 12 Jan 1980 05:00:00 GMT";
            return File(Server.MapPath("/n/index.html"), "text/html");
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("n/assets/{*pathInfo}");

            //https://stackoverflow.com/questions/4684012/how-to-ignore-all-of-a-particular-file-extension-in-mvc-routing
            Action<string> ignoreExtension = ext => routes.IgnoreRoute("{*ext}", new { ext = @".*\." + ext + "(/.*)?" });
            ignoreExtension("woff");
            ignoreExtension("woff2");
            ignoreExtension("eot");
            ignoreExtension("svg");
            ignoreExtension("ttf");
            ignoreExtension("ico");

            routes.MapRoute(
                name: "EmbeddedCustomerApplication",
                url: "n/{*path}",
                defaults: new { controller = "EmbeddedCustomerApplication", action = "Content" }
            );

            routes.MapRoute(
                name: "EmbeddedCustomerApplicationForwardedApiCalls",
                url: "api/embedded-customerapplication/proxy/{module}/{*path}",
                defaults: new { controller = "EmbeddedCustomerApplication", action = "ForwardedApiCall" }
            );
        }

        [NTechApi]
        [Route("api/embedded-customerapplication/download-document")]
        [HttpGet]
        public ActionResult DownloadDocument(string archiveKey, bool skipFilename = false)
        {
            using (var ms = new MemoryStream())
            {
                var r = NHttp
                    .Begin(NEnv.ServiceRegistry.Internal.ServiceRootUri("nDocument"), systemUserBearerToken.Value.GetToken(), TimeSpan.FromMinutes(5))
                    .Get("Archive/Fetch?key=" + archiveKey);

                if (r.IsNotFoundStatusCode)
                    return NTechWebserviceMethod.ToFrameworkErrorActionResult(NTechWebserviceMethod.CreateErrorResponse("No such document exists", errorCode: "noSuchDocumentExists", httpStatusCode: 400));

                r.DownloadFile(ms, out var contentType, out var filename);
                var f = File(ms.ToArray(), contentType);
                if (!skipFilename)
                    f.FileDownloadName = filename;
                return f;
            }
        }

        [NTechApi]
        [Route("api/embedded-customerapplication/fetch-config")]
        [AllowAnonymous]
        public ActionResult FetchConfig()
        {
            var clientConfig = NEnv.ClientCfg;

            var activeServiceNames = NEnv.ServiceRegistry.Internal.Keys
                .Concat(NEnv.ServiceRegistry.External.Keys).DistinctPreservingOrder()
                .ToList();

            var config = new
            {
                IsTest = !NEnv.IsProduction,
                LogoutUrl = Url.Action("Logout", "Common"),
                LoginUrlPattern = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", "portal/navigate-with-login",
                    Tuple.Create("targetName", "___targetname___")).ToString(),
                LoginUrlWithCustomDataPattern = NEnv.ServiceRegistry.External.ServiceUrl("nGccCustomerApplication", "portal/navigate-with-login",
                    Tuple.Create("targetName", "___targetname___"), Tuple.Create("targetCustomData", "___targetcustomdata___")).ToString(),
                Client = new
                {
                    ClientName = clientConfig.ClientName,
                    BaseCountry = clientConfig.Country.BaseCountry,
                    BaseCurrency = clientConfig.Country.BaseCurrency
                },
                ActiveServiceNames = activeServiceNames,
                ActiveFeatures = clientConfig.ActiveFeatures?.Select(x => x.ToLowerInvariant())?.ToHashSet(),
                CurrentDateAndTime = ClockFactory.SharedInstance.Now.ToString("O"),
                UserDisplayName = User.Identity?.Name,
                AuthenticatedRoles = LoginProvider.GetAuthenticatedUserRoles(User),
                IsAuthenticated = User.Identity?.IsAuthenticated,
                Skinning = NEnv.IsSkinningEnabled ? new
                {
                    LogoUrl = Url.Content("~/Skinning/img/menu-header-logo.png")
                } : null,
                Enums = CreditStandardEnumService.Instance.GetApiEnums(language: NEnv.ClientCfg.Country.GetBaseLanguage())
            };
            return new JsonNetActionResult
            {
                Data = config
            };
        }

        [NTechApi]
        [Route("api/embedded-customerapplication/fetch-loggedin-user-details")]
        [HttpPost]
        public ActionResult FetchLoggedInUserDetails()
        {
            var u = User?.Identity as ClaimsIdentity;
            return new JsonNetActionResult
            {
                Data = new
                {
                    name = u?.FindFirst("ntech.claims.name")?.Value,
                    civicRegNr = u?.FindFirst("ntech.claims.civicregnr")?.Value
                }
            };
        }

        private bool IsApiWhiteListedForProxying(string moduleName, string localPath)
        {
            /*
             * When whiteliting things here think about the fact that the enduser can manipulate everything except the customer id.
             * Make sure the apis exposed here are safe to be called (in the sense of only affecting that user) under these premises.
             */
            //if (moduleName.EqualsIgnoreCase("nPreCredit"))
            //{
            //    return localPath.IsOneOfIgnoreCase(
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Fetch-Applications",
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Fetch-Application",
            //        "api/UnsecuredLoanStandard/Set-Customer-CreditDecisionCode",
            //        "api/MortgageLoanStandard/nGccCustomerApplication/Fetch-Application-KycStatus",
            //        "api/MortgageLoanStandard/nGccCustomerApplication/Answer-Kyc-Questions",
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Confirm-Bank-Accounts",
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Save-DirectDebit-Account",
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Confirm-DirectDebit-Account",
            //        "api/MortgageLoanStandard/nGccCustomerApplication/Fetch-Applications",
            //        "api/MortgageLoanStandard/nGccCustomerApplication/Fetch-Application",
            //        "api/bankaccount/validate-nr-batch",
            //        "api/UnsecuredLoanStandard/nGccCustomerApplication/Edit-BankAccounts");
            //}
            //else if (moduleName.EqualsIgnoreCase("nCredit"))
            //{
            //    return localPath.IsOneOfIgnoreCase(
            //        "api/LoanStandard/nGccCustomerApplication/Fetch-Loans",
            //        "api/LoanStandard/nGccCustomerApplication/Fetch-Loan-AmortizationPlan",
            //        "api/LoanStandard/nGccCustomerApplication/Fetch-Interest-History",
            //        "api/LoanStandard/nGccCustomerApplication/Fetch-Capital-Transactions",
            //        "api/LoanStandard/nGccCustomerApplication/Fetch-Documents");
            //}
             if (moduleName.EqualsIgnoreCase("nCustomer"))
            {
                return localPath.IsOneOfIgnoreCase("Api/ContactInfo/UpdateMultiple");
            }
            else if (!NEnv.IsProduction && moduleName.EqualsIgnoreCase("nTest"))
            {
                return localPath.IsOneOfIgnoreCase(
                    "Api/TestPerson/GetOrGenerate",
                    "Api/Company/TestCompany/GetOrGenerateBulk");
            }
            else if (moduleName.EqualsIgnoreCase("NTechHost"))
            {
                return localPath.IsOneOfIgnoreCase(
                    "Api/Customer/KycQuestionUpdate/GetCustomerStatus",
                    "Api/Customer/KycQuestionUpdate/UpdateAnswers")
                    || (NEnv.IsStandardUnsecuredLoansEnabled &&
                        localPath.IsOneOfIgnoreCase(
                            "Api/PreCredit/UnsecuredLoanStandard/Create-Application-KycQuestionSession"));
            }
            else
                return false;
        }

        [NTechApi]
        [HttpPost]
        public ActionResult ForwardedApiCall()
        {
            // api/embedded-customerpages/<moduleName>/<localPathInModule>
            var moduleName = NTechServiceRegistry.NormalizePath(RouteData.Values["module"] as string);
            var localPath = NTechServiceRegistry.NormalizePath(RouteData.Values["path"] as string);

            if (!IsApiWhiteListedForProxying(moduleName, localPath))
                return NTechWebserviceMethod.ToFrameworkErrorActionResult(NTechWebserviceMethod.CreateErrorResponse("That api either doesnt exist or is not whitelisted", errorCode: "notFoundOrNotWhitelisted", httpStatusCode: 400));

            return SendForwardApiCall(request =>
            {
                if (!TrySetOrReplaceCustomerIdFromLoggedInUser(request))
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                return null;
            }, moduleName, localPath);
        }
    }
}