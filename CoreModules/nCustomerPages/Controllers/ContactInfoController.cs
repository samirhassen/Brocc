using nCustomerPages.Code;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;

namespace nCustomerPages.Controllers
{
    [RoutePrefix("contactinfo")]
    [CustomerPagesAuthorize(Roles = LoginProvider.SavingsOrCreditCustomerRoleName)]
    [PreventBackButton]
    public class ContactInfoController : BaseController
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.CurrentPageProductGroup = "ContactInfo";
            base.OnActionExecuting(filterContext);
        }

        protected CustomerLockedCustomerClient CreateCustomerClient()
        {
            return new CustomerLockedCustomerClient(this.CustomerId);
        }

        [Route("")]
        public ActionResult Index()
        {
            string userLanguage = null;
            var translation = GetTranslations(observeUserLanguage: x => userLanguage = x);

            var c = CreateCustomerClient();
            ViewBag.JsonInitialData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                translation = translation,
                customerContactInfo = c.GetContactInfo(),
                productsOverviewUrl = Url.Action("Index", "ProductOverview")
            })));
            var hasKycQuestions = NEnv.IsCustomerPagesKycQuestionsEnabled;
            ViewBag.HasKycQuestions = hasKycQuestions;
            if (hasKycQuestions)
            {
                string langPart = userLanguage == null ? "" : $"&lang={userLanguage}";
                ViewBag.KycUrl = NEnv.IsCustomerPagesKycQuestionsEnabled
                    ? $"/n/kyc/overview?fromTarget={CustomerNavigationTargetName.ProductOverview.ToString()}{langPart}"
                    : null;
                ViewBag.IsKycUpdateRequired = new CustomerLockedHostClient(CustomerId).GetIsKycUpdateRequired();
            }

            return View();
        }
    }
}