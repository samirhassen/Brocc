using nCreditReport.Models;
using NTech.Banking.CivicRegNumbers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace nCreditReport.Code.TestOnly
{
    public class TestAwarePersonCreditReportService : PersonBaseCreditReportService
    {
        private readonly PersonBaseCreditReportService actualService;
        private readonly ICreditReportCommonTestSettings settings;

        public TestAwarePersonCreditReportService(PersonBaseCreditReportService actualService, ICreditReportCommonTestSettings settings) : base(actualService.ProviderName)
        {
            this.actualService = actualService;
            this.settings = settings;
        }

        public override string ForCountry => actualService.ForCountry;

        protected override Result DoTryBuyCreditReport(
            ICivicRegNumber civicRegNr,
            CreditReportRequestData requestData)
        {
            Func<bool, Result> getFromTestModule = (generateIfNotExists) =>
            {
                var tc = new nTestClient();

                var tp = tc.GetTestPerson("creditreport_", civicRegNr, generateIfNotExists);
                if (tp == null)
                    return null;
                else
                {
                    var items = tp.Where(x => x.Key.StartsWith("creditreport_")).Select(x => new SaveCreditReportRequest.Item
                    {
                        Name = x.Key.Substring("creditreport_".Length),
                        Value = x.Value
                    }).ToList();

                    if (!items.Any(x => x.Name == "htmlReportArchiveKey"))
                    {
                        var htmlReportArchiveKey = CreateHtmlPreviewInArchive(items, civicRegNr);
                        items.Add(new SaveCreditReportRequest.Item
                        {
                            Name = "htmlReportArchiveKey",
                            Value = htmlReportArchiveKey
                        });
                    }

                    return new Result
                    {
                        CreditReport = this.CreateResult(civicRegNr, items, requestData)
                    };
                }
            };

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("only"))
                return getFromTestModule(true);

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("preferred"))
            {
                var testModuleResult = getFromTestModule(false);
                if (testModuleResult != null)
                    return testModuleResult;
            }

            var result = actualService.TryBuyCreditReport(civicRegNr, requestData);

            if ((this.settings.TestModuleMode ?? "").IsOneOfIgnoreCase("fallback") && result.IsError && (result.ErrorMessage ?? "").Contains("Objekt-nr saknas i UC:s register"))
                return getFromTestModule(true);
            else
                return result;
        }

        private string CreateHtmlPreviewInArchive(List<SaveCreditReportRequest.Item> creditReportItems, ICivicRegNumber civicRegNr)
        {
            var htmlTemplate = @"<!DOCTYPE html>
<html lang=""en"">
  <head>
    <meta charset=""utf-8"">
    <title>Html preview</title>
  </head>
  <body>
    <h1>Test module creditreport</h1>
    <h2>For {0}</h2>
    <h2>Date {1}</h2>
    <table border=""1"">
        <thead>
            <tr>
                <th>Name</th>
                <th>Value</th>
            </tr>
        </thead>
        <tbody>
            {2}
        </tbody>
    </table>
  </body>
</html>";
            var html = string.Format(htmlTemplate,
                civicRegNr.NormalizedValue,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), //Skipping the time machine since it's useful to be able to see if these have updated while testing
                string.Join("", creditReportItems.Select(x => $"<tr><td>{HttpUtility.HtmlEncode(x.Name)}</td><td>{HttpUtility.HtmlEncode(x.Value)}</td></tr>")));
            return new DocumentClient().ArchiveStore(Encoding.UTF8.GetBytes(html), "text/html", $"testmodule_html_preview_{civicRegNr.NormalizedValue}.html");
        }

        public override bool CanFetchTabledValues() => actualService.CanFetchTabledValues();
        public override List<DictionaryEntry> FetchTabledValues(CreditReportRepository.FetchResult creditReport) => actualService.FetchTabledValues(creditReport);
    }
}