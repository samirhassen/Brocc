using nPreCredit.Code;
using nPreCredit.Code.Agreements;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nPreCredit.Controllers.Api
{
    [NTechApi]
    [NTechAuthorize]
    [RoutePrefix("api/additionalquestions")]
    public class CreditApplicationAdditionalQuestionsController : NController
    {
        private bool TryFetchToken(string id, out string applicationNr, bool alsoRemoveToken, out bool isRemovedOrExpired)
        {
            applicationNr = null;
            using (var c = new PreCreditContext())
            {
                var token = c.CreditApplicationOneTimeTokens.SingleOrDefault(x => x.Token == id && x.TokenType == "AdditionalQuestions");
                if (token == null)
                {
                    NLog.Warning("Additional questions token {token} does not exist", id);
                    isRemovedOrExpired = false;
                    return false;
                }
                else if (token.RemovedBy.HasValue || token.RemovedDate.HasValue)
                {
                    NLog.Warning("Additional questions token {token} removed", id);
                    isRemovedOrExpired = true;
                    return false;
                }
                else if (token.ValidUntilDate < Clock.Now)
                {
                    NLog.Warning("Additional questions token {token} has expired", id);
                    isRemovedOrExpired = true;
                    return false;
                }
                else
                {
                    applicationNr = token.ApplicationNr;
                    if (alsoRemoveToken)
                    {
                        token.RemovedDate = Clock.Now;
                        token.RemovedBy = CurrentUserId;
                        c.SaveChanges();
                    }
                    isRemovedOrExpired = false;
                    return true;
                }
            }
        }


        //TODO: Only used from the additional questions status block on the application. Get rid of this
        [Route("fetch")]
        [HttpPost]
        public ActionResult Fetch(string id)
        {
            List<string> errors = new List<string>();

            string applicationNr;
            bool isRemovedOrExpired;
            if (!TryFetchToken(id, out applicationNr, false, out isRemovedOrExpired))
            {
                if (isRemovedOrExpired)
                {
                    return Json2(new { isTokenRemovedOrExpired = true });
                }
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid token");
                }
            }

            var repo = DependancyInjection.Services.Resolve<IPartialCreditApplicationModelRepository>();
            var appModel = repo.Get(applicationNr, applicantFields: new List<string> { "civicRegNr", "customerId" });

            string providerName;
            bool isAdditionalLoanOffer;

            using (var context = new PreCreditContext())
            {
                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == applicationNr)
                    .Select(x => new { x.CreditCheckStatus, x.CurrentCreditDecision, x.ProviderName })
                    .Single();

                string msg;
                var tmp = AdditionalLoanSupport.HasAdditionalLoanOffer(applicationNr, app.CreditCheckStatus, app.CurrentCreditDecision, out msg);
                if (!tmp.HasValue)
                    return Json2(new { success = false, failedMessage = msg });

                providerName = app.ProviderName;
                isAdditionalLoanOffer = tmp.Value;
            }

            var e = new ExpandoObject();
            var ed = e as IDictionary<string, object>;
            dynamic model = e;

            var kycQuestions = NEnv.KycQuestions;

            var svCountries = ISO3166.GetCountryCodesAndNames("sv").ToDictionary(x => x.code, x => x.name);
            var fiCountries = ISO3166.GetCountryCodesAndNames("fi").ToDictionary(x => x.code, x => x.name);

            var affiliateModel = NEnv.GetAffiliateModel(providerName);

            model.id = id;
            model.isAdditionalLoanOffer = isAdditionalLoanOffer;
            model.providerName = providerName;
            if (affiliateModel?.HasBrandedAdditionalQuestions ?? false)
            {
                model.hasBrandedAdditionalQuestions = true;
                model.brandingTag = affiliateModel?.BrandingTag ?? providerName;
            }
            else
            {
                model.hasBrandedAdditionalQuestions = false;
            }
            model.kycQuestions = kycQuestions?.ToString();
            model.countries = svCountries.Keys.Select(x => new { key = x, sv = svCountries[x], fi = fiCountries[x] }).ToList();
            model.nrOfApplicants = appModel.NrOfApplicants;
            model.isTokenRemovedOrExpired = false;
            int customerId;
            PreCreditCustomerClient customerClient = new PreCreditCustomerClient();
            foreach (var applicantNr in Enumerable.Range(1, appModel.NrOfApplicants))
            {
                var am = appModel.Applicant(applicantNr);
                if (int.TryParse(am.Get("customerId").StringValue.Required, out customerId))
                {
                    var customerItems = customerClient.GetCustomerCardItems(customerId, "firstName", "lastName");
                    ed["applicant" + applicantNr] = new
                    {
                        firstName = customerItems["firstName"],
                        lastName = customerItems.Opt("lastName"),
                        civicRegNr = appModel.Applicant(1).Get("civicRegNr").StringValue.Required
                    };
                }
            }
            if (errors.Count > 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, string.Join(", ", errors));
            }

            return Json2(model);
        }
    }
}
