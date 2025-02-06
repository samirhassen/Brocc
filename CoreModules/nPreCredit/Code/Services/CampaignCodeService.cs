using nPreCredit.DbModel;
using NTech;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CampaignCodeService : ICampaignCodeService
    {
        private readonly IClock clock;
        private readonly PreCreditContextFactoryService contextFactoryService;
        private readonly INTechCurrentUserMetadata currentUser;

        public CampaignCodeService(IClock clock, PreCreditContextFactoryService contextFactoryService, INTechCurrentUserMetadata currentUser)
        {
            this.clock = clock;
            this.contextFactoryService = contextFactoryService;
            this.currentUser = currentUser;
        }

        public List<CreateApplicationRequestModel.ComplexItem> MatchCampaignOnCreateApplication(List<CreateApplicationRequestModel.ComplexItem> currentItems)
        {
            using (var context = contextFactoryService.Create())
            {
                return MatchCampaignOnCreateApplicationComposable(currentItems, context);
            }
        }

        public List<CreateApplicationRequestModel.ComplexItem> MatchCampaignOnCreateApplicationComposable(List<CreateApplicationRequestModel.ComplexItem> currentItems, IPreCreditContext context)
        {
            var campaignParameters = currentItems
                .FirstOrDefault(x => x.ListName == ParameterComplexListName && x.Nr == 1)
                ?.UniqueValues;

            var matchedCodes = FindMatchedCampaignCodes(campaignParameters, context.CampaignCodes, clock);
            if (!matchedCodes.Any())
                return new List<CreateApplicationRequestModel.ComplexItem>();

            var matchedCampaignsIds = context
                .Campaigns
                .Where(x => matchedCodes.Select(y => y.CampaignId).Contains(x.Id) && x.IsActive && !x.IsDeleted)
                .Select(x => x.Id)
                .ToList();

            return matchedCampaignsIds.Select(x => new CreateApplicationRequestModel.ComplexItem
            {
                ListName = AppliedCampaignListName,
                Nr = 1,
                RepeatingValues = new Dictionary<string, List<string>>
                {
                    { "CampaignId", matchedCampaignsIds }
                }
            }).ToList();
        }

        public static IQueryable<CampaignCode> FindMatchedCampaignCodes(
            Dictionary<string, string> campaignParameters,
            IQueryable<CampaignCode> campaignCodes,
            IClock clock)
        {
            var matchedCampaignCodes = campaignCodes.Where(x => 1 == 0);

            var googleCampaign = campaignParameters?.Opt(ParameterNameGoogleCampaign);
            if (!string.IsNullOrWhiteSpace(googleCampaign))
            {
                matchedCampaignCodes =
                    matchedCampaignCodes.Union(campaignCodes.Where(x =>
                        x.IsGoogleCampaign && x.Code == googleCampaign));
            }

            var campaignCode = campaignParameters?.Opt(ParameterNameCode);
            if (!string.IsNullOrWhiteSpace(campaignCode))
            {
                matchedCampaignCodes =
                    matchedCampaignCodes.Union(campaignCodes.Where(x =>
                        !x.IsGoogleCampaign && x.Code == campaignCode));
            }

            var today = clock.Today;

            return matchedCampaignCodes.Where(x =>
                !x.DeletedByUserId.HasValue && (!x.StartDate.HasValue || x.StartDate <= today) &&
                (!x.EndDate.HasValue || x.EndDate >= today));
        }

        public Campaign CreateCampaign(string name, string id = null, IPreCreditContext compositionContext = null)
        {
            return WithCompositionContext(context =>
            {
                id = string.IsNullOrWhiteSpace(id)
                    ? OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(12)
                    : id.Trim();

                if (context.Campaigns.Any(x => x.Id == id)) throw new NTechWebserviceMethodException($"Campaign '{id}' already exists") { ErrorCode = "campaignIdAlreadyExists", ErrorHttpStatusCode = 400, IsUserFacing = true };

                var campaign = new Campaign
                {
                    Id = id,
                    Name = name,
                    CreatedDate = clock.Now.DateTime,
                    CreatedByUserId = currentUser.UserId,
                    IsActive = true
                };

                context.Campaigns.Add(campaign);

                return campaign;
            }, true, compositionContext);
        }

        public Dictionary<string, int> GetApplicationCountByCampaignId(IPreCreditContext compositionsContext = null, string singleCampaignId = null)
        {
            return WithCompositionContext(context =>
            {
                var query = context
                    .ComplexApplicationListItems
                    .Where(x => x.ListName == AppliedCampaignListName && x.Nr == 1 && x.ItemName == "CampaignId");

                if (!string.IsNullOrWhiteSpace(singleCampaignId))
                    query = query.Where(x => x.ItemValue == singleCampaignId);

                return query
                    .Select(x => new { x.ApplicationNr, x.ItemValue })
                    .Distinct()
                    .GroupBy(x => x.ItemValue)
                    .ToDictionary(x => x.Key, x => x.Count());

            }, false, compositionsContext);
        }

        public List<CampaignCode> GetCampaignCodes(string campaignId, IPreCreditContext compositionsContext = null)
        {
            return WithCompositionContext(context =>
            {
                return context
                    .Campaigns
                    .Where(x => x.Id == campaignId)
                    .SelectMany(x => x.CampaignCodes)
                    .ToList();
            }, false, compositionsContext);
        }

        public bool TryDeleteOrInactivateCampaign(string campaignId, bool isDelete, IPreCreditContext compositionsContext = null)
        {
            return WithCompositionContext(context =>
            {
                var campaign = context.Campaigns.SingleOrDefault(x => x.Id == campaignId);
                if (campaign == null)
                    return false;

                if (isDelete)
                {
                    var countById = GetApplicationCountByCampaignId(compositionsContext: compositionsContext, singleCampaignId: campaignId);
                    if (countById.ContainsKey(campaignId) && countById[campaignId] > 0)
                        return false; //Campaigns that are applied to applications cannot be deleted

                    campaign.IsDeleted = true;
                }
                else
                {
                    campaign.IsActive = false;
                }

                campaign.InactivatedOrDeletedByUserId = currentUser.UserId;
                campaign.InactivatedOrDeletedDate = clock.Now.DateTime;

                return true;
            }, true, compositionsContext);
        }

        public bool TryDeleteCampaignCode(int campaignCodeId, IPreCreditContext compositionsContext = null)
        {
            return WithCompositionContext(context =>
            {
                var code = context.CampaignCodes.SingleOrDefault(x => x.Id == campaignCodeId);

                if (code == null || (code.DeletedByUserId.HasValue && code.DelatedDate.HasValue))
                    return false;

                code.DeletedByUserId = currentUser.UserId;
                code.DelatedDate = clock.Now.DateTime;

                return true;
            }, true, compositionsContext);
        }

        public (List<Campaign> Campaigns, int TotalPageCount, int PageNr, Dictionary<string, List<CampaignCode>> CampaignCodesByCampaignId) GetCampaigns(int? pageSize = null, int? pageNr = null, bool includeInactive = false, bool includeDeleted = false, string singleCampaignId = null, IPreCreditContext compositionsContext = null, bool includeCodes = false)
        {
            return WithCompositionContext(context =>
            {
                var filteredData = context.Campaigns.AsQueryable();

                if (!includeDeleted)
                    filteredData = filteredData.Where(x => !x.IsDeleted);

                if (!includeInactive)
                    filteredData = filteredData.Where(x => x.IsActive);

                if (!string.IsNullOrWhiteSpace(singleCampaignId))
                    filteredData = filteredData.Where(x => x.Id == singleCampaignId);

                var totalCount = filteredData.Count();
                var actualPageSize = pageSize ?? totalCount;
                var actualPageNr = pageNr ?? 0;
                var totalPageCount = actualPageSize == 0 ? 0 : (totalCount / actualPageSize) + (totalCount % actualPageSize == 0 ? 0 : 1);

                var orderedData = filteredData.OrderByDescending(x => x.Id);

                var page = orderedData.Skip(actualPageSize * actualPageNr).Take(actualPageSize).ToList();

                Dictionary<string, List<CampaignCode>> campaignCodesByCampaignId = null;

                if (includeCodes)
                {
                    var campaignIds = page.Select(x => x.Id).ToList();
                    campaignCodesByCampaignId = context
                        .CampaignCodes
                        .Where(x => campaignIds.Contains(x.CampaignId) && x.DeletedByUserId == null)
                        .GroupBy(x => x.CampaignId)
                        .ToDictionary(x => x.Key, x => x.ToList());
                }

                return (page, totalPageCount, actualPageNr, campaignCodesByCampaignId);
            }, false, compositionsContext);
        }

        public CampaignCode CreateCampaignCode(
            string campaignId, string code, DateTime? startDate,
            DateTime? endDate, string commentText, bool isGoogleCampaign, IPreCreditContext compositionsContext = null)
        {
            return WithCompositionContext(context =>
            {
                var campaign = context.Campaigns.Include("CampaignCodes").SingleOrDefault(x => x.Id == campaignId);
                if (campaign == null)
                    throw new NTechWebserviceMethodException("No such campaign exists")
                    {
                        ErrorCode = "noSuchCampaignExists",
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };
                if (campaign.CampaignCodes.Any(x => !x.DeletedByUserId.HasValue && x.Code == code))
                    throw new NTechWebserviceMethodException("Code already exists on campaign")
                    {
                        ErrorCode = "duplicateCodeOnCampaign",
                        ErrorHttpStatusCode = 400,
                        IsUserFacing = true
                    };

                var campaignCode = new CampaignCode
                {
                    Campaign = campaign,
                    Code = code,
                    StartDate = startDate,
                    EndDate = endDate,
                    CreatedDate = clock.Now.DateTime,
                    CreatedByUserId = currentUser.UserId,
                    CommentText = commentText,
                    IsGoogleCampaign = isGoogleCampaign
                };
                context.CampaignCodes.Add(campaignCode);

                return campaignCode;
            }, true, compositionsContext);
        }

        private T WithCompositionContext<T>(Func<IPreCreditContext, T> f, bool saveChangesIfNotComposed, IPreCreditContext compositionsContext)
        {
            if (compositionsContext != null)
                return f(compositionsContext);

            using (var context = contextFactoryService.Create())
            {
                var result = f(context);

                if (saveChangesIfNotComposed)
                {
                    context.SaveChanges();
                }

                return result;
            }
        }

        private const string ParameterNameCode = "campaignCode";
        private const string ParameterNameGoogleCampaign = "utm_campaign";
        public const string ParameterComplexListName = "CampaignParameters";
        private const string AppliedCampaignListName = "AppliedCampaign";
    }
}