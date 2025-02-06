using IdentityModel.Client;
using NTech.Core.Module;
using NTech.Core.Module.Shared.Clients;

namespace NTech.Services.Infrastructure.Email
{
    public class MailgunEmailHelper
    {
        private string mailgunApiKey;
        private string mailgunDomain;
        private readonly bool isEuAccount;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IServiceClientSyncConverter syncConverter;

        public MailgunEmailHelper(NTechSimpleSettingsCore mailSettings, IHttpClientFactory httpClientFactory, IServiceClientSyncConverter syncConverter)
        {
            this.mailgunApiKey = mailSettings.Req("mailgun.apikey");
            this.mailgunDomain = mailSettings.Req("mailgun.domain");
            this.isEuAccount = mailSettings.OptBool("mail.iseuaccount");
            this.httpClientFactory = httpClientFactory;
            this.syncConverter = syncConverter;
        }

        public void SendEmail(string subject, string htmlBody, string recipientEmail) =>
            syncConverter.ToSync(() => SendEmailAsync(subject, htmlBody, recipientEmail));

        public async Task SendEmailAsync(string subject, string htmlBody, string recipientEmail)
        {
            var c = httpClientFactory.CreateClient();
            c.SetBasicAuthentication("api", mailgunApiKey);

            var formContent = new FormUrlEncodedContent(new[]
            {
                    new KeyValuePair<string, string>("from", "noreply@" + mailgunDomain),
                    new KeyValuePair<string, string>("to", recipientEmail),
                    new KeyValuePair<string, string>("subject", subject),
                    new KeyValuePair<string, string>("html", htmlBody),
                });
            formContent.Headers.ContentType.CharSet = "UTF-8";
            var baseUrl = isEuAccount ? "api.eu.mailgun.net" : "api.mailgun.net";
            var result = await c.PostAsync(string.Format("https://{0}/v3/{1}/messages", baseUrl, mailgunDomain), formContent);
            result.EnsureSuccessStatusCode();
        }
    }
}
