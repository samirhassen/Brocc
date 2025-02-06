using nCredit;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    internal class PaymentFreeMonthsTests
    {
        [Test]
        public void MaxPaymentFreeMonths()
        {
            /*
             Ruleset being tested:
             - At least three notifications must be paid before a payment free month can be requested
             - There must be three months between each payment free month
             */
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)

                .ForMonth(2)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)

                .ForMonth(3)
                .ExpectToggleNextMonthPaymentFreeNotShown(dayNr: 27, creditNr: CreditNumber)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeAllowed(dayNr: 29, creditNr: CreditNumber)
                .ExpectPaymentFreeMonthRequested(dayNr: 30, creditNr: CreditNumber, paymentFreeMonthNr: 4)

                .ForMonth(4)
                .ExpectPaymentFreeMonth(dayNr: 14, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeDisabled(dayNr: 29, creditNr: CreditNumber)

                .ForMonth(5)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeDisabled(dayNr: 29, creditNr: CreditNumber)

                .ForMonth(6)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeDisabled(dayNr: 29, creditNr: CreditNumber)

                .ForMonth(7)
                .ExpectNotificationFullyPaid(dayNr: 28, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeAllowed(dayNr: 29, creditNr: CreditNumber)
                .ExpectPaymentFreeMonthRequested(dayNr: 30, creditNr: CreditNumber, paymentFreeMonthNr: 8)

                .ForMonth(8)
                .ExpectPaymentFreeMonth(dayNr: 14, creditNr: CreditNumber, dueDay: 28)
                .ExpectToggleNextMonthPaymentFreeDisabled(dayNr: 29, creditNr: CreditNumber)

                .End();

            RunTest(assertion, datesToAddPaymentFreeMonth: new[] { (MonthNr: 3, DayNr: 30), (MonthNr: 7, DayNr: 30) });
        }

        private void RunTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, (int MonthNr, int DayNr)[] datesToAddPaymentFreeMonth)
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(x =>
            {
                new Test(x).RunTest(assertion, datesToAddPaymentFreeMonth);
            });
        }

        private class Test : UlLegacyRunMonthTester
        {
            public Test(UlLegacyTestRunner.TestSupport support, bool debugPrint = false) : base(support, debugPrint) { }

            public void RunTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, (int MonthNr, int DayNr)[] datesToAddPaymentFreeMonth)
            {
                Support.Now = new DateTimeOffset(2023, 1, 1, 5, 0, 0, TimeSpan.FromHours(2));
                foreach(var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
                {
                    RunOneMonth(
                        doAfterDay: dayNr =>
                        {
                            assertion.DoAssert(Support, monthNr, dayNr);
                        }, 
                        doBeforeDay: dayNr =>
                        {
                            if(monthNr == 1 && dayNr == 1)
                                CreditsUlLegacy.CreateCredit(Support, creditNumber: 1);
                            if(datesToAddPaymentFreeMonth.Any(x => x.MonthNr == monthNr && x.DayNr == dayNr))
                            {
                                var s = Support.GetRequiredService<AmortizationPlanService>();
                                var isPaymentFreeMonthAllowed = s.GetAmortizationPlan(CreditNumber).Items.First(x => 
                                    x.EventTypeCode == BusinessEventType.NewNotification.ToString()
                                    && Month.ContainingDate(x.EventTransactionDate).Equals(Support.CurrentMonth.NextMonth))
                                .IsPaymentFreeMonthAllowed ?? false;

                                if (isPaymentFreeMonthAllowed)
                                    s.AddFuturePaymentFreeMonth(CreditNumber, Support.CurrentMonth.NextMonth.GetDayDate(1), false);
                                else
                                    Assert.Fail($"Payment free month not allowed on month {monthNr} and day {dayNr}");
                            }
                        }, payNotificationsOnDueDate: true);
                }
            }
        }

        private const string CreditNumber = "C9871";
    }
}
