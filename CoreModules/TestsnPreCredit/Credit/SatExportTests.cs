using Microsoft.VisualStudio.TestTools.UnitTesting;
using nCredit.Code.Sat;
using nCredit.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestsnPreCredit.Credit
{
    [TestClass]
    public class SatExportTests
    {
        private SatRepository.ActiveCredit CreateTestCredit(Action<SatRepository.ActiveCredit> alterCredit, DateTime now)
        {
            var c = new SatRepository.ActiveCredit
            {
                StartDate = new DateTimeOffset(now.AddMonths(-30)),
                CapitalDebtCaptialBalance = 16451m,
                AnnuityAmount = 264m,
                CreditNr = "L4242",
                CustomerIds = new int[] { 42 },
                NrOfDaysOverdue = 0,
                TotalOpenNotificationInterestBalance = 0m
            };
            alterCredit?.Invoke(c);
            return c;
        }

        [TestMethod]
        public void H15_Date()
        {
            var now = new DateTime(2018, 10, 23);
            Func<Action<SatRepository.ActiveCredit>, Dictionary<int, SatExportItem>> run = alterCredit =>
                {
                    var incomes = new Dictionary<int, Tuple<decimal, DateTime>> { { 42, Tuple.Create(3400m, new DateTime(2017, 7, 1)) } };
                    var civicRegNrs = new Dictionary<int, string> { { 42, "291282-3605" } };
                    var c = CreateTestCredit(alterCredit, now);
                    return SatRepository.GetSatExportItems(new List<SatRepository.ActiveCredit> { c }, incomes, civicRegNrs, now);
                };

            var r1 = run(c => c.StartDate = now.AddMonths(-3));
            Assert.AreEqual(new DateTime(2018, 7, 23), r1.Single().Value.ItemK11);
            Assert.AreEqual(1, r1.Single().Value.ItemH15);

            var r2 = run(c => c.StartDate = now.AddMonths(-13));
            Assert.AreEqual(new DateTime(2017, 9, 23), r2.Single().Value.ItemK11);
            Assert.AreEqual(0, r2.Single().Value.ItemH15);
        }

        [TestMethod]
        public void K11_K12_Dates()
        {
            var now = new DateTime(2018, 10, 23);
            Func<Action<SatRepository.ActiveCredit>, SatRepository.ActiveCredit> createCredit = alterCredit => CreateTestCredit(alterCredit, now);
            var incomes = new Dictionary<int, Tuple<decimal, DateTime>> { { 42, Tuple.Create(3400m, new DateTime(2017, 7, 1)) } };
            var civicRegNrs = new Dictionary<int, string> { { 42, "291282-3605" } };

            var credits = new List<SatRepository.ActiveCredit>
            {
                createCredit(x => x.StartDate = now.AddMonths(-2)),
                createCredit(x => x.StartDate = now.AddMonths(-1)),
                createCredit(x => x.StartDate = now.AddMonths(-4)),
                createCredit(x => x.StartDate = now.AddMonths(-3))
            };

            var r = SatRepository.GetSatExportItems(credits, incomes, civicRegNrs, now);

            Assert.AreEqual(new DateTime(2018, 9, 23), r.Single().Value.ItemK11);
            Assert.AreEqual(new DateTime(2018, 6, 23), r.Single().Value.ItemK12);
        }

        [TestMethod]
        public void D11_D12()
        {
            var now = new DateTime(2018, 10, 23);
            Func<Action<SatRepository.ActiveCredit>, SatRepository.ActiveCredit> createCredit = alterCredit => CreateTestCredit(alterCredit, now);
            var incomes = new Dictionary<int, Tuple<decimal, DateTime>> { { 42, Tuple.Create(3400m, new DateTime(2017, 7, 1)) } };
            var civicRegNrs = new Dictionary<int, string> { { 42, "291282-3605" } };

            var credits = new List<SatRepository.ActiveCredit>
            {
                createCredit(x => x.CapitalDebtCaptialBalance = 100m),
                createCredit(x => x.CapitalDebtCaptialBalance = 42m)
            };

            var r = SatRepository.GetSatExportItems(credits, incomes, civicRegNrs, now);
            var v = r.Single().Value;

            Assert.AreEqual(2, v.Count);
            Assert.AreEqual(2, v.ItemD11);

            Assert.AreEqual(142m, v.ItemC01);
            Assert.AreEqual(142m, v.ItemD12);
        }

        [TestMethod]
        public void T11_T12()
        {
            //t11 Sum of gross annual income, disclosed by the debtor t12 The date of disclosing of gross annual income
            var now = new DateTime(2018, 10, 23);
            Func<Action<SatRepository.ActiveCredit>, SatRepository.ActiveCredit> createCredit = alterCredit => CreateTestCredit(alterCredit, now);
            var incomes = new Dictionary<int, Tuple<decimal, DateTime>> { { 42, Tuple.Create(100m, new DateTime(2017, 7, 1)) } };
            var civicRegNrs = new Dictionary<int, string> { { 42, "291282-3605" } };

            var credits = new List<SatRepository.ActiveCredit> { createCredit(null) };

            var r = SatRepository.GetSatExportItems(credits, incomes, civicRegNrs, now);
            var v = r.Single().Value;

            Assert.AreEqual(100m * 12.5m, v.ItemT11);
            Assert.AreEqual(new DateTime(2017, 7, 1), v.ItemT12);
        }
    }
}
