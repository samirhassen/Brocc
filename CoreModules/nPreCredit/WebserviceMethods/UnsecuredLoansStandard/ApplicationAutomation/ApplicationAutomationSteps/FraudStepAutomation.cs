using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using Serilog;
using System;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation
{
    public class FraudStepAutomation : EventSubscriberBase
    {
        private readonly nPreCreditClient _client;
        private readonly ApplicationInfoModel _applicationInfo;
        private readonly UnsecuredLoanStandardWorkflowService _wfService;

        public FraudStepAutomation(ApplicationInfoModel applicationInfo, UnsecuredLoanStandardWorkflowService wfService)
        {
            _client = new nPreCreditClient(AquireBearerToken);
            _applicationInfo = applicationInfo;
            _wfService = wfService;
        }

        internal bool TryHandleFraudStepAutomation(string applicationNr, out bool isApproved)
        {
            isApproved = false;
            try
            {
                if (!TryRunApproveFraudControls(applicationNr))
                {
                    return false;
                }

                isApproved = TryApproveFraudStep(applicationNr);
                return isApproved;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error when trying to handle application fraud step automation.");
                return false;
            }
        }

        internal bool TryRunApproveFraudControls(string applicationNr)
        {
            try
            {
                var results = _client.RunFraudControls(applicationNr);
                var resultsWithoutMatches = results.FraudControls.Where(x => x.HasMatch != true);

                foreach (var item in resultsWithoutMatches)
                {
                    _client.SetFraudControlItemApproved(item.CheckName, applicationNr);
                }

                if (results.FraudControls.Where(y => y.HasMatch == true).Any())
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error when automatically running application fraud controls.");
                return false;
            }
        }

        internal bool TryApproveFraudStep(string applicationNr)
        {
            try
            {
                var isStepStatusInitial = _wfService.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.FraudStep.Name, _applicationInfo.ListNames);
                var isAllStepsBeforeComplete = _wfService.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.FraudStep.Name, _applicationInfo.ListNames);

                var isDecisionStepCurrent = isStepStatusInitial && isAllStepsBeforeComplete;

                if (!isDecisionStepCurrent)
                    return false;

                _client.UnsecuredLoanStandardApproveFraudStep(applicationNr, isApproved: true, isAutomatic: true);

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error when automatically approving fraud step.");
                return false;
            }
        }

        internal bool TryApproveFraudStepAfterKycCheck(string applicationNr)
        {
            try
            {
                var fraudControls = _client.GetFraudControlItemApproved(applicationNr);

                if (fraudControls != null)
                {
                    var unapprovedFraudControls = fraudControls.FraudControls.Where(x => x.IsApproved != true);

                    if (!unapprovedFraudControls.Any())
                    {
                        _client.UnsecuredLoanStandardApproveFraudStep(applicationNr, isApproved: true, isAutomatic: true);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, $"Error when automatically approving fraud step.");
                return false;
            }
        }
    }
}