using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;
using NTech.Core.Module.Shared.Infrastructure;
using static NTech.Core.Host.IntegrationTests.Credits;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        public enum ScenarioName
        {
            Normal,
            OverrideIncomingAccountButNotDirectDebitAccount,
            OverrideIncomingAccountAndDirectDebitAccount
        }
        private void NotifyCredit(UlStandardTestRunner.TestSupport support, int creditNumber, ScenarioName scenarioName)
        {
            var credit = CreditsUlStandard.GetCreateCredit(support, creditNumber);

            var customerClient = TestPersons.CreateRealisticCustomerClient(support);
            var postalInfo = new CustomerPostalInfoRepository(false, customerClient.Object, support.ClientConfiguration);
            
            IBankAccountNumber? customIncomingPaymentAccount = null;
            BankGiroNumberSe? restoreToGiroNr = null;
            try
            {
                if (scenarioName != ScenarioName.Normal)
                {
                    var a = BankGiroNumberSe.Parse("9009283");
                    Credits.OverrideIncomingPaymentAccount(a, support);
                    if (scenarioName == ScenarioName.OverrideIncomingAccountAndDirectDebitAccount)
                    {
                        restoreToGiroNr = support.AutogiroSettings.BankGiroNr;
                        support.AutogiroSettings.BankGiroNr = a;
                    }
                    customIncomingPaymentAccount = a;
                }
                                
                var notificationService = support.GetRequiredService<NotificationService>();
                var notificationResult = notificationService.CreateNotifications(skipDeliveryExport: true, skipNotify: false, onlyTheseCreditNrs: new List<string> { credit.CreditNr });
                Assert.That(notificationResult.SuccessCount, Is.EqualTo(1), notificationResult.Errors?.FirstOrDefault());

                var renderer = support.GetRequiredService<NotificationRenderer>();
                var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);

                var printContext = renderer.GetLastPrintContext();
                Assert.That(
                    printContext?.Opt("paymentBankGiroNr"),
                    Is.EqualTo(paymentAccountService.FormatIncomingBankAccountNrForDisplay((customIncomingPaymentAccount ?? support.IncomingPaymentAccount))));
            }
            catch (NTechCoreWebserviceException ex)
            {
                if (scenarioName == ScenarioName.OverrideIncomingAccountButNotDirectDebitAccount)
                    Assert.That(ex.ErrorCode, Is.EqualTo("directDebitAccountDiffersFromIncomingPaymentAccount"));
                else
                    throw;
            }
            finally
            {
                if (customIncomingPaymentAccount != null)
                    Credits.ClearIncomingPaymentAccountOverride(support);
                if (restoreToGiroNr != null)
                    support.AutogiroSettings.BankGiroNr = restoreToGiroNr;
            }
        }
    }
}