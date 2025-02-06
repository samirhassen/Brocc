using System.Collections.Generic;
using System.Net.Http;

namespace NTech.Services.Infrastructure.Email
{
    public class MailgunEmailHelper
    {
        private string mailgunApiKey;
        private string mailgunDomain;
        private readonly bool isEuAccount;

        public MailgunEmailHelper(NTechSimpleSettings mailSettings)
        {
            this.mailgunApiKey = mailSettings.Req("mailgun.apikey");
            this.mailgunDomain = mailSettings.Req("mailgun.domain");
            this.isEuAccount = mailSettings.OptBool("mail.iseuaccount");
        }

        public void SendEmail(string subject, string htmlBody, string recipientEmail)
        {
            using (var c = new HttpClient())
            {
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
                var result = c.PostAsync(string.Format("https://{0}/v3/{1}/messages", baseUrl, mailgunDomain), formContent).Result;
                result.EnsureSuccessStatusCode();
            }
        }
    }
}
