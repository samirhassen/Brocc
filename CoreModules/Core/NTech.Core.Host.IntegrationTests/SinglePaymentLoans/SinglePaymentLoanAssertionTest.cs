using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    internal class SinglePaymentLoanAssertionTest : SinglePaymentLoansTestRunner
    {
        private readonly CreditCycleAssertionBuilder.CreditCycleAssertion assertion;
        private readonly CreditCycleAction<TestSupport> action;

        public SinglePaymentLoanAssertionTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, CreditCycleAction<SinglePaymentLoansTestRunner.TestSupport> action)
        {
            this.assertion = assertion;
            this.action = action;
        }

        protected override void DoTest()
        {
            Support.MoveToNextDayOfMonth(1);

            foreach (var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
            {
                ShortTimeCredits.RunOneMonth(Support, 
                    creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr),
                    creditCycleAction: (Action: action, MonthNr: monthNr));
            }
        }

        public static void RunTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, CreditCycleAction<SinglePaymentLoansTestRunner.TestSupport> action)
        {
            new SinglePaymentLoanAssertionTest(assertion, action).RunTest();
        }
    }
}
