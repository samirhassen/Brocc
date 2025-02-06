using NTech.Core.Module.Shared.Services;
using System.Collections.Generic;

namespace nCredit.Code
{
    public class NustacheTemplateService : IMustacheTemplateRenderingService
    {
        public string RenderTemplate(string templateText, Dictionary<string, object> dataMines) => Nustache.Core.Render.StringToString(templateText, dataMines);
        public static NustacheTemplateService SharedInstance { get; private set; } = new NustacheTemplateService();
    }
}