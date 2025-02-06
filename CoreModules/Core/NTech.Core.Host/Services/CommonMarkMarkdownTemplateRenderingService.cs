using NTech.Core.Module.Shared.Services;

namespace NTech.Core.Host.Services
{
    public class CommonMarkMarkdownTemplateRenderingService : IMarkdownTemplateRenderingService
    {
        public string RenderTemplateToHtml(string template) => CommonMark.CommonMarkConverter.Convert(template);
    }
}
