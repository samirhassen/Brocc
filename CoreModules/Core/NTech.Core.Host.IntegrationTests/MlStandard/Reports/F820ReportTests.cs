using nCredit.DomainModel;
using NTech.Core.Credit.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Reports
{
    public class F820ReportTests
    {
        [Test]
        public void TestReport()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                const decimal LoanAmount = 1000000m;
                const decimal InitialFeeAmount = 1000m;
                const decimal NotificationFeeAmount = 20m;

                string creditNr;
                int customerId1;
                int customerId2;
                support.MoveToNextQuarter();
                {
                    support.MoveForwardNDays(2);
                    var credit = CreditsMlStandard.CreateCredit(support, 1,
                        loanAmount: LoanAmount,
                        drawnFromLoanAmountInitialFees: InitialFeeAmount,
                        notificationFeeAmount: NotificationFeeAmount);
                    creditNr = credit.CreditNr;
                    customerId1 = credit.CreditCustomers.Single(x => x.ApplicantNr == 1).CustomerId;
                    customerId2 = credit.CreditCustomers.Single(x => x.ApplicantNr == 2).CustomerId;
                }

                support.MoveToNextQuarter();
                {
                    var reportData = GetReportDataForPreviousQuarter(support);
                    Assert.That(reportData.CapitalBalance, Is.EqualTo(LoanAmount), "CapitalBalance");
                    Assert.That(reportData.PaidOutAmountInQuarter, Is.EqualTo(LoanAmount - InitialFeeAmount), "PaidOutAmountInQuarter");
                    Assert.That(reportData.NrOfCredits, Is.EqualTo(1), "NrOfCredits");
                    Assert.That(reportData.NrOfNewCreditsInQuarter, Is.EqualTo(1), "NrOfNewCreditsInQuarter");
                    Assert.That(reportData.NrOfCustomers, Is.EqualTo(2), "NrOfCustomers");
                    Assert.That(reportData.InterestRevenue, Is.EqualTo(0), "InterestRevenue");
                    Assert.That(reportData.FeeRevenue, Is.EqualTo(InitialFeeAmount), "FeeRevenue");
                    Assert.That(reportData.NrOfImpairedCredits, Is.EqualTo(0), "NrOfImpairedCredits");
                    Assert.That(reportData.NrOfNewImpairedCreditsThisQuarter, Is.EqualTo(0), "NrOfNewImpairedCreditsThisQuarter");
                }
                decimal notificationCapitalAmount;
                decimal notificationInterestAmount;
                {
                    support.Now = support.Now.AddMonths(1);

                    //Fully pay one notification to get some fee/interest revenue and see that the capital balance is counted down
                    support.MoveToNextDayOfMonth(14);
                    Credits.NotifyCredits(support, (creditNr, customerId1, NotificationExpectedResultCode.NotificationCreated));
                    using (var context = support.CreateCreditContextFactory().CreateContext())
                    {
                        var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, support.PaymentOrder(), onlyFetchOpen: false).Values.Single();
                        notificationInterestAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Interest);
                        notificationCapitalAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital);
                        Credits.CreateAndImportPaymentFile(support, new Dictionary<string, decimal>
                        {
                            { creditNr, notification.GetRemainingBalance(support.Clock.Today) }
                        });
                    }
                }

                support.MoveToNextQuarter();
                {
                    var reportData = GetReportDataForPreviousQuarter(support);

                    Assert.That(reportData.CapitalBalance, Is.EqualTo(LoanAmount - notificationCapitalAmount), "CapitalBalance");
                    Assert.That(reportData.PaidOutAmountInQuarter, Is.EqualTo(0m), "PaidOutAmountInQuarter");
                    Assert.That(reportData.NrOfCredits, Is.EqualTo(1), "NrOfCredits");
                    Assert.That(reportData.NrOfNewCreditsInQuarter, Is.EqualTo(0), "NrOfNewCreditsInQuarter");
                    Assert.That(reportData.NrOfCustomers, Is.EqualTo(2), "NrOfCustomers");
                    Assert.That(reportData.FeeRevenue, Is.EqualTo(NotificationFeeAmount), "FeeRevenue");
                    Assert.That(reportData.InterestRevenue, Is.EqualTo(notificationInterestAmount), "InterestRevenue");
                    Assert.That(reportData.NrOfImpairedCredits, Is.EqualTo(0), "NrOfImpairedCredits");
                    Assert.That(reportData.NrOfNewImpairedCreditsThisQuarter, Is.EqualTo(0), "NrOfNewImpairedCreditsThisQuarter");

                    support.MoveToNextDayOfMonth(14);
                    Credits.NotifyCredits(support, (creditNr, customerId1, NotificationExpectedResultCode.NotificationCreated));
                }

                support.MoveToNextQuarter();
                support.MoveToNextQuarter();
                {
                    var reportData = GetReportDataForPreviousQuarter(support);
                    Assert.That(reportData.NrOfImpairedCredits, Is.EqualTo(1), "NrOfImpairedCredits");
                    Assert.That(reportData.NrOfNewImpairedCreditsThisQuarter, Is.EqualTo(1), "NrOfNewImpairedCreditsThisQuarter");

                    CreditsMlStandard.CreateCredit(support, 2, mainApplicantCustomerId: customerId1, coApplicantCustomerId: customerId2);
                }

                support.MoveToNextQuarter();
                {
                    var reportData = GetReportDataForPreviousQuarter(support);
                    Assert.That(reportData.NrOfImpairedCredits, Is.EqualTo(1), "NrOfImpairedCredits");
                    Assert.That(reportData.NrOfNewImpairedCreditsThisQuarter, Is.EqualTo(0), "NrOfNewImpairedCreditsThisQuarter");
                    Assert.That(reportData.InterestRevenue, Is.EqualTo(0m), "InterestRevenue");
                    Assert.That(reportData.NrOfCredits, Is.EqualTo(2));
                    Assert.That(reportData.NrOfCustomers, Is.EqualTo(2)); //Make sure it counts unique customers
                }
            });
        }

        private MortgageLoanBkiF820ReportService.ReportData GetReportDataForPreviousQuarter(MlStandardTestRunner.TestSupport support)
        {
            var quarter = support.CurrentQuarter.GetPrevious();
            var reportService = new MortgageLoanBkiF820ReportService(
                support.Clock,
                new Credit.Shared.Database.CreditContextFactory(() => new CreditContextExtended(support.CurrentUser, support.Clock)));

            return reportService.CreateReportData(quarter);
        }
    }
}
