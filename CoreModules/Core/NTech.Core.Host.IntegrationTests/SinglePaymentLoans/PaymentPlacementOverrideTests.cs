using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans
{
    public class PaymentPlacementOverrideTests
    {        
        private const decimal NotifiedCapitalAmount = 6485.32m;
        private const decimal NotifiedInterestAmount = 619.3m;
        private const decimal NotifiedInitialFeeAmount = 149m;
        private const decimal NotifiedReminderFeeAmount = 60m;
        private const decimal NotNotifiedCapitalAmount = 3514.68m;
        private static decimal InitialPaymentAmount = NotifiedCapitalAmount + NotifiedInterestAmount + NotifiedInitialFeeAmount + NotifiedReminderFeeAmount + NotNotifiedCapitalAmount;

        [Test]
        public void TestPlaceWithoutRestrictions()
        {
            new Test(onlyPlacedAgainstNotified: false, onlyPlaceAgainstType: null, x => 
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr, 
                        notifiedCapitalAmount: NotifiedCapitalAmount, notifiedInterestAmount: NotifiedInterestAmount, notNotifiedCapitalAmount: NotNotifiedCapitalAmount, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: NotifiedReminderFeeAmount, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: NotifiedInitialFeeAmount),
                        leftUnplacedAmount: 0m)).RunTest();
        }

        [Test]
        public void TestPlaceOnlyNotified()
        {
            new Test(onlyPlacedAgainstNotified: true, onlyPlaceAgainstType: null, x =>
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr,
                        notifiedCapitalAmount: NotifiedCapitalAmount, notifiedInterestAmount: NotifiedInterestAmount, notNotifiedCapitalAmount: 0m, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: NotifiedReminderFeeAmount, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: NotifiedInitialFeeAmount),
                        leftUnplacedAmount: NotNotifiedCapitalAmount)).RunTest();
        }

        [Test]
        public void TestPlaceOnlyCapital()
        {
            new Test(onlyPlacedAgainstNotified: false, onlyPlaceAgainstType: PaymentOrderItem.FromAmountType(nCredit.DomainModel.CreditDomainModel.AmountType.Capital), x =>
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr,
                        notifiedCapitalAmount: NotifiedCapitalAmount, notifiedInterestAmount: 0m, notNotifiedCapitalAmount: NotNotifiedCapitalAmount, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: 0m, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: 0m),
                        leftUnplacedAmount: NotifiedInterestAmount + NotifiedReminderFeeAmount + NotifiedInitialFeeAmount)).RunTest();
        }

        [Test]
        public void TestPlaceOnlyNotifiedCapital()
        {
            new Test(onlyPlacedAgainstNotified: true, onlyPlaceAgainstType: PaymentOrderItem.FromAmountType(nCredit.DomainModel.CreditDomainModel.AmountType.Capital), x =>
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr,
                        notifiedCapitalAmount: NotifiedCapitalAmount, notifiedInterestAmount: 0m, notNotifiedCapitalAmount: 0m, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: 0m, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: 0m),
                        leftUnplacedAmount: NotNotifiedCapitalAmount + NotifiedInterestAmount + NotifiedReminderFeeAmount + NotifiedInitialFeeAmount)).RunTest();
        }

        [Test]
        public void TestPlaceOnlyInitialFee()
        {
            new Test(onlyPlacedAgainstNotified: true, onlyPlaceAgainstType: PaymentOrderItem.FromCustomCostCode("initialFeeNotification"), x =>
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr,
                        notifiedCapitalAmount: 0m, notifiedInterestAmount: 0m, notNotifiedCapitalAmount: 0m, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: 0m, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: NotifiedInitialFeeAmount),
                        leftUnplacedAmount: InitialPaymentAmount - NotifiedInitialFeeAmount)).RunTest();
        }

        [Test]
        public void TestPlaceOnlyNotifiedCapitalWithMaxAmount()
        {
            new Test(onlyPlacedAgainstNotified: true, onlyPlaceAgainstType: PaymentOrderItem.FromAmountType(nCredit.DomainModel.CreditDomainModel.AmountType.Capital), x =>
                x.ExpectPaymentPlaced(dayNr: 15, creditNr: CreditNr,
                        notifiedCapitalAmount: NotifiedCapitalAmount - 200m, notifiedInterestAmount: 0m, notNotifiedCapitalAmount: 0m, initialPaymentAmount: InitialPaymentAmount,
                        notifiedReminderFeeAmount: 0m, notifiedCustomAmount: (CustomCode: "initialFeeNotification", Amount: 0m),
                        leftUnplacedAmount: NotNotifiedCapitalAmount + NotifiedInterestAmount + NotifiedReminderFeeAmount + NotifiedInitialFeeAmount + 200m),
                maxPlacedAmount: NotifiedCapitalAmount - 200m).RunTest();
        }

        private class Test : SinglePaymentLoansTestRunner
        {
            private readonly bool onlyPlacedAgainstNotified;
            private readonly PaymentOrderItem? onlyPlaceAgainstType;
            private readonly Func<CreditCycleAssertionBuilder, CreditCycleAssertionBuilder> assertPlaced;
            private readonly decimal? maxPlacedAmount;

            public Test(bool onlyPlacedAgainstNotified, PaymentOrderItem? onlyPlaceAgainstType, Func<CreditCycleAssertionBuilder, CreditCycleAssertionBuilder> assertPlaced, decimal? maxPlacedAmount = null)
            {
                this.onlyPlacedAgainstNotified = onlyPlacedAgainstNotified;
                this.onlyPlaceAgainstType = onlyPlaceAgainstType;
                this.assertPlaced = assertPlaced;
                this.maxPlacedAmount = maxPlacedAmount;
            }

            protected override void DoTest()
            {
                Support.MoveToNextDayOfMonth(1);

                var creditNr = ShortTimeCredits.CreditNrFromIndex(1);

                var b = CreditCycleAssertionBuilder
                    .Begin()

                    //Month 1
                    .ForMonth(monthNr: 1)
                    .ExpectNotification(dayNr: 14, creditNr: creditNr, dueDay: 28, capitalAmount: 3253.34m, interestAmount: 298.97m)

                    //Month 2
                    .ForMonth(monthNr: 2)
                    .ExpectNotification(dayNr: 14, creditNr: creditNr, dueDay: 28, capitalAmount: 3231.98m, interestAmount: 320.33m);

                b = assertPlaced(b);

                var a = b.End();

                foreach (var monthNr in Enumerable.Range(1, a.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if(monthNr == 1 && dayNr == 1)
                        {
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: 3, isRepaymentTimeDays: false,
                                loanAmount: NotifiedCapitalAmount + NotNotifiedCapitalAmount, initialFeeOnFirstNotification: NotifiedInitialFeeAmount);
                        }

                        if(monthNr == 2 && dayNr == 15)
                        {
                            Credits.AddUnplacedPayments(Support, Support.CreditEnvSettings, (InitialPaymentAmount, "Test"));
                            using(var context = Support.CreateCreditContextFactory().CreateContext())
                            {
                                var paymentId = context.IncomingPaymentHeadersQueryable.Single().Id;
                                Credits.PlaceUnplacedPaymentUsingSuggestion(Support, paymentId, creditNr,
                                    onlyPlaceAgainstNotified: onlyPlacedAgainstNotified ? new bool?(true) : new bool?(),
                                    onlyPlaceAgainstPaymentOrderItemUniqueId: onlyPlaceAgainstType?.GetUniqueId(),
                                    maxPlacedAmount: maxPlacedAmount);
                            }
                        }
                    }, creditCycleAssertion: (Assertion: a, MonthNr: monthNr));
                }
            }
        }

        private const string CreditNr = "L10001";
    }
}
