using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class SinglePaymentLoanInitialTests
    {
        [Test]
        public void TenDayLoanCustomerIsNotifiedRightAway()
        {
            const int SinglePaymentLoanRepaymentTimeInDays = 10;
            const decimal LoanAmount = 1000m;

            new HappyFlowTest(
                loanAmount: LoanAmount,
                singlePaymentLoanRepaymentTimeInDays: SinglePaymentLoanRepaymentTimeInDays,
                assertAfterNotify: x =>
                {
                    var credit = x.Credit;
                    var notification = x.Notification;
                    var support = x.Test.Support;

                    var amortizationModel = credit.GetAmortizationModel(support.Clock.Today);
                    var perLoanDueDay = (int?)credit.GetDatedCreditValueOpt(support.Clock.Today, nCredit.DatedCreditValueCode.NotificationDueDay);

                    Assert.That(notification, Is.Not.Null, "Notification should exist");                    
                    Assert.That(notification.DueDate, Is.EqualTo(support.Clock.Today.AddDays(SinglePaymentLoanRepaymentTimeInDays)));
                    Assert.That(notification.DueDate.Day, Is.EqualTo(perLoanDueDay));
                    Assert.That(notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (1)");
                    Assert.That(amortizationModel.UsesAnnuities, Is.EqualTo(false), "Should use rak amortering / fixed monthly payment");
                    Assert.That(amortizationModel.GetActualFixedMonthlyPaymentOrException(), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (2)");
                }).RunTest();
        }

        [Test]
        public void TenDayLoanCustomerIsNotifiedSuperLate()
        {
            const int SinglePaymentLoanRepaymentTimeInDays = 10;
            const decimal LoanAmount = 1000m;

            new HappyFlowTest(
                loanAmount: LoanAmount,
                singlePaymentLoanRepaymentTimeInDays: SinglePaymentLoanRepaymentTimeInDays,
                assertAfterNotify: x =>
                {
                    var credit = x.Credit;
                    var notification = x.Notification;
                    var support = x.Test.Support;

                    var amortizationModel = credit.GetAmortizationModel(support.Clock.Today);
                    var initialPerLoanDueDay = (int?)credit.GetDatedCreditValueOpt(credit.GetStartDate().Date, nCredit.DatedCreditValueCode.NotificationDueDay);
                    var currentPerLoanDueDay = (int?)credit.GetDatedCreditValueOpt(support.Clock.Today, nCredit.DatedCreditValueCode.NotificationDueDay);

                    Assert.That(notification, Is.Not.Null, "Notification should exist");
                    Assert.That(notification.DueDate, Is.EqualTo(support.Clock.Today.AddDays(SinglePaymentLoanRepaymentTimeInDays)));
                    Assert.That(notification.DueDate.Day, Is.Not.EqualTo(initialPerLoanDueDay), "Initial per loan due day");
                    Assert.That(notification.DueDate.Day, Is.EqualTo(currentPerLoanDueDay), "Current per loan due day");
                    Assert.That(notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (1)");
                    Assert.That(amortizationModel.UsesAnnuities, Is.EqualTo(false), "Should use rak amortering / fixed monthly payment");
                    Assert.That(amortizationModel.GetActualFixedMonthlyPaymentOrException(), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (2)");
                },
                waitDaysBeforeNotification: 5).RunTest();
        }

        [Test]
        public void FortyDay_LoanCustomerIsNotified_AfterTwentySixDays()
        {
            const int SinglePaymentLoanRepaymentTimeInDays = 40;
            const decimal LoanAmount = 1000m;

            foreach(var waitDaysBeforeNotification in new[] { 25, 26 })
                new HappyFlowTest(
                    loanAmount: LoanAmount,
                    singlePaymentLoanRepaymentTimeInDays: SinglePaymentLoanRepaymentTimeInDays,
                    assertAfterNotify: x =>
                    {
                        var credit = x.Credit;
                        var notification = x.Notification;
                        var support = x.Test.Support;

                        var amortizationModel = credit.GetAmortizationModel(support.Clock.Today);

                        if(waitDaysBeforeNotification == 25)
                            Assert.That(notification, Is.Null, "Notification should exist");
                        else
                        {
                            //waitDaysBeforeNotification == 26 (ie 14 days before we reach 40 days from loan creation)
                            Assert.That(notification, Is.Not.Null, "Notification should exist");
                            Assert.That(Dates.GetAbsoluteNrOfDaysBetweenDates(notification.NotificationDate, notification.DueDate), Is.EqualTo(14));
                            Assert.That(notification.DueDate, Is.EqualTo(credit.GetStartDate().Date.AddDays(SinglePaymentLoanRepaymentTimeInDays)));
                            Assert.That(notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (1)");
                            Assert.That(amortizationModel.UsesAnnuities, Is.EqualTo(false), "Should use rak amortering / fixed monthly payment");
                            Assert.That(amortizationModel.GetActualFixedMonthlyPaymentOrException(), Is.EqualTo(LoanAmount), "Should be one payment of loan amount (2)");
                        }

                    }, waitDaysBeforeNotification: waitDaysBeforeNotification).RunTest();
        }

        private class HappyFlowTest : SinglePaymentLoansTestRunner
        {
            private readonly int singlePaymentLoanRepaymentTimeInDays;
            private readonly int? waitDaysBeforeNotification;
            private readonly decimal loanAmount;
            private readonly Action<(HappyFlowTest Test, CreditDomainModel Credit, CreditNotificationDomainModel? Notification)> assertAfterNotify;

            public HappyFlowTest(decimal loanAmount, int singlePaymentLoanRepaymentTimeInDays, Action<(HappyFlowTest Test, CreditDomainModel Credit, CreditNotificationDomainModel? Notification)> assertAfterNotify,
                int? waitDaysBeforeNotification = null)
            {
                this.singlePaymentLoanRepaymentTimeInDays = singlePaymentLoanRepaymentTimeInDays;
                this.waitDaysBeforeNotification = waitDaysBeforeNotification;
                this.loanAmount = loanAmount;
                this.assertAfterNotify = assertAfterNotify;
            }

            protected override void DoTest()
            {
                var creditNr = ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: singlePaymentLoanRepaymentTimeInDays, isRepaymentTimeDays: true,
                            loanAmount: loanAmount, initialFeeOnFirstNotification: 0m);

                if (waitDaysBeforeNotification.HasValue)
                    Support.MoveForwardNDays(waitDaysBeforeNotification.Value);
                var notificationService = Support.GetRequiredService<NotificationService>();
                var notificationResult = notificationService.CreateNotifications(true, false);
                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    CreditDomainModel credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, Support.CreditEnvSettings);
                    var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, Support.PaymentOrder(), onlyFetchOpen: false).Values.SingleOrDefault();

                    assertAfterNotify((Test: this, Credit: credit, Notification: notification));
                }
            }
        }
    }
}
