using nPreCredit.Code;
using nPreCredit.Code.Services;
using System;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    public partial class CreditManagementController
    {
        [Route("GetOrCreateApplicationWrapperLink")]
        [HttpPost]
        public ActionResult GetOrCreateApplicationWrapperLink(string applicationNr, int? applicantNr)
        {
            var pattern = NEnv.ApplicationWrapperUrlPattern;
            if (pattern == null)
                return Content("Missing appsetting ntech.credit.applicationwrapper.urlpattern");

            using (var context = Service.Resolve<IPreCreditContextFactoryService>().CreateExtended())
            {
                var token = AgreementSigningProviderHelper.GetOrCreateApplicationWrapperToken(context, Clock.Now, applicationNr, applicantNr ?? 1, CurrentUserId, InformationMetadata);
                context.SaveChanges();

                return Json2(new { wrapperLink = new Uri(pattern.Replace("{token}", token.Token)).ToString() });
            }
        }

        [Route("TestApplicationWrapperLink")]
        public ActionResult TestApplicationWrapperLink(string applicationNr, int? applicantNr)
        {
            if (NEnv.IsProduction)
                return HttpNotFound();

            var pattern = NEnv.ApplicationWrapperUrlPattern;
            if (pattern == null)
                return Content("Missing appsetting ntech.credit.applicationwrapper.urlpattern");

            using (var context = Service.Resolve<IPreCreditContextFactoryService>().CreateExtended())
            {
                var token = AgreementSigningProviderHelper.GetOrCreateApplicationWrapperToken(context, Clock.Now, applicationNr, applicantNr ?? 1, CurrentUserId, InformationMetadata);
                context.SaveChanges();

                return Redirect(pattern.Replace("{token}", token.Token));
            }
        }
    }
}