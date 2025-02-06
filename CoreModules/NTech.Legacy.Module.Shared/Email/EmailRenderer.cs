using System;
using System.Collections.Generic;

namespace NTech.Services.Infrastructure.Email
{
    public class EmailRenderer : IEmailRenderer
    {
        private readonly Func<string, string> convertMarkdownBodyToHtml;
        private readonly Func<string, Dictionary<string, object>, string> replaceMustacheMines;

        public EmailRenderer(Func<string, string> convertMarkdownBodyToHtml, Func<string, Dictionary<string, object>, string> replaceMustacheMines)
        {
            this.convertMarkdownBodyToHtml = convertMarkdownBodyToHtml;
            this.replaceMustacheMines = replaceMustacheMines;
        }

        public string ConvertMarkdownBodyToHtml(string template)
        {
            return convertMarkdownBodyToHtml(template);
        }

        public string ReplaceMustacheMines(string template, Dictionary<string, object> mines)
        {
            return replaceMustacheMines(template, mines);
        }
    }
}
