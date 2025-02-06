using nCustomerPages.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using static nCustomerPages.Code.CustomerLockedCreditClient;

namespace nCustomerPages.Controllers
{
    [CustomerPagesAuthorize(Roles = "CreditCustomer,SavingsCustomer")]
    [PreventBackButton]
    public class ProductOverViewController : BaseController
    {

        [Route("productoverview")]
        public ActionResult Index(string messageTypeCode)
        {
            var model = new ProductOverViewModel();

            string userLanguage = null;
            model.Translations = GetTranslationsSharedDict(this.Url, this.Request, observeUserLanguage: x => userLanguage = x);

            model.HasSavingModule = NEnv.ServiceRegistry.ContainsService("nSavings");
            model.HasCreditModule = NEnv.ServiceRegistry.ContainsService("nCredit");
            if (model.HasCreditModule && User.IsInRole("CreditCustomer"))
            {
                model.Credits = new CustomerLockedCreditClient(CustomerId).GetCredits()?.Credits;
            }
            if (model.HasSavingModule && User.IsInRole("SavingsCustomer"))
            {
                model.SavingsAccounts = new CustomerLockedSavingsClient(CustomerId).GetSavingsAccounts().Accounts;
            }
            model.HasKycQuestions = NEnv.IsCustomerPagesKycQuestionsEnabled;
            if (model.HasKycQuestions)
            {
                string langPart = userLanguage == null ? "" : $"&lang={userLanguage}";
                model.KycUrl = NEnv.IsCustomerPagesKycQuestionsEnabled
                    ? $"/n/kyc/overview?fromTarget={CustomerNavigationTargetName.ProductOverview.ToString()}{langPart}"
                    : null;
                model.IsKycReminderRequired = new CustomerLockedHostClient(CustomerId).GetIsKycReminderRequired();
            }

            model.MessageTypeCode = messageTypeCode;
            return View(model);
        }
    }

    public class ProductOverViewModel
    {

        public IList<GetCreditsResult.Credit> Credits { get; internal set; }
        public Dictionary<string, string> Translations { get; internal set; }
        public IList<CustomerLockedSavingsClient.SavingsAccount> SavingsAccounts { get; internal set; }
        public bool HasCreditModule { get; internal set; }
        public bool HasSavingModule { get; internal set; }
        public bool HasKycQuestions { get; set; }
        public string MessageTypeCode { get; internal set; }
        public string KycUrl { get; set; }
        public bool IsKycReminderRequired { get; set; }

        public string ConvertDecimal(decimal? c, CultureInfo f)
        {
            return c != null ? c.Value.ToString("N2", f) : "";
        }

        public string ConvertCurrency(decimal? c, CultureInfo f)
        {
            return c != null ? c.Value.ToString("C", f) : "";
        }

        public string ConvertDate(DateTime? c, CultureInfo f)
        {
            return c != null ? c.Value.ToString("d", f) : "";
        }
    }
}