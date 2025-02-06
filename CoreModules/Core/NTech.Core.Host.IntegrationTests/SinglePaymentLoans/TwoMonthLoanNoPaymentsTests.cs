using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class TwoMonthLoanNoPaymentsTests
    {
        private class Test : SinglePaymentLoansTestRunner
        {
            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);

                var creditNr2 = ShortTimeCredits.CreditNrFromIndex(2);

                var a = CreditCycleAssertionBuilder
                    .Begin()

                    //Month 1
                    .ForMonth(monthNr: 1)
                    .ExpectNotification(dayNr: 14, creditNr: creditNr2, dueDay: 28)                    

                    //Month 2
                    .ForMonth(monthNr: 2)
                    .ExpectReminder(dayNr: 5, creditNr: creditNr2)
                    .ExpectReminder(dayNr: 12, creditNr: creditNr2)
                    .ExpectNotification(dayNr: 14, creditNr: creditNr2, dueDay: 28)
                    
                    //Month 3
                    .ForMonth(monthNr: 3)
                    .ExpectReminder(dayNr: 4, creditNr: creditNr2)
                    .ExpectReminder(dayNr: 11, creditNr: creditNr2)
                    .ExpectNoNotificationCreated(dayNr: 14, creditNr: creditNr2)
                    
                    //Month 4
                    .ForMonth(monthNr: 4)                    
                    .ExpectTerminationLetter(dayNr: 9, creditNr: creditNr2) //10 days grace period

                    //Month 5
                    .ForMonth(monthNr: 5)
                    .ExpectDebtCollectionExport(9, creditNr2, reminderFeeAmount: 120m, interestAmount: 61.93m)//3 days grace period

                    .End();

                foreach (var monthNr in Enumerable.Range(1, a.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if(monthNr == 1 && dayNr == 1)
                        {
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 2, repaymentTime: RepaymentMonthsCredit2, isRepaymentTimeDays: false,
                                loanAmount: LoanAmount, initialFeeOnFirstNotification: InitialFee);
                        }
                    }, creditCycleAssertion: (Assertion: a, MonthNr: monthNr));
                }
            }

            const decimal LoanAmount = 1000m;
            const decimal InitialFee = 149m;
            const int RepaymentMonthsCredit2 = 2;
        }

        [Test]
        public void TestTwoMonthLoanLoanNoPaymentsCreditLifecycle()
        {
            new Test().RunTest(overrideClientConfig: x =>
            {
                x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(10));
                //Setting this to something other than TerminationLetterGraceDays so we could tell if one of the jobs used the wrong one
                //In prod these are likely to be the same quite often.
                x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(3));                
            });
        }
    }
}
