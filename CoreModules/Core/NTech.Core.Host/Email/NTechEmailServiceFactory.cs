using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Services.Infrastructure.Email
{
    public class NTechEmailServiceFactory : INTechEmailServiceFactory
    {
        private readonly Lazy<NTechSimpleSettingsCore> emailSettings;
        private readonly IEmailRenderer renderer;
        private readonly NEnv env;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IServiceClientSyncConverter syncConverter;

        public NTechEmailServiceFactory(IEmailRenderer renderer, NEnv env, IHttpClientFactory httpClientFactory, IServiceClientSyncConverter syncConverter) : this(new Lazy<NTechSimpleSettingsCore>(() =>
        {
            var settingsFile = GetEmailSettingsFile(env, true);
            return NTechSimpleSettingsCore.ParseSimpleSettingsFile(settingsFile.FullName);
        }), renderer)
        {
            this.env = env;
            this.httpClientFactory = httpClientFactory;
            this.syncConverter = syncConverter;
        }

        public NTechEmailServiceFactory(Lazy<NTechSimpleSettingsCore> emailSettings, IEmailRenderer renderer)
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
                else if (!env.IsProduction && p?.ToLowerInvariant() == NTechEmailProviderCode.InMemoryTestList.ToString().ToLowerInvariant())
                    return NTechEmailProviderCode.InMemoryTestList;
                else
                    throw new Exception($"Invalid email provider");
            }
        }

        public DirectoryInfo TemplateFolder
        {
            get
            {
                return env.ClientResourceDirectory("ntech.email.templatefolder", "EmailTemplates", false);
            }
        }

        bool INTechEmailServiceFactory.HasEmailProvider => GetEmailSettingsFile(env, false).Exists;
        public INTechEmailService CreateEmailService()
        {
            var service = env.IsProduction
                ? CreateActualEmailService()
                : new OfflineSimulatingTestEmailService(CreateActualEmailService());
            return new ErrorTrapEmailService(service);
        }

        private INTechEmailService CreateActualEmailService()
        {
            var p = EmailProvider;

            switch (p)
            {
                case NTechEmailProviderCode.Mailgun: return new NTechMailgunEmailService(TemplateFolder, renderer, emailSettings.Value, httpClientFactory, syncConverter, env);
                case NTechEmailProviderCode.InMemoryTestList: return new InMemoryEmailTestService(TemplateFolder, renderer, env);
                case NTechEmailProviderCode.Smtp: return new SmtpEmailService(TemplateFolder, renderer, emailSettings.Value, env);
                default: throw new NotImplementedException();
            }
        }

        private static FileInfo GetEmailSettingsFile(NEnv env, bool mustExist) => env.StaticResourceFile("ntech.email.settingsfile", "emailsettings.txt", mustExist);

        public static bool HasEmailProvider(NEnv env) => GetEmailSettingsFile(env, false).Exists;

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