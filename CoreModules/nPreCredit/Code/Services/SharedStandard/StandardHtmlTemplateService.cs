using NTech.Core.Module.Shared.Clients;
using System;

namespace nPreCredit.Code.Services.SharedStandard
{
    public class StandardHtmlTemplateService
    {
        private readonly ICustomerClient _customerClient;
        private const string PageBreakDelimiter = "[[[PAGE_BREAK]]]";

        public StandardHtmlTemplateService(ICustomerClient customerClient)
        {
            _customerClient = customerClient;
        }

        /// <summary>
        /// Expects one or more pages defined in settings with an optional delimiter for each page. 
        /// Builds up html that can be used by WeasyPrint. (note that other html-to-pdf-software can have different html markup rules)
        /// Div class page = one of these per page
        /// Div class blackborder = adds a black border under the company logo on top of each page
        /// Div class pt-3 = adds padding under border and company logo for page body text
        /// </summary>
        /// <param name="templateName">Ex. generalTermsHtmlTemplate</param>
        /// <param name="pageHeader">Optional text that will be added as a header of the page</param>
        /// <returns></returns>
        public string BuildWeasyPrintHtmlFromSettingsTemplate(string templateName, string pageHeader = null)
        {
            var settings = _customerClient.LoadSettings(templateName);

            var templateHtml = settings[templateName];
            var splitByPageBreak = templateHtml.Split(new string[] { PageBreakDelimiter }, StringSplitOptions.None);

            var rawHtml = "";
            var pageHeaderHtml = pageHeader != null ? $@"<div class=""header""><h1>{pageHeader}</h1></div>" : "";

            foreach (var pageHtml in splitByPageBreak)
            {
                rawHtml += $@"
                    <div class=""page"">
                        <div class=""blackborder"">{pageHeaderHtml}</div>
                        <div class=""pt-3"">
                            {pageHtml}
                        </div>
                    </div>";
            }

            return rawHtml;
        }

    }
}