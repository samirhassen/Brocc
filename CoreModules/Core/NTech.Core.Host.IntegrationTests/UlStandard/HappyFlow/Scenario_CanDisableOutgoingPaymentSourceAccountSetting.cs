using nCredit.DbModel.BusinessEvents;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Customer.Shared.Settings.BankAccount;
using NTech.Core.Host.IntegrationTests.UlStandard;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void CanDisableOutgoingPaymentSourceAccountSetting(UlStandardTestRunner.TestSupport support)
        {
            IBankAccountNumber overrideBankAccountNr = BankAccountNumberSe.Parse("33000705219806");

            var mgr = new BusinessEventManagerOrServiceBase(support.CurrentUser, support.Clock, support.ClientConfiguration);
            var paymentAccountService = support.CreatePaymentAccountService(support.CreditEnvSettings);
            var settingsService = support.CreateSettingsService();
            IBankAccountNumber GetCurrentAccount() => paymentAccountService.GetOutgoingPaymentSourceBankAccountNr();

            settingsService.ClearUserValues("outgoingPaymentSourceBankAccount");

            //With no setting uses config value
            Assert.That(
                GetCurrentAccount()?.FormatFor(null),
                Is.EqualTo(support.CreditEnvSettings.OutgoingPaymentBankAccountNr.FormatFor(null)));

            //Set a value but not enabled
            settingsService.SaveSettingsValues("outgoingPaymentSourceBankAccount",
                BankAccountSettingModel.BankAccountToSettingsValues(false, overrideBankAccountNr), (IsSystemUser: true, GroupMemberships: new HashSet<string>()));

            Assert.That(
                settingsService.LoadSettingsValues("outgoingPaymentSourceBankAccount").Opt("bankAccountNr"),
                Is.EqualTo(overrideBankAccountNr.FormatFor(null)));

            Assert.That(
                GetCurrentAccount()?.FormatFor(null),
                Is.EqualTo(support.CreditEnvSettings.OutgoingPaymentBankAccountNr.FormatFor(null)));

            //Set the same value but enabled
            settingsService.SaveSettingsValues("outgoingPaymentSourceBankAccount",
                BankAccountSettingModel.BankAccountToSettingsValues(true, overrideBankAccountNr), (IsSystemUser: true, GroupMemberships: new HashSet<string>()));

            Assert.That(
                GetCurrentAccount()?.FormatFor(null),
                Is.EqualTo(overrideBankAccountNr.FormatFor(null)));

            settingsService.ClearUserValues("outgoingPaymentSourceBankAccount");
        }
    }
}