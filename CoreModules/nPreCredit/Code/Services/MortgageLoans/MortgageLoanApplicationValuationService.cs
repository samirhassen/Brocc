using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationValuationService : IMortgageLoanApplicationValuationService
    {
        private readonly IServiceRegistryUrlService urlService;
        private readonly IKeyValueStoreService keyValueStoreService;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;

        public MortgageLoanApplicationValuationService(IServiceRegistryUrlService urlService, IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository, IKeyValueStoreService keyValueStoreService, IMortgageLoanWorkflowService mortgageLoanWorkflowService)
        {
            this.urlService = urlService;
            this.keyValueStoreService = keyValueStoreService;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
        }

        public const string WorkflowStepName = "MortgageLoanObjectValuation";

        public bool IsNewValuationAllowed(ApplicationInfoModel applicationInfo)
        {
            return applicationInfo.IsActive
                && mortgageLoanWorkflowService.AreAllStepsBeforeComplete(WorkflowStepName, applicationInfo.ListNames)
                && !applicationInfo.IsPartiallyApproved
                && !applicationInfo.IsFinalDecisionMade
                && !applicationInfo.IsWaitingForAdditionalInformation;
        }

        public MortgageLoanApplicationValuationStatusModel FetchStatus(ApplicationInfoModel applicationInfo, bool? autoAcceptSuggestion)
        {
            Dictionary<string, string> valuationItems = null;
            if (applicationInfo.MortgageLoanValuationStatus == "Accepted")
            {
                var model = partialCreditApplicationModelRepository.Get(applicationInfo.ApplicationNr, new PartialCreditApplicationModelRequest
                {
                    ErrorIfGetNonLoadedField = true,
                    ApplicationFields = new List<string> { ApplicationInfoService.UcbvServiceCreditApplicationItemName }
                });

                var ucbvValuationId = model.Application.Get(ApplicationInfoService.UcbvServiceCreditApplicationItemName).StringValue.Required;
                valuationItems = UcbvService.GetValuationResultFromStore(this.keyValueStoreService, ucbvValuationId);
            }
            return new MortgageLoanApplicationValuationStatusModel
            {
                IsNewMortgageApplicationValuationAllowed = IsNewValuationAllowed(applicationInfo),
                NewMortgageApplicationValuationUrl = this.urlService.LoggedInUserNavigationUrl(
                    "Ui/MortgageLoan/Valuation/NewValuation",
                    Tuple.Create("applicationNr", applicationInfo.ApplicationNr),
                    Tuple.Create("autoAcceptSuggestion", autoAcceptSuggestion?.ToString()))?.ToString(),
                ValuationItems = valuationItems
            };
        }
    }

    public interface IMortgageLoanApplicationValuationService
    {
        MortgageLoanApplicationValuationStatusModel FetchStatus(ApplicationInfoModel applicationInfo, bool? autoAcceptSuggestion);
    }

    public class MortgageLoanApplicationValuationStatusModel
    {
        public bool IsNewMortgageApplicationValuationAllowed { get; set; }
        public string NewMortgageApplicationValuationUrl { get; set; }
        public Dictionary<string, string> ValuationItems { get; set; }
    }
}