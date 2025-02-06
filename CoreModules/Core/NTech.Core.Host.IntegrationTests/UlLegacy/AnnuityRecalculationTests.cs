using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Banking.LoanModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public class AnnuityRecalculationTests
    {
        [Test]
        public void NeverPaidBackLoan_WhenNotRecalculating_IsStillNeverPaidBack_AfterInterestChange()
        {
            var initialAmount = 6000m;
            var initialMarginRate = 15m;
            var initialReferenceRate = 0.0m;
            var changedReferenceRate = 5.0m;
            var initialAnnuity = 0.01m;

            TestReferenceInterestChange(initialAmount, initialMarginRate, initialReferenceRate, initialAnnuity, changedReferenceRate, false,
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.EqualTo(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.Null);
                },
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.EqualTo(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.Null);
                }, allowNeverRepaidLoans: true);
        }

        [Test]
        public void PaidBackLoan_WhenNotRecalculating_TurnsNeverPaidBack_AfterInterestChange()
        {
            var initialAmount = 6000m;
            var initialMarginRate = 0.01m;
            var initialReferenceRate = 0.0m;
            var changedReferenceRate = 19.99m;

            var initialAnnuity = PaymentPlanCalculation
                .BeginCreateWithRepaymentTime(initialAmount, 132, initialReferenceRate + initialMarginRate, true, null, false)
                .EndCreate()
                .AnnuityAmount;

            TestReferenceInterestChange(initialAmount, initialMarginRate, initialReferenceRate, initialAnnuity, changedReferenceRate, false,
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.GreaterThan(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.GreaterThan(0));
                },
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.EqualTo(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.Null);
                });
        }

        [Test]
        public void NeverPaidBackLoan_WhenRecalculating_ChangesToPaidBackAfterMaxMonths_AfterInterestChange()
        {
            var initialAmount = 6000m;
            var initialMarginRate = 15m;
            var initialReferenceRate = 0.0m;
            var changedReferenceRate = 5.0m;
            var initialAnnuity = 0.01m;

            TestReferenceInterestChange(initialAmount, initialMarginRate, initialReferenceRate, initialAnnuity, changedReferenceRate, true,
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.EqualTo(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.Null);
                },
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.GreaterThan(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.GreaterThan(0));
                }, allowNeverRepaidLoans: true);
        }

        [Test]
        public void PaidBackLoan_WhenRecalculating_MaintainsRepaymentMonths_AfterInterestChange()
        {
            var initialAmount = 6000m;
            var initialMarginRate = 0.01m;
            var initialReferenceRate = 0.0m;
            var changedReferenceRate = 19.99m;
            var initialAnnuity = PaymentPlanCalculation
                .BeginCreateWithRepaymentTime(initialAmount, 132, initialReferenceRate + initialMarginRate, true, null, false)
                .EndCreate()
                .AnnuityAmount;

            int firstRepaymentTime = 0;
            TestReferenceInterestChange(initialAmount, initialMarginRate, initialReferenceRate, initialAnnuity, changedReferenceRate, true,
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.GreaterThan(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.GreaterThan(0));
                    firstRepaymentTime = n.RemainingRepaymentTimeInMonthsAfter.Value;
                },
                n =>
                {
                    Assert.That(n.NotificationCapitalAmount, Is.GreaterThan(0));
                    Assert.That(n.NotificationAmount, Is.GreaterThan(0));
                    Assert.That(n.RemainingRepaymentTimeInMonthsAfter, Is.AnyOf(firstRepaymentTime, firstRepaymentTime - 1, firstRepaymentTime + 1));
                });
        }

        private class TestState
        {
            public decimal AnnuityAmount { get; set; }
            public decimal NotificationCapitalAmount { get; set; }
            public int? RemainingRepaymentTimeInMonthsAfter { get; set; }
            public decimal NotificationAmount { get; set; }
        }

        private void TestReferenceInterestChange(
            decimal initialAmount, decimal initialMarginRate,
            decimal initialReferenceRate, decimal initialAnnuity,
            decimal changedReferenceRate, bool recalculateAnnityOnChange,
            Action<TestState> afterFirstNotification, Action<TestState> afterSecondNotification,
            bool allowNeverRepaidLoans = false)
        {
            UlLegacyTestRunner.RunTestStartingFromEmptyDatabases(support =>
            {
                support.ShouldRecalculateAnnuityOnInterestChange = recalculateAnnityOnChange;

                var creditNr = CreditsUlLegacy.CreateCredit(support, 1,
                    creditAmount: initialAmount, referenceInterestRatePercent: initialReferenceRate, marginInterestRatePercent: initialMarginRate,
                    annuityAmount: initialAnnuity).CreditNr;

                var runner = new UlLegacyRunMonthTester(support);

                var paymentOrderService = support.GetRequiredService<PaymentOrderService>();
                TestState LookupNotificationState(int notificationsSkipCount)
                {
                    using var context = support.CreateCreditContextFactory().CreateContext();
                    var notification = CreditNotificationDomainModel.CreateForCredit(creditNr, context, paymentOrderService.GetPaymentOrderItems(), onlyFetchOpen: false).Values.OrderBy(x => x.NotificationDate).Skip(notificationsSkipCount).First();
                    var credit = CreditDomainModel.PreFetchForSingleCredit(creditNr, context, support.CreditEnvSettings);
                    var notifiedCapitalAmount = notification.GetRemainingBalance(support.Clock.Today, CreditDomainModel.AmountType.Capital);
                    var annuityAmount = credit.GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.AnnuityAmount);
                    var referenceInterestRate = credit.GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.ReferenceInterestRate);
                    var marginInterestRate = credit.GetDatedCreditValue(support.Clock.Today, nCredit.DatedCreditValueCode.MarginInterestRate);
                    var notNotifiedCapitalBalance = credit.GetNotNotifiedCapitalBalance(support.Clock.Today);

                    int? remainingRepaymentTimeInMonthsAfter;
                    try
                    {
                        remainingRepaymentTimeInMonthsAfter = PaymentPlanCalculation
                            .BeginCreateWithAnnuity(notNotifiedCapitalBalance, annuityAmount, referenceInterestRate + marginInterestRate, null, support.CreditEnvSettings.CreditsUse360DayInterestYear)
                            .EndCreate()
                            .Payments
                            .Count;
                    }
                    catch
                    {
                        remainingRepaymentTimeInMonthsAfter = null;
                    }

                    return new TestState
                    {
                        NotificationCapitalAmount = notifiedCapitalAmount,
                        AnnuityAmount = annuityAmount,
                        RemainingRepaymentTimeInMonthsAfter = remainingRepaymentTimeInMonthsAfter,
                        NotificationAmount = notification.GetRemainingBalance(support.Clock.Today)
                    };
                }

                runner.RunOneMonth((
                    Notification: NotificationExpectedResultCode.NotificationCreated,
                    IsMidMonthReminderSent: false,
                    IsEndOfMonthReminderSent: false,
                    IsTerminationLetterSent: false,
                    IsSentToDebtCollection: false),
                    doAfterNotification: () =>
                    {
                        var notificationState = LookupNotificationState(0);
                        afterFirstNotification(notificationState);

                        Credits.CreateAndPlaceUnplacedPayments(support, new Dictionary<string, decimal>
                        {
                            { creditNr, notificationState.NotificationAmount }
                        });
                    },
                    doAfterTerminationLetters: () =>
                    {
                        var s = new ReferenceInterestChangeService(x => x, new KeyValueStoreService(() => support.CreateCreditContextFactory().CreateContext()),
                            support.Clock, new LegalInterestCeilingService(support.CreditEnvSettings), support.CreditEnvSettings,
                            support.ClientConfiguration, support.CreateCreditContextFactory());
                        var changeModel = s.BeginChangeReferenceInterest(changedReferenceRate, support.CurrentUser);
                        var wasInterestChanged = s.TryChangeReferenceInterest(changeModel, support.CurrentUser, out var failedMessage);

                        using var context = support.CreateCreditContextFactory().CreateContext();
                    });

                runner.RunOneMonth((
                    Notification: NotificationExpectedResultCode.NotificationCreated,
                    IsMidMonthReminderSent: false,
                    IsEndOfMonthReminderSent: false,
                    IsTerminationLetterSent: false,
                    IsSentToDebtCollection: false),
                    doAfterNotification: () =>
                    {
                        var notificationState = LookupNotificationState(1);
                        afterSecondNotification(notificationState);
                    });
            }, setupClientConfig: x =>
            {
                if (allowNeverRepaidLoans)
                    x.Setup(x => x.IsFeatureEnabled("ntech.allowneverrepaid")).Returns(true);
            });
        }
    }
}
