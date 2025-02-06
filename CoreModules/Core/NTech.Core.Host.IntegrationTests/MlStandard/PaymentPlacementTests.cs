using nCredit.DbModel.BusinessEvents;
using NTech.Core.Host.IntegrationTests.MlStandard.Utilities;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy;

namespace NTech.Core.Host.IntegrationTests.MlStandard
{
    internal class PaymentPlacementTests
    {
        private static string creditNr1 = CreditsMlStandard.GetCreditsWithAgreementCreditNr(1);
        private static string creditNr2 = CreditsMlStandard.GetCreditsWithAgreementCreditNr(2);
        private const string SharedPaymentOcr = "1111111108";
        private const decimal OneMillion = 1000000m;

        /*
         All testcases are two loans with 24 month binding time of one million each with 5.2% interest rate
         */

        [Test]
        public void CoNotified_SinglePayment_PaysBothNotifications()
        {
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotificationFullyPaid(28, creditNr1, dueDay: 28)
                .ExpectNotificationFullyPaid(28, creditNr2, dueDay: 28)
                .AssertCustom(28, x => Assert.That(x.Context.IncomingPaymentHeadersQueryable.Count(), Is.EqualTo(1), $"{x.Prefix}Expected single payment"))

                .End();

            RunTest(assertion, payDirectDebitOnSchedule: true);
        }

        [Test]
        public void SettlementAttemptUsingSharedOcr_LeftUnplaced_ThenPlacedManually()
        {
            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectExtraAmortization(18, creditNr1, OneMillion - 833m)
                .ExpectExtraAmortization(18, creditNr2, OneMillion - 833m)
                .ExpectCreditSettled(18, creditNr1)
                .ExpectCreditSettled(18, creditNr2)
                .ExpectedUnplacedBalanceAmount(18, 2992007.38m)

                .End();

            RunTest(assertion, payDirectDebitOnSchedule: false, beforeDay: x =>
            {
                if(x.MonthNr == 1 && x.DayNr == 17)
                {
                    Credits.CreateAndImportPaymentFileWithOcr(x.Support, new Dictionary<string, decimal> { { SharedPaymentOcr, OneMillion * 5m } });
                }
                else if(x.MonthNr == 1 && x.DayNr == 18)
                {
                    var paymentId = x.Support.WithCreditDb(context => context.IncomingPaymentFileHeaders.Single().Id);
                    Credits.PlaceUnplacedPaymentUsingSuggestion(x.Support, paymentId, SharedPaymentOcr);
                }
            });
        }

        [Test]
        public void SettlementAttemptUsingSingleOcr_AndSettlementOffer_IsPlaced()
        {
            const decimal SettlementAmount = 1006552.36m;

            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(2)

                .ExpectedUnplacedBalanceAmount(9, SettlementAmount)
                .ExpectPaidSwedishRseDebt(10, creditNr2, amount: 1000m)
                .ExpectPaidExtraInterest(10, creditNr2, amount: 1566.05m)
                .ExpectCreditSettled(10, creditNr2)
                .ExpectedUnplacedBalanceAmount(10, 0m)

                .End();
            
            const string OcrCredit2 = "1111111306"; //test utan avi
            RunTest(assertion, payDirectDebitOnSchedule: false, beforeDay: x =>
            {
                if (x.MonthNr == 2 && x.DayNr == 8)
                {
                    var mgr = x.Support.GetRequiredService<CreditSettlementSuggestionBusinessEventManager>();
                    var isOk = mgr.TryComputeSettlementSuggestion(creditNr2, x.Support.Clock.Today.AddDays(1), out var failedMessage, out var suggestion, swedishRseInterestRatePercent: 7m, forceSwedishRseAmount: 1000m);
                    Assert.IsTrue(isOk, failedMessage);
                    isOk = mgr.TryCreateAndSendSettlementSuggestion(creditNr2, x.Support.Clock.Today.AddDays(1), suggestion.swedishRse.estimatedAmount, suggestion.swedishRse.interestRatePercent, out failedMessage, out var offer, null);
                    Assert.That(offer.settlementAmount, Is.EqualTo(SettlementAmount));
                    Assert.IsTrue(isOk, failedMessage);
                }
                if (x.MonthNr == 2 && x.DayNr == 9)
                {
                    Credits.CreateAndImportPaymentFileWithOcr(x.Support, new Dictionary<string, decimal> { { OcrCredit2, SettlementAmount } });
                }
                else if (x.MonthNr == 2 && x.DayNr == 10)
                {
                    var paymentId = x.Support.WithCreditDb(context => context.IncomingPaymentFileHeaders.Single().Id);
                    Credits.PlaceUnplacedPaymentUsingSuggestion(x.Support, paymentId, OcrCredit2);
                }
            });
        }

