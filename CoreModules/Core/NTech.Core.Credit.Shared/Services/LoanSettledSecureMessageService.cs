using nCredit;
using nCredit.Code.Services;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NTech.Core.Credit.Shared.Services
{
    public class LoanSettledSecureMessageService
    {
        private readonly CachedSettingsService settingsService;
        private readonly CreditContextFactory contextFactory;
        private readonly ICustomerClient customerClient;
        private readonly IMustacheTemplateRenderingService templateRenderingService;
        private readonly ICreditEnvSettings envSettings;
        private readonly ILoggingService loggingService;

        public LoanSettledSecureMessageService(CachedSettingsService settingsService, CreditContextFactory contextFactory, ICustomerClient customerClient,
            IMustacheTemplateRenderingService templateRenderingService, ICreditEnvSettings envSettings, ILoggingService loggingService)
        {
            this.settingsService = settingsService;
            this.contextFactory = contextFactory;
            this.customerClient = customerClient;
            this.templateRenderingService = templateRenderingService;
            this.envSettings = envSettings;
            this.loggingService = loggingService;
        }

        public LoanSettledSecureMessageResponse SendLoanSettledSecureMessages(LoanSettledSecureMessageRequest _)
        {
            var templateSetting = settingsService.LoadSettings("loanSettledSecureMessage");
            if (templateSetting.Opt("isEnabled") != "true")
                return new LoanSettledSecureMessageResponse();

            using (var context = contextFactory.CreateContext())
            {
                //NOTE: It's assumed by all the logic that these statues are final and there can never be a status after any of them.
                var fourDaysAgo = context.CoreClock.Today.AddDays(-4); //Dont send messages to loans closed way before this feature was introduced. Just enough time to recover from downtime.
                var creditsToMessage = context
                    .CreditHeadersQueryable
                    .Where(x =>
                        x.DatedCreditStrings.Any(y => y.Name == DatedCreditStringCode.CreditStatus.ToString() && y.Value == CreditStatus.Settled.ToString()
                            && y.TransactionDate >= fourDaysAgo && y.BusinessEvent.EventType != BusinessEventType.CreditCorrectAndClose.ToString()
                            && x.Status == CreditStatus.Settled.ToString())
                        && !context.KeyValueItemsQueryable.Any(y => y.KeySpace == KeyValueStoreKeySpaceCode.LoanSettledMessageSentV1.ToString() && y.Key == x.CreditNr))
                    .Select(x => new
                    {
                        x.CreditNr,
                        CustomerId = x.CreditCustomers.OrderBy(y => y.ApplicantNr).Select(y => y.CustomerId).FirstOrDefault()
                    })
                    .ToList();

                var sentCount = 0;
                var errors = new List<string>();
                foreach (var credit in creditsToMessage)
                {
                    if (TrySendLoanSettledMessage(credit.CreditNr, errors, templateSetting, credit.CustomerId, context))
                        sentCount++;
                }

                return new LoanSettledSecureMessageResponse
                {
                    SentCount = sentCount,
                    Errors = errors
                };
            }
        }

        private bool TrySendLoanSettledMessage(string creditNr, List<string> errors, Dictionary<string, string> templateSetting, int customerId, ICreditContextExtended context)
        {
            var messageText = string.Join("", templateSetting.Req("templateText"));
            var mines = new Dictionary<string, object> { { "creditNr", creditNr } };

            messageText = templateRenderingService.RenderTemplate(messageText, mines);

            try
            {
                customerClient.SendSecureMessage(customerId, creditNr, $"Credit_{envSettings.ClientCreditType}", messageText, true, "markdown");
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, $"Settlement secure message failed for {creditNr}");
                errors.Add($"Failed for {creditNr}");
                return false;
            }

            KeyValueStoreService.SetValueComposable(context, creditNr, KeyValueStoreKeySpaceCode.LoanSettledMessageSentV1.ToString(), "true");

            context.SaveChanges();

            return true;
        }
    }

    public class LoanSettledSecureMessageRequest
    {

    }


    public class LoanSettledSecureMessageResponse : ScheduledJobResult
    {
        public int SentCount { get; set; }
    }
}