using System.Collections.Generic;

namespace NTech.Core.Module.Shared.Services
{
    public interface IMustacheTemplateRenderingService
    {
        string RenderTemplate(string templateText, Dictionary<string, object> dataMines);
    }
}
