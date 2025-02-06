using nCredit;
using nCredit.Code.Services;
using nCredit.DbModel.BusinessEvents;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.DbModel;
using NTech.Core.Credit.Shared.DomainModel;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static NTech.Core.Credit.Shared.Services.AlternatePaymentPlanService;
namespace NTech.Core.Credit.Shared.Services
{
    public class AlternatePaymentPlanSecureMessagesService : BusinessEventManagerOrServiceBase
    {
        private readonly CreditContextFactory contextFactory;
        private readonly INotificationProcessSettingsFactory notificationProcessSettingsFactory;
        private readonly ICreditEnvSettings envSettings;
        private readonly CachedSettingsService settingsService;
        private readonly ICustomerClient customerClient;
        private readonly AlternatePaymentPlanService alternatePaymentPlanService;
        private readonly IMustacheTemplateRenderingService templateRenderingService;

        public AlternatePaymentPlanSecureMessagesService(CreditContextFactory contextFactory, INotificationProcessSettingsFactory notificationProcessSettingsFactory, ICreditEnvSettings envSettings,
            IClientConfigurationCore clientConfiguration, CachedSettingsService settingsService, ICustomerClient customerClient, AlternatePaymentPlanService alternatePaymentPlanService,
            INTechCurrentUserMetadata user, ICoreClock clock, IMustacheTemplateRenderingService templateRenderingService) : base(user, clock, clientConfiguration)
        {
            this.contextFactory = contextFactory;
            this.notificationProcessSettingsFactory = notificationProcessSettingsFactory;
            this.envSettings = envSettings;
            this.settingsService = settingsService;
            this.customerClient = customerClient;
            this.alternatePaymentPlanService = alternatePaymentPlanService;
            this.templateRenderingService = templateRenderingService;
        }

        public bool IsPaymentPlanEnabled => IsPaymentPlanEnabledShared(ClientCfg);
        private Dictionary<string, string> Settings => settingsService.LoadSettings("altPaymentPlanSecureMessageTemplates");
        private CreditType CreditType => envSettings.ClientCreditType;

        public (int SuccessCount, List<string> Warnings, List<string> Errors) SendEnabledSecureMessages()
        {
            if (!IsPaymentPlanEnabled)
            {
                return (
                    SuccessCount: 0,
                    Warnings: new List<string> { "Payment plans not enabled" },
                    Errors: new List<string>()
                );
            }

            var onNotificationResult = TrySendOnNotificationSecureMessages();
            var onMissedPaymentResult = TrySendOnMissedPaymentSecureMessages();

            var result = (
                SuccessCount: onNotificationResult.SuccessCount + onMissedPaymentResult.SuccessCount,
                Warnings: onNotificationResult.Warnings.Concat(onMissedPaymentResult.Warnings).ToList(),
                Errors: onNotificationResult.Errors.Concat(onMissedPaymentResult.Errors).ToList()
            );

            if (onNotificationResult.SuccessCount == 0 && onMissedPaymentResult.SuccessCount == 0)
            {
                result.Warnings.Add("No payment plan secure messages sent");
            }

            return result;
        }

