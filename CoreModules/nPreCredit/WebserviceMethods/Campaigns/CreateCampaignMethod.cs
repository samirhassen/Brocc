using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.Campaigns
{
    public class CreateCampaignMethod : TypedWebserviceMethod<CreateCampaignMethod.Request, CreateCampaignMethod.Response>
    {
        public override string Path => "Campaigns/Create";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var result = requestContext.Resolver().Resolve<CampaignCodeService>().CreateCampaign(
                request.Name, id: request.Id);

            return new Response
            {
                CampaignId = result.Id
            };
        }

        public class Request
        {
            public string Id { get; set; }

            [Required]
            public string Name { get; set; }
        }

        public class Response
        {
            public string CampaignId { get; set; }
        }
    }
}