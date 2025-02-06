using nPreCredit.Code.Services;
using Serilog;
using System;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation
{
    public class CreditDecisionStepAutomation
    {
        private readonly PreCreditContextExtended _context;
        private readonly UnsecuredLoanStandardWorkflowService _workflowService;
        private readonly IApplicationCancellationService _applicationCancellationService;

        public CreditDecisionStepAutomation(UnsecuredLoanStandardWorkflowService workflowService, IApplicationCancellationService applicationCancellationService, PreCreditContextExtended context)
        {
            _context = context;
            _applicationCancellationService = applicationCancellationService;
            _workflowService = workflowService;
        }

        internal bool TryAutomaticallyApproveOnCustomerAcceptsOffer(string applicationNr)
        {
            try
            {
                _workflowService.ChangeStepStatusComposable(_context, UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, _workflowService.AcceptedStatusName, applicationNr);
                _context.CreateAndAddComment("Waiting for customer to accept offer approved automatically", "CustomerOfferDecisionStepApproved", applicationNr);

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error when trying to automatically set customer credit offer to accepted.");
                return false;
            }
        }

        internal bool TryAutomaticallyCancelOnCustomerRejectsOffer(string applicationNr)
        {
            try
            {
                _applicationCancellationService.TryCancelApplication(applicationNr, out string failedMessage, isAutomatic: true);
                if (!string.IsNullOrEmpty(failedMessage))
                {
                    NLog.Error($"Try cancel application failed message: '{failedMessage}'");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error when trying to automatically cancel application.");
                return false;
            }
        }
    }
}