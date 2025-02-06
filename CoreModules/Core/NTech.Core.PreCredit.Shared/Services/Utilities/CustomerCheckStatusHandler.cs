using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class CustomerCheckStatusHandler
    {
        private ICustomerClient customerClient;
        private IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository;

        public CustomerCheckStatusHandler(
            ICustomerClient customerClient,
            IPartialCreditApplicationModelRepository partialCreditApplicationModelRepository
            )
        {
            this.customerClient = customerClient;
            this.partialCreditApplicationModelRepository = partialCreditApplicationModelRepository;
        }

        public class ApplicationData
        {
            public string ApplicationNr { get; set; }
            public bool IsActive { get; set; }
            public bool IsFinalDecisionMade { get; set; }
            public bool IsPartiallyApproved { get; set; }
            public string CreditCheckStatus { get; set; }
            public string CustomerCheckStatus { get; set; }
            public string AgreementStatus { get; set; }
        }

        public class CustomerCheckDataModel
        {
            public int CustomerId { get; set; }
            public int ApplicantNr { get; set; }
            public bool IsKnownSanctionFlagged { get; set; }
            public bool WasOnboardedExternally { get; set; }
            public bool IsKycScreened { get; set; }
            public DateTime? LatestKycScreenDate { get; set; }
            public bool IsMissingNameOrAddress { get; set; }
            public bool IsMissingFatcaDecision { get; set; }
            public bool IsMissingEmail { get; set; }
            public bool IsMissingPepOrSanctionDecision { get; set; }
        }

        private class CustomerCardProperty
        {
            public bool IsMissing { get; set; }
            public string Value { get; set; }
        }

        public string GetUpdateCustomerCheckStatusUpdateOrNull(ApplicationData h, Action<List<CustomerCheckDataModel>> observeModels = null)
        {
            if (!h.IsActive)
                return null;

            if (h.IsFinalDecisionMade || h.IsPartiallyApproved)
                return null;

            var app = partialCreditApplicationModelRepository.Get(
                h.ApplicationNr,
                new PartialCreditApplicationModelRequest { ApplicantFields = new List<string>() { "customerId" } });

            var models = new List<CustomerCheckDataModel>();

            var customerIdByApplicantNr = new Dictionary<int, int>();
            app.DoForEachApplicant(applicantNr =>
                {
                    customerIdByApplicantNr[applicantNr] =
                        app.Applicant(applicantNr).Get("customerId").IntValue.Required;
                });

            var itemsByCustomerId = customerClient.BulkFetchPropertiesByCustomerIdsD(
                customerIdByApplicantNr.Values.ToHashSetShared(),
                "sanction", "wasOnboardedExternally", "firstName", "addressZipcode", "includeInFatcaExport", "email");

            var onboardingStatusByCustomerId = customerClient.FetchCustomerOnboardingStatuses(customerIdByApplicantNr.Values.ToHashSetShared(), null, null, false);

            app.DoForEachApplicant(applicantNr =>
            {
                var customerId = customerIdByApplicantNr[applicantNr];

                var items = itemsByCustomerId.Opt(customerId);
                var onboardingStatus = onboardingStatusByCustomerId[customerId];
                Func<string, bool> isMissingProperty = x => string.IsNullOrWhiteSpace(items.Opt(x));

                var c = new CustomerCheckDataModel
                {
                    CustomerId = customerId,
                    ApplicantNr = applicantNr,
                    IsKnownSanctionFlagged = onboardingStatus.IsSanction == true,
                    WasOnboardedExternally = items.Opt("wasOnboardedExternally")?.ToLowerInvariant() == "true",
                    IsKycScreened = onboardingStatus.LatestScreeningDate.HasValue,
                    LatestKycScreenDate = onboardingStatus.LatestScreeningDate,
                    IsMissingNameOrAddress = isMissingProperty("firstName") || isMissingProperty("addressZipcode"),
                    IsMissingFatcaDecision = isMissingProperty("includeInFatcaExport"),
                    IsMissingEmail = isMissingProperty("email"),
                    IsMissingPepOrSanctionDecision = !onboardingStatus.IsPep.HasValue || !onboardingStatus.IsSanction.HasValue
                };
                models.Add(c);
            });

            observeModels?.Invoke(models);

            //The reason for looking at credit check status when determining rejected vs initial is that if something changes late in the process it needs to be flagged for manual attention
            bool useRejectedToFlagManualAttention = h.CreditCheckStatus == "Accepted" && h.AgreementStatus == "Accepted";

            Func<string> getNewStatus = () =>
            {
                //Sanction
                if (models.Any(x => x.IsKnownSanctionFlagged))
                {
                    return "Rejected";
                }

                //Kyc screen
                if (models.Any(x => !x.IsKycScreened && !x.WasOnboardedExternally))
                {
                    return useRejectedToFlagManualAttention ? "Rejected" : "Initial";
                }

                if (models.Any(x => x.IsMissingNameOrAddress))
                {
                    return useRejectedToFlagManualAttention ? "Rejected" : "Initial";
                }

                if (models.Any(x => x.IsMissingEmail))
                {
                    return "Initial";
                }

                if (models.Any(x => x.IsMissingFatcaDecision))
                {
                    return "Initial";
                }

                if (models.Any(x => x.IsMissingPepOrSanctionDecision))
                {
                    return "Initial";
                }

                return "Accepted";
            };
            var changeStatusTo = getNewStatus();
            if (h.CustomerCheckStatus != changeStatusTo)
            {
                return changeStatusTo;
            }
            else
            {
                return null;
            }
        }
    }
}