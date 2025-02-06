using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.Campaigns
{
    public class DeleteOrInactivateCampaignMethod : TypedWebserviceMethod<DeleteOrInactivateCampaignMethod.Request, DeleteOrInactivateCampaignMethod.Response>
    {
        public override string Path => "Campaigns/DeleteOrInactivate";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.IsDelete.GetValueOrDefault() == request.IsInactivate.GetValueOrDefault())
                return Error("Exactly one of IsDelete or IsInactivate must be true");

            var wasChanged = requestContext.Resolver().Resolve<CampaignCodeService>().TryDeleteOrInactivateCampaign(
                request.CampaignId, request.IsDelete.GetValueOrDefault());

            if (!wasChanged)
                return Error("Delete or inactivate not allowed", errorCode: "deleteOrInactivateNotAllowed");

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public string CampaignId { get; set; }

            public bool? IsDelete { get; set; }
            public bool? IsInactivate { get; set; }
        }

        public class Response
        {

        }
    }
}