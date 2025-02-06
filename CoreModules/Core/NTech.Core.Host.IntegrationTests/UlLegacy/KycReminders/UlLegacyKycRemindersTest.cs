using Moq;
using NTech.Core.Customer.Database;
using NTech.Core.Customer.Shared;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Customer.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using NTech.Core.Module.Shared.Settings.KycUpdateFrequency;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public class UlLegacyKycRemindersTest
    {
        [Test]
        public void KycRemindersTest()
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                var customerContextFactory = new CustomerContextFactory(() => new CustomerContextExtended(support.CurrentUser, support.Clock));

                var credit = CreditsUlLegacy.CreateCredit(support, 1);

                var customerId = credit.CreditCustomers.First().CustomerId;

                var customerEnvSettings = new Mock<ICustomerEnvSettings>(MockBehavior.Strict);
                customerEnvSettings
                    .Setup(x => x.DefaultKycQuestionsSets)
                    .Returns(new Dictionary<string, Customer.Shared.Models.KycQuestionsTemplate>());

                var settingsService = support.CreateCachedSettingsService();
                var templateService = new KycQuestionsTemplateService(customerContextFactory, customerEnvSettings.Object, support.ClientConfiguration);
                var kycAnswersUpdateService = new KycAnswersUpdateService(customerContextFactory, support.CurrentUser, support.Clock,
                    templateService, settingsService, support.EncryptionService);

                const int DefaultUpdateMonths = 14;
                const int NrOfDaysBeforeUpdate = 3;
                TestSettings.UpdateSetting("kycUpdateRequiredSecureMessage", currentValue =>
                {
                    currentValue["nrOfDaysBeforeUpdate"] = NrOfDaysBeforeUpdate.ToString();
                    return currentValue;
                }, support);

                TestSettings.UpdateSetting("kycUpdateFrequency", _ =>
                {
                    return KycUpdateFrequencyDataModel.ConvertToStoredSettingValues(DefaultUpdateMonths, null);
                }, support);

                void AssertReminderAndUpdateRequired(bool isReminderRequired, bool isUpdateRequired)
                {
                    var relationStatus = kycAnswersUpdateService!.GetCustomerPagesStatusForCustomer(customerId).ActiveRelations.Single();
                    Assert.That(relationStatus.IsReminderRequired, Is.EqualTo(isReminderRequired));
                    Assert.That(relationStatus.IsUpdateRequired, Is.EqualTo(isUpdateRequired));
                }

                AssertReminderAndUpdateRequired(false, false);

                support.Now = credit.StartDate.AddMonths(DefaultUpdateMonths).AddDays(-(NrOfDaysBeforeUpdate + 1));
                AssertReminderAndUpdateRequired(false, false);

                support.Now = support.Now.AddDays(1);
                AssertReminderAndUpdateRequired(true, false);

                support.Now = support.Now.AddDays(NrOfDaysBeforeUpdate);
                AssertReminderAndUpdateRequired(true, true);
            });
        }
    }
}