        [Test]
        public void TwoMonthDelay_ManuallyPaidAfter()
        {
            const decimal Notification1Amount = 4819.31m;
            const decimal Notification2Amount = 5104.05m;
            const decimal NotificationFee = 20m;
            const decimal ReminderFee = 60m;
            const decimal PaidAmount = 2m * Notification1Amount + 2 * Notification2Amount + ReminderFee + 2 * NotificationFee - 100m;

            var assertion = CreditCycleAssertionBuilder
                .Begin()

                .ForMonth(1)
                .ExpectNotification(14, creditNr1, dueDay: 28, initialAmount: Notification1Amount + NotificationFee)
                .ExpectNotification(14, creditNr2, dueDay: 28, initialAmount: Notification1Amount)

                .ForMonth(2)
                .ExpectNotification(14, creditNr1, dueDay: 28, initialAmount: Notification2Amount + NotificationFee)
                .ExpectNotification(14, creditNr2, dueDay: 28, initialAmount: Notification2Amount)

                .ForMonth(3)
                .ExpectNotificationFullyPaid(3, creditNr1, 28, fromMonthNr: 1)
                .ExpectNotificationFullyPaid(3, creditNr1, 28, fromMonthNr: 2)
                .ExpectNotificationFullyPaid(3, creditNr2, 28, fromMonthNr: 1)
                .ExpectNotificationPartiallyPaid(3, creditNr2, 28, fromMonthNr: 2, balanceAfterAmount: 100m, capitalAfterAmount: 100m)
                .ExpectedUnplacedBalanceAmount(3, 0m)

                .End();

            RunTest(assertion, payDirectDebitOnSchedule: false, beforeDay: x =>
            {
                if (x.MonthNr == 3 && x.DayNr == 3)
                    Credits.CreateAndImportPaymentFileWithOcr(x.Support, new Dictionary<string, decimal> { { SharedPaymentOcr, PaidAmount } });
            });
        }

        private void RunTest(CreditCycleAssertionBuilder.CreditCycleAssertion assertion, bool payDirectDebitOnSchedule = false, Action<(int MonthNr, int DayNr, MlStandardTestRunner.TestSupport Support)>? beforeDay = null)
        {
            MlStandardTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.MoveToNextDayOfMonth(1);
                foreach (var monthNr in Enumerable.Range(1, assertion.MaxMonthNr))
                {
                    CreditsMlStandard.RunOneMonth(support, beforeDay: dayNr =>
                    {
                        if (dayNr == 1 && monthNr == 1)
                        {
                            CreditsMlStandard.CreateCreditsWithAgreement(support, minCreditIndex: 1, nrOfLoans: 2, interestRebindMounthCount: 24);
                        }
                        beforeDay?.Invoke((MonthNr: monthNr, DayNr: dayNr, Support: support));
                    }, creditCycleAssertion: (Assertion: assertion, MonthNr: monthNr), payDirectDebitOnSchedule: payDirectDebitOnSchedule);
                }
            });
        }
    }
}
