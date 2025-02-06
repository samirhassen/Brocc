using Microsoft.VisualStudio.TestTools.UnitTesting;
using nPreCredit.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit
{
    [TestClass]
    public class ReportingPercentageTests
    {
        [TestMethod]
        public void PercentageDist_ZeroDecimals()
        {
            var decimals = new List<decimal>()
            {
                13.626332m,
                47.989636m,
                 9.596008m,
                28.788024m,
            };
            var p = new ReportingPercentageHelper();
            var pp = p.GetRoundedListThatSumsCorrectly(decimals, 0);
            foreach (var v in decimals.Zip(pp, (a, b) => new { a, b }))
            {
                Console.WriteLine($"{v.a} -> {v.b}");
            }
            Console.WriteLine($"{decimals.Sum()} -> {pp.Sum()}");

            Assert.AreEqual(14m, pp[0]);
            Assert.AreEqual(48m, pp[1]);
            Assert.AreEqual(9m, pp[2]);
            Assert.AreEqual(29m, pp[3]);
        }

        [TestMethod]
        public void PercentageDist_OneDecimal()
        {
            var decimals = new List<decimal>()
            {
                13.626332m,
                47.989636m,
                 9.596008m,
                28.788024m,
            };
            var p = new ReportingPercentageHelper();
            var pp = p.GetRoundedListThatSumsCorrectly(decimals, 1);
            foreach (var v in decimals.Zip(pp, (a, b) => new { a, b }))
            {
                Console.WriteLine($"{v.a} -> {v.b}");
            }
            Console.WriteLine($"{decimals.Sum()} -> {pp.Sum()}");

            Assert.AreEqual(13.6m, pp[0]);
            Assert.AreEqual(48m, pp[1]);
            Assert.AreEqual(9.6m, pp[2]);
            Assert.AreEqual(28.8m, pp[3]);
        }
    }
}