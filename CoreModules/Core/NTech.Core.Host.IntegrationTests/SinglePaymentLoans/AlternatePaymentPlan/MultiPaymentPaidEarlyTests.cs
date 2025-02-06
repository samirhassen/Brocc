using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans.AlternatePaymentPlan
{
    public class MultiPaymentPaidEarlyTests
    {
        [Test]
        public void PaidEarlyRestartNotificationOnNextMonthlyNotificationDay()
        {
            var assert = CreditCycleAssertionBuilder
                  .Begin()

                  .ForMonth(1)
                  .ExpectNewLoan(dayNr: 1, CreditNr)
                  .ExpectNotification(dayNr: 14, CreditNr, dueDay: 28)
                  .ExpectAlternatePaymentPlanStarted(dayNr: 15, CreditNr, totalAmount: PaymentPlanAmount, null)
                  .ExpectAlternatePaymentPlanFullyPaid(dayNr: 16, CreditNr)

                  .ForMonth(2)
                  .ExpectNotification(dayNr: 14, CreditNr, dueDay: 28, interestAmount: 61.95m, capitalAmount: 645.78m)

                  .End();
            
            var act = CreditCycleActionBuilder<SinglePaymentLoansTestRunner.TestSupport>
                .Begin()

                .ForMonth(1)
                .AddAction(dayNr: 1, t => ShortTimeCredits.CreateCredit(t.Support, creditIndex: 1, repaymentTime: 3, 
                    isRepaymentTimeDays: false, loanAmount: 2000, initialFeeOnFirstNotification: 149))
                .AddAction(dayNr: 15, t => Credits.StartPaymentPlan(t.Support, CreditNr))
                .AddAction(dayNr: 16, t => Credits.CreateAndImportPaymentFileWithOcr(t.Support, new Dictionary<string, decimal> { { CreditOcr, PaymentPlanAmount } }))

                .End();

            SinglePaymentLoanAssertionTest.RunTest(assert, act);
        }

        const string CreditOcr = "1111111108";
        const string CreditNr = "L10001";
        const decimal PaymentPlanAmount = 859.46m;
    }
}
