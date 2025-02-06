using nCredit.DbModel.BusinessEvents;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public class WriteOffNotificationsOnPaymentTests
    {
        [Test]
        public void PartiallyWrittenOffNotification_ShouldBeClosed()
        {
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNewLoan(dayNr: 1, CreditNr, loanAmount: LoanAmount)

                .ExpectNotification(dayNr: 14, CreditNr, dueDay: 28, initialAmount: NotificationTotalAmount, capitalAmount: NotifiedCapitalAmount)
                .ExpectLoanBalance(dayNr: 14, CreditNr, notNotifiedCapitalAmount: LoanAmount - NotifiedCapitalAmount)

                .ExpectNotificationPartiallyPaid(dayNr: 15, CreditNr, dueDay: 28, balanceAfterAmount: 0m,
                    writtenOffNotNotifiedCapitalAmount: NotifiedCapitalAmount, isClosed: true)
                .ExpectedUnplacedBalanceAmount(dayNr: 15, NotifiedCapitalAmount)
                .ExpectLoanBalance(dayNr: 15, CreditNr, notNotifiedCapitalAmount: LoanAmount, capitalAmount: LoanAmount)

                .End();

            var action = CreditCycleActionBuilder<UlLegacyTestRunner.TestSupport>
                .Begin()

                .ForMonth(1)
                .AddAction(dayNr: 1, t => CreditsUlLegacy.CreateCredit(t.Support, 1))
                .AddAction(dayNr: 15, t => Credits.CreateAndPlaceUnplacedPayment(t.Support, CreditNr, NotificationTotalAmount, instruction =>
                {
                    //Simulate the user choosing to write off the capital of the notification instead of placing against it
                    var notificationCapitalItem = instruction.NotificationPlacementItems.Single(x => x.CostTypeUniqueId == "b_Capital");
                    notificationCapitalItem.AmountWrittenOff = notificationCapitalItem.AmountCurrent;
                    notificationCapitalItem.AmountPlaced = 0m;
                }))

                .End();

            UlLegacyAssertionTest.RunTest(assertion, action);
        }

        [Test]
        public void FullyPaidNotification_ShouldBeClosed()
        {
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNewLoan(dayNr: 1, CreditNr, loanAmount: LoanAmount)
                .ExpectNotification(dayNr: 14, CreditNr, dueDay: 28, initialAmount: NotificationTotalAmount, capitalAmount: NotifiedCapitalAmount)
                .ExpectNotificationPartiallyPaid(dayNr: 15, CreditNr, dueDay: 28, balanceAfterAmount: 0m, writtenOffNotNotifiedCapitalAmount: 0m, isClosed: true)
                .ExpectLoanBalance(dayNr: 15, CreditNr, notNotifiedCapitalAmount: LoanAmount - NotifiedCapitalAmount, capitalAmount: LoanAmount - NotifiedCapitalAmount)
                .ExpectedUnplacedBalanceAmount(dayNr: 15, 0m)

                .End();

            var action = CreditCycleActionBuilder<UlLegacyTestRunner.TestSupport>
                .Begin()

                .ForMonth(1)
                .AddAction(dayNr: 1, t => CreditsUlLegacy.CreateCredit(t.Support, 1))
                .AddAction(dayNr: 15, t => Credits.CreateAndPlaceUnplacedPayment(t.Support, CreditNr, NotificationTotalAmount))

                .End();

            UlLegacyAssertionTest.RunTest(assertion, action);
        }

        const decimal LoanAmount = 6000m;
        const decimal NotificationTotalAmount = 195.77m;
        const decimal NotifiedCapitalAmount = 149.78m;
        const string CreditNr = "C9871";
    }
}