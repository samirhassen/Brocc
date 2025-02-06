using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System.Collections.Generic;

namespace TestsnPreCredit.Validation
{
    [TestClass]
    public class BankAccountNrValidationTests
    {
        [TestMethod]
        public void EmptyBankAccountNrType_ShouldDefaultToStandardForCountry()
        {
            var client = new StrictMock<IClientConfigurationCore>();
            client.Setup(x => x.Country).Returns(new ClientConfigurationCoreCountry
            {
                BaseCountry = "SE",
                BaseCurrency = "SEK",
                BaseFormattingCulture = "sv-SE"
            });
            var s = new BankAccountNrValidationService(client.Object, x => new Dictionary<string, string>());

            var request = new nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request
            {
                Accounts = new List<nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request.Account>
                {
                    new nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request.Account
                    {
                        BankAccountNr = "33008708214104",
                        BankAccountNrType = "",
                        RequestKey = "b"
                    }
                }
            };
            var result = s.Validate(request);

            Assert.AreEqual(result?.ValidatedAccountsByKey["b"]?.IsValid, true);
        }

        [TestMethod]
        public void NonExistingBankAccountNrType_ShouldReturnInvalid()
        {
            var client = new StrictMock<IClientConfigurationCore>();
            client.Setup(x => x.Country).Returns(new ClientConfigurationCoreCountry
            {
                BaseCountry = "SE",
                BaseCurrency = "SEK",
                BaseFormattingCulture = "sv-SE"
            });
            var s = new BankAccountNrValidationService(client.Object, x => new Dictionary<string, string>());

            var request = new nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request
            {
                Accounts = new List<nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request.Account>
                {
                    new nPreCredit.WebserviceMethods.ValidateBankAccountNrBatchMethod.Request.Account
                    {
                        BankAccountNr = "33008708214104",
                        BankAccountNrType = "none",
                        RequestKey = "b"
                    }
                }
            };
            var result = s.Validate(request);

            Assert.AreEqual(result?.ValidatedAccountsByKey["b"]?.IsValid, false);
        }
    }
}
