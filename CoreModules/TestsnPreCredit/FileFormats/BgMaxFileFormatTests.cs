using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Banking.IncomingPaymentFiles;
using nTest.RandomDataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestsnPreCredit.FileFormats
{
    [TestClass]
    public class BgMaxFileFormatTests
    {
        private const string ExampleBgMaxFile1 =
@"01BGMAX               0120160714173035010331P	                                
050009912346          SEK                                                       
20000000000008221230                 00000000000001000024                       
20000000000008221231                 00000000000002000024                       
20000000000008222232                 00000000000003000024                       
20000000000008230303                 00000000000001000024                       
15000000000000000000058410000010098232016071400036000000000000070000SEK00000004 
7000000004000000000000000000000001                                              
";

        private void WithStream(string fileData, Action<MemoryStream> a)
        {
            var ms = new MemoryStream(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(fileData));
            a(ms);
        }

        [TestMethod]
        public void ParseStructured()
        {
            IncomingPaymentFile pf;
            string errMsg;

            var format = new IncomingPaymentFileFormat_BgMax(false, true);

            WithStream(ExampleBgMaxFile1, fileStream =>
            {
                Assert.IsTrue(format.TryParse(fileStream.ToArray(), out pf, out errMsg), errMsg);
                Assert.AreEqual(new DateTime(2016, 7, 14, 17, 30, 35), pf.ExternalCreationDate);
                Assert.AreEqual("8221230", pf.Accounts[0].DateBatches[0].Payments[0].OcrReference);
                Assert.AreEqual(700m, pf.Accounts[0].DateBatches[0].Payments.Sum(x => x.Amount));
            });
        }

        [TestMethod]
        public void ParseRaw()
        {
            WithStream(ExampleBgMaxFile1, fileStream =>
            {
                IncomingPaymentFileFormat_BgMax.RawFile f;
                string errorMessage;
                var result = IncomingPaymentFileFormat_BgMax.RawFile.TryParse(fileStream, out f, out errorMessage);
                Assert.IsTrue(result);
                Assert.AreEqual(1, f.sections.Count);
                Assert.AreEqual(4, f.sections.SelectMany(x => x.payments).Count());
            });
        }

        [TestMethod]
        public void CanParseGeneratedTestFile()
        {
            var now = new DateTime(2016, 7, 14, 17, 30, 35);
            var pf = new TestPaymentFileCreator();
            var fileBytes = pf.Create_BgMax_File(now, new List<TestPaymentFileCreator.Payment>
            {
                new TestPaymentFileCreator.Payment
                {
                    Amount = 1200.99m,
                    BookkeepingDate = now.AddDays(-1).Date,
                    OcrReference = "996612"
                },
                new TestPaymentFileCreator.Payment
                {
                    Amount = 500m,
                    BookkeepingDate = now.AddDays(-2).Date,
                    OcrReference = "88543"
                }
            }, "9020033");

            var format = new IncomingPaymentFileFormat_BgMax(false, true);
            IncomingPaymentFile f;
            string errorMessage;
            Assert.IsTrue(format.TryParse(fileBytes, out f, out errorMessage), errorMessage);

            Assert.AreEqual(now, f.ExternalCreationDate);
            var account = f.Accounts.Single();
            Assert.AreEqual("9020033", account.AccountNr.NormalizedValue);

            var d1 = account.DateBatches.First();
            Assert.AreEqual(now.AddDays(-1).Date, d1.BookKeepingDate);
            Assert.AreEqual(1200.99m, d1.Payments.Single().Amount);
            Assert.AreEqual("996612", d1.Payments.Single().OcrReference);

            var d2 = account.DateBatches.Skip(1).First();
            Assert.AreEqual(now.AddDays(-2).Date, d2.BookKeepingDate);
            Assert.AreEqual(500m, d2.Payments.Single().Amount);
            Assert.AreEqual("88543", d2.Payments.Single().OcrReference);
        }
    }
}
