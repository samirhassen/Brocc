using Microsoft.VisualStudio.TestTools.UnitTesting;
using NTech.Services.Infrastructure.Email;
using System.Net.Mail;

namespace TestsnPreCredit
{
    [TestClass]
    public class SmtpEmailSenderTests
    {
        private static MailMessage CreateMessage()
        {
            return new MailMessage("f@example.org", "t@example.org", "subject", "body");
        }

        public interface ISender
        {
            void Send(MailMessage m);
        }

        [TestMethod]
        public void OkIsSentExactlyOnce()
        {
            var m = CreateMessage();
            var s = new StrictMock<ISender>();
            s.Setup(x => x.Send(m));
            var sender = new SmtpEmailSender(s.Object.Send);

            sender.SendEmail(m);

            s.Verify(x => x.Send(m), Moq.Times.Once);
        }

        [TestMethod]
        public void ServiceNotAvailableAndThenOk()
        {
            var m = CreateMessage();
            var s = new StrictMock<ISender>();
            var isFirst = true;
            s
                .Setup(x => x.Send(m))
                .Callback(() =>
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        throw new SmtpException(SmtpStatusCode.ServiceNotAvailable);
                    }
                });
            var sender = new SmtpEmailSender(s.Object.Send);

            sender.SendEmail(m);

            s.Verify(x => x.Send(m), Moq.Times.Exactly(2));
        }

        [TestMethod]
        public void ServiceNotAvailableIsRetriedOnce()
        {
            var m = CreateMessage();
            var s = new StrictMock<ISender>();
            s.Setup(x => x.Send(m))
                .Throws(new SmtpException(SmtpStatusCode.ServiceNotAvailable));
            var sender = new SmtpEmailSender(s.Object.Send);

            try
            {
                sender.SendEmail(m);
            }
            catch (SmtpException)
            {
                s.Verify(x => x.Send(m), Moq.Times.Exactly(2));
            }
        }

        [TestMethod]
        public void MailboxUnavailableNotRetried()
        {
            var m = CreateMessage();
            var s = new StrictMock<ISender>();
            s.Setup(x => x.Send(m))
                .Throws(new SmtpException(SmtpStatusCode.MailboxUnavailable));
            var sender = new SmtpEmailSender(s.Object.Send);

            try
            {
                sender.SendEmail(m);
            }
            catch (SmtpException)
            {
                s.Verify(x => x.Send(m), Moq.Times.Exactly(1));
            }
        }
    }
}