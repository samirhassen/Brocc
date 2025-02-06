using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace NTech.Services.Infrastructure.Email
{
    public class NTechEmailServiceFactory
    {
        private readonly Lazy<NTechSimpleSettings> emailSettings;
        private readonly IEmailRenderer renderer;

        public NTechEmailServiceFactory(IEmailRenderer renderer) : this(new Lazy<NTechSimpleSettings>(() =>
        {
            var settingsFile = GetEmailSettingsFile(true); ;
            return NTechSimpleSettings.ParseSimpleSettingsFile(settingsFile.FullName);
        }), renderer)
        {
        }

        public NTechEmailServiceFactory(Lazy<NTechSimpleSettings> emailSettings, IEmailRenderer renderer)
        {
            this.emailSettings = emailSettings;
            this.renderer = renderer;
        }

        public NTechEmailProviderCode EmailProvider
        {
            get
            {
                var p = emailSettings.Value.Req("provider");

                if (p?.ToLowerInvariant() == NTechEmailProviderCode.Mailgun.ToString().ToLowerInvariant())
                    return NTechEmailProviderCode.Mailgun;
                else if (p?.ToLowerInvariant() == NTechEmailProviderCode.Smtp.ToString().ToLowerInvariant())
                    return NTechEmailProviderCode.Smtp;
                else if (!NTechEnvironment.Instance.IsProduction && p?.ToLowerInvariant() == NTechEmailProviderCode.InMemoryTestList.ToString().ToLowerInvariant())
                    return NTechEmailProviderCode.InMemoryTestList;
                else
                    throw new Exception($"Invalid email provider");
            }
        }

        public DirectoryInfo TemplateFolder
        {
            get
            {
                return NTechEnvironment.Instance.ClientResourceDirectory("ntech.email.templatefolder", "EmailTemplates", false);
            }
        }

        public INTechEmailService CreateEmailService()
        {
            var service = NTechEnvironment.Instance.IsProduction
                ? CreateActualEmailService()
                : new OfflineSimulatingTestEmailService(CreateActualEmailService());
            return new ErrorTrapEmailService(service);
        }

        private INTechEmailService CreateActualEmailService()
        {
            var p = EmailProvider;

            switch (p)
            {
                case NTechEmailProviderCode.Mailgun: return new NTechMailgunEmailService(TemplateFolder, renderer, emailSettings.Value);
                case NTechEmailProviderCode.InMemoryTestList: return new InMemoryEmailTestService(TemplateFolder, renderer);
                case NTechEmailProviderCode.Smtp: return new SmtpEmailService(TemplateFolder, renderer, emailSettings.Value);
                default: throw new NotImplementedException();
            }
        }

        private static FileInfo GetEmailSettingsFile(bool mustExist) => NTechEnvironment.Instance.StaticResourceFile("ntech.email.settingsfile", "emailsettings.txt", mustExist);

        public static bool HasEmailProvider => GetEmailSettingsFile(false).Exists;

        public class ErrorTrapEmailService : INTechEmailService
        {
            private readonly INTechEmailService actualService;

            public ErrorTrapEmailService(INTechEmailService actualService)
            {
                this.actualService = actualService;
            }

            private void TrapError(Action a)
            {
                try
                {
                    a();
                }
                catch (Exception ex)
                {
                    throw new NTechCoreWebserviceException(ex.Message, ex)
                    {
                        ErrorCode = "emailError"
                    };
                }
            }

            public (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled)? LoadClientResourceTemplate(string templateName, bool isRequired) =>
                actualService.LoadClientResourceTemplate(templateName, isRequired); //NOTE: Dont add error trapping here as this will never call the provider

            public void SendRawEmail(List<string> recipients, string subjectTemplateText, string bodyTemplateText, Dictionary<string, object> mines, string sendingContext) =>
                TrapError(() => actualService.SendRawEmail(recipients, subjectTemplateText, bodyTemplateText, mines, sendingContext));


            public void SendTemplateEmail(List<string> recipients, string templateName, Dictionary<string, string> mines, string sendingContext) =>
                TrapError(() => actualService.SendTemplateEmail(recipients, templateName, mines, sendingContext));


            public void SendTemplateEmailComplex(List<string> recipients, string templateName, Dictionary<string, object> mines, string sendingContext) =>
                TrapError(() => actualService.SendTemplateEmailComplex(recipients, templateName, mines, sendingContext));
        }

        public class OfflineSimulatingTestEmailService : INTechEmailService
        {
            private readonly INTechEmailService actualEmailService;

            public OfflineSimulatingTestEmailService(INTechEmailService actualEmailService)
            {
                this.actualEmailService = actualEmailService;
            }

            private static void SimulateDown()
            {
                if (!IsSimulatedDownNow())
                    return;
                Thread.Sleep(1000);
                throw new Exception("Simulating email provider down/wrong credentials or similar that causes it to blow up.");
            }

            public static void SetIsDown(bool isDown)
            {
                var fileName = GetIsDownMarkerTempFileName();
                if (isDown && !File.Exists(fileName))
                    File.WriteAllText(fileName, "test email down marker");
                else if (!isDown && File.Exists(fileName))
                    File.Delete(fileName);
            }

            public static bool IsSimulatedDownNow() => File.Exists(GetIsDownMarkerTempFileName());


            //Include actual date in name so this resets automatically every day.
            private static string GetIsDownMarkerTempFileName() =>
                Path.Combine(Path.GetTempPath(), $"ntech-testemail-down-{DateTime.Now.ToString("yyyy-MM-dd")}.txt");

            public (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled)? LoadClientResourceTemplate(string templateName, bool isRequired) =>
                actualEmailService.LoadClientResourceTemplate(templateName, isRequired);

            public void SendRawEmail(List<string> recipients, string subjectTemplateText, string bodyTemplateText, Dictionary<string, object> mines, string sendingContext)
            {
                SimulateDown();
                actualEmailService.SendRawEmail(recipients, subjectTemplateText, bodyTemplateText, mines, sendingContext);
            }

            public void SendTemplateEmail(List<string> recipients, string templateName, Dictionary<string, string> mines, string sendingContext)
            {
                SimulateDown();
                actualEmailService.SendTemplateEmail(recipients, templateName, mines, sendingContext);
            }

            public void SendTemplateEmailComplex(List<string> recipients, string templateName, Dictionary<string, object> mines, string sendingContext)
            {
                SimulateDown();
                actualEmailService.SendTemplateEmailComplex(recipients, templateName, mines, sendingContext);
            }
        }
    }
}