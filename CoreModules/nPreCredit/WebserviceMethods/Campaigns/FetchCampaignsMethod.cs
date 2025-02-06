using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.Campaigns
{
    public class FetchCampaignsMethod : TypedWebserviceMethod<FetchCampaignsMethod.Request, FetchCampaignsMethod.Response>
    {
        public override string Path => "Campaigns/Fetch";

        public override bool IsEnabled => NEnv.ClientCfg.IsFeatureEnabled("ntech.feature.campaignui");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            using (var context = resolver.Resolve<PreCreditContextFactoryService>().CreateExtendedConcrete())
            {
                var campaignService = resolver.Resolve<CampaignCodeService>();

                var campaignsResult = campaignService.GetCampaigns(
                    pageSize: request.PageSize,
                    pageNr: request.ZeroBasedPageNr,
                    includeInactive: request.IncludeInactive ?? false,
                    includeDeleted: request.IncludeDeleted ?? false,
                    singleCampaignId: request.SingleCampaignId,
                    compositionsContext: context,
                    includeCodes: request.IncludeCodes ?? false);

                var applicationCountByCampaignId = campaignService.GetApplicationCountByCampaignId(compositionsContext: context);

                var userNameByUserId = resolver.Resolve<IUserDisplayNameService>().GetUserDisplayNamesByUserId();
                string GetUserName(int userId) => userNameByUserId.Opt(userId.ToString()) ?? $"User {userId}";

                var response = new Response
                {
                    CurrentPageNr = campaignsResult.PageNr,
                    TotalPageCount = campaignsResult.TotalPageCount,
                    Campaigns = campaignsResult.Campaigns.Select(x => new Response.CampaignModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        CreatedDate = x.CreatedDate,
                        CreatedByUserId = x.CreatedByUserId,
                        CreatedByUserDisplayName = GetUserName(x.CreatedByUserId),
                        IsActive = x.IsActive,
                        IsDeleted = x.IsDeleted,
                        AppliedToApplicationCount = applicationCountByCampaignId.ContainsKey(x.Id)
                            ? applicationCountByCampaignId[x.Id]
                            : 0
                    }).ToList()
                };

                if (campaignsResult.CampaignCodesByCampaignId != null)
                {
                    foreach (var campaign in response.Campaigns)
                    {
                        campaign.AreCodesIncluded = true;
                        campaign.Codes = campaignsResult
                            .CampaignCodesByCampaignId
                            .Opt(campaign.Id)
                            ?.Select(x => new Response.CampaignCodeModel
                            {
                                Id = x.Id,
                                Code = x.Code,
                                StartDate = x.StartDate,
                                EndDate = x.EndDate,
                                CreatedDate = x.CreatedDate,
                                CreatedByUserId = x.CreatedByUserId,
                                CreatedByUserDisplayName = GetUserName(x.CreatedByUserId),
                                CommentText = x.CommentText,
                                IsGoogleCampaign = x.IsGoogleCampaign
                            })
                            ?.ToList() ?? new List<Response.CampaignCodeModel>();
                    }
                }

                return response;
            }
        }

        public class Request
        {
            public int? PageSize { get; set; }
            public int? ZeroBasedPageNr { get; set; }
            public bool? IncludeInactive { get; set; }
            public bool? IncludeDeleted { get; set; }
            public string SingleCampaignId { get; set; }
            public bool? IncludeCodes { get; set; }
        }


        public class Response
        {
            public List<CampaignModel> Campaigns { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalPageCount { get; set; }

            public class CampaignModel
            {
                public string Id { get; set; }
                public string Name { get; set; }
                public DateTime CreatedDate { get; set; }
                public int CreatedByUserId { get; set; }
                public string CreatedByUserDisplayName { get; set; }
                public bool IsActive { get; set; }
                public bool IsDeleted { get; set; }
                public int AppliedToApplicationCount { get; set; }
                public List<CampaignCodeModel> Codes { get; set; }
                public bool AreCodesIncluded { get; set; }
            }

            public class CampaignCodeModel
            {
                public int Id { get; set; }
                public string Code { get; set; }
                public DateTime? StartDate { get; set; }
                public DateTime? EndDate { get; set; }
                public DateTime CreatedDate { get; set; }
                public int CreatedByUserId { get; set; }
                public string CreatedByUserDisplayName { get; set; }
                public string CommentText { get; set; }
                public bool IsGoogleCampaign { get; set; }
            }
        }
    }
}