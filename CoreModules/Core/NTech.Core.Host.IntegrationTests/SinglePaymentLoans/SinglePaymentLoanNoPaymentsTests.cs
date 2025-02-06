using nCredit.Code.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class SinglePaymentLoanNoPaymentsTests
    {
        private class Test : SinglePaymentLoansTestRunner
        {
            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);

                var creditNr1 = ShortTimeCredits.CreditNrFromIndex(1);
                var creditNr2 = ShortTimeCredits.CreditNrFromIndex(2);

                var a = CreditCycleAssertionBuilder
                    .Begin()

                    //Month 1
                    .ForMonth(monthNr: 1)
                    .ExpectNotification(dayNr: 2, creditNr: creditNr1, dueDay: 12)
                    //NOTE: The reason we get 2 extra interest days is that the notification is delayed by one day and we always take at least one day so today -> today is one day and so on
                    .ExpectScheduledDirectDebitPayment(dayNr: 4, creditNr: creditNr1, amount: LoanAmount + InitialFee + Math.Round(LoanAmount * 0.39m * (RepaymentDaysCredit1 + 2) / 365.25m, 2))
                    .ExpectNotification(dayNr: 14, creditNr: creditNr2, dueDay: 28)
                    .ExpectReminder(dayNr: 19, creditNr: creditNr1)
                    .ExpectReminder(dayNr: 26, creditNr: creditNr1)
                    
                    //Month 2
                    .ForMonth(monthNr: 2)
                    .ExpectReminder(dayNr: 5, creditNr: creditNr2)
                    .ExpectReminder(dayNr: 12, creditNr: creditNr2)
                    .ExpectNotification(dayNr: 14, creditNr: creditNr2, dueDay: 28)
                    
                    //Month 3
                    .ForMonth(monthNr: 3)
                    .ExpectReminder(dayNr: 4, creditNr: creditNr2)
                    .ExpectReminder(dayNr: 11, creditNr: creditNr2)
                    .ExpectNotShownInTerminationLetterUi(dayNr: 12, creditNr: creditNr1)
                    .ExpectShownInTerminationLetterUi(dayNr: 13, creditNr: creditNr1, letterSentMonthNr: 3, letterSentDayNr: 23)
                    .ExpectTerminationLetter(dayNr: 23, creditNr: creditNr1) //10 days grace period
                    .ExpectNotification(dayNr: 14, creditNr: creditNr2, dueDay: 28)
                    
                    //Month 4
                    .ForMonth(monthNr: 4)
                    .ExpectReminder(dayNr: 5, creditNr: creditNr2)
                    .ExpectTerminationLetter(dayNr: 9, creditNr: creditNr2) //10 days grace period
                    .ExpectNotShownInDebtCollectionUi(dayNr: 20, creditNr: creditNr1)
                    .ExpectShownInDebtCollectionUi(dayNr: 21, creditNr: creditNr1, debtCollectionExportMonthNr: 4, debtCollectionExportDayNr: 24)
                    .ExpectDebtCollectionExport(24, creditNr1) //3 days grace period

                    //Month 5
                    .ForMonth(monthNr: 5)
                    .ExpectDebtCollectionExport(9, creditNr2)//3 days grace period

                    .End();

                foreach (var monthNr in Enumerable.Range(1, a.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if(monthNr == 1 && dayNr == 1)
                        {
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: RepaymentDaysCredit1, isRepaymentTimeDays: true,
                                loanAmount: LoanAmount, initialFeeOnFirstNotification: InitialFee);
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 2, repaymentTime: RepaymentMonthsCredit2, isRepaymentTimeDays: false,
                                loanAmount: LoanAmount, initialFeeOnFirstNotification: InitialFee);
                        }
                    }, creditCycleAssertion: (Assertion: a, MonthNr: monthNr));
                }
            }

            const decimal LoanAmount = 1000m;
            const decimal InitialFee = 149m;
            const int RepaymentDaysCredit1 = 10;
            const int RepaymentMonthsCredit2 = 4;
        }

        [Test]
        public void TestSingleLoanNoPaymentsCreditLifecycle()
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
