using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace nPreCredit.Controllers
{
    [NTechAuthorizeCreditMiddle]
    public class MiddleSharedUiController : SharedUiControllerBase
    {
        protected override bool IsEnabled => true;

        protected override void ExtendParameters(IDictionary<string, object> p)
        {

        }

        [Route("Ui/Document/DocumentsToSign")]
        public ActionResult DocumentsToSign()
        {
            var urlToHere = Url.ActionStrict("DocumentsToSign", "MiddleSharedUi", new { });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/Document/DocumentsToSign").ToString();

            var tagText = NEnv.IsMortgageLoansEnabled ? "Mortgage loan - " : "";
            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "manual-documents-to-sign", $"{tagText}Documents to sign", new Dictionary<string, object>
                {
                });
        }

        [Route("Ui/Campaigns")]
        public ActionResult Campaigns()
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui"))
                return HttpNotFound();

            var urlToHere = Url.ActionStrict("Campaigns", "MiddleSharedUi", new { });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/Campaigns").ToString();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "campaigns", $"Campaigns", new Dictionary<string, object>
                {
                });
        }

        [Route("Ui/Campaign")]
        public ActionResult Campaign(string campaignId)
        {
            if (!NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui"))
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(campaignId))
                return HttpNotFound();

            var urlToHere = Url.ActionStrict("Campaigns", "MiddleSharedUi", new { });
            var urlToHereFromOtherModule = NEnv.ServiceRegistry.External.ServiceUrl(
                "nPreCredit", "Ui/Campaign", Tuple.Create("campaignId", campaignId)).ToString();

            return RenderComponent(
                urlToHere, urlToHereFromOtherModule,
                "campaign", "Campaign", new Dictionary<string, object>
                {
                    { "campaignId", campaignId }
                });
        }
    }
}