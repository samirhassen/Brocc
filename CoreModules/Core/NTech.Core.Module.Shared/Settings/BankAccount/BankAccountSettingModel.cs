using NTech.Banking.BankAccounts;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace NTech.Core.Customer.Shared.Settings.BankAccount
{
    public class BankAccountSettingModel
    {
        public SettingsModel.FormDataModel.DefaultValue DefaultBankAccountNr { get; set; }
        public bool? IsInitiallyEnabled { get; set; }
        public ISet<BankAccountNumberTypeCode> ExcludedAccountNrTypes { get; set; }
        public ISet<BankAccountNumberTypeCode> OnlyTheseBankAccountNrTypes { get; set; }

        //Format should be like <country>:<code>:<accountnr>
        public static IBankAccountNumber ParseDefaultValue(string value)
        {
            if (value == "none")
            {
                return null;
            }
            var firstSepartorIndex = value.IndexOf(':');
            var countryIsoCode = value.Substring(0, firstSepartorIndex);
            var secondSeparatorIndex = value.IndexOf(':', firstSepartorIndex + 1);
            var bankAccountNrType = value.Substring(firstSepartorIndex + 1, secondSeparatorIndex - firstSepartorIndex - 1);
            var bankAccountNr = value.Substring(secondSeparatorIndex + 1);
            return new BankAccountNumberParser(countryIsoCode).ParseBankAccount(bankAccountNr, Enums.ParseReq<BankAccountNumberTypeCode>(bankAccountNrType));
        }
        public static IBankAccountNumber GetBankAccountNumberOverrideFromSetting(Dictionary<string, string> settings)
        {
            if (settings["isEnabled"] != "true")
                return null;
            else
                return ParseBankAccountFromSetting(settings);
        }
        private static IBankAccountNumber ParseBankAccountFromSetting(Dictionary<string, string> settings)
        {
            var parser = new BankAccountNumberParser(settings["twoLetterIsoCountryCode"]);
            if (parser.TryParseBankAccount(settings["bankAccountNr"], Enums.ParseReq<BankAccountNumberTypeCode>(settings["bankAccountNrTypeCode"]), out var nr))
                return nr;
            return null;
        }

        public static Dictionary<string, string> PopulateSettingOnLoad(BankAccountSettingModel setting, Dictionary<string, string> userValues, Func<string, string> getClientConfigValue)
        {
            if (userValues == null || userValues.Count == 0)
                return BankAccountToSettingsValues(false, null);

            var isEnabled = userValues["isEnabled"] == "true";
            var bankAccountNumberParsed = userValues["bankAccountNr"] == "none" ? null : ParseBankAccountFromSetting(userValues);

            return BankAccountToSettingsValues(isEnabled, bankAccountNumberParsed);
        }

        public static Dictionary<string, string> BankAccountToSettingsValues(bool isEnabled, IBankAccountNumber bankAccountNumber)
        {
            return new Dictionary<string, string>
            {
                ["isEnabled"] = bankAccountNumber == null ? "false" : (isEnabled ? "true" : "false"),
                ["bankAccountNr"] = bankAccountNumber?.FormatFor(null) ?? "none",
                ["twoLetterIsoCountryCode"] = bankAccountNumber?.TwoLetterCountryIsoCode ?? "none",
                ["bankAccountNrTypeCode"] = bankAccountNumber == null ? "none" : bankAccountNumber.AccountType.ToString()
            };
        }

        public static Dictionary<string, string> TransformUiSaveValuesToStoredSettingValues(BankAccountSettingModel setting, Dictionary<string, string> uiValues)
        {
            bool isEnabled;
            IBankAccountNumber bankAccountNrParsed;

            if (!setting.IsInitiallyEnabled.HasValue)
                isEnabled = true;
            else if (!uiValues.ContainsKey("isEnabled") || !uiValues["isEnabled"].IsOneOf("true", "false"))
                throw new NTechCoreWebserviceException("Needs value isEnabled that must be true or false")
                {
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };
            else
                isEnabled = uiValues["isEnabled"] == "true";

            if (!uiValues.ContainsKey("twoLetterIsoCountryCode") || !uiValues.ContainsKey("bankAccountNrTypeCode") || !uiValues.ContainsKey("bankAccountNr"))
                throw new NTechCoreWebserviceException("Missing one of twoLetterIsoCountryCode, bankAccountNrTypeCode and bankAccountNr")
                {
                    IsUserFacing = true,
                    ErrorHttpStatusCode = 400
                };

            if (uiValues["bankAccountNrTypeCode"] == "none")
            {
                bankAccountNrParsed = null;
                isEnabled = false;
            }
            else
            {
                try
                {
                    var parser = new BankAccountNumberParser(uiValues["twoLetterIsoCountryCode"]);
                    if (!parser.TryParseBankAccount(uiValues["bankAccountNr"], Enums.ParseReq<BankAccountNumberTypeCode>(uiValues["bankAccountNrTypeCode"]), out bankAccountNrParsed))
                        throw new NTechCoreWebserviceException("Invalid bank account nr")
                        {
                            IsUserFacing = true,
                            ErrorHttpStatusCode = 400
                        };
                }
                catch
                {
                    throw new NTechCoreWebserviceException("Invalid combination of twoLetterIsoCountryCode, bankAccountNrTypeCode and bankAccountNr")
                    {
                        IsUserFacing = true,
                        ErrorHttpStatusCode = 400
                    };
                }
            }

            return BankAccountToSettingsValues(isEnabled, bankAccountNrParsed);
        }
    }
}