using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans.AlternatePaymentPlan
{
    public class NoPaymentsEarlyPlanTests
    {
        [Test]
        public void AlternatePaymentPlanCreatedAsSoonAsPossible()
        {
            new Test().RunTest();
        }

        private class Test : SinglePaymentLoansTestRunner
        {
            protected override void DoTest()
            {
                Support.Now = Support.Now.AddMonths(-3);
                Support.MoveToNextDayOfMonth(1); //January first

                var creditNr1 = ShortTimeCredits.CreditNrFromIndex(1);

                var a = CreditCycleAssertionBuilder
                    .Begin()

                    //Month 1
                    .ForMonth(monthNr: 1)
                    .ExpectNewLoan(dayNr: 21, creditNr: creditNr1, singlePaymentRepaymentDays: 10) //Intended due date 31
                    .ExpectNotification(dayNr: 22, creditNr: creditNr1, dueDay: 1)

                    //Month 2
                    .ForMonth(monthNr: 2)
                    .ExpectReminder(dayNr: 8, creditNr: creditNr1)
                    .ExpectReminder(dayNr: 15, creditNr: creditNr1)
                    .ExpectAlternatePaymentPlanStarted(dayNr: 16, creditNr: creditNr1, null, firstDueDate: (MonthNr: 3, DayNr: 1), 407m, 407m, 407.81m)

                    //Month 3
                    .ForMonth(monthNr: 3)
                    .ExpectAlternatePaymentPlanCancelled(dayNr: 9, creditNr: creditNr1)

                    //Month 4
                    .ForMonth(monthNr: 4)
                    .ExpectTerminationLetter(dayNr: 2, creditNr: creditNr1)
                    .ExpectDebtCollectionExport(dayNr: 30, creditNr: creditNr1)

                    .End();

                foreach (var monthNr in Enumerable.Range(1, a.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if (monthNr == 1 && dayNr == 21)
                        {
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: RepaymentDaysCredit1, isRepaymentTimeDays: true,
                                loanAmount: LoanAmount, initialFeeOnFirstNotification: InitialFee);
                        }
                        else if (monthNr == 2 && dayNr == 16)
                        {
                            var s = Support.GetRequiredService<AlternatePaymentPlanService>();
                            var paymentPlan = s.GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
                            {
                                CreditNr = creditNr1,
                                ForceStartNextMonth = false,
                                NrOfPayments = 3
                            });
                            s.StartPaymentPlanFromSpecification(paymentPlan);
                        }
                    }, creditCycleAssertion: (Assertion: a, MonthNr: monthNr));
                }
            }

            const decimal LoanAmount = 1000m;
            const decimal InitialFee = 149m;
            const int RepaymentDaysCredit1 = 10;
        }
    }
}
