using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    internal class UlLegacyAssertionTest : UlLegacyTestRunner
    {
        private readonly CreditCycleAssertionBuilder.CreditCycleAssertion assertion;
        private readonly CreditCycleAction<TestSupport> action;

        public UlLegacyAssertionTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, CreditCycleAction<TestSupport> action)
        {
            this.assertion = assertion;
            this.action = action;
        }

        protected override void DoTest()
        {
            Support.MoveToNextDayOfMonth(1);

            var runner = new UlLegacyRunMonthTester(Support);
            foreach (var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
            {
                runner.RunOneMonth(
                    creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr),
                    creditCycleAction: (Action: action, MonthNr: monthNr));
            }
        }

        public static void RunTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, CreditCycleAction<TestSupport> action)
        {
            new UlLegacyAssertionTest(assertion, action).RunTest();
        }
    }
}