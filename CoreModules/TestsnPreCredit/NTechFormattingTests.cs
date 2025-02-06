using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure;
using System;

namespace TestsnPreCredit
{
    [TestClass]
    public class NTechFormattingTests
    {
        [TestMethod]
        public void CurrencyThousandSeparators()
        {
            var se = NTechFormatting.GetPrintFormattingCulture("sv-SE");
            var fi = NTechFormatting.GetPrintFormattingCulture("fi-FI");

            const decimal Amount = 12345.67m;
            Assert.AreEqual("12 345,67 €", Amount.ToString("C", fi));
            Assert.AreEqual("12 345,67 kr", Amount.ToString("C", se));
        }

        [TestMethod]
        public void NonCurrencyThousandSeparators()
        {
            var se = NTechFormatting.GetPrintFormattingCulture("sv-SE");
            var fi = NTechFormatting.GetPrintFormattingCulture("fi-FI");

            const decimal Amount = 12345.67m;
            Assert.AreEqual("12 345,67", Amount.ToString("N", fi));
            Assert.AreEqual("12 345,67", Amount.ToString("N", se));
        }

        [TestMethod]
        public void MonthFormatting()
        {
            var se = NTechFormatting.GetPrintFormattingCulture("sv-SE");
            var fi = NTechFormatting.GetPrintFormattingCulture("fi-FI");

            var m = new DateTime(2018, 11, 22, 23, 59, 0);
            Assert.AreEqual("2018.11", NTechFormatting.FormatMonth(m, fi));
            Assert.AreEqual("2018-11", NTechFormatting.FormatMonth(m, se));
        }
    }
}
