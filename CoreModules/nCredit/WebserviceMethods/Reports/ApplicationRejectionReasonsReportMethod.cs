using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class ApplicationRejectionReasonsReportMethod : FileStreamWebserviceMethod<ApplicationRejectionReasonsReportMethod.Request>
    {
        public override string Path => "Reports/ApplicationRejectionReasons";

        public override bool IsEnabled => NEnv.IsUnsecuredLoansEnabled && !NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, x =>
            {
                x.Require(r => r.ProviderName);
            });

            var dc = new DataWarehouseClient();
            var p = new ExpandoObject();
            (p as IDictionary<string, object>)["providerName"] = request.ProviderName;

            var items = dc.FetchReportData<ProviderRejectionModel>("providerRejections1", p);

            var allRejectionReasons = new HashSet<string>();
            var rows = items
                .Select(x =>
                {
                    var reasons = ParseReasons(x.RejectionReasons);
                    foreach (var r in reasons)
                        allRejectionReasons.Add(r);
                    return new
                    {
                        ApplicationNr = x.ApplicationNr,
                        Reasons = reasons
                    };
                })
                .ToList();

            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);

            cols.Add(rows.Col(x => x.ApplicationNr, ExcelType.Text, "Application nr"));
            foreach (var r in allRejectionReasons.OrderBy(x => x))
            {
                cols.Add(rows.Col(x => x.Reasons.Contains(r) ? 1 : 0, ExcelType.Number, r, nrOfDecimals: 0, includeSum: true));
            }

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Rejections - {request.ProviderName}"
                        }
                }
            };
            excelRequest.Sheets[0].SetColumnsAndData(rows, cols.ToArray());

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"RejectionReasons-{request.ProviderName}.xlsx");
        }

        private class ProviderRejectionModel
        {
            public string ApplicationNr { get; set; }
            public string RejectionReasons { get; set; }
        }

        private static HashSet<string> ParseReasons(string rejectionReasons)
        {
            var h = new HashSet<string>();
            var r = (rejectionReasons ?? "").ToLowerInvariant().Trim();
            var i = r.IndexOf("other");

            if (i >= 0) h.Add("other");

            if (i == 0)
                r = "";
            else if (i > 0)
                r = r.Substring(0, i);

            foreach (var reason in r.Split(',').Select(x => string.IsNullOrWhiteSpace(x) ? null : x?.Trim()?.ToLowerInvariant()).Where(x => x != null))
                h.Add(reason);

            return h;
        }

        public class Request
        {
            public string ProviderName { get; set; }
        }
    }
}