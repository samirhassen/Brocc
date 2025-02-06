using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;
using static NTech.Core.Credit.Shared.Services.MortgageLoanAverageInterstRateReportService;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Reports
{
    public class AverageInterestReportTests
    {
        private const decimal OneMillion = 1000000m;

        [Test]
        public void TestReport()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.Now = new DateTimeOffset(2022, 1, 1, support.Now.Hour, support.Now.Minute, support.Now.Second, support.Now.Offset);

                CreditsMlStandard.SetupFixedInterestRates(support, new Dictionary<int, decimal>
                {
                    { 3, 0.1m },
                    { 12, 0m }
                });

                TestContext.WriteLine("2022-01");
                CreditsMlStandard.RunOneMonth(support,
                    //No loans existed last month
                    doOnFirstOfMonth: () => CheckLastMonth(support, 0),
                    doAfterTerminationLetters: () =>
                    {
                        CreditsMlStandard.CreateCredit(support, 1, loanAmount: OneMillion, referenceInterestRatePercent: 0m, marginInterestRatePercent: 2m, interestRebindMounthCount: 3);
                        CreditsMlStandard.CreateCredit(support, 2, loanAmount: 2 * OneMillion, referenceInterestRatePercent: 0m, marginInterestRatePercent: 3m, interestRebindMounthCount: 3);
                        CreditsMlStandard.CreateCredit(support, 3, loanAmount: OneMillion, referenceInterestRatePercent: 0m, marginInterestRatePercent: 2m, interestRebindMounthCount: 12);
                    });

                TestContext.WriteLine("2022-02");
                CreditsMlStandard.RunOneMonth(support,
                    //Three loans created last month so both should be included
                    doOnFirstOfMonth: () =>
                    {
                        var result = CheckLastMonth(support, 2,
                            (RebindMonthCount: 3, ExpectedAverageInterestRatePercent: 2.67m),
                            (RebindMonthCount: 12, ExpectedAverageInterestRatePercent: 2m));

                        var creditNr1 = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;
                        Assert.That(
                            result.AllIncludedCredits.SingleOrDefault(x => x.CreditNr == creditNr1)?.CapitalBalance,
                            Is.EqualTo(OneMillion));
                    });

                TestContext.WriteLine("2022-03");
                CreditsMlStandard.RunOneMonth(support,
                    //Three loans exist but all are in a binding period so none should be included
                    doOnFirstOfMonth: () => CheckLastMonth(support, 0));

                TestContext.WriteLine("2022-04");
                CreditsMlStandard.RunOneMonth(support,
                    doOnFirstOfMonth: () => CheckLastMonth(support, 0));

                TestContext.WriteLine("2022-05");
                CreditsMlStandard.RunOneMonth(support,
                    //Rebinding of the 3 month loans happend last month
                    doOnFirstOfMonth: () => CheckLastMonth(support, 1,
                        (RebindMonthCount: 3, ExpectedAverageInterestRatePercent: 2.77m)));
            });
        }

        private (List<AverageInterestRateItem> AverageRates, List<CreditItem> AllIncludedCredits) CheckLastMonth(MlStandardTestRunner.TestSupport support, int expectedAverageCount, params (int RebindMonthCount, decimal? ExpectedAverageInterestRatePercent)[] expectedRates)
        {
            var forDate = support.Clock.Today.AddMonths(-1);
            var result = new MortgageLoanAverageInterstRateReportService(support.CreateCreditContextFactory()).GetAverageInterestRates(forDate);
            foreach (var expectedRate in expectedRates)
            {
                var actualRate = result.AverageRates.SingleOrDefault(x => x.RebindMonthCount == expectedRate.RebindMonthCount);
                if (expectedRate.ExpectedAverageInterestRatePercent.HasValue)
                {
                    Assert.That(actualRate?.AverageInterestRatePercent, Is.EqualTo(expectedRate.ExpectedAverageInterestRatePercent.Value));
                }
                else
                {
                    Assert.That(actualRate, Is.Null, $"Expected no rate for {expectedRate.RebindMonthCount} but got {actualRate?.AverageInterestRatePercent}%");
                }
            }
            Assert.That(result.AverageRates.Count, Is.EqualTo(expectedAverageCount));
            TestContext.WriteLine(JsonConvert.SerializeObject(result.AverageRates, Formatting.Indented));

            return result;
        }
    }
}