        private (int SuccessCount, List<string> Warnings, List<string> Errors) TrySendOnNotificationSecureMessages()
        {
            var isNotificationEnabled = Settings["onNotification"] == "true";
            if (!isNotificationEnabled)
            {
                return (SuccessCount: 0, Warnings: new List<string>(), Errors: new List<string>());
            }

            using (var context = contextFactory.CreateContext())
            {
                var count = 0;
                var warnings = new List<string>();
                var errors = new List<string>();

                var notificationSettings = notificationProcessSettingsFactory.GetByCreditType(CreditType);

                var today = Clock.Today;
                var thisMonth = Month.ContainingDate(today);
                var activePaymentPlansData = alternatePaymentPlanService.GetActivePaymentPlansCompleteOrCancelData(context);

                var earliestAllowedDueDate = today.AddDays(7);
                var latestAllowedDueDate = today.AddDays(14);

                var plans = activePaymentPlansData
                    .Select(p => new
                    {
                        p.PlanHeader.Id,
                        p.PlanHeader.CreditNr,
                        p.PlanHeader,
                        p.MainApplicantCustomerId,
                        NextMonth = p.GetNextMonth(Clock.Today),
                        PlanData = p
                    })
                    .Where(x => x.NextMonth != null && x.NextMonth.DueDate >= earliestAllowedDueDate && x.NextMonth.DueDate <= latestAllowedDueDate)
                    .ToList();

                string GetSentKeySpace(AlternatePaymentPlanMonth p) => $"AltPaymentPlanOnNotificationSent#{p.DueDate:yyyy-MM}";

                var messagedPaymentPlanIdsByKeySpace = GetMessagedPaymentPlanIdsForMonths(context, plans.Select(x => GetSentKeySpace(x.NextMonth)).Distinct().ToArray());
                plans = plans.Where(x => messagedPaymentPlanIdsByKeySpace.Opt(GetSentKeySpace(x.NextMonth))?.Contains(x.Id) != true).ToList();

                var paymentPlanPrintContext = GetPaymentPlanPrintContexts(context, new PaymentAccountService(settingsService, envSettings, ClientCfg), ClientCfg,
                        plans.Select(x => x.Id).ToHashSetShared(),
                        plans.ToDictionary(x => x.PlanHeader.Id, x => x.PlanData));

                foreach (var plan in plans)
                {
                    try
                    {
                        var mines = new Dictionary<string, object> 
                        {
                            { "creditNr", plan.CreditNr },
                            { "monthlyAmount", plan.NextMonth.MonthAmount.ToString("f2", PrintFormattingCulture) },
                            { "remainingMonthlyAmount", Math.Max(plan.NextMonth.TotalAmount - plan.PlanData.GetTotalPaidAmount(), 0m).ToString("f2", PrintFormattingCulture) },
                            { "dueDate", plan.NextMonth.DueDate.ToString("d", PrintFormattingCulture) }
                        }
                        .Concat(paymentPlanPrintContext.Where(x => x.Key == plan.Id).Select(x => x.Value).FirstOrDefault())
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        SendSecureMessageWithSettingsTemplate(Settings, "onNotificationTemplateText", plan.MainApplicantCustomerId, plan.CreditNr,
                            mines, customerClient, envSettings, templateRenderingService);

                        KeyValueStoreService.SetValueComposable(context, plan.Id.ToString(), GetSentKeySpace(plan.NextMonth), "true");

                        context.SaveChanges();

                        count++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"SendPaymentPlanMessageOnNotification error: '{ex.Message}'");
                    }
                }


                context.SaveChanges();
                return (count, warnings, errors);
            }
        }

