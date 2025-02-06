using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class SinglePaymentLoanHappyFlowTests
    {
        [Test]
        public void TenDayLoan_PaidOnDueDateByDirectDebit()
        {
            /*
               Why 12 days rather than 10?
             - Since the notification is sent the day after the loan is created that adds one day
             - Also since a loan with 0 days repayment time would still have to pay interest for the day when they both got the loan and paid there will always be one more than the repayment time
             */
            var firstNotificationInterestAmount = Credits.InterestAmountForNDays(loanAmount: 1000m, intrestRatePercent: 39m, nrOfDays: 12);
            string creditNr = "L10001";
            var firstNotificationAmount = 149m + 1000m + firstNotificationInterestAmount;

            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNewLoan(1, creditNr: creditNr, loanAmount: 1000m, marginInterestRate: 39m, singlePaymentRepaymentDays: 10, firstNotificationCost: (Code: "initialFeeNotification", Amount: 149m))
                .ExpectNotification(2, creditNr: creditNr, dueDay: 12, initialAmount: firstNotificationAmount, isSnailMailDeliveryExpected: false)
                .ExpectScheduledDirectDebitPayment(4, creditNr: creditNr, amount: firstNotificationAmount)
                .ExpectNotificationFullyPaid(12, creditNr: creditNr, dueDay: 12)
                .ExpectCreditSettled(12, creditNr: creditNr)

                .End();

            new HappyFlowTest(assertion).RunTest();
        }

        private class HappyFlowTest : SinglePaymentLoansTestRunner
        {
            private readonly CreditCycleAssertionBuilder.CreditCycleAssertion assertion;

            public HappyFlowTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion)
            {
                this.assertion = assertion;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);
                var maxMonthNr = assertion.MaxMonthNr;
                foreach(var monthNr in Enumerable.Range(1, maxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr), afterDay: dayNr =>
                    {
                        if(monthNr == 1 && dayNr == 1)
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: 10, isRepaymentTimeDays: true,
                                        loanAmount: 1000m, initialFeeOnFirstNotification: 149m, marginInterestRatePercent: 39m);
                    }, payDirectWhenScheduled: true);
                }
            }
        }
    }
}
