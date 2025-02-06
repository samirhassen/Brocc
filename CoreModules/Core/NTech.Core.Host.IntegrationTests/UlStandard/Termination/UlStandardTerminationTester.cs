using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlStandard.Utilities;
using static NTech.Core.Host.IntegrationTests.UlStandard.Termination.UlStandardTerminationTests;

namespace NTech.Core.Host.IntegrationTests.UlStandard.Termination
{
    internal class UlStandardTerminationTester
    {
        public UlStandardTerminationTester(TestVariationCode variation, UlStandardTestRunner.TestSupport support)
        {
            Variation = variation;
            Support = support;
        }

        public void RunOneMonth((NotificationExpectedResultCode Notification, bool IsMidMonthReminderSent, bool IsEndOfMonthReminderSent, bool IsTerminationLetterSent, bool IsSentToDebtCollection) creditExpectation,
            Action? doBeforeDebtCollection = null,
            Action? doAfterNotification = null,
            Action? doBeforeTerminationLetters = null,
            Action? doAfterTerminationLetters = null,
            Action? doBeforeNotification = null,
            Action? doAfterDebtCollection = null,
            int? midMonthReminderCount = null,
            int? endOfMonthReminderCount = null,
            Action<IDictionary<string, object>>? observeTerminationLetterPrintContext = null,
            decimal? expectedNotNotifiedInterestAmountOnDebtCollection = null)
        {
            TestContext.WriteLine($"Month {Support.Now:yyyy-MM}");

            var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
            var creditNr = credit.CreditNr;
            var customerId = credit.CreditCustomers.Single().CustomerId;

            Support.MoveToNextDayOfMonth(14);

            doBeforeNotification?.Invoke();
            Credits.NotifyCredits(Support, (creditNr, customerId, creditExpectation.Notification));
            doAfterNotification?.Invoke();

            Credits.RemindCredits(Support, (creditNr, creditExpectation.IsMidMonthReminderSent ? midMonthReminderCount ?? 1 : 0));

            doBeforeTerminationLetters?.Invoke();
            Credits.CreateTerminationLetters(Support, observeTerminationLetterPrintContext, (creditNr, creditExpectation.IsTerminationLetterSent));
            doAfterTerminationLetters?.Invoke();

            Support.MoveToNextDayOfMonth(20);

            doBeforeDebtCollection?.Invoke();
            Credits.SendCreditsToDebtCollectionExtended(Support, (creditNr, creditExpectation.IsSentToDebtCollection, expectedNotNotifiedInterestAmountOnDebtCollection));
            doAfterDebtCollection?.Invoke();

            Support.MoveToNextDayOfMonth(28);
            Credits.RemindCredits(Support, (creditNr, creditExpectation.IsEndOfMonthReminderSent ? endOfMonthReminderCount ?? 1 : 0));
        }

        public void RunTest()
        {
            /*
             Schedule:             
             14: Notification, Reminders, TerminationLetters
             20:  DebtCollection
             28: Reminders
             */
            Support.Now = new DateTimeOffset(2022, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));

