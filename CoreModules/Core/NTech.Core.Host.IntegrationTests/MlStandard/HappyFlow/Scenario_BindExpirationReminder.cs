using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class MlStandardLikeScenarioTests
    {
        private void BindExpirationReminder(MlStandardTestRunner.TestSupport support)
        {
            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var rebindReminderService = new BoundInterestExpirationReminderService(support.CreateCreditContextFactory(), support.CurrentUser,
                support.Clock, support.ClientConfiguration, customerClient.Object, support.CreditEnvSettings, support.CreateCachedSettingsService());
            var creditNr = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;

            CreditsMlStandard.RunOneMonth(support, payNotificationsOnDueDate: true);
            CreditsMlStandard.RunOneMonth(support, payNotificationsOnDueDate: true);
            CreditsMlStandard.RunOneMonth(support, payNotificationsOnDueDate: true, afterDay: dayNr =>
            {
                if (dayNr == 9)
                {
                    Assert.That(rebindReminderService.HasBeenRemindedThisRebind(creditNr), Is.False, "Found unexpected rebinding reminder");
                }
                else if (dayNr == 10)
                {
                    //This is where reminders would normally have been sent if the setting had been enabled
                    Assert.That(rebindReminderService.HasBeenRemindedThisRebind(creditNr), Is.False, "Found unexpected rebinding reminder");

                    //Enable reminder messages
                    TestSettings.UpdateSetting("mlBindingExpirationSecureMessage", x =>
                    {
                        var newSetting = new Dictionary<string, string>(x);
                        newSetting["isEnabled"] = "true";
                        return newSetting;
                    }, support);
                }
                else if (dayNr == 11)
                {
                    Assert.That(rebindReminderService.HasBeenRemindedThisRebind(creditNr), Is.True, "Missing expected rebinding reminder");
                }
            });
        }
    }
}