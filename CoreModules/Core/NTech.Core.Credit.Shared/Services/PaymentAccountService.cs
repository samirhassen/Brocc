using nCredit;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Customer.Shared.Settings.BankAccount;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Core.Credit.Shared.Services
{
    public class PaymentAccountService
    {
        private readonly CachedSettingsService settingsService;
        private readonly ICreditEnvSettings envSettings;
        private readonly IClientConfigurationCore clientConfiguration;

        public PaymentAccountService(CachedSettingsService settingsService, ICreditEnvSettings envSettings, IClientConfigurationCore clientConfiguration)
        {
            this.settingsService = settingsService;
            this.envSettings = envSettings;
            this.clientConfiguration = clientConfiguration;
        }

        public IBankAccountNumber GetOutgoingPaymentSourceBankAccountNr()
        {
            var outgoingPaymentSourceBankAccountSetting = settingsService.LoadSettings("outgoingPaymentSourceBankAccount");
            var overrideFromAccount = BankAccountSettingModel.GetBankAccountNumberOverrideFromSetting(outgoingPaymentSourceBankAccountSetting);
            if (overrideFromAccount != null)
                return overrideFromAccount;

            var settingAccount = envSettings.OutgoingPaymentBankAccountNr;

            if (settingAccount != null)
                return settingAccount;

            throw new NTechCoreWebserviceException("Missing source account for outgoing payments. It needs to be set either using the setting 'outgoingPaymentSourceBankAccount' or using appsettings")
            {
                IsUserFacing = false,
                ErrorCode = "missingOutgoingPaymentAccount",
                ErrorHttpStatusCode = 500
            };
        }

        public BankGiroNumberSe GetIncomingPaymentBankAccountNrRequireBankgiro()
        {
            var account = GetIncomingPaymentBankAccountNr();
            if (account.AccountType != BankAccountNumberTypeCode.BankGiroSe)
                throw new Exception("Account must be of type BankGiroSe");
            return (BankGiroNumberSe)account;
        }

        public IBANFi GetIncomingPaymentBankAccountNrRequireIbanFi()
        {
            var account = GetIncomingPaymentBankAccountNr();
            if (account.AccountType != BankAccountNumberTypeCode.IBANFi)
                throw new Exception("Account must be of type IBANFi");
            return (IBANFi)account;
        }

        public IBankAccountNumber GetIncomingPaymentBankAccountNr()
        {
            IBankAccountNumber GetAccount()
            {
                var userSetting = settingsService.LoadSettings("incomingPaymentBankAccount");
                var overrideAccount = BankAccountSettingModel.GetBankAccountNumberOverrideFromSetting(userSetting);
                if (overrideAccount != null)
                    return overrideAccount;

                var appSettingAccount = envSettings.IncomingPaymentBankAccountNr;

                if (appSettingAccount != null)
                    return appSettingAccount;

                throw new NTechCoreWebserviceException("Missing account for incoming payments. It needs to be set either using the setting 'incomingPaymentBankAccount' or using appsettings")
                {
                    IsUserFacing = false,
                    ErrorCode = "missingIncomingPaymentAccount",
                    ErrorHttpStatusCode = 500
                };
            }
            var account = GetAccount();

            if (envSettings.IsDirectDebitPaymentsEnabled)
            {
                var agAccount = envSettings.AutogiroSettings?.BankGiroNr;
                if (agAccount != null && agAccount.FormatFor(null) != account.FormatFor(null))
                    throw new NTechCoreWebserviceException("Direct debit is enabled but the bg account for that is different from the incoming payment account which is not allowed.")
                    {
                        IsUserFacing = false,
                        ErrorCode = "directDebitAccountDiffersFromIncomingPaymentAccount",
                        ErrorHttpStatusCode = 500
                    };
            }

            return account;
        }

        public string FormatIncomingBankAccountNrForDisplay(IBankAccountNumber acocuntNr)
        {
            var country = clientConfiguration.Country.BaseCountry;
            if (country == "FI" && acocuntNr.AccountType == BankAccountNumberTypeCode.IBANFi)
            {
                return ((IBANFi)acocuntNr).GroupsOfFourValue;
            }
            else if (country == "SE" && acocuntNr.AccountType == BankAccountNumberTypeCode.BankGiroSe)
            {
                return ((BankGiroNumberSe)acocuntNr).DisplayFormattedValue;
            }
            else
                return acocuntNr.FormatFor("display");
        }
    }
}