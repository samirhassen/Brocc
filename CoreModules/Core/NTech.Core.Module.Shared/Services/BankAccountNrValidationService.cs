using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using NTech.Banking.Shared.BankAccounts.Fi;

namespace NTech.Core.Module.Shared.Services
{
    public class BankAccountNrValidationService
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly Func<IBankAccountNumber, Dictionary<string, string>> getExternalData;
        private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());

        public BankAccountNrValidationService(IClientConfigurationCore clientConfiguration, Func<IBankAccountNumber, Dictionary<string, string>> getExternalData)
        {
            this.clientConfiguration = clientConfiguration;
            this.getExternalData = getExternalData;
        }

        public ValidateBankAccountNrBulkResponse Validate<TAccount>(IValidateBankAccountNrBulkRequest<TAccount> request) where TAccount : IValidateBankAccountNrBulkRequestAccount
        {
            var p = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);
            var result = new Dictionary<string, ValidateBankAccountNrBulkResponse.AccountResultModel>();
            foreach (var account in request.Accounts)
            {
                var accountResult = new ValidateBankAccountNrBulkResponse.AccountResultModel
                {
                    RawNr = account.BankAccountNr,
                    IsValid = false
                };

                IBankAccountNumber parsedAccount = null;

                //TODO: Drop this after updating the NTech.Banking.BankAccounts nuget package to have formatFor("display") not explode
                string FormatForTemp(IBankAccountNumber nr)
                {
                    try
                    {
                        return nr.FormatFor("display");
                    }
                    catch
                    {
                        return nr.FormatFor(null);
                    }
                }

                if (!IsInvalidBankAccountNumberTypeCode(account.BankAccountNrType) && p.TryParseFromStringWithDefaults(account.BankAccountNr, account.BankAccountNrType, out parsedAccount))
                {
                    accountResult.IsValid = true;
                    accountResult.ValidAccount = new ValidateBankAccountNrBulkResponse.ValidAccountModel
                    {
                        BankAccountNrType = parsedAccount.AccountType.ToString(),
                        DisplayNr = FormatForTemp(parsedAccount)
                    };
                    switch (parsedAccount.AccountType)
                    {
                        case BankAccountNumberTypeCode.BankAccountSe:
                            {
                                var a = (BankAccountNumberSe)parsedAccount;
                                accountResult.ValidAccount.BankName = a.BankName;
                                accountResult.ValidAccount.AccountNr = a.AccountNr;
                                accountResult.ValidAccount.ClearingNr = a.ClearingNr;
                                accountResult.ValidAccount.NormalizedNr = a.PaymentFileFormattedNr;
                            }
                            break;
                        case BankAccountNumberTypeCode.IBANFi:
                            {
                                var a = (IBANFi)parsedAccount;
                                accountResult.ValidAccount.NormalizedNr = a.NormalizedValue;
                                accountResult.ValidAccount.BankName = iBANToBICTranslatorInstance.Value.InferBankName(a);
                                accountResult.ValidAccount.Bic = iBANToBICTranslatorInstance.Value.InferBic(a);
                            }
                            break;
                        default:
                            {
                                accountResult.ValidAccount.NormalizedNr = parsedAccount.FormatFor(null);
                            }
                            break;
                    }
                }

                if (accountResult.IsValid && request.AllowExternalSources == true)
                {
                    accountResult.ValidAccount.ExternalData = getExternalData(parsedAccount);
                }

                result[account.RequestKey] = accountResult;
            }

            return new ValidateBankAccountNrBulkResponse
            {
                ValidatedAccountsByKey = result
            };
        }

        private bool IsInvalidBankAccountNumberTypeCode(string bankAccountNrType)
        {
            if (string.IsNullOrWhiteSpace(bankAccountNrType))
                return false;

            return !Enums.Parse<BankAccountNumberTypeCode>(bankAccountNrType).HasValue;
        }
    }

    public interface IValidateBankAccountNrBulkRequest<T> where T : IValidateBankAccountNrBulkRequestAccount
    {
        bool? AllowExternalSources { get; }
        List<T> Accounts { get; }
    }

    public interface IValidateBankAccountNrBulkRequestAccount
    {
        string BankAccountNr { get; }
        string BankAccountNrType { get; }
        string RequestKey { get; set; }
    }

    public class ValidateBankAccountNrBulkResponse
    {
        public Dictionary<string, AccountResultModel> ValidatedAccountsByKey { get; set; }
        public class AccountResultModel
        {
            public string RawNr { get; set; }
            public bool IsValid { get; set; }
            public ValidAccountModel ValidAccount { get; set; }
        }
        public class ValidAccountModel
        {
            public string BankName { get; set; }
            public string ClearingNr { get; set; }
            public string AccountNr { get; set; }
            public string NormalizedNr { get; set; }
            public string Bic { get; set; }
            public string DisplayNr { get; set; }
            public string BankAccountNrType { get; set; }
            public Dictionary<string, string> ExternalData { get; set; }
        }
    }
}
