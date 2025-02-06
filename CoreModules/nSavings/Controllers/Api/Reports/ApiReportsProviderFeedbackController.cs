using Newtonsoft.Json;
using nSavings.Code;
using nSavings.Code.Services;
using nSavings.Excel;
using NTech.Services.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace nSavings.Controllers
{
    [NTechApi]
    public class ApiReportsProviderFeedbackController : NController
    {
        [Route("Api/Reports/GetProviderFeedback")]
        [HttpGet()]
        public ActionResult GetProviderFeedback(DateTime date1, DateTime date2)
        {
            try
            {
                var fromDate = date1.Date;
                var toDate = date2.Date;
                using (var context = new SavingsContext())
                {
                    var accountsBase = context
                        .SavingsAccountHeaders
                        .Where(x => x.CreatedByEvent.TransactionDate >= fromDate && x.CreatedByEvent.TransactionDate <= toDate)
                        .Select(x => new
                        {
                            x.SavingsAccountNr,
                            x.CreatedByBusinessEventId,
                            CreatedDate = x.CreatedByEvent.TransactionDate,
                            ExternalVariablesKey = x
                                .DatedStrings
                                .Where(y => y.Name == DatedSavingsAccountStringCode.ExternalVariablesKey.ToString())
                                .OrderByDescending(y => y.BusinessEventId)
                                .Select(y => y.Value)
                                .FirstOrDefault()
                        })
                        .OrderByDescending(x => x.CreatedByBusinessEventId)
                        .ToList();

                    Dictionary<string, Dictionary<string, string>> externalVariablesByKey;
                    var keys = new HashSet<string>(accountsBase.Where(x => x.ExternalVariablesKey != null).Select(x => x.ExternalVariablesKey).ToList());
                    if (keys.Any())
                    {
                        var result = this.Service.KeyValueStore(GetCurrentUserMetadata()).GetValues(keys, KeyValueStoreKeySpaceCode.SavingsExternalVariablesV1.ToString());
                        externalVariablesByKey = keys
                            .Select(x => new { Key = x, Value = result.Opt(x) })
                            .Where(x => x.Value != null)
                            .Select(x => new
                            {
                                Key = x.Key,
                                Variables = JsonConvert.DeserializeAnonymousType(x.Value, new[] { new { Name = "", Value = "" } }).ToDictionary(y => y.Name, y => y.Value)
                            })
                            .ToDictionary(x => x.Key, x => x.Variables);
                    }
                    else
                        externalVariablesByKey = new Dictionary<string, Dictionary<string, string>>();

                    var providerNameVariables = new HashSet<string>() { "utm_source", "pp" };
                    var campaignNameVariables = new HashSet<string>() { "utm_campaign", "cc" };
                    var allPredefinedVariables = new HashSet<string>();
                    providerNameVariables.ToList().ForEach(x => allPredefinedVariables.Add(x));
                    campaignNameVariables.ToList().ForEach(x => allPredefinedVariables.Add(x));

                    var accounts = accountsBase.Select(x => new
                    {
                        x.CreatedDate,
                        x.SavingsAccountNr,
                        ExternalVariables = (x.ExternalVariablesKey != null ? externalVariablesByKey.Opt(x.ExternalVariablesKey) : null) ?? new Dictionary<string, string>()
                    })
                        .Select(x => new
                        {
                            x.CreatedDate,
                            x.SavingsAccountNr,
                            ProviderName = x.ExternalVariables.Where(y => providerNameVariables.Contains(y.Key)).Select(y => y.Value).FirstOrDefault(),
                            CampaignName = x.ExternalVariables.Where(y => campaignNameVariables.Contains(y.Key)).Select(y => y.Value).FirstOrDefault(),
                            OtherVariables = x.ExternalVariables.Where(y => !allPredefinedVariables.Contains(y.Key)).ToDictionary(y => y.Key, y => y.Value)
                        })
                        .ToList();

                    var allOtherVariables = new HashSet<string>(accounts.SelectMany(x => x.OtherVariables.Keys));

                    var sheets = new List<DocumentClientExcelRequest.Sheet>();
                    sheets.Add(new DocumentClientExcelRequest.Sheet
                    {
                        AutoSizeColumns = true,
                        Title = $"{fromDate.ToString("yyyy-MM-dd")} {toDate.ToString("yyyy-MM-dd")}"
                    });

                    var request = new DocumentClientExcelRequest
                    {
                        Sheets = sheets.ToArray()
                    };

                    var s = request.Sheets[0];

                    var cols = DocumentClientExcelRequest.CreateDynamicColumnList(accounts);

                    cols.Add(accounts.Col(x => x.SavingsAccountNr, ExcelType.Text, "Account nr"));
                    cols.Add(accounts.Col(x => x.CreatedDate, ExcelType.Date, "Created date"));
                    cols.Add(accounts.Col(x => x.ProviderName, ExcelType.Text, "Provider"));
                    cols.Add(accounts.Col(x => x.CampaignName, ExcelType.Text, "Campaign"));
                    foreach (var v in allOtherVariables)
                    {
                        cols.Add(accounts.Col(x => x.OtherVariables.Opt(v), ExcelType.Text, v));
                    }

                    s.SetColumnsAndData(accounts, cols.ToArray());

                    var dc = new DocumentClient();
                    var report = dc.CreateXlsx(request);

                    return new FileStreamResult(report, XlsxContentType) { FileDownloadName = $"savingsProviderFeedback-{fromDate.ToString("yyyy-MM-dd")}-{fromDate.ToString("yyyy-MM-dd")}.xlsx" };
                }
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Failed to create providerFeedback report");
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Internal server error");
            }
        }
    }
}