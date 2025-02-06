namespace NTech.Core.Module.Shared.Services
{
    public interface IMarkdownTemplateRenderingService
    {
        string RenderTemplateToHtml(string template);
    }
}
