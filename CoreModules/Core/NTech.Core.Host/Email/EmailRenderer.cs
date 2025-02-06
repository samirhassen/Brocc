using NTech.Core.Module.Shared.Services;

namespace NTech.Services.Infrastructure.Email
{
    public class EmailRenderer : IEmailRenderer
    {
        private readonly IMarkdownTemplateRenderingService markdownService;
        private readonly IMustacheTemplateRenderingService mustacheService;

        public EmailRenderer(IMarkdownTemplateRenderingService markdownService, IMustacheTemplateRenderingService mustacheService)
        {
            this.markdownService = markdownService;
            this.mustacheService = mustacheService;
        }

        public string ConvertMarkdownBodyToHtml(string template) => markdownService.RenderTemplateToHtml(template);
        public string ReplaceMustacheMines(string template, Dictionary<string, object> mines) => mustacheService.RenderTemplate(template, mines);
    }
}
