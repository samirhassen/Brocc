using nCustomerPages.Code;
using Newtonsoft.Json;
using NTech.Banking.Conversion;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers
{
    [CustomerPagesAuthorize(AllowEmptyRole = true)]
    [RoutePrefix("portal")]
    public class CustomerPortalController : BaseController
    {
        private ActionResult NavigateI(CustomerNavigationTargetName t, string extVarKey, string targetCustomData)
        {
            if (CustomerPortalService.ShouldBeForcedToAnswerKycQuestions(t, CustomerId))
            {
                return NavigateToKycQuestions(fromTarget: t, hideBackUntilAnswered: true);
            }

            if (NEnv.IsStandardMortgageLoansEnabled)
            {
                return NavigateStandardMortgageLoan(t, extVarKey, targetCustomData);
            }

            if (NEnv.IsStandardUnsecuredLoansEnabled)
            {
                return NavigateStandardUnsecuredLoan(t, extVarKey, targetCustomData);
            }

            if (t == CustomerNavigationTargetName.Overview)
            {
                if (User.IsInRole(LoginProvider.SavingsCustomerRoleName))
                    t = CustomerNavigationTargetName.ProductOverview;
                else if (User.IsInRole(LoginProvider.CreditCustomerRoleName))
                    t = CustomerNavigationTargetName.ProductOverview;
                else if (User.IsInRole(LoginProvider.EmbeddedMortageLoanCustomerPagesCustomer))
                    t = CustomerNavigationTargetName.MortgageLoanOverview;
            }

            switch (t)
            {
                case CustomerNavigationTargetName.ProductOverview:
                    {
                        if (User.IsInRole(LoginProvider.CreditCustomerRoleName) || User.IsInRole(LoginProvider.SavingsCustomerRoleName))
                            return RedirectToAction("Index", "ProductOverview");
                        else
                            return Fallback();
                    };
                case CustomerNavigationTargetName.MortgageLoanOverview:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedMortageLoanCustomerPagesCustomer))
                            return Redirect("/c/");
                        else
                            return Fallback();
                    }
                case CustomerNavigationTargetName.SavingsStandardApplication:
                    {
                        if (NEnv.IsSavingsApplicationActive)
                            return RedirectToAction("Index", "SavingsStandardApplication", new { extVarKey });
                        else
                            return Fallback();
                    };
                case CustomerNavigationTargetName.MortgageLoanApplication:
                    {
                        return Fallback();
                    }
                case CustomerNavigationTargetName.SecureMessages:
                    {
                        if (NEnv.IsSecureMessagesEnabled)
                            return RedirectToAction("Index", "SecureMessages");
                        else
                            return Fallback();
                    }
                case CustomerNavigationTargetName.KycQuestions:
                    {
                        return NavigateToKycQuestions();
                    }
                default:
                    return Fallback();
            }
        }

        private ActionResult NavigateToKycQuestions(CustomerNavigationTargetName? fromTarget = null, bool hideBackUntilAnswered = false)
        {
            if (NEnv.IsCustomerPagesKycQuestionsEnabled)
            {
                return Redirect($"/n/kyc/overview?lang={GetUserLanguage(Request)}"
                    + (fromTarget.HasValue ? $"&fromTarget={fromTarget.Value.ToString()}" : "")
                    + (hideBackUntilAnswered ? "&hbu=1" : ""));
            }
            else
            {
                return Fallback();
            }
        }

        private ActionResult NavigateStandardMortgageLoan(CustomerNavigationTargetName t, string extVarKey, string targetCustomData)
        {
            switch (t)
            {
                case CustomerNavigationTargetName.ContinueMortgageLoanApplication:
                    return Redirect($"/n/mortgage-loan-applications/secure/webapplication/{WebUtility.UrlEncode(targetCustomData)}/start");
                case CustomerNavigationTargetName.ApplicationsOverview:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/mortgage-loan-applications/secure/overview");
                        }
                        else
                        {
                            ViewBag.Message = "Du har inga ansökningar hos oss.";
                            return View("EmbedddedCustomerPagesSimpleMessage");
                        }
                    }
                case CustomerNavigationTargetName.StandardOverview:
                    {
                        var hasApplicationButNoCredit = HasApplicationButNoCredit();
                        if (hasApplicationButNoCredit)
                            return NavigateStandardMortgageLoan(CustomerNavigationTargetName.ApplicationsOverview, extVarKey, targetCustomData);

                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/overview");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                case CustomerNavigationTargetName.SecureMessages:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/messages");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                case CustomerNavigationTargetName.StandardMyData:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/data");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                default:
                    return Fallback();
            }
        }

        private ActionResult NavigateStandardUnsecuredLoan(CustomerNavigationTargetName t, string extVarKey, string targetCustomData)
        {
            switch (t)
            {
                case CustomerNavigationTargetName.StandardOverview:
                    {
                        var hasApplicationButNoCredit = HasApplicationButNoCredit();
                        if (hasApplicationButNoCredit)
                            return NavigateStandardUnsecuredLoan(CustomerNavigationTargetName.ApplicationsOverview, extVarKey, targetCustomData);

                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/overview");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                case CustomerNavigationTargetName.SecureMessages:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/messages");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                case CustomerNavigationTargetName.ApplicationsOverview:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/unsecured-loan-applications/overview");
                        }
                        else
                        {
                            ViewBag.Message = "Du har inga ansökningar hos oss.";
                            return View("EmbedddedCustomerPagesSimpleMessage");
                        }
                    }
                case CustomerNavigationTargetName.Application:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            var applicationNr = new string((targetCustomData ?? "").Where(Char.IsLetterOrDigit).ToArray());
                            if (applicationNr.Length > 0)
                                return Redirect("/n/unsecured-loan-applications/application/" + applicationNr);
                            else
                                return Redirect("/n/unsecured-loan-applications/overview");
                        }
                        else
                        {
                            return Content("Du har inga ansökningar hos oss/You have no applications with us.");
                        }
                    }
                case CustomerNavigationTargetName.StandardMyData:
                    {
                        if (User.IsInRole(LoginProvider.EmbeddedCustomerPagesStandardRoleName))
                        {
                            return Redirect("/n/my/data");
                        }
                        else
                        {
                            return Fallback();
                        }
                    }
                default:
                    return Fallback();
            }
        }

        private bool HasApplicationButNoCredit()
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.precredit"))
            {
                return false;
            }

            //Customers with no active loans but active applications are redirected to prevent navigation mistakes
            //where customers pass over the application link and go directly to mypages from the clients homepage
            //Remove this when applications are folded into mypages
            var creditClient = new CustomerLockedCreditClient(CustomerId);
            if (!creditClient.HasActiveCredit())
            {
                var preCreditClient = new PreCreditClient(() => NEnv.SystemUserBearerToken);
                if (preCreditClient.GetApplications(CustomerId).Any())
                {
                    return true;
                }
            }
            return false;
        }

        private ActionResult Fallback() =>
            (IsStrongIdentity)
                ? RedirectToAction("NotACustomer", "CustomerPortal")
                : RedirectToAction("Logout", "Common");

        [Route("navigate")]
        public ActionResult Navigate(string targetName, string extVarKey, string targetCustomData)
        {
            CustomerNavigationTargetName t;
            if (string.IsNullOrWhiteSpace(targetName))
                t = CustomerNavigationTargetName.Overview;
            else
                t = Enums.Parse<CustomerNavigationTargetName>(targetName) ?? CustomerNavigationTargetName.Overview;

            return NavigateI(t, extVarKey, targetCustomData);
        }

        [Route("navigate-with-login")]
        [AllowAnonymous]
        public ActionResult NavigateWithLogin(string targetName, string extVarKey, string targetCustomData)
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return Navigate(targetName, extVarKey, targetCustomData);
            }
            else if (NEnv.IsDirectEidAuthenticationModeEnabled)
            {
                return RedirectToAction("LoginWithEid", "EidSignatureLogin", new { targetName, extVarKey, targetCustomData });
            }
            else
                return RedirectToAction("Navigate", new { targetName, extVarKey, targetCustomData }); //To trigger the normal access denied

        }

        [Route("notacustomer")]
        public ActionResult NotACustomer()
        {
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = BaseController.GetTranslationsShared(this.Url, this.Request)
            })));
            return View();
        }
    }
}