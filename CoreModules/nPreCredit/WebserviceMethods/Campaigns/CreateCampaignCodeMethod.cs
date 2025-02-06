using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.Campaigns
{
    public class CreateCampaignCodeMethod : TypedWebserviceMethod<CreateCampaignCodeMethod.Request, CreateCampaignCodeMethod.Response>
    {
        public override string Path => "Campaigns/CreateCampaignCode";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var code = requestContext.Resolver().Resolve<CampaignCodeService>().CreateCampaignCode(
                request.CampaignId,
                request.Code,
                request.StartDate,
                request.EndDate,
                request.CommentText,
                request.IsGoogleCampaign);

            return new Response
            {
                Id = code.Id
            };
        }

        public class Request
        {
            [Required]
            public string CampaignId { get; set; }

            [Required]
            public string Code { get; set; }

            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string CommentText { get; set; }
            public bool IsGoogleCampaign { get; set; }
        }

        public class Response
        {
            public int Id { get; set; }
        }
    }
}