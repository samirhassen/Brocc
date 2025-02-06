using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Host.IntegrationTests.UlLegacy.Termination;
using NTech.Core.Host.IntegrationTests.UlLegacy.Utilities;

namespace NTech.Core.Host.IntegrationTests.UlLegacy
{
    public partial class AlternatePaymentPlanTests
    {
        private class Tester : UlLegacyTestRunner
        {
            public int PlanStartDayNr { get; set; } = 17;

            private Action<int>[]? doAfterDays;

            public List<(DateTime Date, string TemplateName, Dictionary<string, object> TemplateDataMines)>? AlternatePaymentPlanMessagesSent = null;

            public void RunTestOverMonths(params Action<int>[] doAfterDays)
            {
                this.doAfterDays = doAfterDays;
                bool wasObserverSet = false;
                if(AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage == null)
                {
                    AlternatePaymentPlanMessagesSent = new List<(DateTime Date, string TemplateName, Dictionary<string, object> Context)>();
                    AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = x =>
                        AlternatePaymentPlanMessagesSent.Add((Date: Support.Clock.Today, x.TemplateName, x.TemplateDataMines));
                    wasObserverSet = true;
                }

                try
                {
                    RunTest();
                }
                finally
                {
                    if(wasObserverSet)
                        AlternatePaymentPlanSecureMessagesService.ObserveSendSecureMessage = null;
                }                
            }
  
            public void PrintRepaymentTime(string? context = null)
            {
                Console.WriteLine($"{Support.Clock.Today}:Repayment time = {Credits.GetCurrentPaymentPlan(Support, CreditNr).Payments.Count}{(context == null ? "" : "[" + context + "]")}");
            }

            protected override void DoTest()
            {
                var m = new UlLegacyRunMonthTester(Support);
                m.RunOneMonth(doBeforeDay: dayNr =>
                {
                    if (dayNr == 5)
                    {
                        CreditsUlLegacy.CreateCredit(Support, 1);
                        PrintRepaymentTime("credit created");
                    }
                });
                m.RunOneMonth();
                m.RunOneMonth(doAfterDay: dayNr =>
                {
                    var creditNr = CreditsUlLegacy.GetCreateCredit(Support, 1).CreditNr;
                    if (dayNr == PlanStartDayNr)
                    {                        
                        var service = Support.GetRequiredService<AlternatePaymentPlanService>();
                        var planSuggestion = service.GetSuggestedPaymentPlan(new GetPaymentPlanSuggestedRequest
                        {
                            CreditNr = creditNr,
                            ForceStartNextMonth = false,
                            NrOfPayments = 6
                        });
                        PrintRepaymentTime("just before payment plan created");
                        service.StartPaymentPlanFromSpecification(planSuggestion);
                        PrintRepaymentTime("just after payment plan created");
                    }

                    if(doAfterDays!.Length > 0)
                        doAfterDays[0](dayNr);
                });
                foreach(var doAfterDay in doAfterDays!.Skip(1))
                {
                    m.RunOneMonth(doAfterDay: doAfterDay);
                }         
            }

            public string CreditNr => CreditsUlLegacy.GetCreateCredit(Support, 1).CreditNr;

            public ICreditContextExtended CreateCreditContext() => this.Support.CreateCreditContextFactory().CreateContext();

            public void AssertMessagesSent(string templateName, params DateTime[] datesSent)
            {
                foreach(var dateSent in datesSent)
                {
                    var wasSent = AlternatePaymentPlanMessagesSent != null && AlternatePaymentPlanMessagesSent.Any(x => x.Date == dateSent && x.TemplateName == templateName);
                    if (!wasSent && AlternatePaymentPlanMessagesSent != null)
                        Console.WriteLine("Messages sent:" + string.Join(Environment.NewLine, AlternatePaymentPlanMessagesSent.Select(x => $"{x.Date:yyyy-MM-dd}: {x.TemplateName}")));
                    Assert.That(wasSent, Is.True, $"Expected message {templateName} to be sent on {dateSent:yyyy-MM-dd}");
                }
            }
        }        
    }
}
