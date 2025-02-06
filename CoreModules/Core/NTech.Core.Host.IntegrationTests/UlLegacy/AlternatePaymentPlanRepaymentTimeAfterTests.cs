using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    internal class AlternatePaymentPlanRepaymentTimeAfterTests
    {
        [Test]
        public void AnnuityRecalculatedToPreserveRemainingMonthCount()
        {
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNewLoan(dayNr: 1, creditNr: CreditNr, loanAmount: 6000m)
                
                .ForMonth(2)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNr, dueDay: 28)

                .ForMonth(3)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNr, dueDay: 28)
                
                .ForMonth(4)
                .ExpectNotificationNotPaid(dayNr: 28, creditNr: CreditNr, dueDay: 28)

                .ForMonth(5)
                .ExpectNotificationNotPaid(dayNr: 28, creditNr: CreditNr, dueDay: 28)

                .ForMonth(6)
                .ExpectNrOfFutureNotifications(dayNr: 2, creditNr: CreditNr, count: RemainingPaymentsBeforeCount)
                .ExpectAlternatePaymentPlanStarted(dayNr: 3, creditNr: CreditNr, null, firstDueDate: (MonthNr: 6, DayNr: 28))
                .ExpectExtraAmortization(dayNr: 9, creditNr: CreditNr, amount: PaymentPlanAmount)
                .ExpectNotification(dayNr: 14, creditNr: CreditNr, dueDay: 28)

                .End();

            Action<(Test TestContext, int MonthNr, int DayNr)> action = t =>
            {
                var support = t.TestContext.Support;

                if (t.MonthNr == 1 && t.DayNr == 1)
                {
                    CreditsUlLegacy.CreateCredit(support, 1);
                    t.TestContext.PayNotificationsOnDueDate = true;
                }                    

                if(t.MonthNr == 4 && t.DayNr == 1)                    
                    t.TestContext.PayNotificationsOnDueDate = false;

                if(t.MonthNr == 6 && t.DayNr == 3)                   
                    Credits.CreateAlternatePaymentPlan(support, CreditNr, nrOfMonths: PaymentPlanMonthCount);

                if (t.MonthNr == 6 && t.DayNr == 9)
                    Credits.CreateAndImportPaymentFile(support, new Dictionary<string, decimal> { { CreditNr, PaymentPlanAmount } });
            };

            new Test(assertion, doAfterDay: action).RunTest();
        }

        private class Test : UlLegacyTestRunner
        {
            private CreditCycleAssertionBuilder.CreditCycleAssertion assertion;
            private readonly Action<(Test TestContext, int MonthNr, int DayNr)> doAfterDay;
            public bool PayNotificationsOnDueDate { get; set; } 

            public Test(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, Action<(Test TestContext, int MonthNr, int DayNr)> doAfterDay)
            {
                this.assertion = assertion;
                this.doAfterDay = doAfterDay;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);

                var monthRunner = new UlLegacyRunMonthTester(Support, debugPrint: true);
                foreach(var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
                {
                    monthRunner.RunOneMonth(creditExpectation: null, doAfterDay: dayNr =>
                    {
                        doAfterDay((TestContext: this, MonthNr: monthNr, DayNr: dayNr));
                        assertion.DoAssert(Support, monthNr, dayNr);
                    }, payNotificationsOnDueDate: PayNotificationsOnDueDate);
                }
            }
        }

        const string CreditNr = "C9871";
        const int PaymentPlanMonthCount = 2;
        const decimal PaymentPlanAmount = 195.77m;
        const int RemainingPaymentsBeforeCount = 32;
    }
}
