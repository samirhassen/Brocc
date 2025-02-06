using Moq;
using nCredit;
using nCredit.Code.Services;
using nCredit.DomainModel;
using nCredit.Excel;
using Newtonsoft.Json;
using NTech.Core.Host.IntegrationTests.UlStandard;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Core.Host.IntegrationTests
{
    public partial class UlStandardLikeScenarioTests
    {
        private void F818_QuarterlyReport(UlStandardTestRunner.TestSupport support)
        {
            HashSet<string> creditNrs = new HashSet<string>();

            SwedishQuarterlyF818ReportService.ReportData GetReportData(Quarter quarter, bool renderReport = false)
            {
                var documentClient = new Mock<IDocumentClient>(MockBehavior.Strict);
                documentClient.Setup(x => x.CreateXlsx(It.IsAny<DocumentClientExcelRequest>())).Returns(new MemoryStream());
                var reportService = new SwedishQuarterlyF818ReportService(documentClient.Object, support.CreateCreditContextFactory());

                var data = reportService.GetReportData(quarter, onlyTheseCreditNrs: creditNrs);
                if (renderReport)
                    Assert.That(reportService.CreateReport(data).DownloadFilename, Is.Not.Null); //Just make sure the excel bit doesnt blow up
                return data;
            }

            int creditNumber = 3;
            const decimal DefaultNotificationFeeAmount = 20m;
            CreditHeader AddCredit(Action<CreditsUlStandard.CreateCreditOptions>? editOptions = null)
            {
                var options = new CreditsUlStandard.CreateCreditOptions
                {
                    AnnuityAmount = 300m,
                    PaidToCustomerAmount = 1000m,
                    SettledAmount = 1000m,
                    MarginInterestRatePercent = 9m,
                    ReferenceInterestRatePercent = 1m,
                    WithheldInitialFeeAmount = 200m,
                    NotificationFeeAmount = DefaultNotificationFeeAmount
                };
                editOptions?.Invoke(options);
                var credit = CreditsUlStandard.CreateCredit(support, creditNumber++, options: options);
                creditNrs.Add(credit.CreditNr);
                return credit;
            }
            SwedishQuarterlyF818ReportService.ReportSummary? lastQuarterSummary = null;

            //Simulate some events in the quarter, move to the next quarter, grab the report for the previous quarter and run test
            void SimulateQuarter(Action simulateEvents,
                bool renderReport = false,
                decimal? assertTotalEndQuarterBalanceR1 = null,
                decimal? assertPaidOutDuringQuarterR2 = null,
                int? assertTotalActiveCreditCountR4 = null,
                int? assertNrOfNewCreditsDuringQuarterR5 = null,
                int? assertNrOfActiveCustomersR6 = null,
                decimal? assertPaidInterestDuringQuarterR7 = null,
                decimal? assertPaidFeesDuringQuarterR8 = null,
                int? assertNrOfImpairedCreditsR9 = null,
                int? assertNrOfNewImpairedCreditsDuringQuarterR10 = null
                )
            {
                simulateEvents();

                support.MoveToNextQuarter();
                var previousQuarter = support.CurrentQuarter.GetPrevious();

                var data = GetReportData(previousQuarter, renderReport: renderReport);
                var summary = data.Summary;

                try
                {
                    if (assertTotalEndQuarterBalanceR1.HasValue)
                        Assert.That(summary.CurrentCapitalDebt_R1, Is.EqualTo(assertTotalEndQuarterBalanceR1.Value));

                    if (assertPaidOutDuringQuarterR2.HasValue)
                        Assert.That(summary.NewPaidOutCreditAmountDuringQuarter_R2, Is.EqualTo(assertPaidOutDuringQuarterR2.Value));

                    if (assertTotalActiveCreditCountR4.HasValue)
                        Assert.That(summary.NrOfActiveCredits_R4, Is.EqualTo(assertTotalActiveCreditCountR4.Value));

                    if (assertNrOfNewCreditsDuringQuarterR5.HasValue)
                        Assert.That(summary.NrOfNewCreditsDuringQuarter_R5, Is.EqualTo(assertNrOfNewCreditsDuringQuarterR5.Value));

                    if (assertNrOfActiveCustomersR6.HasValue)
                        Assert.That(summary.NrOfCustomersOnActiveCredits_R6, Is.EqualTo(assertNrOfActiveCustomersR6.Value));

                    if (assertPaidInterestDuringQuarterR7.HasValue)
                        Assert.That(summary.InterestRevenueCurrentQuarter_R7, Is.EqualTo(assertPaidInterestDuringQuarterR7.Value));

                    if (assertPaidFeesDuringQuarterR8.HasValue)
                        Assert.That(summary.FeesRevenueDuringQuarter_R8, Is.EqualTo(assertPaidFeesDuringQuarterR8.Value));

                    if (assertNrOfImpairedCreditsR9.HasValue)
                        Assert.That(summary.NrOfImpairedCredits_R9, Is.EqualTo(assertNrOfImpairedCreditsR9.Value));

                    if (assertNrOfNewImpairedCreditsDuringQuarterR10.HasValue)
                        Assert.That(summary.NrOfNewImpairedCreditsDuringQuarter_R10, Is.EqualTo(assertNrOfNewImpairedCreditsDuringQuarterR10.Value));
                }
                catch
                {
                    TestContext.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                    throw;
                }

                lastQuarterSummary = summary;
            }

            //Start in the beginning of a quarter
            support.MoveToNextQuarter();

            /*
             Add a credit and partially pay it during the quarter
             */
            int firstCreditCustomerId = 0;
            string firstCreditCreditNr = "";
            SimulateQuarter(() =>
                {
                    var credit = AddCredit();
                    firstCreditCustomerId = credit.CreditCustomers.Single().CustomerId;
                    firstCreditCreditNr = credit.CreditNr;

                    //Pay down the credit a bit during the quarter to ensure the r1 balance really is for the end of the quarter
                    support.MoveForwardNDays(20);
                    Credits.CreateAndPlaceUnplacedPayment(support, credit.CreditNr, 500m);
                },
                renderReport: true,
                assertTotalEndQuarterBalanceR1: 2200m - 500m, //initial - the payment
                assertPaidOutDuringQuarterR2: 2200m - 200m, //fees are not considered paid out
                assertTotalActiveCreditCountR4: 1,
                assertNrOfNewCreditsDuringQuarterR5: 1,
                assertPaidFeesDuringQuarterR8: 200m //Initial fee
            );

            /*
             Add a credit and settle it during the quarter. Make sure it's included in "new" but not end of quarter balance.
             */
            SimulateQuarter(() =>
                {
                    var credit = AddCredit(x =>
                    {
                        x.PaidToCustomerAmount = 100m;
                        x.WithheldInitialFeeAmount = 0m;
                        x.SettledAmount = 0m;
                    });

                    support.MoveForwardNDays(20);
                    Credits.CreateAndPlaceUnplacedPayment(support, credit.CreditNr, 120m);
                    Credits.AssertIsSettled(support, credit.CreditNr);
                },
                assertTotalEndQuarterBalanceR1: lastQuarterSummary!.CurrentCapitalDebt_R1,
                assertPaidOutDuringQuarterR2: 100m,
                assertTotalActiveCreditCountR4: 1,
                assertNrOfNewCreditsDuringQuarterR5: 1,
                assertPaidInterestDuringQuarterR7: Math.Round(21 * 100m * 0.1m / 365.25m, 2) //21 days (21 rather than 20 since you pay for one day if settled instanly ie zero days)
            );

            /*
             Add a second credit on an existing customer and make sure the customer is not double counted.
             */
            string secondCreditNr = "";
            SimulateQuarter(() =>
                {
                    var credit = AddCredit(x => x.MainApplicantCustomerId = firstCreditCustomerId);
                    secondCreditNr = credit.CreditNr;
                },
                assertTotalActiveCreditCountR4: 2,
                assertNrOfActiveCustomersR6: 1
            );

            /*
             Create a notification, send a reminder and then pay it in full, checking that both the notification and reminder fees are part of the quarters fees
             */
            SimulateQuarter(() =>
                {
                    support.MoveToNextDayOfMonth(support.NotificationProcessSettings.NotificationNotificationDay);
                    Credits.NotifyCreditsSimple(support, (CreditNr: firstCreditCreditNr, CustomerId: firstCreditCustomerId));

                    support.MoveToNextDayOfMonth(support.NotificationProcessSettings.NotificationDueDay);
                    Credits.RemindCredits(support, (firstCreditCreditNr, 0)); //Reminder wont be created since there is a grace period for payment

                    support.MoveToNextDayOfMonth(support.NotificationProcessSettings.NotificationNotificationDay);
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        Credits.RemindCredits(support, (firstCreditCreditNr, 1));

                        var notification = CreditNotificationDomainModel.CreateForCredit(firstCreditCreditNr, context, support.PaymentOrder(), onlyFetchOpen: true).Single().Value;
                        var notificationBalance = notification.GetRemainingBalance(support.Clock.Today);

                        Credits.CreateAndPlaceUnplacedPayment(support, firstCreditCreditNr, notificationBalance);
                    }
                },
                assertPaidFeesDuringQuarterR8: DefaultNotificationFeeAmount + support.NotificationProcessSettings.ReminderFeeAmount
            );

            /*
             Notify the second credit and then let it just sit for 90+ days to make sure it becomes impaired but is only considered new the first month.
             */
            SimulateQuarter(() =>
            {
                support.MoveToNextDayOfMonth(support.NotificationProcessSettings.NotificationNotificationDay);
                Credits.NotifyCreditsSimple(support, (CreditNr: secondCreditNr, CustomerId: firstCreditCustomerId));
            }, assertNrOfImpairedCreditsR9: 0);
            SimulateQuarter(() =>
            {
                //Check that Overdue dates in the current quarter are only counted up until today not until the last of the quarter (in the future)
                var thisQuarter = Quarter.ContainingDate(support.Clock.Today);

                var data = GetReportData(thisQuarter);
                Assert.That(data.Credits.Count(x => x.Credit.IsImpaired), Is.EqualTo(0));

                support.MoveForwardNDays(60); //Move forward so the credits pass 90 days

                data = GetReportData(thisQuarter);
                var nrOfOverDueDays = data.Credits.Single(x => x.Credit.IsImpaired).Credit.NrOfOverdueDays;

                support.MoveForwardNDays(5);

                data = GetReportData(thisQuarter);
                var nrOfOverDueDaysFiveDaysLater = data.Credits.Single(x => x.Credit.IsImpaired).Credit.NrOfOverdueDays;

                Assert.That(nrOfOverDueDaysFiveDaysLater - nrOfOverDueDays, Is.EqualTo(5));
            }, assertNrOfImpairedCreditsR9: 1, assertNrOfNewImpairedCreditsDuringQuarterR10: 1);
            SimulateQuarter(() => { }, assertNrOfImpairedCreditsR9: 1, assertNrOfNewImpairedCreditsDuringQuarterR10: 0);
        }

        private List<AffiliateModel> CreateAffiliates() => new List<AffiliateModel>
        {
            new AffiliateModel
            {
                IsSelf = true,
                DisplayToEnduserName = "The client",
                ProviderName = "self"
            }
        };
    }
}