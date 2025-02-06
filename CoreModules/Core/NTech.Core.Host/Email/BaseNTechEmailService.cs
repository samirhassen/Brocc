using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NTech.Services.Infrastructure.Email
{
    public abstract class BaseNTechEmailService : INTechEmailService
    {
        private readonly DirectoryInfo templateFolder;
        private readonly IEmailRenderer renderer;

        protected BaseNTechEmailService(DirectoryInfo templateFolder, IEmailRenderer renderer)
        {
            this.templateFolder = templateFolder;
            this.renderer = renderer;
        }

        public (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled)? LoadClientResourceTemplate(string templateName, bool isRequired)
        {
            var templateFile = Path.Combine(templateFolder.FullName, templateName);
            if (!templateName.EndsWith(".txt"))
                templateFile += ".txt";
            if (!isRequired && !File.Exists(templateFile))
                return null;

            var templateLines = File.ReadAllLines(templateFile);

            return (
                SubjectTemplateText: templateLines[0],
                BodyTemplateText: string.Join(Environment.NewLine, templateLines.Skip(1)),
                IsEnabled: true
            );
        }

        private Tuple<string, string> RenderTemplate(List<string> recipients, string templateName, Dictionary<string, object> mines, string sendingContext)
        {
            var template = LoadClientResourceTemplate(templateName, true).Value;

            return RenderTemplateDirect(recipients, template.SubjectTemplateText, template.BodyTemplateText, mines, sendingContext);
        }

        private Tuple<string, string> RenderTemplateDirect(List<string> recipients, string subjectTemplateText, string bodyTemplateText, Dictionary<string, object> mines, string sendingContext)
        {
            var additionalLines = AppendAdditionalEmailTemplateLines(recipients, mines, sendingContext);

            var subject = subjectTemplateText;
            var body = additionalLines == null || additionalLines.Count == 0
                ? bodyTemplateText
                : bodyTemplateText + Environment.NewLine + string.Join(Environment.NewLine, additionalLines);

            mines = mines ?? new Dictionary<string, object>();
            body = renderer.ReplaceMustacheMines(body, mines);
            subject = renderer.ReplaceMustacheMines(subject, mines);

            body = renderer.ConvertMarkdownBodyToHtml(body);

            return Tuple.Create(subject, body);
        }

        public void SendTemplateEmailComplex(List<string> recipients, string templateName, Dictionary<string, object> mines, string sendingContext)
        {
            var subjectAndHtmlBody = RenderTemplate(recipients, templateName, mines, sendingContext);
            var actualRecipients = TransformRecipients(recipients);
            foreach (var recipientEmail in actualRecipients)
            {
                SendHtmlMail(subjectAndHtmlBody.Item1, subjectAndHtmlBody.Item2, recipientEmail);
            }
        }

        public void SendTemplateEmail(List<string> recipients, string templateName, Dictionary<string, string> mines, string sendingContext)
        {
            SendTemplateEmailComplex(recipients, templateName, mines?.ToDictionary(x => x.Key, x => (object)x.Value), sendingContext);
        }

        public void SendRawEmail(List<string> recipients, string subjectTemplateText, string bodyTemplateText, Dictionary<string, object> mines, string sendingContext)
        {
            var subjectAndHtmlBody = RenderTemplateDirect(recipients, subjectTemplateText, bodyTemplateText, mines, sendingContext);
            var actualRecipients = TransformRecipients(recipients);
            foreach (var recipientEmail in actualRecipients)
            {
                SendHtmlMail(subjectAndHtmlBody.Item1, subjectAndHtmlBody.Item2, recipientEmail);
            }
        }

        protected abstract IList<string> TransformRecipients(IList<string> recipients);

        protected virtual IList<string> AppendAdditionalEmailTemplateLines(List<string> recipients, Dictionary<string, object> mines, string sendingContext)
        {
            return null;
        }

        protected abstract void SendHtmlMail(string subject, string htmlBody, string recipientEmail);
    }
}
