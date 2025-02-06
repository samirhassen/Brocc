using NTech.Banking.BankAccounts.Fi;
using NTech.Banking.BankAccounts.Se;
using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace nCredit.Code.Services
{
    public class BankAccountValidationService : IBankAccountValidationService
    {
        private readonly IClientConfigurationCore clientConfiguration;
        private static Lazy<IBANToBICTranslator> iBANToBICTranslatorInstance = new Lazy<IBANToBICTranslator>(() => new IBANToBICTranslator());
        public static IBANToBICTranslator IBANToBICTranslatorInstance => iBANToBICTranslatorInstance.Value;

        public BankAccountValidationService(IClientConfigurationCore clientConfiguration)
        {
            this.clientConfiguration = clientConfiguration;
        }

        public BankAccountNrValidationResult ValidateBankAccountNr(string bankAccountNr)
        {
            var baseCountry = clientConfiguration.Country.BaseCountry;
            if (baseCountry == "SE")
            {
                string failedMessage;
                BankAccountNumberSe b;
                var isValid = BankAccountNumberSe.TryParse(bankAccountNr, out b, out failedMessage);
                return new BankAccountNrValidationResult
                {
                    RawNr = bankAccountNr,
                    IsValid = isValid,
                    ValidAccount = isValid ? new BankAccountNrValidationResult.ValidAccountModel
                    {
                        Type = BankAccountNrValidationResult.BankAccountNrValidationTypeCode.bankaccount.ToString(),
                        BankName = b.BankName,
                        ClearingNr = b.ClearingNr,
                        AccountNr = b.AccountNr,
                        NormalizedNr = b.PaymentFileFormattedNr
                    } : null
                };
            }
            else if (baseCountry == "FI")
            {
                IBANFi b;
                var isValid = IBANFi.TryParse(bankAccountNr, out b);
                return new BankAccountNrValidationResult
                {
                    RawNr = bankAccountNr,
                    IsValid = isValid,
                    ValidAccount = isValid ? new BankAccountNrValidationResult.ValidAccountModel
                    {
                        Type = BankAccountNrValidationResult.BankAccountNrValidationTypeCode.iban.ToString(),
                        BankName = IBANToBICTranslatorInstance.InferBankName(b),
                        Bic = IBANToBICTranslatorInstance.InferBic(b),
                        NormalizedNr = b.NormalizedValue
                    } : null
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public class BankAccountNrValidationResult
    {
        public string RawNr { get; set; }
        public bool IsValid { get; set; }
        public ValidAccountModel ValidAccount { get; set; }
        public class ValidAccountModel
        {
            public string Type { get; set; }
            public string BankName { get; set; }
            public string ClearingNr { get; set; }
            public string AccountNr { get; set; }
            public string Bic { get; set; }
            public string NormalizedNr { get; set; }
        }
        public enum BankAccountNrValidationTypeCode
        {
            iban,
            bankaccount
        }
    }

    public interface IBankAccountValidationService
    {
        BankAccountNrValidationResult ValidateBankAccountNr(string bankAccountNr);
    }
}