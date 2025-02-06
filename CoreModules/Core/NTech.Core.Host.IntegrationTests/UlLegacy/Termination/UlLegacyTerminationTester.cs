using nCredit.DomainModel;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using static NTech.Core.Host.IntegrationTests.UlLegacy.Termination.UlLegacyTerminationTests;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Termination
{
    internal class UlLegacyTerminationTester : UlLegacyRunMonthTester
    {
        public UlLegacyTerminationTester(TestVariationCode variation, UlLegacyTestRunner.TestSupport support) : base(support)
        {
            Variation = variation;
        }

        public void RunTest()
        {
            /*
             Schedule:
             9:  DebtCollection
             14: Notification, Reminders, TerminationLetters
             28: Reminders
             */
            Support.Now = new DateTimeOffset(2022, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));

            TestContext.WriteLine($"Month {Support.Now.ToString("yyyy-MM")}");
            {
                Support.MoveToNextDayOfMonth(9);

                Credits.SendCreditsToDebtCollection(Support);

                Support.MoveToNextDayOfMonth(14);

                Credits.NotifyCredits(Support);
                Credits.RemindCredits(Support);
                Credits.CreateTerminationLetters(Support);

                Support.MoveToNextDayOfMonth(28);
                Credits.RemindCredits(Support);

                Support.MoveToNextDayOfMonth(29);

                CreditsUlLegacy.CreateCredit(Support, 1);
            }

            Support.MoveToNextDayOfMonth(1);

            /// Run three up until the first month where a termination letter could be sent which is common for all cases
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));

            if (Variation == TestVariationCode.NoPaymentsStandardCase)
                NoPaymentsStandardCase();
            else if (Variation == TestVariationCode.DebtCollectionPreventedByNewNotification)
                DebtCollectionPreventedByNewNotification();
            else if (Variation == TestVariationCode.TerminationLetterPreventedByPostpone)
                TerminationLetterPreventedByPostpone();
            else if (Variation == TestVariationCode.TerminationLetterNonOverDuePreventNewOne)
                TerminationLetterNonOverDuePreventNewOne();
            else if (Variation == TestVariationCode.PartialNotificationPaymentDoesNotPreventTerminatonLetter)
                PartialNotificationPaymentDoesNotPreventTerminatonLetter();
            else if (Variation == TestVariationCode.FullNotificationPaymentPreventsTerminationLetter)
                FullNotificationPaymentPreventsTerminationLetter();
            else if (Variation == TestVariationCode.OldestNotificationOnlyPaymentDoesNotPreventDebtCollection)
                OldestNotificationOnlyPaymentDoesNotPreventDebtCollection();
            else if (Variation == TestVariationCode.TerminationLetterOverDueNotificationPaymentDoesNotPreventDebtCollection)
                TerminationLetterOverDueNotificationPaymentDoesNotPreventDebtCollection();
            else if (Variation == TestVariationCode.AllOverdueNotificationsPaymentDoesPreventDebtCollection)
                AllOverdueNotificationsPaymentDoesPreventDebtCollection();
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// No payments are made and no special action is taken by the client so after kk3
        /// a termination letter is sent and the month after the credit is sent to debt collection
        /// which writes it off so after that nothing else happens.
        /// </summary>
        private void NoPaymentsStandardCase()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: true));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));
        }

        /// <summary>
        /// The client postpones debt collection during the first run so the credit is not sent
        /// 
        /// The client the triggers debt collection a second time during the same month after notifications are sent
        /// and the credit is then again not sent since the new notification prevents it.
        /// </summary>
        private void DebtCollectionPreventedByNewNotification()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doBeforeDebtCollection: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    Credits.PostponeOrResumeDebtCollection(Support, credit.CreditNr, Support.Clock.Today.AddDays(1));
                },
                doAfterNotification: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    Credits.SendCreditsToDebtCollection(Support, (credit.CreditNr, false));
                });

            RunOneMonth((
                 Notification: NotificationExpectedResultCode.NotNotified,
                 IsMidMonthReminderSent: false,
                 IsEndOfMonthReminderSent: false,
                 IsTerminationLetterSent: false,
                 IsSentToDebtCollection: true));
        }

        /// <summary>
        /// Termination letter postponed which delays the normal process by one month.
        /// </summary>
        private void TerminationLetterPreventedByPostpone()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeTerminationLetters: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    Credits.PostponeTerminationLetter(Support, credit.CreditNr, Support.Clock.Today.AddDays(1));
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: true));
        }

        /// <summary>
        /// The client triggers termination letters again by accident but no letter is sent since one already exists that is not overdue
        /// </summary>
        private void TerminationLetterNonOverDuePreventNewOne()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doAfterTerminationLetters: () =>
                {
                    Support.MoveForwardNDays(1); //Move the the 15th
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    Credits.CreateTerminationLetters(Support, (credit.CreditNr, false));
                });
        }

        /// <summary>
        /// A small partial payment of the oldest notification does not prevent the termination letter
        /// </summary>
        private void PartialNotificationPaymentDoesNotPreventTerminatonLetter()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doBeforeNotification: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                        var paymentAmount = Math.Round(oldestNotification.GetRemainingBalance(Support.Clock.Today) / 2m);
                        Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
                    }
                });
        }

        /// <summary>
        /// A full payment of the oldest notification postpones the termination letter
        /// </summary>
        private void FullNotificationPaymentPreventsTerminationLetter()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeNotification: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                        var paymentAmount = oldestNotification.GetRemainingBalance(Support.Clock.Today);
                        Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
                    }
                });
        }

        /// <summary>
        /// If the oldest unpaid notification is fully paid after the termination letter is sent,
        /// that is still not enough to prevent debt collection
        /// </summary>
        private void OldestNotificationOnlyPaymentDoesNotPreventDebtCollection()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doAfterTerminationLetters: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                        var paymentAmount = oldestNotification.GetRemainingBalance(Support.Clock.Today);
                        Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
                    }
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: true));
        }

        /// <summary>
        /// If all the notifications that were overdue when the termination letter was sent are fully paid,
        /// that is still not enough to prevent debt collection. All notifications overdue at the time of debt collection must be paid.
        /// </summary>
        private void TerminationLetterOverDueNotificationPaymentDoesNotPreventDebtCollection()
        {
            decimal overDueWhenTerminationLetterSentAmount = 0m;
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doBeforeTerminationLetters: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var notifications = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values;
                        overDueWhenTerminationLetterSentAmount = notifications.Where(x => x.DueDate <= Support.Clock.Today).Sum(x => x.GetRemainingBalance(Support.Clock.Today));
                    }
                },
                doAfterTerminationLetters: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, overDueWhenTerminationLetterSentAmount);
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: true));
        }

        private void AllOverdueNotificationsPaymentDoesPreventDebtCollection()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeDebtCollection: () =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var notifications = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values;
                        var allOverdueNotificationsAmount = notifications.Where(x => x.DueDate <= Support.Clock.Today).Sum(x => x.GetRemainingBalance(Support.Clock.Today));
                        Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, allOverdueNotificationsAmount);
                    }
                });
        }

        public TestVariationCode Variation { get; }
    }
}
