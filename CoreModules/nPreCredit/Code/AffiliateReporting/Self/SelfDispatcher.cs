using NTech.Core.PreCredit.Shared.Services;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.AffiliateReporting.Self
{
    /// <summary>
    /// Handle any callbacks on affiliates considered to be self
    /// </summary>
    public class SelfDispatcher : AffiliateCallbackDispatcherBase
    {
        private readonly IAdServiceIntegrationService adServiceIntegrationService;

        public const string DispatcherName = "self";

        public SelfDispatcher(IAdServiceIntegrationService adServiceIntegrationService)
        {
            this.adServiceIntegrationService = adServiceIntegrationService;
        }

        protected override HandleEventResult HandleLoanPaidOutEvent(LoanPaidOutEventModel evt)
        {
            if (adServiceIntegrationService.IsEnabled)
            {
                var names = adServiceIntegrationService.GetUsedExternalVariableNames();
                Dictionary<string, string> exteralVariables;
                using (var context = new PreCreditContext())
                {
                    exteralVariables = context
                        .CreditApplicationItems
                        .Where(x => x.ApplicationNr == evt.ApplicationNr && x.GroupName == "external" && names.Contains(x.Name))
                        .Select(x => new
                        {
                            x.Name,
                            x.Value
                        })
                        .ToList()
                        .ToDictionary(x => x.Name, x => x.Value);
                }
                if (exteralVariables.Any())
                {
                    this.adServiceIntegrationService.ReportConversion(exteralVariables, evt.CreditNr, "loan");
                    return new HandleEventResult
                    {
                        Status = AffiliateReportingEventResultCode.Success,
                        Message = $"Reported conversion to adservices. {string.Join(", ", exteralVariables.Select(x => $"{x.Key}={x.Value}"))}"
                    };
                }
                else
                    return new HandleEventResult
                    {
                        Status = AffiliateReportingEventResultCode.Ignored
                    };
            }
            return NotSubscribed();
        }
    }
}