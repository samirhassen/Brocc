using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;
using static NTech.Core.Host.IntegrationTests.Shared.CreditCycleAssertionBuilder;

namespace NTech.Core.Host.IntegrationTests.UlLegacy.Termination
{
    internal class UlLegacyRunMonthTester
    {
        private readonly bool debugPrint;

        public UlLegacyRunMonthTester(UlLegacyTestRunner.TestSupport support, bool debugPrint = false)
        {
            Support = support;
            this.debugPrint = debugPrint;
        }

        public virtual void RunOneMonth(
            (NotificationExpectedResultCode Notification, bool IsMidMonthReminderSent, bool IsEndOfMonthReminderSent, bool IsTerminationLetterSent, bool IsSentToDebtCollection)? creditExpectation = null,
            Action? doBeforeDebtCollection = null,
            Action? doAfterNotification = null,
            Action? doBeforeTerminationLetters = null,
            Action? doAfterTerminationLetters = null,
            Action? doBeforeNotification = null,
            Action<int>? doBeforeDay = null,
            Action<int>? doAfterDay = null,
            int? midMonthReminderCountExpected = null,
            int? endOfMonthReminderCountExpected = null,
            bool payNotificationsOnDueDate = false,
            (CreditCycleAssertion Assertion, int MonthNr)? creditCycleAssertion = null,
            (CreditCycleAction<UlLegacyTestRunner.TestSupport> Action, int MonthNr)? creditCycleAction = null
            )
        {
            var month = Month.ContainingDate(Support.Now.Date);
            if (debugPrint)
            {
                DebugPrinter.PrintMonthStart(Support);
            }

            while (Support.Clock.Today <= month.LastDate)
            {
                var dayNr = Support.Clock.Today.Day;
                Lazy<(int CustomerId, string CreditNr)> credit = new Lazy<(int CustomerId, string CreditNr)>(() =>
                {
                    var credit = CreditsUlLegacy.GetCreateCredit(Support, 1);
                    return (CustomerId: credit.CreditCustomers.Single().CustomerId, CreditNr: credit.CreditNr);
                });

                string CreditNr() => credit.Value.CreditNr;
                int CustomerId() => credit.Value.CustomerId;

                doBeforeDay?.Invoke(dayNr);

                if (dayNr == 9)
                {
                    doBeforeDebtCollection?.Invoke();
                    Credits.SendCreditsToDebtCollection(Support, (CreditNr(), creditExpectation?.IsSentToDebtCollection));
                }
                else if (dayNr == 14)
                {
                    doBeforeNotification?.Invoke();
                    Credits.NotifyCredits(Support, (CreditNr(), CustomerId(), creditExpectation?.Notification));
                    doAfterNotification?.Invoke();

                    Credits.RemindCredits(Support, (CreditNr(), creditExpectation.HasValue
                        ? (creditExpectation.Value.IsMidMonthReminderSent ? midMonthReminderCountExpected ?? 1 : 0)
                        : new int?()));

                    doBeforeTerminationLetters?.Invoke();
                    Credits.CreateTerminationLetters(Support, (CreditNr(), creditExpectation?.IsTerminationLetterSent));
                    doAfterTerminationLetters?.Invoke();
                }
                else if (dayNr == 28)
                {
                    Credits.RemindCredits(Support, (CreditNr(), creditExpectation.HasValue
                        ? (creditExpectation.Value.IsEndOfMonthReminderSent ? endOfMonthReminderCountExpected ?? 1 : 0)
                        : new int?()));
                    if (payNotificationsOnDueDate)
                    {
                        Credits.PayOverdueNotifications(Support);
                    }
                }

                if (AlternatePaymentPlanService.IsPaymentPlanEnabledShared(Support.ClientConfiguration))
                {
                    var paymentPlanService = Support.GetRequiredService<AlternatePaymentPlanSecureMessagesService>();
                    var messageResult = paymentPlanService.SendEnabledSecureMessages();
                    var errorText = string.Join(",", messageResult.Errors ?? new List<string>()).Trim() ?? "";
                    Assert.That(errorText.Length, Is.Zero, "SendEnabledSecureMessages alt paymentplan:" + errorText);
                }

                doAfterDay?.Invoke(dayNr);
                if (debugPrint)
                {
                    DebugPrinter.PrintDay(Support, Support.Clock.Today);
                }

                var dayOfMonth = Support.Clock.Today.Day;

                if (creditCycleAction != null)
                    creditCycleAction.Value.Action.ExecuteActions(Support, creditCycleAction.Value.MonthNr, dayOfMonth);

                if (creditCycleAssertion != null)
                    creditCycleAssertion.Value.Assertion.DoAssert(Support, creditCycleAssertion.Value.MonthNr, dayOfMonth);

                Support.MoveForwardNDays(1);
            }

            if (debugPrint)
            {
                DebugPrinter.PrintMonthEnd();
            }
        }

        public UlLegacyTestRunner.TestSupport Support { get; }
    }
}