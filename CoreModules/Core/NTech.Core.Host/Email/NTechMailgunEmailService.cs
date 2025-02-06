using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Services.Infrastructure.Email
{
    public class NTechMailgunEmailService : BaseNTechEmailService
    {
        private readonly MailgunEmailHelper mailgunEmailHelper;
        private readonly Lazy<List<string>> testEmailAddresses;
        private readonly NEnv env;

        public NTechMailgunEmailService(DirectoryInfo templateFolder, IEmailRenderer renderer, NTechSimpleSettingsCore mailSettings, 
            IHttpClientFactory httpClientFactory, IServiceClientSyncConverter syncConverter, NEnv env) : base(templateFolder, renderer)
        {
            this.mailgunEmailHelper = new MailgunEmailHelper(mailSettings, httpClientFactory, syncConverter);
            this.testEmailAddresses = new Lazy<List<string>>(() => mailSettings.Req("testemail").Split(';').ToList());
            this.env = env;
        }

        protected override IList<string> TransformRecipients(IList<string> recipients)
        {
            return env.IsProduction ? recipients : testEmailAddresses.Value;
        }

        protected override IList<string> AppendAdditionalEmailTemplateLines(List<string> recipients, Dictionary<string, object> mines, string sendingContext)
        {
            if (!env.IsProduction)
            {
                var newLines = new List<string>();
                newLines.Add("<div style=\"color:Gainsboro;font-size:smaller;\">");
                newLines.Add("<br />");
                newLines.Add("<br />");
                newLines.Add("--TEST ONLY SECTION--");
                newLines.Add("<br />");
                newLines.Add("Recipients: " + string.Join(", ", recipients));
                newLines.Add("<br />");
                newLines.Add($"Context: {sendingContext}");
                newLines.Add("</div>");
                return newLines;
            }
            else
            {
                return null;
            }
        }

        protected override void SendHtmlMail(string subject, string htmlBody, string recipientEmail)
        {
            this.mailgunEmailHelper.SendEmail(subject, htmlBody, recipientEmail);
        }
    }
}
