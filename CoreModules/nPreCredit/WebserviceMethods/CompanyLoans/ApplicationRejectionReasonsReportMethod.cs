using nPreCredit.Code;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class ApplicationRejectionReasonsReportMethod : FileStreamWebserviceMethod<ApplicationRejectionReasonsReportMethod.Request>
    {
        public override string Path => "Reports/CompanyLoan/Applications";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var clock = requestContext.Clock();

            var today = clock.Today;
            if (request.PeriodName == "today")
            {
                request.FromApplicationDate = today;
                request.ToApplicationDate = today;
            }
            else if (request.PeriodName == "thismonth")
            {
                request.FromApplicationDate = new DateTime(today.Year, today.Month, 1);
                request.ToApplicationDate = request.FromApplicationDate.Value.AddMonths(1).AddDays(-1);
            }
            else if (request.PeriodName == "thisweek")
            {
                request.FromApplicationDate = today.MondayOfWeek();
                request.ToApplicationDate = today.MondayOfWeek().AddDays(7);
            }
            else if (!request.FromApplicationDate.HasValue || !request.ToApplicationDate.HasValue)
            {
                return Error("Either PeriodName must be one of today|thismonth|thisweek or both FromApplicationDate and ToApplicationDate must be set");
            }

            var wf = requestContext.Resolver().Resolve<ICompanyLoanWorkflowService>();

            var knownRejectionReasons =
                CompanyLoanRejectionScoringSetup.Instance.RejectionReasons.Select(x => x.Name).ToList();
            if (!knownRejectionReasons.Contains("other"))
                knownRejectionReasons.Add("other");

            DocumentClientExcelRequest excelRequest;

            using (var context = new PreCreditContext())
            {
                var fromDateInc = request.FromApplicationDate.Value.Date;
                var toDateEx = request.ToApplicationDate.Value.Date.AddDays(1);

                var appsPre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationDate >= fromDateInc && x.ApplicationDate < toDateEx);
                if (!string.IsNullOrWhiteSpace(request.ProviderName))
                    appsPre = appsPre.Where(x => x.ProviderName == request.ProviderName);

                var apps = appsPre.Select(x => new
                {
                    x.ApplicationDate,
                    x.ApplicationNr,
                    ListNames = x.ListMemberships.Select(y => y.ListName),
                    x.ProviderName,
                    RejectionReasons = x.CurrentCreditDecision.SearchTerms.Where(y => y.TermName == "RejectionReason").Select(y => y.TermValue)
                })
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .ToList();

                var allRejectionReasons = apps.SelectMany(x => x.RejectionReasons).Concat(knownRejectionReasons).DistinctPreservingOrder().ToList();

                var cols = DocumentClientExcelRequest.CreateDynamicColumnList(apps);

                cols.Add(apps.Col(x => x.ApplicationDate.Date, ExcelType.Date, "Date"));
                cols.Add(apps.Col(x => x.ApplicationNr, ExcelType.Text, "Application nr"));
                cols.Add(apps.Col(x => x.ProviderName, ExcelType.Text, "Provider"));
                cols.Add(apps.Col(x => GetListDisplayName(x.ListNames, wf), ExcelType.Text, "Category"));
                foreach (var r in allRejectionReasons.OrderBy(x => x))
                {
                    cols.Add(apps.Col(x => x.RejectionReasons.Contains(r) ? 1 : 0, ExcelType.Number, r, nrOfDecimals: 0, includeSum: true));
                }

                excelRequest = new DocumentClientExcelRequest
                {
                    Sheets = new DocumentClientExcelRequest.Sheet[]
                    {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = $"Applications"
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Filter"
                        }
                    }
                };
                excelRequest.Sheets[0].SetColumnsAndData(apps, cols.ToArray());

                var filters = new List<Tuple<string, string>>
                {
                    Tuple.Create("From date", request.FromApplicationDate.Value.ToString("yyyy-MM-dd")),
                    Tuple.Create("To date", request.ToApplicationDate.Value.ToString("yyyy-MM-dd"))
                };
                if (!string.IsNullOrWhiteSpace(request.ProviderName))
                    filters.Add(Tuple.Create("Provider", request.ProviderName));
                excelRequest.Sheets[1].SetColumnsAndData(filters,
                    filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                    filters.Col(x => x.Item2, ExcelType.Text, "Value"));
            }

            var client = new nDocumentClient();
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"Applications-{request.FromApplicationDate.Value.ToString("yyyy-MM-dd")}-{request.ToApplicationDate.Value.ToString("yyyy-MM-dd")}.xlsx");
        }

        private string GetListDisplayName(IEnumerable<string> listNames, ICompanyLoanWorkflowService wf)
        {
            var listName = wf.GetCurrentListName(listNames);
            if (listName == null)
                return "Unknown";

            bool isMissingTranslation = false;
            var translatedName = Translations.GetTranslation($"companyLoan.listName.{listName}", "en", observeIsMissingTranslation: () => isMissingTranslation = true);
            if (!isMissingTranslation)
                return translatedName;

            if (wf.TryDecomposeListName(listName, out var stepNameAndStatusName))
                return $"{stepNameAndStatusName.Item1} {stepNameAndStatusName.Item2}";
            else
                return listName;
        }

        public class Request
        {
            public string ProviderName { get; set; }

            public DateTime? FromApplicationDate { get; set; }
            public DateTime? ToApplicationDate { get; set; }

            public string PeriodName { get; set; }
        }
    }
}