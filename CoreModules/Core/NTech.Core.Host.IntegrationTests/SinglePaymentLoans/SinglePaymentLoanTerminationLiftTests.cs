using Moq;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;
using NTech.Core.Module.Shared.Infrastructure;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class SinglePaymentLoanTerminationLiftTests
    {
        [Test]
        public void TestSingleLoanNoPaymentsCreditLifecycle()
        {
            const string creditNr1 = "L10001";
            var a = CreditCycleAssertionBuilder
                .Begin()

                //Month 1
                .ForMonth(monthNr: 1)
                .ExpectNotification(dayNr: 2, creditNr: creditNr1, dueDay: 12)
                .ExpectReminder(dayNr: 19, creditNr: creditNr1)
                .ExpectReminder(dayNr: 26, creditNr: creditNr1)

                //Month 2 - No events

                //Month 3
                .ForMonth(monthNr: 3)
                .ExpectTerminationLetter(dayNr: 23, creditNr: creditNr1)
                .ExpectTerminationLetterLifted(dayNr: 24, creditNr: creditNr1)
                .ExpectTerminationLetterPaused(dayNr: 24, creditNr: creditNr1, pausedUntil: (MonthNr: 4, DayNr: 14))

                //Month 4
                .ForMonth(monthNr: 4)
                .ExpectTerminationLetter(dayNr: 14, creditNr: creditNr1)
                
                .ForMonth(monthNr: 5)
                .ExpectNotShownInDebtCollectionUi(dayNr: 10, creditNr: creditNr1)
                .ExpectShownInDebtCollectionUi(dayNr: 11, creditNr: creditNr1, debtCollectionExportMonthNr: 5, debtCollectionExportDayNr: 14)
                .ExpectDebtCollectionExport(14, creditNr1)

                .End();

            new Test(a).RunTest();
        }

        private class Test : SinglePaymentLoansTestRunner
        {
            public Test(CreditCycleAssertionBuilder.CreditCycleAssertion assertion) : base()
            {
                this.assertion = assertion;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);

                var creditNr1 = ShortTimeCredits.CreditNrFromIndex(1);

                foreach (var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if (monthNr == 1 && dayNr == 1)
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: 10, isRepaymentTimeDays: true,
                                loanAmount: 1000m, initialFeeOnFirstNotification: 189m);
                        if (monthNr == 3 && dayNr == 24)
                        {
                            Support.GetRequiredService<TerminationLetterInactivationService>().InactivateTerminationLetters(
                                new InactivateTerminationLettersRequest { CreditNrs = new List<string> { creditNr1 } });
                        }
                    }, creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr));
                }
            }

            public override void RunTest(Action<Mock<IClientConfigurationCore>>? overrideClientConfig = null)
            {
                base.RunTest(overrideClientConfig: x =>
                {
                    x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(10));
                    //Setting this to something other than TerminationLetterGraceDays so we could tell if one of the jobs used the wrong one
                    //In prod these are likely to be the same quite often.
                    x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(3));
                });
            }

            private readonly CreditCycleAssertionBuilder.CreditCycleAssertion assertion;
        }
    }
}
