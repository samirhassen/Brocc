using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace NTech.Services.Infrastructure.Email
{
    public class SmtpEmailService : BaseNTechEmailService
    {
        private readonly Lazy<SmtpEmailSender> smtpEmailSender;
        private readonly Lazy<List<string>> testEmailAddresses;
        private readonly Lazy<string> senderEmailAddress;

        public SmtpEmailService(DirectoryInfo templateFolder, IEmailRenderer renderer, NTechSimpleSettings mailSettings) : base(templateFolder, renderer)
        {
            this.smtpEmailSender = new Lazy<SmtpEmailSender>(() => new SmtpEmailSender(
                    mailSettings.Req("smtpUsername"),
                    mailSettings.Req("smtpPassword"),
                    mailSettings.Req("smtpHost"),
                    int.Parse(mailSettings.Opt("smtpPort") ?? "25")));
            this.senderEmailAddress = new Lazy<string>(() => mailSettings.Req("smtpSenderEmailAddress"));
            this.testEmailAddresses = new Lazy<List<string>>(() => mailSettings.Req("testemail").Split(';').ToList());
        }

        protected override IList<string> TransformRecipients(IList<string> recipients)
        {
            return NTechEnvironment.Instance.IsProduction ? recipients : testEmailAddresses.Value;
        }

        protected override IList<string> AppendAdditionalEmailTemplateLines(List<string> recipients, Dictionary<string, object> mines, string sendingContext)
        {
            if (!NTechEnvironment.Instance.IsProduction)
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
            if (!SmtpEmailSender.TryParseMailAddress(this.senderEmailAddress.Value, out var senderMailAdr))
                throw new Exception("Malformed sender email");
            if (!SmtpEmailSender.TryParseMailAddress(recipientEmail, out var recipientEmailAdr))
                throw new Exception("Malformed sender email");

            this.smtpEmailSender.Value.SendEmail(senderMailAdr, new List<System.Net.Mail.MailAddress> { recipientEmailAdr }, subject, htmlBody, true);
        }
    }

    public class SmtpEmailSender
    {
        public SmtpEmailSender(string username, string password, string host, int port) : this(m =>
        {
            using (var client = new SmtpClient(host, port))
            {
                client.UseDefaultCredentials = false; //True will use the current process user
                client.Credentials = new NetworkCredential(username, password);
                //client.Timeout //Default is 100000 (100 seconds)
                client.Send(m);
            }
        })
        {
        }

        //Testing constructor
        public SmtpEmailSender(Action<MailMessage> sendMailMessage)
        {
            this.sendMailMessage = sendMailMessage;
        }

        private readonly Action<MailMessage> sendMailMessage;

        public static bool TryParseMailAddress(string email, out MailAddress mailAddress)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                mailAddress = null;
                return false;
            }
            try
            {
                mailAddress = new MailAddress(email);
                return true;
            }
            catch
            {
                mailAddress = null;
                return false;
            }
        }

        public void SendEmail(MailMessage message)
        {
            RetryNTimesOnSmtpException(
                () => this.sendMailMessage(message),
                1,
                TimeSpan.FromMilliseconds(200));
            ;
        }

        public void SendEmail(MailAddress sender, List<MailAddress> recipients, string subjectText, string bodyText, bool isBodyHtml, MailAddress separateReplyToAddress = null)
        {
            using (var m = new System.Net.Mail.MailMessage())
            {
                m.Sender = sender;
                m.From = sender;

                foreach (var r in recipients)
                    m.To.Add(r);

                if (separateReplyToAddress != null)
                    m.ReplyToList.Add(separateReplyToAddress);

                m.Subject = subjectText;
                m.SubjectEncoding = System.Text.Encoding.UTF8;

                m.Body = bodyText;
                m.BodyEncoding = System.Text.Encoding.UTF8;
                m.IsBodyHtml = isBodyHtml;

                SendEmail(m);
            }
        }

        private static void RetryNTimesOnSmtpException(Action a, int maxNrOfRetries, TimeSpan timeBetweenRetries)
        {
            Func<SmtpException> f = () =>
            {
                try
                {
                    a();
                    return null;
                }
                catch (SmtpException ex)
                {
                    if (IsExceptionWorthRetrying(ex.StatusCode))
                    {
                        return ex;
                    }
                    else
                    {
                        throw;
                    }
                }
            };
            SmtpException lastException = null;
            for (var i = 0; i < (maxNrOfRetries + 1); ++i) //+1 since "one retry" is actullay "two tries in total"
            {
                lastException = f();
                if (lastException == null)
                {
                    return;
                }
                if (timeBetweenRetries > TimeSpan.Zero)
                {
                    Thread.Sleep(timeBetweenRetries);
                }
            }
            throw lastException;
        }

        private static bool IsExceptionWorthRetrying(SmtpStatusCode statusCode)
        {
            //Based on: https://stackoverflow.com/questions/6213741/net-smtp-send-with-smtpstatuscode-when-should-a-retry-occur
            //But disagree with the 500-exceptions in the beginning. These should not be retried.

            //The 4yz codes are 'Transient Negative Completion reply' codes, which means we should re-add the recipient and let the calling routine try again after a timeout.
            if (statusCode == SmtpStatusCode.ServiceNotAvailable ||
                statusCode == SmtpStatusCode.MailboxBusy ||
                statusCode == SmtpStatusCode.LocalErrorInProcessing ||
                statusCode == SmtpStatusCode.InsufficientStorage ||
                statusCode == SmtpStatusCode.ClientNotPermitted ||
                //The ones below are 'Positive Completion reply' 2yz codes. Not likely to occur in this scenario but we will account for them anyway.
                statusCode == SmtpStatusCode.SystemStatus ||
                statusCode == SmtpStatusCode.HelpMessage ||
                statusCode == SmtpStatusCode.ServiceReady ||
                statusCode == SmtpStatusCode.ServiceClosingTransmissionChannel ||
                statusCode == SmtpStatusCode.Ok ||
                statusCode == SmtpStatusCode.UserNotLocalWillForward ||
                statusCode == SmtpStatusCode.CannotVerifyUserWillAttemptDelivery ||
                statusCode == SmtpStatusCode.StartMailInput ||
                statusCode == SmtpStatusCode.CannotVerifyUserWillAttemptDelivery ||
                //The code below (552) may be sent by some incorrect server implementations instead of 452 (InsufficientStorage).
                statusCode == SmtpStatusCode.ExceededStorageAllocation)
            {
                return true;
            }

            //Anything else indicates a very serious error (probably of the 5yz variety that we haven't handled yet). Tell the calling routine to fail fast.
            return false;
        }
    }
}