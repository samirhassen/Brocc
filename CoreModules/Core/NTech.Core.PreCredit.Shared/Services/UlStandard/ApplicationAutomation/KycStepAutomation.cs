using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services.UlStandard.ApplicationAutomation
{
    public class KycStepAutomation
    {
        private readonly ICustomerClient customerClient;
        private readonly IPreCreditClient preCreditClient;
        private readonly ILoggingService loggingService;

        public KycStepAutomation(ICustomerClient customerClient, IPreCreditClient preCreditClient, ILoggingService loggingService)
        {
            this.customerClient = customerClient;
            this.preCreditClient = preCreditClient;
            this.loggingService = loggingService;
        }

        public bool TryHandleKycStepAutomation(string applicationNr, List<int> customerIds, DateTime today, out bool isApproved)
        {
            isApproved = false;
            try
            {
                if (!TryKycScreenCustomer(customerIds, today))
                {
                    return false;
                }

                isApproved = TrApproveKycStepIfChecksOK(applicationNr, customerIds);
                return isApproved;
            }
            catch (Exception ex)
            {
                if (DisableErrorSupression)
                    throw;
                loggingService.Error(ex, "Error when trying to handle KYC step automation.");
                return false;
            }
        }

        public bool TryKycScreenCustomer(List<int> customerIds, DateTime today)
        {
            try
            {
                var result = customerClient.KycScreenNew(customerIds.ToHashSetShared(), today, true);
                var failedReason = result.Where(x => customerIds.Contains(x.Key)).FirstOrDefault().Value;

                if (failedReason != null)
                {
                    loggingService.Error(failedReason, $"Error when automatically KYC screening customer.");
                    return false;
                }

                return failedReason == null;
            }

            catch (Exception ex)
            {
                if (DisableErrorSupression)
                    throw;
                loggingService.Error(ex, $"Error when automatically KYC screening customer.");
                return false;
            }
        }

        public bool TrApproveKycStepIfChecksOK(string applicationNr, List<int> customerIds)
        {
            try
            {
                var onboardingResults = customerClient.FetchCustomerOnboardingStatuses(customerIds.ToHashSetShared(), "UnsecuredLoanApplication", applicationNr, false);

                if (onboardingResults.Values.Any(x => !x.IsPep.HasValue || !x.IsSanction.HasValue))
                    return false;

                if (onboardingResults.Values.Any(x => !x.LatestScreeningDate.HasValue))
                    return false;

                if (onboardingResults.Any(x => !x.Value.HasNameAndAddress))
                    return false;

                //TODO: Migrate the approve code to core and just call it directly
                preCreditClient.LoanStandardApproveKycStep(applicationNr, isApproved: true, isAutomatic: true);

                return true;
            }
            catch (Exception ex)
            {
                if (DisableErrorSupression)
                    throw;
                loggingService.Error(ex, $"Error when automatically setting KYC step to approved.");
                return false;
            }
        }

        //Used for testing
        public static bool DisableErrorSupression { get; set; }
    }
}