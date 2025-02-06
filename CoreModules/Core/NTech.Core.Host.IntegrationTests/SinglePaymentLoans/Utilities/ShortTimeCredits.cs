using nCredit.DbModel.BusinessEvents.NewCredit;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Host.IntegrationTests.Shared;
using static NTech.Core.Host.IntegrationTests.Shared.CreditCycleAssertionBuilder;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities
{
    internal class ShortTimeCredits
    {
        public static string CreditNrFromIndex(int creditIndex) => $"L{10000 + creditIndex}";

        public static string CreateCredit(SinglePaymentLoansTestRunner.TestSupport support, int creditIndex,
            int repaymentTime,
            bool isRepaymentTimeDays,
            decimal loanAmount = 1000m,
            decimal initialFeeOnFirstNotification = 149m,
            decimal marginInterestRatePercent = 39m)
        {
            var customerId = TestPersons.EnsureTestPerson(support, creditIndex);
            var creditEventManager = support.GetRequiredService<NewCreditBusinessEventManager>();
            var creditNr = CreditNrFromIndex(creditIndex);

            using (var context = support.CreateCreditContextFactory().CreateContext())
            {
                context.BeginTransaction();
                try
                {
                    creditEventManager.CreateNewCredit(context, new NewCreditRequest
                    {
                        BankAccountNr = "33008912135673",
                        CreditAmount = loanAmount,
                        RepaymentTimeInMonths = isRepaymentTimeDays ? null : repaymentTime,
                        SinglePaymentLoanRepaymentTimeInDays = isRepaymentTimeDays ? repaymentTime : null,
                        CreditNr = creditNr,
                        ProviderName = "self",
                        NrOfApplicants = 1,
                        MarginInterestRatePercent = marginInterestRatePercent,
                        Applicants = new List<NewCreditRequestExceptCapital.Applicant>
                        {
                            new NewCreditRequestExceptCapital.Applicant
                            {
                                ApplicantNr = 1,
                                CustomerId = customerId
                            }
                        },
                        FirstNotificationCosts = initialFeeOnFirstNotification > 0m ? new List<NewCreditRequest.FirstNotificationCostItem>
                        {
                            new NewCreditRequest.FirstNotificationCostItem
                            {
                                CostAmount = initialFeeOnFirstNotification,
                                CostCode = SinglePaymentLoansTestRunner.InitialFeeNotificationCode
                            }
                        } : null,
                        DirectDebitDetails = new NewCreditRequest.DirectDebitDetailsModel
                        {
                            AccountNr = "33008912135673",
                            AccountOwner = 1,
                            IsActive = true
                        }
                    }, new Lazy<decimal>(() => 0m));
                    context.SaveChanges();
                    context.CommitTransaction();

                    return creditNr;
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }
            }
        }

        public static void RunOneMonth(
                    SinglePaymentLoansTestRunner.TestSupport support,
                    Action<int>? beforeDay = null,
                    Action<int>? afterDay = null,
                    (CreditCycleAssertion Assertion, int MonthNr)? creditCycleAssertion = null,
                    (CreditCycleAction<SinglePaymentLoansTestRunner.TestSupport> Action, int MonthNr)? creditCycleAction = null,
                    bool payDirectWhenScheduled = false,
                    Action? extraMorningJobs = null)
        {
            support.AssertDayOfMonth(1);
            var month = Month.ContainingDate(support.Clock.Today);
            var lastDateOfMonth = month.LastDate;
            List<Flaggable<(DateTime DueDate, decimal Amount, string Ocr)>> scheduledDirectDebitPayments = new();
            while(support.Clock.Today <= lastDateOfMonth)
            {
                var dayOfMonth = support.Clock.Today.Day;

                beforeDay?.Invoke(dayOfMonth);
                //---Add more "jobs" below this---

                Credits.NotifyCredits(support);
                Credits.RemindCredits(support);
                Credits.CreateTerminationLetters(support);
                Credits.SendCreditsToDebtCollection(support);
                var dailyScheduledDirectDebitPayments = Credits.ScheduledDirectDebitPayments(support);
                if (dailyScheduledDirectDebitPayments.Count > 0)
                    scheduledDirectDebitPayments.AddRange(dailyScheduledDirectDebitPayments.Select(x => Flaggable.Create(x)));

                extraMorningJobs?.Invoke();

                //TODO: bookkeeping

                //Incoming payments
                if (payDirectWhenScheduled)
                {
                    var dailyDueDirectDebitPayments = scheduledDirectDebitPayments.Where(x => !x.IsFlagged && x.Item.DueDate == support.Clock.Today).ToList();
                    if (dailyDueDirectDebitPayments.Count > 0)
                    {
                        Credits.CreateAndImportPaymentFileWithOcr(support, dailyDueDirectDebitPayments
                            .GroupBy(x => x.Item.Ocr)
                            .ToDictionary(x => x.Key, x => x.Sum(y => y.Item.Amount)));
                        dailyDueDirectDebitPayments.ForEach(x => x.IsFlagged = true);
                    }
                }
                Credits.CreateOutgoingPaymentFile(support);


                //---Add more "jobs" above this---
                afterDay?.Invoke(support.Clock.Today.Day);

                if (creditCycleAction != null)
                    creditCycleAction.Value.Action.ExecuteActions(support, creditCycleAction.Value.MonthNr, dayOfMonth);

                if (creditCycleAssertion != null)
                    creditCycleAssertion.Value.Assertion.DoAssert(support, creditCycleAssertion.Value.MonthNr, dayOfMonth);

                support.MoveForwardNDays(1);
            }
        }
    }
}
