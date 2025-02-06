using nCustomerPages.Code;
using NTech.Core.Module.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using System.Collections.Generic;

namespace nCustomerPages.Controllers
{
    public static class CustomerPortalService
    {
        private static HashSet<CustomerNavigationTargetName> RedirectToKycQuestionsTargets = new HashSet<CustomerNavigationTargetName>
        {
            CustomerNavigationTargetName.ProductOverview,
            CustomerNavigationTargetName.StandardOverview,
            CustomerNavigationTargetName.Overview,
            CustomerNavigationTargetName.SavingsOverview,
            CustomerNavigationTargetName.CreditOverview,
            CustomerNavigationTargetName.MortgageLoanOverview
        };

        public static bool ShouldBeForcedToAnswerKycQuestions(CustomerNavigationTargetName navigationTarget, int customerId)
        {
            if (!NEnv.IsCustomerPagesKycQuestionsEnabled)
                return false;

            if (!RedirectToKycQuestionsTargets.Contains(navigationTarget))
                return false;

            var customerClient = LegacyServiceClientFactory.CreateCustomerClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var settingsService = new CachedSettingsService(customerClient);
            //NOTE: We cant use the cache on customer pages since we have no mechanism for sending messages from internal modules to public ones, only in the opposite direction so the cache break event cant get here
            var setting = settingsService.LoadSettingsNoCache("kycUpdateRequiredSecureMessage");
            var isOverdueLoginRedirectEnabled = setting != null && setting.ContainsKey("isOverdueLoginRedirectEnabled") && setting["isOverdueLoginRedirectEnabled"] == "true";
            if (!isOverdueLoginRedirectEnabled)
                return false;

            return new CustomerLockedHostClient(customerId).GetIsKycReminderRequired();
        }
    }
}