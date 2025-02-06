using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure.NTechWs.Attributes;

namespace TestsnPreCredit.Validation
{
    [TestClass]
    public class PaymentReferenceNumberValidatorTests
    {
        [DataTestMethod]
        [DataRow("BankGiroSe", "4242")]
        [DataRow("PlusGiroSe", "4242")]
        [DataRow("BankGiroSe", "42")]
        [DataRow("PlusGiroSe", "42")]
        [DataRow("BankGiroSe", "2423423421")]
        [DataRow("PlusGiroSe", "2423423421")]
        [DataRow("BankGiroSe", "3737558")]
        [DataRow("PlusGiroSe", "3737558")]
        [DataRow("BankGiroSe", "1234567891234567891234566")]
        [DataRow("PlusGiroSe", "1234567891234567891234566")]
        public void IsValidPaymentReferenceNr_BgcValid_ShouldReturnTrue(string accountType, string input)
        {
            var isValid = PaymentReferenceNumberValidator.IsValidPaymentReferenceNr(accountType, input);
            Assert.IsTrue(isValid);
        }

        [DataTestMethod]
        [DataRow("BankGiroSe", "42042")]
        [DataRow("PlusGiroSe", "42420")]
        [DataRow("BankGiroSe", "43")]
        [DataRow("PlusGiroSe", "43")]
        [DataRow("BankGiroSe", "2423423423")]
        [DataRow("PlusGiroSe", "2423423423")]
        [DataRow("BankGiroSe", "3737458")]
        [DataRow("PlusGiroSe", "3737458")]
        [DataRow("BankGiroSe", "12345678912345678912345660")]
        [DataRow("PlusGiroSe", "12345678912345678912345660")]
        public void IsValidPaymentReferenceNr_BgcInvalidNumbers_ShouldReturnFalse(string accountType, string input)
        {
            var isValid = PaymentReferenceNumberValidator.IsValidPaymentReferenceNr(accountType, input);
            Assert.IsFalse(isValid);
        }
    }
}
