using nPreCredit.Code.Services;
using NTech.Core;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.PreCredit.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit
{
    public class CreditManagementWorkListService
    {
        private ICoreClock clock;
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly IPreCreditEnvSettings envSettings;
        private readonly ICustomerClient customerClient;

        public CreditManagementWorkListService(ICoreClock clock, IPreCreditContextFactoryService contextFactoryService, IPreCreditEnvSettings envSettings,
            ICustomerClient customerClient)
        {
            this.clock = clock;
            this.contextFactoryService = contextFactoryService;
            this.envSettings = envSettings;
            this.customerClient = customerClient;
        }

        public enum CreditApplicationCategoryCode
        {
            PendingCreditCheck,
            PendingCustomerCheck,
            PendingDocumentCheck,
            PendingFraudCheck,
            PendingFinalDecision,
            WaitingForData, //Answer to additional questions
            WaitingForSignature,
            WaitingForDocument,
            WaitingForAdditionalInformation
        }

        public List<string> GetAllCategoryCodes()
        {
            return Enum.GetValues(typeof(CreditApplicationCategoryCode)).Cast<CreditApplicationCategoryCode>().Select(x => x.ToString()).ToList();
        }

        public class CreditManagementFilter
        {
            public string CategoryCode { get; set; }
            public string ProviderName { get; set; }
        }

        public FilteredCreditManagementResult GetOmniSearchPage(string omniSearchValue, Func<string, string> createUrlFromApplicationNr, bool? includeCategoryCodes = false)
        {
            Func<List<string>, IPreCreditContextExtended, FilteredCreditManagementResult> fromApplicationNrs = (applicationNrs, context) =>
            {
                var itemsPre1 = GetSearchModel(context, true)
                    .Where(x => applicationNrs.Contains(x.ApplicationNr))
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr);
                var itemsPre2 = includeCategoryCodes.GetValueOrDefault()
                    ? itemsPre1.Select(x => new FilterPageTempItem
                    {
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        ApplicationNr = x.ApplicationNr,
                        ApplicationAmount = x.ApplicationAmount,
                        CategoryCodes = x.CategoryCodes,
                        ArchivedDate = x.ArchivedDate
                    })
                    : itemsPre1.Select(x => new FilterPageTempItem
                    {
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        ApplicationNr = x.ApplicationNr,
                        ApplicationAmount = x.ApplicationAmount,
                        ArchivedDate = x.ArchivedDate
                    });
                var items = itemsPre2
                    .ToList()
                    .Select(x => new FilteredCreditManagementResult.Hit
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        NavigationUrl = createUrlFromApplicationNr(x.ApplicationNr),
                        ApplicationAmount = StringItem.ParseDecimal(x.ApplicationAmount),
                        CategoryCodes = x.CategoryCodes?.ToList(),
                        ArchivedDate = x.ArchivedDate
                    })
                    .ToList();

                return new FilteredCreditManagementResult
                {
                    CurrentPageNr = 0,
                    TotalNrOfPages = items.Count == 0 ? 0 : 1,
                    Page = items
                };
            };

            Func<List<string>, FilteredCreditManagementResult> fromCustomerIds = (customerIds) =>
            {
                using (var c = contextFactoryService.CreateExtended())
                {
                    List<string> applicationNrs = c.CreditApplicationItemsQueryable
                        .Where(x => x.Name == "customerId" && customerIds.Contains(x.Value))
                        .Select(x => x.ApplicationNr)
                        .ToList();

                    return fromApplicationNrs(applicationNrs, c);
                }
            };

            //Exact match for application nr first
            using (var c = contextFactoryService.CreateExtended())
            {
                var applicationNrSearchHits = fromApplicationNrs(new List<string> { omniSearchValue }, c);
                if (applicationNrSearchHits.Page.Count > 0)
                    return applicationNrSearchHits;
            }

            //Fall back to customer data otherwise
            var omniSearchCustomerIds = customerClient.FindCustomerIdsOmni(omniSearchValue);
            return fromCustomerIds(omniSearchCustomerIds.Select(x => x.ToString()).ToList());
        }

        private class FilterPageTempItem
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public string LatestSystemCommentText { get; set; }
            public string ProviderName { get; set; }
            public bool IsActive { get; set; }
            public string ApplicationAmount { get; set; }
            public IEnumerable<string> CategoryCodes { get; set; }
            public DateTimeOffset? ArchivedDate { get; set; }
        }

        public FilteredCreditManagementResult GetFilteredPage(
            CreditManagementFilter filter,
            int pageSize,
            int pageNr,
            Func<string, string> createUrlFromApplicationNr,
            bool includeCategoryCodes = false)
        {
            filter = filter ?? new CreditManagementFilter();

            Func<string, bool?> parseBool = s => string.IsNullOrWhiteSpace(s) ? new bool?() : s.ToLowerInvariant() == "true";
            Func<string, string> n = s => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

            using (var context = contextFactoryService.CreateExtended())
            {
                var baseResult = GetSearchModel(context, false).Where(x => x.IsActive);

                if (!string.IsNullOrWhiteSpace(filter.ProviderName))
                {
                    baseResult = baseResult.Where(x => x.ProviderName == filter.ProviderName);
                }

                //Count categories
                var countsByCode = baseResult
                        .SelectMany(x => x.CategoryCodes.Select(y => new { x.ApplicationNr, CategoryCode = y }))
                        .GroupBy(x => x.CategoryCode)
                        .Select(x => new { x.Key, Count = x.Count() })
                        .ToList()
                        .Where(x => x?.Key != null)
                        .ToDictionary(x => x.Key, y => y.Count);

                var d = new Dictionary<string, int>();
                foreach (var c in Enum.GetValues(typeof(CreditApplicationCategoryCode)).Cast<CreditApplicationCategoryCode>())
                {
                    var cs = c.ToString();
                    d[cs] = countsByCode.ContainsKey(cs) ? countsByCode[cs] : 0;
                }

                if (!string.IsNullOrWhiteSpace(filter.CategoryCode))
                {
                    if (filter.CategoryCode == CreditApplicationCategoryCode.PendingCreditCheck.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingCreditCheck && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.PendingCustomerCheck.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingCustomerCheck && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.WaitingForData.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingOrWaitingForAdditionalQuestions && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.WaitingForSignature.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingOrWaitingForSignature && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.PendingFraudCheck.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingFraudCheck && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.PendingFinalDecision.ToString())
                    {
                        baseResult = baseResult.Where(x => x.IsPendingFinalDecision && !x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else if (filter.CategoryCode == CreditApplicationCategoryCode.WaitingForAdditionalInformation.ToString())
                    {
                        baseResult = baseResult.Where(x => x.WaitingForAdditionalInformationDate.HasValue);
                    }
                    else
                        baseResult = baseResult.Where(x => x.CategoryCodes.Contains(filter.CategoryCode));
                }

                var totalCount = baseResult.Count();

                var currentPagePre = baseResult
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Skip(pageSize * pageNr)
                    .Take(pageSize);

                var currentPagePre2 = includeCategoryCodes
                    ? currentPagePre.Select(x => new FilterPageTempItem
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        ApplicationAmount = x.ApplicationAmount,
                        CategoryCodes = x.CategoryCodes
                    })
                    : currentPagePre.Select(x => new FilterPageTempItem
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        IsActive = x.IsActive,
                        ApplicationAmount = x.ApplicationAmount
                    });

                var currentPage = currentPagePre2
                    .ToList()
                    .Select(x => new FilteredCreditManagementResult.Hit
                    {
                        ApplicationNr = x.ApplicationNr,
                        ApplicationDate = x.ApplicationDate,
                        LatestSystemCommentText = x.LatestSystemCommentText,
                        ProviderName = x.ProviderName,
                        NavigationUrl = createUrlFromApplicationNr(x.ApplicationNr),
                        IsActive = x.IsActive,
                        ApplicationAmount = StringItem.ParseDecimal(x.ApplicationAmount),
                        CategoryCodes = x.CategoryCodes?.ToList()
                    })
                    .ToList();
                var nrOfPages = (totalCount / pageSize) + (totalCount % pageSize == 0 ? 0 : 1);

                return new FilteredCreditManagementResult
                {
                    CategoryCounts = d,
                    CurrentPageNr = pageNr,
                    TotalNrOfPages = nrOfPages,
                    Page = currentPage
                };
            }
        }

        public List<ProviderItem> GetAllProviders()
        {
            using (var context = contextFactoryService.CreateExtended())
            {
                return context
                    .CreditApplicationHeadersQueryable
                    .Select(creditHeader => creditHeader.ProviderName)
                    .Distinct()
                    .ToList()
                    .OrderBy(x => x)
                    .Select(providerName => new ProviderItem
                    {
                        ProviderName = providerName,
                        DisplayName = providerName,
                        DisplayToEnduserName = (envSettings.GetAffiliateModel(providerName, allowMissing: true)?.DisplayToEnduserName ?? providerName)
                    })
                    .ToList();
            }
        }

        public static string PickCancelleationCategoryCode(IEnumerable<string> categoryCodes)
        {
            if (categoryCodes == null)
                return "Unknown";

            string primaryCategoryCode;
            if (categoryCodes.Contains(CreditApplicationCategoryCode.WaitingForData.ToString()))
                primaryCategoryCode = CreditApplicationCategoryCode.WaitingForData.ToString();
            else if (categoryCodes.Contains(CreditApplicationCategoryCode.WaitingForSignature.ToString()))
                primaryCategoryCode = CreditApplicationCategoryCode.WaitingForSignature.ToString();
            else
                primaryCategoryCode = categoryCodes.FirstOrDefault() ?? "Unknown";

            return primaryCategoryCode;
        }

        public class ProviderItem
        {
            public string ProviderName { get; set; }
            public string DisplayName { get; set; }
            public string DisplayToEnduserName { get; set; }
        }

        public class FilteredCreditManagementResult
        {
            public class Hit
            {
                public string ApplicationNr { get; set; }
                public string ProviderName { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public string LatestSystemCommentText { get; set; }
                public string NavigationUrl { get; set; }
                public bool IsActive { get; set; }
                public decimal? ApplicationAmount { get; set; }
                public List<string> CategoryCodes { get; set; } //BEWARE: Only loaded when requested ahead of time
                public DateTimeOffset? ArchivedDate { get; set; }
            }

            public List<Hit> Page { get; set; }
            public Dictionary<string, int> CategoryCounts { get; set; }
            public int CurrentPageNr { get; set; }
            public int TotalNrOfPages { get; set; }
        }

        public class Application2Model
        {
            public DateTimeOffset? WaitingForAdditionalInformationDate { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
            public string ApplicationNr { get; set; }
            public bool HasAnsweredAdditionalQuestions { get; set; }
            public bool IsPendingExternalAdditionalQuestions { get; set; }
            public bool IsActive { get; set; }
            public bool IsPendingCreditCheck { get; set; }
            public bool IsPendingCustomerCheck { get; internal set; }
            public bool IsPendingFinalDecision { get; set; }
            public bool IsPendingFraudCheck { get; set; }
            public string LatestSystemCommentText { get; set; }
            public DateTimeOffset? LatestSystemCommentDate { get; set; }
            public string ProviderName { get; set; }
            public IEnumerable<string> CategoryCodes { get; set; }
            public string ApplicationAmount { get; set; }
            public bool IsPendingOrWaitingForAdditionalQuestions { get; set; }
            public bool IsPendingOrWaitingForSignature { get; set; }
            public DateTimeOffset? ArchivedDate { get; set; }
        }

        public void GetCurrentApplicationState(IPreCreditContextExtended context, string applicationNr, out bool canAnswerAdditionalQuestions, out bool canSignAgreement, out bool isActive)
        {
            var model = GetSearchModel(context, true).SingleOrDefault(x => x.ApplicationNr == applicationNr);

            isActive = false;
            canAnswerAdditionalQuestions = false;
            canSignAgreement = false;

            if (model == null || !model.IsActive)
                return;

            if (model.IsPendingOrWaitingForAdditionalQuestions)
            {
                canAnswerAdditionalQuestions = true;
                isActive = true;
            }
            else if (model.IsPendingOrWaitingForSignature)
            {
                canSignAgreement = true;
                isActive = true;
            }
            else
            {
                isActive = model.IsActive;
            }
        }

        public IQueryable<Application2Model> GetSearchModel(IPreCreditContextExtended context, bool forceShowUserHiddenItems)
        {
            var isMortageLoanEnabled = envSettings.IsMortgageLoansEnabled;
            var baseQuery = context.CreditApplicationHeadersQueryable.Where(x => !x.IsFinalDecisionMade && x.MortgageLoanExtension == null);
            if (!forceShowUserHiddenItems)
            {
                baseQuery = baseQuery
                    .Where(x => (!x.HideFromManualListsUntilDate.HasValue || x.HideFromManualListsUntilDate < clock.Now));
            }

            return baseQuery
                .Select(x => new
                {
                    App = x,
                    IsPendingExternalAdditionalQuestions = x.Items.Any(y => y.GroupName == "application" && y.Name == "isPendingExternalKycQuestions" && y.Value == "true")
                })
                .Select(x => new
                {
                    App = x.App,
                    HasAnsweredAdditionalQuestions = x.App.Items.Any(y => y.AddedInStepName == "AdditionalQuestions" && !x.IsPendingExternalAdditionalQuestions),
                    x.IsPendingExternalAdditionalQuestions
                })
                .Select(x => new
                {
                    App = x.App,
                    DocumentCheckStatus = x.App.Items.Where(y => y.Name == "documentCheckStatus").Select(y => y.Value).FirstOrDefault() ?? "Initial",
                    IsPendingCustomerCheck = isMortageLoanEnabled
                                                ? x.App.CustomerCheckStatus == "Rejected"
                                                : ((x.App.CustomerCheckStatus == "Rejected" || (x.App.CustomerCheckStatus != "Accepted" && x.App.AgreementStatus == "Accepted"))
                                                    && x.App.CreditCheckStatus == "Accepted"
                                                    && x.HasAnsweredAdditionalQuestions),
                    x.HasAnsweredAdditionalQuestions,
                    IsApprovePossible = x.App.IsActive && x.App.CreditCheckStatus == "Accepted" && x.App.FraudCheckStatus == "Accepted" && x.App.CustomerCheckStatus == "Accepted" && x.App.AgreementStatus == "Accepted",
                    IsRejectPossible = x.App.IsActive && x.App.CreditCheckStatus == "Rejected",
                    x.IsPendingExternalAdditionalQuestions
                })
                .Select(x => new
                {
                    ApplicationNr = x.App.ApplicationNr,
                    IsActive = x.App.IsActive,
                    ApplicationDate = x.App.ApplicationDate,
                    ProviderName = x.App.ProviderName,
                    HasAnsweredAdditionalQuestions = x.HasAnsweredAdditionalQuestions,
                    IsPendingCustomerCheck = x.IsPendingCustomerCheck,
                    IsPendingCreditCheck = x.App.CreditCheckStatus == "Initial",
                    IsPendingOrWaitingForAdditionalQuestions = x.App.CreditCheckStatus == "Accepted" && !(x.HasAnsweredAdditionalQuestions || x.App.CanSkipAdditionalQuestions),
                    IsPendingOrWaitingForSignature = x.App.CreditCheckStatus == "Accepted" && x.App.AgreementStatus != "Accepted" && (x.HasAnsweredAdditionalQuestions || x.App.CanSkipAdditionalQuestions),
                    IsPendingFraudCheck = x.App.CreditCheckStatus == "Accepted" && x.App.AgreementStatus == "Accepted" && (x.HasAnsweredAdditionalQuestions || x.App.CanSkipAdditionalQuestions) && x.App.FraudCheckStatus == "Initial" && x.App.CustomerCheckStatus != "Rejected" && x.DocumentCheckStatus == "Accepted",
                    IsPendingFinalDecision = !x.App.IsPartiallyApproved && x.App.IsActive && (x.IsApprovePossible || x.IsRejectPossible),
                    NrOfApplicantsWithAttachedDocumentCheckDocuments = x.App.Documents.Where(y => !y.RemovedByUserId.HasValue && y.ApplicantNr.HasValue && y.DocumentType == CreditApplicationDocumentTypeCode.DocumentCheck.ToString()).Select(y => y.ApplicantNr).Distinct().Count(),
                    AgreementStatus = x.App.AgreementStatus,
                    IsDocumentCheckComplete = (x.DocumentCheckStatus == "Accepted" || x.DocumentCheckStatus == "Rejected"),
                    NrOfApplicants = x.App.NrOfApplicants,
                    LatestSystemComment = x.App.Comments.Where(y => y.EventType != "UserComment").OrderByDescending(y => y.Id).FirstOrDefault(),
                    ApplicationAmount = x.App.Items.Where(y => y.GroupName == "application" && y.Name == "amount").Select(y => y.Value).FirstOrDefault(),
                    WaitingForAdditionalInformationDate = x.App.WaitingForAdditionalInformationDate,
                    ArchivedDate = x.App.ArchivedDate,
                    x.IsPendingExternalAdditionalQuestions
                })
                .Select(x => new Application2Model
                {
                    ApplicationNr = x.ApplicationNr,
                    IsActive = x.IsActive,
                    ApplicationDate = x.ApplicationDate,
                    ProviderName = x.ProviderName,
                    HasAnsweredAdditionalQuestions = x.HasAnsweredAdditionalQuestions,
                    WaitingForAdditionalInformationDate = x.WaitingForAdditionalInformationDate,
                    IsPendingCustomerCheck = x.IsPendingCustomerCheck,
                    IsPendingCreditCheck = x.IsPendingCreditCheck,
                    IsPendingFraudCheck = x.IsPendingFraudCheck,
                    IsPendingFinalDecision = x.IsPendingFinalDecision,
                    LatestSystemCommentText = x.LatestSystemComment.CommentText,
                    LatestSystemCommentDate = x.LatestSystemComment.CommentDate,
                    IsPendingOrWaitingForAdditionalQuestions = x.IsPendingOrWaitingForAdditionalQuestions,
                    IsPendingOrWaitingForSignature = x.IsPendingOrWaitingForSignature,
                    CategoryCodes =
                        new[]
                        {
                            x.IsPendingCreditCheck && !x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.PendingCreditCheck.ToString() : null,
                            x.IsPendingCustomerCheck && !x.WaitingForAdditionalInformationDate.HasValue ?  CreditApplicationCategoryCode.PendingCustomerCheck.ToString() : null,
                            x.IsPendingOrWaitingForAdditionalQuestions && !x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.WaitingForData.ToString() : null,
                            x.IsPendingOrWaitingForSignature && !x.WaitingForAdditionalInformationDate.HasValue ?  CreditApplicationCategoryCode.WaitingForSignature.ToString() : null,
                            x.IsPendingFraudCheck && !x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.PendingFraudCheck.ToString() : null,
                            x.IsPendingFinalDecision && !x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.PendingFinalDecision.ToString() : null,
                            x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.WaitingForAdditionalInformation.ToString() : null,

                            //Waiting for document
                            //Start: alla aktiva ansökningar där agreement är signed på alla sökande.
                            //Slut: slutkriterium när minst ett dokument är bifogat på samtliga sökande.
                            x.IsActive && x.AgreementStatus == "Accepted" && x.NrOfApplicantsWithAttachedDocumentCheckDocuments < x.NrOfApplicants && !x.WaitingForAdditionalInformationDate.HasValue  ? CreditApplicationCategoryCode.WaitingForDocument.ToString() : null,

                            //Pending document check
                            //alla aktiva ansökningar där minst ett dokument är bifogat på samtliga sökanden.
                            //slutkriterium när dokumentkontrollen är genomförd.
                            x.IsActive &&  x.AgreementStatus == "Accepted" && x.NrOfApplicantsWithAttachedDocumentCheckDocuments >= x.NrOfApplicants && !x.IsDocumentCheckComplete && !x.WaitingForAdditionalInformationDate.HasValue ? CreditApplicationCategoryCode.PendingDocumentCheck.ToString() : null
                        },
                    ApplicationAmount = x.ApplicationAmount,
                    ArchivedDate = x.ArchivedDate,
                    IsPendingExternalAdditionalQuestions = x.IsPendingExternalAdditionalQuestions
                });
        }
    }
}