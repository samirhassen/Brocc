using nCredit;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Rse
{
    public class MlStandardRseTests
    {
        [Test]
        public void TestRseVsKonsumenterna()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.Now = new DateTime(2022, 1, 1);

                const decimal intestRatePercent = 10m;
                const decimal loanAmount = 1000000m;
                const int bindingMonthCount = 5 * 12; // 5 years
                var credit = CreditsMlStandard.CreateCredit(
                         support: support,
                         creditNumber: 1,
                         loanAmount: loanAmount,
                         referenceInterestRatePercent: intestRatePercent,
                         marginInterestRatePercent: 0m,
                         interestRebindMounthCount: bindingMonthCount);

                var rseService = new SwedishMortgageLoanRseService(support.CreateCreditContextFactory(), support.GetNotificationProcessSettingsFactory(),
                    support.Clock, support.ClientConfiguration, support.CreditEnvSettings);


                void CheckRse(decimal expectedRseAmount, decimal rseComparisonInterestRatePercent, decimal konsumenternasValue, bool debug = false)
                {
                    var result = rseService!.CalculateRseForCredit(new RseForCreditRequest
                    {
                        ComparisonInterestRatePercent = rseComparisonInterestRatePercent,
                        CreditNr = credit!.CreditNr
                    });

                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        var creditD = CreditDomainModel.PreFetchForSingleCredit(credit.CreditNr, context, support.CreditEnvSettings);
                        var model = AmortizationPlan.GetHistoricalCreditModel(credit.CreditNr, context, true);
                        var lastDueDate = context.CreditNotificationHeadersQueryable.Where(x => x.ClosedTransactionDate != null).Max(x => (DateTime?)x.DueDate);

                        Console.WriteLine();
                        Console.WriteLine($"------ {support.Clock.Today:yyyy-MM-dd} ------");
                        Console.WriteLine("Konsumenterna input:");
                        Console.WriteLine($"Räntan på ditt lån: {creditD.GetInterestRatePercent(support.Clock.Today) / 100m:P}");
                        Console.WriteLine($"Kvarvarande skuld: {creditD.GetNotNotifiedCapitalBalance(support.Clock.Today):C}");
                        Console.WriteLine($"Amortering per månad: {creditD.GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.MonthlyAmortizationAmount):C}");
                        Console.WriteLine($"När är lånets sista betalningstillfälle?: {lastDueDate:yyyy-MM-dd}");
                        Console.WriteLine($"När går bindningstiden för lånet ut?: {creditD.GetDatedCreditDate(support.Clock.Today, DatedCreditDateCode.MortgageLoanNextInterestRebindDate, null):yyyy-MM-dd}");
                        Console.WriteLine($"När ska du lösa lånet?: {support.Clock.Today:yyyy-MM-dd}");
                    }

                    if (debug)
                    {
                        Console.WriteLine(JsonConvert.SerializeObject(result.Rse, Formatting.Indented));
                    }

                    var diffVsKonsumenterna = (result.Rse?.RseAmount ?? 0m) - konsumenternasValue;
                    Console.WriteLine($"Diff vs konsumenterna: {diffVsKonsumenterna}");
                    Assert.That(result.Rse?.RseAmount ?? 0m, Is.EqualTo(expectedRseAmount));

                    if (Math.Abs(diffVsKonsumenterna) / konsumenternasValue > 0.1m)
                    {
                        Assert.Fail($"Diff vs konsumenterna > 10% for {support.Clock.Today:yyyy-MM-dd}");
                    }
                }

                void RunOneMonth(Action<int>? afterDay = null)
                {
                    CreditsMlStandard.RunOneMonth(support, beforeDay: day =>
                    {
                        if (day == 28)
                        {
                            Credits.PayOverdueNotifications(support);
                        }
                    }, afterDay: afterDay);
                }

                //2022-01
                RunOneMonth();

                //2022-02
                RunOneMonth(afterDay: day =>
                {
                    if (day == 17) //2022-02-17
                    {

                        CheckRse(
                            expectedRseAmount: 337416.81m,
                            rseComparisonInterestRatePercent: 2.2273m,
                            konsumenternasValue: 334441m,
                            debug: false);

                    }
                });
                for (int i = 0; i < 10; i++)
                {
                    RunOneMonth();
                }
                RunOneMonth(afterDay: day =>
                {
                    if (day == 3)
                    {
                        CheckRse(
                            expectedRseAmount: 190768.77m,
                            rseComparisonInterestRatePercent: 4.4620m,
                            konsumenternasValue: 185382m,
                            debug: false);

                    }
                    else if (day == 16)
                    {
                        CheckRse(
                            expectedRseAmount: 201878.44m,
                            rseComparisonInterestRatePercent: 4.117m,
                            konsumenternasValue: 201400m,
                            debug: false);
                    }
                });
            });
        }
    }
}