            TestContext.WriteLine($"Month {Support.Now:yyyy-MM}");
            {
                Support.MoveToNextDayOfMonth(14);

                Credits.NotifyCredits(Support);
                Credits.RemindCredits(Support);
                Credits.CreateTerminationLetters(Support);

                Support.MoveToNextDayOfMonth(20);

                Credits.SendCreditsToDebtCollection(Support);

                Support.MoveToNextDayOfMonth(28);
                Credits.RemindCredits(Support);

                Support.MoveToNextDayOfMonth(29);

                CreditsUlStandard.CreateCredit(Support, 1);
            }

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
            {
                NoPaymentsStandardCase();
            }
            else if (Variation == TestVariationCode.DebtCollectionExportAfterSuspensionWithoutNewTerminationLetter)
            {
                DebtCollectionExportAfterSuspensionWithoutNewTerminationLetter();
            }
            else if (Variation == TestVariationCode.TerminationLetterPreventedByPostpone)
            {
                TerminationLetterPreventedByPostpone();
            }
            else if (Variation == TestVariationCode.InactivatedTerminationLetterPreventsDebtCollection)
            {
                InactivatedTerminationLetterPreventsDebtCollection();
            }
            else if (Variation == TestVariationCode.TerminationLetterNonOverDuePreventNewOne)
            {
                TerminationLetterNonOverDuePreventNewOne();
            }
            else if (Variation == TestVariationCode.PartialNotificationPaymentDoesNotPreventTerminatonLetter)
            {
                PartialNotificationPaymentDoesNotPreventTerminatonLetter();
            }
            else if (Variation == TestVariationCode.FullNotificationPaymentPreventsTerminationLetter)
            {
                FullNotificationPaymentPreventsTerminationLetter();
            }
            else if (Variation == TestVariationCode.OldestNotificationOnlyPaymentDoesNotPreventDebtCollection)
            {
                OldestNotificationOnlyPaymentDoesNotPreventDebtCollection();
            }
            else if (Variation == TestVariationCode.TerminationLetterOverDueNotificationPaymentDoesPreventDebtCollection)
            {
                TerminationLetterOverDueNotificationPaymentDoesPreventDebtCollection();
            }
            else if (Variation == TestVariationCode.AllOverdueNotificationsPaymentPreventsDebtCollection)
            {
                AllOverdueNotificationsPaymentPreventsDebtCollection();
            }
            else
            {
                throw new NotImplementedException();
            }
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
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: true),
                expectedNotNotifiedInterestAmountOnDebtCollection: Math.Round(5500m * 9.56m / 100m / 365.25m * 23m, 2)); //23 interest days

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));
        }

        /// <summary>
        /// The client postpones debt collection which causes the process to be delayed by one month but
        /// the credit is sent automatically next month.
        /// </summary>
        private void DebtCollectionExportAfterSuspensionWithoutNewTerminationLetter()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeDebtCollection: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    Credits.PostponeOrResumeDebtCollection(Support, credit.CreditNr, Support.Clock.Today.AddDays(1));
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
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    Credits.PostponeTerminationLetter(Support, credit.CreditNr, Support.Clock.Today.AddDays(1));
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
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
        /// The client inactivates the termination letter which prevents debt collection
        /// </summary>
        private void InactivatedTerminationLetterPreventsDebtCollection()
        {
            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false));

            var creditNr = CreditsUlStandard.GetCreateCredit(Support, 1).CreditNr;
            var amortizationPlan = CreditsUlStandard.GetCreditAmortizationPlan(Support, creditNr, notificationSettings);
            Assert.That(amortizationPlan.Items.Where(x => !x.IsFutureItem).Last().IsTerminationLetterProcessSuspension, Is.True, "Amortization plan shows suspension");

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeDebtCollection: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    Credits.InactivateTerminationLetter(Support, credit.CreditNr);
                },
                endOfMonthReminderCount: 2);

            amortizationPlan = CreditsUlStandard.GetCreditAmortizationPlan(Support, creditNr, notificationSettings);
            Assert.That(amortizationPlan.Items.Where(x => !x.IsFutureItem).Last().IsTerminationLetterProcessReActivation, Is.True, "Amortization plan shows re-activation");

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
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
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doAfterTerminationLetters: () =>
                {
                    Support.MoveForwardNDays(1); //Move the the 15th
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
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
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doBeforeNotification: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    using var context = Support.CreateCreditContextFactory().CreateContext();
                    var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                    var paymentAmount = Math.Round(oldestNotification.GetRemainingBalance(Support.Clock.Today) / 2m);
                    Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
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
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    using var context = Support.CreateCreditContextFactory().CreateContext();
                    var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                    var paymentAmount = oldestNotification.GetRemainingBalance(Support.Clock.Today);
                    Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
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
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                doAfterTerminationLetters: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    using var context = Support.CreateCreditContextFactory().CreateContext();
                    var oldestNotification = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values.OrderBy(x => x.DueDate).First();
                    var paymentAmount = oldestNotification.GetRemainingBalance(Support.Clock.Today);
                    Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, paymentAmount);
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
        /// that does prevent debt collection.
        /// </summary>
        private void TerminationLetterOverDueNotificationPaymentDoesPreventDebtCollection()
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
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    using var context = Support.CreateCreditContextFactory().CreateContext();
                    var notifications = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values;
                    overDueWhenTerminationLetterSentAmount = notifications.Where(x => x.DueDate <= Support.Clock.Today).Sum(x => x.GetRemainingBalance(Support.Clock.Today));
                },
                doAfterTerminationLetters: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    Credits.CreateAndPlaceUnplacedPayment(Support, credit.CreditNr, overDueWhenTerminationLetterSentAmount);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        Assert.That(
                            context.CreditTerminationLetterHeadersQueryable.Single().InactivatedByBusinessEventId,
                            Is.Not.Null, "Termination letter inactivated by event");
                    }
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));
        }

        private void AllOverdueNotificationsPaymentPreventsDebtCollection()
        {
            IDictionary<string, object>? terminationLetterPrintContext = null;
            decimal? allOverdueNotificationsAmount = null;

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: true,
                IsSentToDebtCollection: false),
                observeTerminationLetterPrintContext: printContext => terminationLetterPrintContext = printContext,
                doAfterTerminationLetters: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);
                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        var notifications = CreditNotificationDomainModel.CreateForCredit(credit.CreditNr, context, Support.PaymentOrder(), onlyFetchOpen: true).Values;
                        allOverdueNotificationsAmount = notifications.Where(x => x.DueDate <= Support.Clock.Today).Sum(x => x.GetRemainingBalance(Support.Clock.Today));

                        //Make sure the letter has this same amount
                        TestContext.WriteLine(JsonConvert.SerializeObject(terminationLetterPrintContext, Formatting.Indented));
                        Assert.That(
                            terminationLetterPrintContext?.Opt("notifiedOverdueDebt"),
                            Is.EqualTo(allOverdueNotificationsAmount?.ToString("C", Support.FormattingCulture)),
                            "Termination latter to pay amount");
                    }
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotNotified,
                IsMidMonthReminderSent: false,
                IsEndOfMonthReminderSent: true,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false),
                doBeforeDebtCollection: () =>
                {
                    var credit = CreditsUlStandard.GetCreateCredit(Support, 1);

                    Assert.That(allOverdueNotificationsAmount, Is.Not.Null);

                    Credits.CreateAndImportPaymentFile(Support, new Dictionary<string, decimal> { { credit.CreditNr, allOverdueNotificationsAmount.Value } });

                    using (var context = Support.CreateCreditContextFactory().CreateContext())
                    {
                        Assert.That(
                            context.CreditTerminationLetterHeadersQueryable.Single().InactivatedByBusinessEventId,
                            Is.Not.Null, "Termination letter inactivated by event");
                    }
                });

            RunOneMonth((
                Notification: NotificationExpectedResultCode.NotificationCreated,
                IsMidMonthReminderSent: true,
                IsEndOfMonthReminderSent: false,
                IsTerminationLetterSent: false,
                IsSentToDebtCollection: false));
        }

        private readonly NotificationProcessSettings notificationSettings = new NotificationProcessSettings
        {
            NotificationNotificationDay = 14,
            NotificationDueDay = 25,
            PaymentFreeMonthMinNrOfPaidNotifications = 3,
            PaymentFreeMonthExcludeNotificationFee = false,
            PaymentFreeMonthMaxNrPerYear = 0,
            ReminderFeeAmount = 60,
            ReminderMinDaysBetween = 7,
            SkipReminderLimitAmount = 0,
            NotificationOverDueGraceDays = 5,
            MaxNrOfReminders = 2,
            AreBackToBackPaymentFreeMonthsAllowed = false,
            MaxNrOfRemindersWithFees = 1,
            NrOfFreeInitialReminders = 0,
            FirstReminderDaysBefore = 7
        };

        public TestVariationCode Variation { get; }
        public UlStandardTestRunner.TestSupport Support { get; }
    }
}
