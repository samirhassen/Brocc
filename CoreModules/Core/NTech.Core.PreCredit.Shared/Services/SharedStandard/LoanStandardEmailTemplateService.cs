using NTech.Core.Module.Shared.Services;
using NTech.Services.Infrastructure.Email;
using System;
using System.Collections.Generic;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class LoanStandardEmailTemplateService
    {
        private readonly INTechEmailServiceFactory emailServiceFactory;
        private readonly CachedSettingsService settingsService;
        private readonly ILoggingService loggingService;

        public LoanStandardEmailTemplateService(INTechEmailServiceFactory emailServiceFactory, CachedSettingsService settingsService, ILoggingService loggingService)
        {
            this.emailServiceFactory = emailServiceFactory;
            this.settingsService = settingsService;
            this.loggingService = loggingService;
        }

        public (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled) LoadTemplate(string settingCode, string subjectSettingName, string bodySettingName, string clientResourceTemplateName)
        {
            if (clientResourceTemplateName != null)
            {
                var emailService = emailServiceFactory.CreateEmailService();
                var clientResourceTemplate = emailService.LoadClientResourceTemplate(clientResourceTemplateName, false);
                if (clientResourceTemplate.HasValue)
                    return clientResourceTemplate.Value;
            }

            var settings = settingsService.LoadSettings(settingCode);
            var isEnabled = settings.Opt("isEnabled") != "false"; //Allow mail setting groups that dont have this setting at all to default to enabled until that setting is added
            return (SubjectTemplateText: settings[subjectSettingName], BodyTemplateText: settings[bodySettingName], IsEnabled: isEnabled);
        }


        public void SendEmail(EmailTemplateData templateData)
        {
            try
            {
                var s = emailServiceFactory.CreateEmailService();
                s.SendRawEmail(templateData.RecipientEmails,
                    templateData.SubjectTemplateText,
                    templateData.BodyTemplateText,
                    templateData.EmailTemplateContext,
                    templateData.SendingContext);
            }
            catch (Exception ex)
            {
                loggingService.Error(ex, $"Failed to send email: {templateData.SendingContext}");
                templateData?.OnFailedToSend();
            }
        }

        public class EmailTemplateData
        {
            public List<string> RecipientEmails { get; set; }
            public string BodyTemplateText { get; set; }
            public Dictionary<string, object> EmailTemplateContext { get; set; }
            public string SubjectTemplateText { get; set; }
            public string SendingContext { get; set; }
            public Action OnFailedToSend { get; set; }
        }
    }
}