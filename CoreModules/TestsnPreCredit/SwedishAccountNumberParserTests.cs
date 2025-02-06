using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Banking.BankAccounts.Se;
using System;

namespace TestsnPreCredit
{
    [TestClass]
    public class SwedishAccountNumberParserTests
    {
        [TestMethod]
        public void CanCreateValidHandelsbankenBankAccount()
        {
            var account = BankAccountNumberSe.Parse("6789123456789");
            Assert.AreEqual("Handelsbanken", account.BankName);
            Assert.AreEqual("6789", account.ClearingNr);
            Assert.AreEqual("123456789", account.AccountNr);
        }

        [TestMethod]
        public void CanCreateValidNordeaBankAccount()
        {
            var account = BankAccountNumberSe.Parse("3300192208319232");
            Assert.AreEqual("Nordea", account.BankName);
            Assert.AreEqual("3300", account.ClearingNr);
            Assert.AreEqual("2208319232", account.AccountNr);
        }

        [TestMethod]
        public void CanCreateValidNordeaBankAccount2()
        {
            var account = BankAccountNumberSe.Parse("33002208319232");
            Assert.AreEqual("Nordea", account.BankName);
            Assert.AreEqual("3300", account.ClearingNr);
            Assert.AreEqual("2208319232", account.AccountNr);
        }

        [TestMethod]
        public void CanCreateValidSebBankAccount()
        {
            var account = BankAccountNumberSe.Parse("50001234560");
            Assert.AreEqual("SEB", account.BankName);
            Assert.AreEqual("5000", account.ClearingNr);
            Assert.AreEqual("1234560", account.AccountNr);
        }

        [TestMethod]
        public void CanCreateValidSwedbankBankAccount()
        {
            var account = BankAccountNumberSe.Parse("888812345674");
            Assert.AreEqual("Swedbank", account.BankName);
            Assert.AreEqual("8888", account.ClearingNr);
            Assert.AreEqual("12345674", account.AccountNr);
        }

        [TestMethod]
        public void CanCreateValidSwedbankBankAccount2()
        {
            var account = BankAccountNumberSe.Parse("8888912345674");
            Assert.AreEqual("Swedbank", account.BankName);
            Assert.AreEqual("8888", account.ClearingNr);
            Assert.AreEqual("12345674", account.AccountNr);
        }

        //
        // Invalid Accounts
        //

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreatingHandelsbankenAccountWithInvalidChecksumThrowsExcption()
        {
            var account = BankAccountNumberSe.Parse("6789123456780");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreatingNordeaAccountWithInvalidChecksumThrowsExcption()
        {
            var account = BankAccountNumberSe.Parse("3300192208319231");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreatingSebAccountWithInvalidChecksumThrowsExcption()
        {
            var account = BankAccountNumberSe.Parse("50001234561");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreatingSwedbankAccountWithInvalidChecksumThrowsExcption()
        {
            var account = BankAccountNumberSe.Parse("888812345675");
        }
    }
}
