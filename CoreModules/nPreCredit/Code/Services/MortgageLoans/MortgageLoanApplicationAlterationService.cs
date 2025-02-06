using Newtonsoft.Json;
using NTech.Banking.PluginApis.CreateApplication;
using NTech.Core.PreCredit.Shared.Services;

namespace nPreCredit.Code.Services
{
    public class MortgageLoanApplicationCreationService : IMortgageLoanApplicationCreationService
    {
        private readonly IPublishEventService publishEventService;
        private readonly IMortgageLoanWorkflowService mortgageLoanWorkflowService;
        private readonly SharedCreateApplicationService sharedCreateApplicationService;

        public MortgageLoanApplicationCreationService(
            SharedCreateApplicationService sharedCreateApplicationService,
            IPublishEventService publishEventService,
            IMortgageLoanWorkflowService mortgageLoanWorkflowService)
        {
            this.publishEventService = publishEventService;
            this.mortgageLoanWorkflowService = mortgageLoanWorkflowService;
            this.sharedCreateApplicationService = sharedCreateApplicationService;
        }

        public bool TryCreateApplication(CreateApplicationRequestModel request,
            out string failedMessage,
            out string applicationNr)
        {
            var result = sharedCreateApplicationService.CreateApplication(request, CreditApplicationTypeCode.mortgageLoan, mortgageLoanWorkflowService, CreditApplicationEventCode.MortgageLoanApplicationCreated);
            applicationNr = result.ApplicationNr;
            publishEventService.Publish(PreCreditEventCode.CreditApplicationCreated, JsonConvert.SerializeObject(new { applicationNr = applicationNr, disableAutomation = request.DisableAutomation }));
            failedMessage = null;
            return true;
        }
    }

    public interface IMortgageLoanApplicationCreationService
    {
        bool TryCreateApplication(CreateApplicationRequestModel request,
            out string failedMessage,
            out string applicationNr);
    }
}