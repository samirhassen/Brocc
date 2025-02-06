using System.Collections.Generic;

namespace NTech.Services.Infrastructure.Email
{
    public interface INTechEmailService
    {
        void SendTemplateEmailComplex(List<string> recipients, string templateName, Dictionary<string, object> mines, string sendingContext);
        void SendTemplateEmail(List<string> recipients, string templateName, Dictionary<string, string> mines, string sendingContext);
        void SendRawEmail(List<string> recipients, string subjectTemplateText, string bodyTemplateText, Dictionary<string, object> mines, string sendingContext);
        (string SubjectTemplateText, string BodyTemplateText, bool IsEnabled)? LoadClientResourceTemplate(string templateName, bool isRequired);
    }

    public interface INTechEmailServiceFactory
    {
        INTechEmailService CreateEmailService();
        bool HasEmailProvider { get; }
    }
}
