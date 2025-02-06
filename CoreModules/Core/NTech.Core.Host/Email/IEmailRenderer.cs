using System.Collections.Generic;

namespace NTech.Services.Infrastructure.Email
{
    public interface IEmailRenderer
    {
        string ReplaceMustacheMines(string template, Dictionary<string, object> mines);
        string ConvertMarkdownBodyToHtml(string template);
    }
}
