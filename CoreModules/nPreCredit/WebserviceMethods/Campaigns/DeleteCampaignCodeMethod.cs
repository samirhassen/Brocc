using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.Campaigns
{
    public class DeleteCampaignCodeMethod : TypedWebserviceMethod<DeleteCampaignCodeMethod.Request, DeleteCampaignCodeMethod.Response>
    {
        public override string Path => "Campaigns/DeleteCampaignCode";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var wasChanged = requestContext.Resolver().Resolve<CampaignCodeService>().TryDeleteCampaignCode(request.CampaignCodeId.Value);

            if (!wasChanged)
                return Error("Delete  not allowed", errorCode: "deleteNotAllowed");

            return new Response
            {

            };
        }

        public class Request
        {
            [Required]
            public int? CampaignCodeId { get; set; }
        }

        public class Response
        {

        }
    }
}