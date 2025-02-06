using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard.Reports
{
    public class SharedEsmaReportTests
    {
        [Test]
        public void TestAnnexTwelveReport()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                SwedishMortgageLoanReportData.OverrideGraceDays = 0; //Just to make testing late payments easier. Otherwise we would need to wait an extra month.

                support.MoveToNextDayOfMonth(1);

                /*
                 Loan 1 will have:
                    - a termination letter sent 2022-07-14 with due date 2022-08-11
                    - it's sent to debt collection 2022-08-20
                 */

                void PlacePaymentOnLoanOne(decimal amount)
                {
                    var creditNr = CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr;
                    Credits.CreateAndPlaceUnplacedPayment(support, creditNr, amount);
                }

                void AssertPaidOnLoan1(decimal capitalAmount, decimal interestAmount)
                {
                    var lastMonth = Month.ContainingDate(support.Clock.Today).PreviousMonth;
                    var annex12Service = support.GetRequiredService<AnnexTwelveEsmaReportService>();
                    var loan = annex12Service.GetAnnexTwelveReportData(new Credit.Shared.Models.FromDateToDateReportRequest
                    {
                        FromDate = lastMonth.FirstDate,
                        ToDate = lastMonth.LastDate
                    }).Loans.Single();

                    Assert.That(loan.InPeriodRecoveredCapitalDebtAmount, Is.EqualTo(capitalAmount));
                    Assert.That(loan.InPeriodRecoveredInterestDebtAmount, Is.EqualTo(interestAmount));
                }

                // 2022-04
                CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr =>
                {
                    TestContext.WriteLine(support.Clock.Today);
                    if(dayNr == 1) CreditsMlStandard.CreateCredit(support, 1);
                });
                CreditsMlStandard.RunOneMonth(support); // 2022-05
                CreditsMlStandard.RunOneMonth(support); // 2022-06
                CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr => // 2022-07
                {
                    if(dayNr == 13)
                        PlacePaymentOnLoanOne(1m); //Before termination letter sent
                    else if(dayNr == 16)
                        PlacePaymentOnLoanOne(1m); //After termination letter sent but before overdue
                });

                AssertPaidOnLoan1(capitalAmount: 0m, interestAmount: 0m);

                CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr => // 2022-08
                {
                    if(dayNr == 12)
                        PlacePaymentOnLoanOne(3500m); //After termination letter overdue
                });

                AssertPaidOnLoan1(capitalAmount: 128.97m, interestAmount: 3371.03m);
            });
        }

        [Test]
        public void FundOwnerReport()
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                SwedishMortgageLoanReportData.OverrideGraceDays = 0; //Just to make testing late payments easier. Otherwise we would need to wait an extra month.

                support.MoveToNextDayOfMonth(1);

                void AssertLoan1(Action<FundOwnerReportLoan> assert)
                {
                    var lastMonth = Month.ContainingDate(support.Clock.Today).PreviousMonth;
                    var service = support.GetRequiredService<FundOwnerReportService>();
                    var loan = service.GetFundOwnerReportData(new Credit.Shared.Models.FromDateToDateReportRequest
                    {
                        FromDate = lastMonth.FirstDate,
                        ToDate = lastMonth.LastDate
                    }).Loans.Single();

                    assert(loan);
                }

                // 2022-04 - Create a loan and add an extra amortization before the first notification is sent.
                const decimal NotificationFeeAmount = 20m;
                CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr =>
                {
                    TestContext.WriteLine(support.Clock.Today);

                    if (dayNr == 1) CreditsMlStandard.CreateCredit(support, 1,
                        notificationFeeAmount: NotificationFeeAmount,
                        loanOwnerName: "Owner 1");
                    else if (dayNr == 2)
                        Credits.CreateAndPlaceUnplacedPayment(support, CreditsMlStandard.GetCreatedCredit(support, 1).CreditNr, 50m);
                }, payNotificationsOnDueDate: true);

                AssertLoan1(loan =>
                {
                    Assert.That(loan.CreditNr, Is.Not.Null);
                    Assert.That(loan.MainCustomerId, Is.GreaterThan(0));
                    Assert.That(loan.MainCustomerAddressZipcode, Is.EqualTo("92025"));
                    Assert.That(loan.CollateralZipcode, Is.EqualTo("111 11"));
                    Assert.That(loan.LoanOwnerName, Is.EqualTo("Owner 1"));
                    Assert.That(loan.InPeriodPaidNotificationFeeAmount, Is.EqualTo(NotificationFeeAmount));
                    Assert.That(loan.InPeriodExtraAmortizationAmount, Is.EqualTo(50m));
                });

                CreditsMlStandard.RunOneMonth(support, payNotificationsOnDueDate: true); // 2022-05

                AssertLoan1(loan =>
                {
                    //Checking again to make sure it does not include historical payments but is just in this period
                    Assert.That(loan.InPeriodPaidNotificationFeeAmount, Is.EqualTo(NotificationFeeAmount));
                    Assert.That(loan.InPeriodExtraAmortizationAmount, Is.EqualTo(0m));
                });
            });
        }
    }
}
