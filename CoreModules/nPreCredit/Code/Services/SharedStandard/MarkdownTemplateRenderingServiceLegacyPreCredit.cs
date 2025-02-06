using NTech.Core.Module.Shared.Services;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class MarkdownTemplateRenderingServiceLegacyPreCredit : IMarkdownTemplateRenderingService
    {
        public string RenderTemplateToHtml(string template) => CommonMark.CommonMarkConverter.Convert(template);
    }
}