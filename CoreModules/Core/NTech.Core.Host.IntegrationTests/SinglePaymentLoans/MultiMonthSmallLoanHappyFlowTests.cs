using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class MultiMonthSmallLoanHappyFlowTests
    {
        private const int FixedDueDay = 28;
        private const string InitialFeeCostCode = "initialFeeNotification";
        private const decimal InitialFeeAmount = 149m;

        [Test]
        public void MultiMonthLoanUsesFixedDueDate()
        {
            const decimal LoanAmount = 1000m;            

            //Time starts one the 2:d
            foreach(var waitDaysBeforeNotification in new[] { 0, 10, 12 })
            {
                new HappyFlowTest(
                    loanAmount: LoanAmount,
                    nrOfMonths: 4,
                    assertAfterNotify: x =>
                    {
                        var credit = x.Credit;
                        var notification = x.Notification;
                        var support = x.Test.Support;

                        if(x.MonthNr == 1)
                        {
                            var amortizationModel = credit.GetAmortizationModel(support.Clock.Today);
                            if (waitDaysBeforeNotification == 12)
                            {
                                Assert.That(notification, Is.Not.Null, "Notification should exist");
                                Assert.That(notification.DueDate, Is.EqualTo(Month.ContainingDate(support.Clock.Today).GetDayDate(FixedDueDay)));
                                Assert.That(notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital), Is.LessThan(LoanAmount / 4m), "Should be one payment of loan amount (1)");
                                Assert.That(notification.GetRemainingBalance(support.Clock.Today, PaymentOrderItem.FromCustomCostCode(InitialFeeCostCode)), Is.EqualTo(InitialFeeAmount));
                            }
                            else
                                Assert.That(notification, Is.Null, "Notification should not exist");

                            Assert.That(amortizationModel.UsesAnnuities, Is.EqualTo(true), "Should use annuities");
                            Assert.That(amortizationModel.GetActualAnnuityOrException(), Is.GreaterThanOrEqualTo(LoanAmount / 4m), "Should be one payment of loan amount (2)");
                        } 
                        else
                        {
                            Assert.That(notification!.GetRemainingBalance(support.Clock.Today, PaymentOrderItem.FromCustomCostCode(InitialFeeCostCode)), Is.EqualTo(0m));
                            Assert.That(notification!.GetRemainingBalance(support.Clock.Today), Is.GreaterThan(0m));
                        }
                    },
                    waitDaysBeforeNotification: waitDaysBeforeNotification).RunTest();
            }
        }

        private class HappyFlowTest : SinglePaymentLoansTestRunner
        {
            private readonly int? waitDaysBeforeNotification;
            private readonly decimal loanAmount;
            private readonly int nrOfMonths;
            private readonly Action<(HappyFlowTest Test, CreditDomainModel Credit, CreditNotificationDomainModel? Notification, int MonthNr)> assertAfterNotify;

            public HappyFlowTest(decimal loanAmount, int nrOfMonths, Action<(HappyFlowTest Test, CreditDomainModel Credit, CreditNotificationDomainModel? Notification, int MonthNr)> assertAfterNotify,
                int? waitDaysBeforeNotification = null)
            {
                this.waitDaysBeforeNotification = waitDaysBeforeNotification;
                this.loanAmount = loanAmount;
                this.nrOfMonths = nrOfMonths;
                this.assertAfterNotify = assertAfterNotify;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(2);

                var creditNr = ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: nrOfMonths, isRepaymentTimeDays: false, 
                    loanAmount: loanAmount, initialFeeOnFirstNotification: InitialFeeAmount);

                if (waitDaysBeforeNotification.HasValue)
                    Support.MoveForwardNDays(waitDaysBeforeNotification.Value);
                var notificationService = Support.GetRequiredService<NotificationService>();
                var notificationResult = notificationService.CreateNotifications(true, false);
                DateTime? firstNotificationDueDate = null;

                using (var context = Support.CreateCreditContextFactory().CreateContext())
                {
                    CreditDomainModel credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, Support.CreditEnvSettings);
                    var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, Support.PaymentOrder(), onlyFetchOpen: false).Values.SingleOrDefault();
                    firstNotificationDueDate = notification?.DueDate;
                    assertAfterNotify((Test: this, Credit: credit, Notification: notification, MonthNr: 1));
                }

                if (!firstNotificationDueDate.HasValue || nrOfMonths == 1)
                    return;

                foreach (var monthNr in Enumerable.Range(2, nrOfMonths - 1))
                {
                    Assert.That(Support.Now.Date.Day, Is.EqualTo(FixedDueDay - 14));
                    Support.MoveForwardNDays(1);
                    Support.MoveToNextDayOfMonth(FixedDueDay - 14);
                    notificationResult = notificationService.CreateNotifications(true, false);

                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        CreditDomainModel credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, Support.CreditEnvSettings);
                        var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, Support.PaymentOrder(), onlyFetchOpen: false).Values
                            .Where(x => x.DueDate > Support.Now.Date).Single();
                        assertAfterNotify((Test: this, Credit: credit, Notification: notification, MonthNr: monthNr));
                    }
                }
            }
        }
    }
}
