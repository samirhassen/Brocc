using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code.Fileformats;
using NTech.Banking.BankAccounts;
using NTech.Banking.BankAccounts.Se;
using NTech.Banking.OrganisationNumbers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.OutgoingPaymentfiles
{
    [TestClass]
    public class OutgoingPaymentFileFormat_SUSTests
    {
        private OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings GetSettings()
        {
            return new OutgoingPaymentFileFormat_SUS_SE.SwedbankSeSettings
            {
                AgreementPaymentType = "08",
                AgreementNumber = "553322",
                CustomerPaymentTransactionMessage = "Lån Reloan",
                ClientOrgNr = OrganisationNumberSe.Parse("7746777015"),
                FileFormat = "SUS"
            };
        }

        [TestMethod]
        public void CreateRows_BankgiroPayment_ShouldCreateTwoPosts()
        {
            // Posttypes 05, 30, 66, 80

            var payments = new List<OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment>
            {
                new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
                {
                    PaymentId = "L10301",
                    ToBankAccount = BankGiroNumberSe.Parse("2222222"),
                    Amount = 25000.0M,
                    Ocr = "4242"
                }
            };

            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, GetSettings(), DateTime.Now);
            var rows = fileFormat.CreateRows(payments);
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("30", rows[1].Substring(0, 2));
            Assert.AreEqual("66", rows[2].Substring(0, 2));
        }

        [TestMethod]
        public void CreateRows_BankgiroWithMessage_ShouldCreateRowType65()
        {
            var unstructuredMessage = "Lösen av lån 10365434";
            var payments = new List<OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment>
            {
                new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
                {
                    PaymentId = "L10301",
                    ToBankAccount = BankGiroNumberSe.Parse("2222222"),
                    Amount = 25000.0M,
                    UnstructuredMessage = unstructuredMessage
                }
            };

            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, GetSettings(), DateTime.Now);
            var rows = fileFormat.CreateRows(payments);
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("30", rows[1].Substring(0, 2));

            var messageRow = rows[2];
            Assert.AreEqual("65", messageRow.Substring(0, 2));
            Assert.AreEqual(unstructuredMessage, messageRow.Substring(61, 64).Trim());
        }

        [TestMethod]
        public void CreateRows_TwoPaymentsOnSameLoan_ShouldHaveUniqueIdentifier()
        {
            // Posttypes 05, 30, 66, 80

            var payments = new List<OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment>
            {
                new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
                {
                    PaymentId = "L10301",
                    ToBankAccount = BankAccountNumberSe.Parse("6133230707"),
                    Amount = 20000.0M
                },
                new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
                {
                    PaymentId = "L10301",
                    ToBankAccount = BankGiroNumberSe.Parse("2222222"),
                    Amount = 25000.0M,
                    Ocr = "4242"
                }
            };

            var date = DateTime.Now;
            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, GetSettings(), date);
            var rows = fileFormat.CreateRows(payments);
            Assert.AreEqual(5, rows.Count);

            Assert.AreEqual(2, rows.Count(x => x.Substring(0, 2) == "30")); // Two rows of type 30
            Assert.AreEqual(1, rows.Count(x => x.Substring(0, 2) == "66"));

            var identifiersForPostType30 = rows.Where(row => row.Substring(0, 2) == "30").Select(row => row.Substring(8, 44)).ToList();
            var total = identifiersForPostType30.Count;
            var uniqueIdentifiers = identifiersForPostType30.Distinct().Count();

            Assert.AreEqual(total, uniqueIdentifiers); // Every type 30 must be unique
        }

        [TestMethod]
        public void CreateRows_SuperLongIdentifier_ShouldLimitLength()
        {
            // Id-utbetalning should br 44 chars. If we have a superlong identifier/creditNr etc. 
            // it should be truncated in the field. 

            var paymentId = "UL1054334554345543453452423423423423";
            var payments = new List<OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment>
            {
                new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
                {
                    PaymentId = paymentId,
                    ToBankAccount = BankGiroNumberSe.Parse("2222222"),
                    Amount = 25000.0M,
                    Ocr = "4242"
                }
            };

            var date = DateTime.Now;
            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, GetSettings(), date);
            var rows = fileFormat.CreateRows(payments);

            var expected = $"{date.AddMonths(1).ToString("yyyy-MM-dd HH:mm")} 1 ";
            expected += paymentId.Substring(0, 25); // Should only take first 25 of the paymentId
            var idUtbetalningvalue = rows[1].Substring(8, 44);

            Assert.AreEqual(expected, idUtbetalningvalue);
        }

        [TestMethod]
        public void GetAmount_Decimal_ShouldShowWithCents()
        {
            Assert.AreEqual("1234567", OutgoingPaymentFileFormat_SUS_SE.GetAmount(12345.67m));
            Assert.AreEqual("5555500", OutgoingPaymentFileFormat_SUS_SE.GetAmount(55555m));
            Assert.AreEqual("2300000", OutgoingPaymentFileFormat_SUS_SE.GetAmount(23000m));
            Assert.AreEqual("500050", OutgoingPaymentFileFormat_SUS_SE.GetAmount(5000.50m));
            Assert.AreEqual("500000", OutgoingPaymentFileFormat_SUS_SE.GetAmount(5000.00m));
        }

        [DataTestMethod]
        [DataRow(BankAccountNumberTypeCode.BankAccountSe, "6133230707", null)]
        [DataRow(BankAccountNumberTypeCode.BankAccountSe, "33007003129843", "PK")]
        [DataRow(BankAccountNumberTypeCode.BankGiroSe, "902-0017", "BG")]
        [DataRow(BankAccountNumberTypeCode.PlusGiroSe, "90 20 01-7", "PG")]
        public void KodKontoOrNull_WithInput_ShouldReturnCorrect(BankAccountNumberTypeCode typeCode, string accountNumber, string expectedResult)
        {
            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, null, DateTime.Now);

            var parser = new BankAccountNumberParser("SE");
            var account = parser.ParseBankAccount(accountNumber, typeCode);

            var kodkonto = fileFormat.KodKontoOrNull(account);
            Assert.AreEqual(expectedResult, kodkonto);
        }

        [DataTestMethod]
        [DataRow(BankAccountNumberTypeCode.BankAccountSe, "6133230707", "6133")]
        [DataRow(BankAccountNumberTypeCode.BankAccountSe, "33007003129843", null)]
        [DataRow(BankAccountNumberTypeCode.BankGiroSe, "902-0017", null)]
        [DataRow(BankAccountNumberTypeCode.PlusGiroSe, "90 20 01-7", null)]
        public void ClearingNrOrNull_WithInput_ShouldReturnCorrect(BankAccountNumberTypeCode typeCode, string accountNumber, string expectedResult)
        {
            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, null, DateTime.Now);

            var parser = new BankAccountNumberParser("SE");
            var account = parser.ParseBankAccount(accountNumber, typeCode);

            var result = fileFormat.ClearingNrOrNull(account);
            Assert.AreEqual(expectedResult, result);
        }

        [DataTestMethod]
        [DataRow(BankAccountNumberTypeCode.PlusGiroSe, "Test ostrukturerat meddelande plusgiro", 35)]
        [DataRow(BankAccountNumberTypeCode.PlusGiroSe, "Test ostrukturerat meddelande plusgiro väldigt extremt superlångt", 35)]
        [DataRow(BankAccountNumberTypeCode.PlusGiroSe, "Kortare än 35 tecken", 20)]
        [DataRow(BankAccountNumberTypeCode.BankGiroSe, "Kortare än 50 tecken", 20)]
        [DataRow(BankAccountNumberTypeCode.BankGiroSe, "Den här texten är 40 tecken lång just nu", 40)]
        [DataRow(BankAccountNumberTypeCode.BankGiroSe, "Längre än 50 tecken som är max för bankgiro enligt docs", 50)]
        public void GetLimitedUnstructuredMessage_ClipIfLonger_ShouldOnlyClipIfLongerThanAllowed(BankAccountNumberTypeCode typeCode, string msg, int expectedLength)
        {
            var parser = new BankAccountNumberParser("SE");

            var payment = new OutgoingPaymentFileFormat_SUS_SE.SwedbankPayment
            {
                PaymentId = "abc123",
                ToBankAccount = parser.ParseBankAccount("2222222", typeCode),
                Amount = 25000.0M,
                UnstructuredMessage = msg
            };

            var fileFormat = new OutgoingPaymentFileFormat_SUS_SE(false, null, DateTime.Now);

            var result = fileFormat.GetLimitedUnstructuredMessage(payment);

            Assert.AreEqual(msg.Substring(0, expectedLength), result);
        }

    }
}
