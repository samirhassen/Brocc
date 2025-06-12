using Microsoft.VisualStudio.TestTools.UnitTesting;
using nSavings.Code.Services;
using NTech;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TestsnPreCredit.Savings
{
    [TestClass]
    public class SavingsYearlySummaryTests
    {
        private class HistoryBuilder
        {
            private int eventId = 0;

            private List<YearlySummaryService.EventModel> events = new List<YearlySummaryService.EventModel>();
            private YearlySummaryService.EventModel currentEvent;

            public static HistoryBuilder New()
            {
                return new HistoryBuilder();
            }

            public HistoryBuilder T(decimal amount, string accountTypeCode, string bookKeepingDate)
            {
                currentEvent.Transactions.Add(new YearlySummaryService.TransactionModel
                {
                    AccountTypeCode = accountTypeCode,
                    Amount = amount,
                    BookKeepingDate = Dates.ParseDateTimeExactOrNull(bookKeepingDate, "yyyy-MM-dd").Value,
                    BusinessEventId = currentEvent.BusinessEventId,
                    BusinessEventTypeCode = currentEvent.BusinessEventTypeCode
                });
                return this;
            }

            public HistoryBuilder E(string businessEventTypeCode, string transactionDate)
            {
                currentEvent = new YearlySummaryService.EventModel
                {
                    BusinessEventId = ++eventId,
                    BusinessEventTypeCode = businessEventTypeCode,
                    TransactionDate = Dates.ParseDateTimeExactOrNull(transactionDate, "yyyy-MM-dd").Value,
                    Transactions = new List<YearlySummaryService.TransactionModel>()
                };
                events.Add(currentEvent);
                return this;
            }

            public List<YearlySummaryService.EventModel> Events()
            {
                return events;
            }
        }

        private YearlySummaryService CreateService(DateTime? now = null)
        {
            var clock = new StrictMock<IClock>();
            if (now.HasValue)
                clock.Setup(x => x.Today).Returns(now.Value.Date);
            return new YearlySummaryService(() => throw new NotImplementedException(),
                () => throw new NotImplementedException(),
                () => throw new NotImplementedException(), clock.Object,
                CultureInfo.InvariantCulture);
            ;
        }

        private void AssertSummary(List<YearlySummaryService.EventModel> events, int year,
            YearlySummaryService.SummaryDataModel expected)
        {
            YearlySummaryService s = CreateService(now: new DateTime(year + 1, 1, 1));
            YearlySummaryService.SummaryDataModel actual = s.ComputeSummary(events, year);

            string Desc() =>
                $"BalanceAfterAmount={actual?.BalanceAfterAmount} vs {expected?.BalanceAfterAmount}, TotalInterestAmount={actual?.TotalInterestAmount} vs {expected?.TotalInterestAmount}, WithheldTaxAmount={actual?.WithheldTaxAmount} vs {expected?.WithheldTaxAmount}";

            Assert.AreEqual(expected?.BalanceAfterAmount, actual?.BalanceAfterAmount, Desc());
            Assert.AreEqual(expected?.TotalInterestAmount, actual?.TotalInterestAmount, Desc());
            Assert.AreEqual(expected?.WithheldTaxAmount, actual?.WithheldTaxAmount, Desc());
        }

        private void AssertYears(List<YearlySummaryService.EventModel> events, params int[] expectedYears)
        {
            YearlySummaryService s =
                CreateService(now: expectedYears.Any() ? new DateTime(expectedYears.Max() + 1, 1, 1) : new DateTime?());
            var actualYears = s.GetAllYearsWithSummaries(events);
            CollectionAssert.AreEqual(expectedYears, actualYears.ToArray());
        }

        [TestMethod]
        public void NewlyOpenedAccount()
        {
            var events = HistoryBuilder
                .New()
                .Events();

            AssertSummary(events, 2016, null);
            AssertYears(events);
        }

        [TestMethod]
        public void ClosedWithoutAnyTransactions()
        {
            var events = HistoryBuilder
                .New()
                .E("AccountClosure", "2016-05-23")
                .Events();

            AssertSummary(events, 2016,
                new YearlySummaryService.SummaryDataModel
                {
                    BalanceAfterAmount = 0, TotalInterestAmount = 0, WithheldTaxAmount = 0
                });
            AssertYears(events, 2016);
        }

        [TestMethod]
        public void ClosedDuringYear()
        {
            var events = HistoryBuilder
                .New()
                .E("IncomingPaymentFileImport", "2016-03-12")
                .T(100, "Capital", "2016-03-11")
                .E("AccountClosure", "2016-05-23")
                .T(10, "Capital", "2016-05-23") //Interest
                .T(10, "CapitalizedInterest", "2016-05-23")
                .T(3, "WithheldCapitalizedInterestTax", "2016-05-23") //Withheld tax
                .T(-3, "Capital", "2016-05-23")
                .T(-107, "Capital", "2016-05-23") //Withdrawal                    
                .Events();

            AssertSummary(events, 2016,
                new YearlySummaryService.SummaryDataModel
                {
                    BalanceAfterAmount = 0, TotalInterestAmount = 10, WithheldTaxAmount = 3
                });
            AssertYears(events, 2016);
        }

        [TestMethod]
        public void NormalCapitalization()
        {
            var events = HistoryBuilder
                .New()
                .E("IncomingPaymentFileImport", "2016-03-12")
                .T(100, "Capital", "2016-03-11")
                .E("YearlyInterestCapitalization", "2017-01-02")
                .T(10, "Capital", "2016-12-31") //Interest
                .T(10, "CapitalizedInterest", "2016-12-31")
                .T(3, "WithheldCapitalizedInterestTax", "2016-12-31") //Withheld tax
                .T(-3, "Capital", "2016-12-31")
                .Events();

            AssertSummary(events, 2016,
                new YearlySummaryService.SummaryDataModel
                {
                    BalanceAfterAmount = 107, TotalInterestAmount = 10, WithheldTaxAmount = 3
                });
            AssertYears(events, 2016);
        }

        [TestMethod]
        public void CapitalizeAndCloseOnTheSameDay()
        {
            var events = HistoryBuilder
                .New()
                .E("IncomingPaymentFileImport", "2016-03-12")
                .T(100, "Capital", "2016-03-11")
                .E("YearlyInterestCapitalization", "2017-01-01")
                .T(10, "Capital", "2016-12-31") //Interest
                .T(10, "CapitalizedInterest", "2016-12-31")
                .T(3, "WithheldCapitalizedInterestTax", "2016-12-31") //Withheld tax
                .T(-3, "Capital", "2016-12-31")
                .E("AccountClosure", "2017-01-01")
                .T(-107, "Capital", "2017-01-01") //Withdrawal
                .Events();

            AssertSummary(events, 2016,
                new YearlySummaryService.SummaryDataModel
                {
                    BalanceAfterAmount = 107, TotalInterestAmount = 10, WithheldTaxAmount = 3
                });
            AssertSummary(events, 2017,
                new YearlySummaryService.SummaryDataModel
                {
                    BalanceAfterAmount = 0, TotalInterestAmount = 0, WithheldTaxAmount = 0
                });
            AssertYears(events, 2017, 2016);
        }
    }
}