
using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code;
using nPreCredit;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace NTech.Core.PreCredit.Shared.Services.UlLegacy
{
    public class LegacyUlApplicationBasisService
    {
        private readonly TranslationService translationService;
        private readonly PreCreditContextFactory contextFactory;
        private readonly IPartialCreditApplicationModelRepositoryExtended repository;
        private readonly ApplicationInfoService applicationInfoService;

        public LegacyUlApplicationBasisService(TranslationService translationService, PreCreditContextFactory contextFactory, IPartialCreditApplicationModelRepositoryExtended repository,
            ApplicationInfoService applicationInfoService)
        {
            this.translationService = translationService;
            this.contextFactory = contextFactory;
            this.repository = repository;
            this.applicationInfoService = applicationInfoService;
        }

        private PartialCreditApplicationModel GetPartialCreditApplicationModelForCreditCheck(string applicationNr)
        {
            return PetrusOnlyCreditCheckService.GetPartialCreditApplicationModelExtendedForCreditCheck(applicationNr, repository);
        }

        public UlLegacyApplicationBasisResponse GetApplicationBasisData(UlLegacyApplicationBasisRequest request)
        {
            var application = GetPartialCreditApplicationModelForCreditCheck(request.ApplicationNr);
            var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr);

            using (var context = contextFactory.CreateContext())
            {
                return new UlLegacyApplicationBasisResponse
                {
                    ApplicationNr = request.ApplicationNr,
                    IsEditAllowed = ai.IsActive && !ai.IsFinalDecisionMade && ai.AgreementStatus == CreditApplicationMarkerStatusName.Initial,
                    Application = application.ToGroupedItems(),
                    Translations = translationService.GetTranslationTable(),
                    ChangedCreditApplicationItems = context
                        .CreditApplicationChangeLogItemsQueryable
                        .Where(x => x.ApplicationNr == request.ApplicationNr)
                        .Select(x => new { x.GroupName, x.Name })
                        .Distinct()
                        .ToList()
                        .Select(x => new UlLegacyApplicationBasisChangedItem { GroupName = x.GroupName, ItemName = x.Name })
                        .ToList()
                };
            }
        }
    }

    public class UlLegacyApplicationBasisRequest
    {
        [Required]
        public string ApplicationNr { get; set; }
    }

    public class UlLegacyApplicationBasisResponse
    {
        public string ApplicationNr { get; set; }
        public bool IsEditAllowed { get; set; }
        public Dictionary<string, Dictionary<string, string>> Translations { get; set; }        
        public GroupedItemPartialCreditApplicationModel Application { get; set; }
        public List<UlLegacyApplicationBasisChangedItem> ChangedCreditApplicationItems { get; set; }
    }

    public class UlLegacyApplicationBasisChangedItem
    {
        public string GroupName { get; set; }
        public string ItemName { get; set; }
    }
}