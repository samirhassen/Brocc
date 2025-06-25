using Microsoft.VisualStudio.TestTools.UnitTesting;
using nSavings.DbModel.BusinessEvents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TestsnPreCredit.Savings
{
    [TestClass]
    public class SavingsTests
    {
        [TestMethod]
        public void RunningTotalCache_Empty()
        {
            var items = new List<Tuple<DateTime, decimal>>();
            var r = RunningTotalCache.Create(items, x => x.Item1, x => x.Item2, false);
            Assert.AreEqual(0m, r.GetRunningTotal(new DateTime(2016, 12, 11)));
            Assert.AreEqual(0m, r.GetRunningTotal(new DateTime(2016, 12, 16)));
        }

        [TestMethod]
        public void RunningTotalCache_BasicUsecase()
        {
            var items = new List<Tuple<DateTime, decimal>>
            {
                Tuple.Create(new DateTime(2016, 12, 12), 1m),
                Tuple.Create(new DateTime(2016, 12, 16), 10m),
                Tuple.Create(new DateTime(2016, 12, 12), 2m)
            };

            var r = RunningTotalCache.Create(items, x => x.Item1, x => x.Item2, false);
            Assert.AreEqual(0m, r.GetRunningTotal(new DateTime(2016, 12, 11)));
            Assert.AreEqual(3m, r.GetRunningTotal(new DateTime(2016, 12, 12)));
            Assert.AreEqual(3m, r.GetRunningTotal(new DateTime(2016, 12, 13)));
            Assert.AreEqual(3m, r.GetRunningTotal(new DateTime(2016, 12, 15)));
            Assert.AreEqual(13m, r.GetRunningTotal(new DateTime(2016, 12, 16)));
            Assert.AreEqual(13m, r.GetRunningTotal(new DateTime(2016, 12, 17)));
        }

        [TestMethod]
        public void LatestItemCache_BasicUsecase()
        {
            var items = new List<Tuple<DateTime, decimal>>()
            {
                Tuple.Create(new DateTime(2016, 12, 12), 1m),
                Tuple.Create(new DateTime(2016, 12, 16), 10m),
                Tuple.Create(new DateTime(2016, 12, 12), 2m)
            };

            var c = LatestItemCache.Create<Tuple<DateTime, decimal>, decimal>(items, x => x.Item1, x => x.Item2, false);

            Assert.AreEqual(null, c.GetCurrentValue(new DateTime(2016, 12, 10)));
            Assert.AreEqual(2m, c.GetCurrentValue(new DateTime(2016, 12, 12)));
            Assert.AreEqual(2m, c.GetCurrentValue(new DateTime(2016, 12, 15)));
            Assert.AreEqual(10m, c.GetCurrentValue(new DateTime(2016, 12, 16)));
            Assert.AreEqual(10m, c.GetCurrentValue(new DateTime(2016, 12, 17)));
        }

        [TestMethod]
        public void ExternalArticleTestCase1()
        {
            //http://www.finanstidningen.biz/index.php/finansartiklar/bors-och-marknad/128-hur-beraknas-rantan-pa-bankkontot
            var capitalTransactions =
                new List<YearlyInterestCapitalizationBusinessEventManager.InputModel.CapitalTransactionModel>();

            Deposit(new DateTime(2007, 12, 31), 10000m);
            Deposit(new DateTime(2008, 1, 1), 10000m);
            Withdrawal(new DateTime(2008, 1, 4), 5000m);
            Deposit(new DateTime(2008, 1, 6), 10000m);
            Deposit(new DateTime(2008, 1, 10), 10000m);
            Withdrawal(new DateTime(2008, 1, 13), 15000m);
            Deposit(new DateTime(2008, 1, 16), 20000m);
            Withdrawal(new DateTime(2008, 1, 18), 4000m);
            Deposit(new DateTime(2008, 1, 20), 10000m);
            Withdrawal(new DateTime(2008, 1, 20), 10000m);
            Deposit(new DateTime(2008, 1, 23), 20000m);
            Withdrawal(new DateTime(2008, 1, 27), 40000m);

            var accounts = new List<YearlyInterestCapitalizationBusinessEventManager.InputModel>
            {
                new YearlyInterestCapitalizationBusinessEventManager.InputModel
                {
                    SavingsAccountNr = "1",
                    CreatedByBusinessEventId = 42,
                    LatestCapitalizationInterestFromDate = new DateTime?(),
                    OrderedCapitalTransactions = capitalTransactions.OrderBy(x => x.InterestFromDate).ToList(),
                    OrderedInterestRateChanges =
                        new List<YearlyInterestCapitalizationBusinessEventManager.InputModel.InterestRateChangeModel>()
                        {
                            new YearlyInterestCapitalizationBusinessEventManager.InputModel.InterestRateChangeModel
                            {
                                InterestRatePercent = 4m, ValidFromDate = new DateTime(2008, 1, 1),
                            },
                            new YearlyInterestCapitalizationBusinessEventManager.InputModel.InterestRateChangeModel
                            {
                                InterestRatePercent = 12m,
                                ValidFromDate = new DateTime(2008, 1, 15),
                                AppliesToAccountsSinceBusinessEventId = 43
                            }
                        }
                }
            };

            var ok = YearlyInterestCapitalizationBusinessEventManager.TryComputeInterestRateUntilDate(accounts,
                new DateTime(2008, 1, 31), true,
                out IDictionary<string, YearlyInterestCapitalizationBusinessEventManager.ResultModel> result,
                out string failedMessage);
            Assert.IsTrue(ok, failedMessage);
            //Console.WriteLine(JsonConvert.SerializeObject(result.Values.Single(), Formatting.Indented));

            var c = CultureInfo.InvariantCulture;

            foreach (var item in result.Values.Single().SummarizeParts())
            {
                var fromDate = item.Min(x => x.Date);
                var toDate = item.Max(x => x.Date);
                var f = item.First();
                Console.WriteLine(
                    $"{fromDate.ToString("yyyy-MM-dd")}{(fromDate == toDate ? "" : "-" + toDate.ToString("yyyy-MM-dd"))}{(f.IsLeapYear ? "L" : "N")}: {item.Count()} x {f.AccountBalance.ToString("F2", c)} x {(f.AccountInterestRatePercent.HasValue ? (f.AccountInterestRatePercent.Value / 100m).ToString("P") : "-")} =>  {f.Amount.ToString("F2", c)}");
            }

            Assert.AreEqual(91.69m, result.Values.Single().TotalInterestAmount);
            return;

            void Withdrawal(DateTime d, decimal am) => capitalTransactions.Add(
                new YearlyInterestCapitalizationBusinessEventManager.InputModel.CapitalTransactionModel
                {
                    Amount = -am, InterestFromDate = d
                });

            void Deposit(DateTime d, decimal am) => capitalTransactions.Add(
                new YearlyInterestCapitalizationBusinessEventManager.InputModel.CapitalTransactionModel
                {
                    Amount = am, InterestFromDate = d.AddDays(1)
                });
        }
    }
}