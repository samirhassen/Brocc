using NTech.Core.Module.Shared.Services;
using Stubble.Core.Builders;

namespace NTech.Core.Credit.Services
{
    public class StubbleMustacheTemplateService : IMustacheTemplateRenderingService
    {
        public string RenderTemplate(string templateText, Dictionary<string, object> dataMines)
        {
            var stubble = new StubbleBuilder().Build();
            return stubble.Render(templateText, dataMines);
        }
    }
}
