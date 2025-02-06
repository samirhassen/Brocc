using NTech.Core.Module;

namespace NTech.Services.Infrastructure.Email
{
    public class InMemoryEmailTestService : BaseNTechEmailService
    {
        public class TestEmail
        {
            public DateTimeOffset Date { get; set; }
            public string Subject { get; set; }
            public string HtmlBody { get; set; }
            public string RecipientEmail { get; set; }
        }

        private static Lazy<RingBuffer<TestEmail>> storedEmails = new Lazy<RingBuffer<TestEmail>>(() => new RingBuffer<TestEmail>(200));
        private readonly NEnv env;

        public InMemoryEmailTestService(DirectoryInfo templateFolder, IEmailRenderer renderer, NEnv env) : base(templateFolder, renderer)
        {
            if (env.IsProduction)
            {
                throw new Exception("The InMemoryEmailTestService is only available in test!");
            }

            this.env = env;
        }

        public IList<TestEmail> GetStoredEmails()
        {
            if (env.IsProduction)
            {
                throw new Exception("The InMemoryEmailTestService is only available in test!");
            }
            return storedEmails.IsValueCreated
                ? storedEmails.Value.ToList()
                : new List<TestEmail>();
        }

        public void ClearStoredEmails()
        {
            if (env.IsProduction)
            {
                throw new Exception("The InMemoryEmailTestService is only available in test!");
            }
            storedEmails.Value.Clear();
        }

        protected override void SendHtmlMail(string subject, string htmlBody, string recipientEmail)
        {
            if ((recipientEmail ?? "").ToLowerInvariant().Contains(".error@"))
            {
                throw new System.Net.Http.HttpRequestException("Testing error handling. Triggered because the email contains .error@");
            }
            storedEmails.Value.Add(new TestEmail { Subject = subject, HtmlBody = htmlBody, RecipientEmail = recipientEmail, Date = DateTimeOffset.Now });
        }

        protected override IList<string> TransformRecipients(IList<string> recipients)
        {
            return recipients;
        }

        protected override IList<string> AppendAdditionalEmailTemplateLines(List<string> recipients, Dictionary<string, object> mines, string sendingContext)
        {
            var newLines = new List<string>();
            newLines.Add("<div style=\"color:Gainsboro;font-size:smaller;\">");
            newLines.Add("<br />");
            newLines.Add("<br />");
            newLines.Add("--TEST ONLY SECTION--");
            newLines.Add("<br />");
            newLines.Add($"Context: {sendingContext}");
            newLines.Add("</div>");
            return newLines;
        }
    }
}