        private (int SuccessCount, List<string> Warnings, List<string> Errors) TrySendOnMissedPaymentSecureMessages()
        {
            using (var context = contextFactory.CreateContext())
            {
                var count = 0;
                var warnings = new List<string>();
                var errors = new List<string>();

                var isOnMissedPaymentEnabled = Settings["onMissedPayment"] == "true";
                if (!isOnMissedPaymentEnabled)
                {
                    return (count, warnings, errors);
                }

                var missedPaymentDaysSetting = int.Parse(Settings.Req("nrOfDaysOnMissedPayment"));
                var today = Clock.Today;
                var nrOfDaysAgo = today.AddDays(-missedPaymentDaysSetting);
                var sentKeySpace = $"AltPaymentPlanOnMissedSent#{today:yyyy-MM}";

                var activePaymentPlansData = alternatePaymentPlanService.GetActivePaymentPlansCompleteOrCancelData(context);
                var messagedPaymentPlanIds = GetMessagedPaymentPlanIdsForMonth(context, sentKeySpace);

                var plansWithMissedPayments = activePaymentPlansData
                    .Where(p =>
                        p.IsLateOnPayments(nrOfDaysAgo, 0)
                        && !messagedPaymentPlanIds.Contains(p.PlanHeader.Id))
                    .Select(p => new
                    {
                        p.PlanHeader.Id,
                        p.PlanHeader.CreditNr,
                        p.MainApplicantCustomerId,
                        PlanData = p,
                        LastOverdueMonth = p.GetLastOverdueMonth(Clock.Today, 0),
                        MissingPaymentAmount = p.GetMissingPaymentAmount(Clock.Today, 0)
                    }).ToList();

                var paymentPlanPrintContext = GetPaymentPlanPrintContexts(context, new PaymentAccountService(settingsService, envSettings, ClientCfg), ClientCfg,
                        plansWithMissedPayments.Select(x => x.Id).ToHashSetShared(),
                        plansWithMissedPayments.ToDictionary(x => x.Id, x => x.PlanData));

                foreach (var plan in plansWithMissedPayments)
                {
                    try
                    {
                        var mines = new Dictionary<string, object> {
                                { "creditNr", plan.CreditNr },
                                { "dueDate", plan.LastOverdueMonth.DueDate.ToString("d", PrintFormattingCulture) },
                                { "minimumAmountToPay", plan.MissingPaymentAmount.ToString("f2", PrintFormattingCulture) },
                            }
                        .Concat(paymentPlanPrintContext.Where(x => x.Key == plan.Id).Select(x => x.Value).FirstOrDefault())
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        SendSecureMessageWithSettingsTemplate(Settings, "onMissedPaymentTemplateText", plan.MainApplicantCustomerId, plan.CreditNr,
                            mines, customerClient, envSettings, templateRenderingService);

                        KeyValueStoreService.SetValueComposable(context, plan.Id.ToString(), sentKeySpace, "true");

                        context.SaveChanges();

                        count++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"OnMissedPaymentMessageOnNotification error: '{ex.Message}'");
                    }
                }

                context.SaveChanges();
                return (count, warnings, errors);
            }
        }

        public static bool SendSecureMessageWithSettingsTemplate(Dictionary<string, string> settings, string settingTemplateName, int customerId, string creditNr, Dictionary<string, object> mines,
            ICustomerClient customerClient, ICreditEnvSettings envSettings, IMustacheTemplateRenderingService templateRenderingService)
        {
            try
            {
                var messageText = string.Join("", settings.Req(settingTemplateName));

                if (mines != null)
                {
                    messageText = templateRenderingService.RenderTemplate(messageText, mines);
                }

                ObserveSendSecureMessage?.Invoke((TemplateName: settingTemplateName, TemplateDataMines: mines, MessageText: messageText));

                customerClient.SendSecureMessage(customerId, creditNr, $"Credit_{envSettings.ClientCreditType}", messageText, true, "markdown");
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return true;
        }

        public static Action<(string TemplateName, Dictionary<string, object> TemplateDataMines, string MessageText)> ObserveSendSecureMessage { get; set; } = null;

        private static Dictionary<int, Dictionary<string, object>> GetPaymentPlanPrintContexts(
            ICreditContextExtended context,
            PaymentAccountService paymentAccountService,
            IClientConfigurationCore clientConfiguration,
            HashSet<int> paymentPlanIds,
            Dictionary<int, PaymentPlanDataCompleteOrCancelData> paymentPlanData)
        {
            var incomingPaymentBankAccountNr = paymentAccountService.GetIncomingPaymentBankAccountNr();
            string payToBankAccount;
            if (clientConfiguration.Country.BaseCountry == "FI")
                payToBankAccount = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr);
            else if (clientConfiguration.Country.BaseCountry == "SE")
                payToBankAccount = paymentAccountService.FormatIncomingBankAccountNrForDisplay(incomingPaymentBankAccountNr);
            else
                throw new NotImplementedException();

            var extraDataPerPlan = context
                .AlternatePaymentPlanHeadersQueryable
                .Where(x => paymentPlanIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    OcrPaymentReference = x
                        .Credit
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.OcrPaymentReference.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault(),
                    SharedOcrPaymentReference = x
                        .Credit
                        .DatedCreditStrings
                        .Where(y => y.Name == DatedCreditStringCode.SharedOcrPaymentReference.ToString())
                        .OrderByDescending(y => y.BusinessEventId)
                        .Select(y => y.Value)
                        .FirstOrDefault()
                })
                .ToDictionary(x => x.Id, x => new
                {
                    x.OcrPaymentReference,
                    x.SharedOcrPaymentReference
                });

            return paymentPlanData.ToDictionary(x => x.Key, x =>
            {
                var planId = x.Key;
                var planData = x.Value;
                var extraData = extraDataPerPlan[planId];

                return new Dictionary<string, object>
                {
                    { "payToBankAccount", payToBankAccount },
                    { "ocrReference", extraData.OcrPaymentReference },
                    { "sharedOcrReference",  extraData.SharedOcrPaymentReference ?? extraData.OcrPaymentReference } //Shared so that the setting template writer can choose
                };
            });
        }
        private static HashSet<int> GetMessagedPaymentPlanIdsForMonth(ICreditContextExtended context, string monthKeySpace) =>
            GetMessagedPaymentPlanIdsForMonths(context, monthKeySpace).Opt(monthKeySpace) ?? new HashSet<int>();

        private static Dictionary<string, HashSet<int>> GetMessagedPaymentPlanIdsForMonths(ICreditContextExtended context, params string[] monthKeySpaces) =>
            context.KeyValueItemsQueryable.Where(x => monthKeySpaces.Contains(x.KeySpace)).Select(x => new { x.Key, x.KeySpace }).ToList()
            .GroupBy(x => x.KeySpace).ToDictionary(x => x.Key, x => x.Select(y => int.Parse(y.Key)).ToHashSetShared());
    }
}
