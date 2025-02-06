using Moq;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.Shared;
using NTech.Core.Host.IntegrationTests.SinglePaymentLoans.Utilities;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.IntegrationTests.SinglePaymentLoans.AlternatePaymentPlan
{
    public class NoPaymentsLatePlanTests
    {
        [Test]
        public void AlternatePaymentPlanCreateCloseToTermination()
        {
            new Test().RunTest(overrideClientConfig: x =>
            {
                x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "TerminationLetterGraceDays")).Returns(new int?(10));
                x.Setup(x => x.GetSingleCustomInt(false, "NotificationProcessSettings", "DebtCollectionGraceDays")).Returns(new int?(10));
            });
        }

        private class Test : SinglePaymentLoansTestRunner
        {
            protected override void DoTest()
            {
                Support.Now = Support.Now.AddMonths(-3);
                Support.MoveToNextDayOfMonth(1); //January first

                var planMessagesSent = new List<(DateTime Date, string TemplateName)>();

                var creditNr1 = ShortTimeCredits.CreditNrFromIndex(1);

                void AssertMessageSent(string templateName, string prefix) =>
                    Assert.That(planMessagesSent!.Count(x => x.Date == Support.Clock.Today && x.TemplateName == templateName), Is.EqualTo(1), $"{prefix}Message expected: {templateName}");

                var a = CreditCycleAssertionBuilder
                    .Begin()

                    //Month 1
                    .ForMonth(monthNr: 1)
                    .ExpectNewLoan(dayNr: 1, creditNr: creditNr1, singlePaymentRepaymentDays: 10)
                    .ExpectNotification(dayNr: 2, creditNr: creditNr1, dueDay: 12)
                    .ExpectReminder(dayNr: 19, creditNr: creditNr1)
                    .ExpectReminder(dayNr: 26, creditNr: creditNr1)

                    //Month 2 (no events)

                    //Month 3
                    .ForMonth(monthNr: 3)
                    .ExpectShownInTerminationLetterUi(dayNr: 13, creditNr: creditNr1, letterSentMonthNr: 3, letterSentDayNr: 23)
                    .ExpectAlternatePaymentPlanStarted(dayNr: 14, creditNr: creditNr1, null, firstDueDate: (MonthNr: 4, DayNr: 12), 407m, 407m, 407.81m)                    
                    .AssertCustom(dayNr: 29, t => AssertMessageSent("onNotificationTemplateText", t.Prefix)) //due 4 - 12

                    //Month 4
                    .ForMonth(monthNr: 4)
                    .AssertCustom(dayNr: 18, t => AssertMessageSent("onMissedPaymentTemplateText", t.Prefix)) //due 4 - 12
                    .ExpectAlternatePaymentPlanCancelled(dayNr: 20, creditNr: creditNr1)
                    .ExpectTerminationLetter(dayNr: 20, creditNr: creditNr1)

                    //Month 5
                    .ForMonth(monthNr: 5)
                    .ExpectDebtCollectionExport(dayNr: 28, creditNr: creditNr1)

                    .End();

                foreach (var monthNr in Enumerable.Range(1, a.MaxMonthNr))
                {
                    ShortTimeCredits.RunOneMonth(Support, afterDay: dayNr =>
                    {
                        if (monthNr == 1 && dayNr == 1)
                        {
                            ShortTimeCredits.CreateCredit(Support, creditIndex: 1, repaymentTime: RepaymentDaysCredit1, isRepaymentTimeDays: true,
                                loanAmount: LoanAmount, initialFeeOnFirstNotification: InitialFee);
                        }
                        else if (monthNr == 3 && dayNr == 14)
                        {
                            var s = Support.GetRequiredService<AlternatePaymentPlanService>();
                            var paymentPlan = s.GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
                            {
                                CreditNr = creditNr1,
                                ForceStartNextMonth = false,
                                NrOfPayments = 3
                            });
                            s.StartPaymentPlanFromSpecification(paymentPlan);
                        }
                    }, creditCycleAssertion: (Assertion: a, MonthNr: monthNr), extraMorningJobs: () =>
                    {
                        var s = Support;
                        var renderingService = new Mock<IMustacheTemplateRenderingService>(MockBehavior.Strict);
                        renderingService.Setup(x => x.RenderTemplate(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Returns("test");
                        var messageService = new AlternatePaymentPlanSecureMessagesService(
                            s.CreateCreditContextFactory(), s.GetNotificationProcessSettingsFactory(), s.CreditEnvSettings, s.ClientConfiguration,
                            s.CreateCachedSettingsService(), s.GetRequiredService<ICustomerClient>(), s.GetRequiredService<AlternatePaymentPlanService>(),
                            s.CurrentUser, s.Clock, renderingService.Object);

                        var initialObserver = AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage;
                        try
                        {
                            AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = x =>
                            {
                                planMessagesSent.Add((Date: s.Clock.Today, TemplateName: x.TemplateName));
                            };
                            var result = messageService.SendEnabledSecureMessages();
                            if (result.Errors?.Count > 0)
                                Assert.Fail(string.Join(", ", result.Errors));
                        }
                        finally
                        {
                            AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = initialObserver;
                        }
                    });
                }
            }

            const decimal LoanAmount = 1000m;
            const decimal InitialFee = 149m;
            const int RepaymentDaysCredit1 = 10;
        }
    }
}
