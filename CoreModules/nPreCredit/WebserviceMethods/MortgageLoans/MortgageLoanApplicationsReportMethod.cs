using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class MortgageLoanApplicationsReportMethod : FileStreamWebserviceMethod<MortgageLoanApplicationsReportMethod.Request>
    {
        public override string Path => "MortgageLoan/Reports/Applications";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

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

            if (Dates.GetAbsoluteNrOfDaysBetweenDates(request.FromApplicationDate.Value, request.ToApplicationDate.Value) > 35)
                return Error("Report interval cannot be more than 35 days");

            DocumentClientExcelRequest excelRequest;

            using (var context = new PreCreditContext())
            {
                var fromDateInc = request.FromApplicationDate.Value.Date;
                var toDateEx = request.ToApplicationDate.Value.Date.AddDays(1);

                var appsPre = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationDate >= fromDateInc && x.ApplicationDate < toDateEx);

                if (!string.IsNullOrWhiteSpace(request.ProviderName))
                {
                    appsPre = appsPre.Where(x => x.ProviderName == request.ProviderName);
                }

                var hasCampaignParameterFilter = !string.IsNullOrWhiteSpace(request.CampaignParameterName) &&
                                                 !string.IsNullOrWhiteSpace(request.CampaignParameterValue);
                if (hasCampaignParameterFilter)
                {
                    appsPre = appsPre.Where(x => x.ComplexApplicationListItems.Any(y =>
                        y.ListName == CampaignCodeService.ParameterComplexListName
                        && y.Nr == 1
                        && y.ItemName == request.CampaignParameterName
                        && y.ItemValue == request.CampaignParameterValue));
                }


                var creditNrItemNames = new List<string> { "creditNr", "mainLoanCreditNr", "childLoanCreditNr" };
                var apps = appsPre.Select(x => new
                {
                    x.ApplicationDate,
                    x.ApplicationNr,
                    x.IsActive,
                    x.IsRejected,
                    x.IsFinalDecisionMade,
                    x.IsCancelled,
                    ListNames = x.ListMemberships.Select(y => y.ListName),
                    x.ProviderName,
                    RejectionReasons = x.CurrentCreditDecision.DecisionItems.Where(y => y.IsRepeatable && y.ItemName == "rejectionReasonText").Select(y => y.Value),
                    CreditNrs = x.Items.Where(y => y.GroupName == "application" && creditNrItemNames.Contains(y.Name)).Select(y => y.Value),
                    CampaignParameters = x
                        .ComplexApplicationListItems
                        .Where(y => y.ListName == CampaignCodeService.ParameterComplexListName && y.Nr == 1)
                        .Select(y => new { y.ItemName, y.ItemValue })
                })
                .OrderBy(x => x.ApplicationDate)
                .ThenBy(x => x.ApplicationNr)
                .ToList();

                var wf = requestContext.Resolver().Resolve<IMortgageLoanWorkflowService>();

                Func<IEnumerable<string>, string> getCategory = listNames =>
                {
                    var currentListName = wf.GetCurrentListName(listNames);
                    if (currentListName == null)
                        return "Unknown";
                    if (!wf.TryDecomposeListName(currentListName, out var stepNameAndStatusName))
                        return currentListName;
                    return wf.GetStepDisplayName(stepNameAndStatusName.Item1) ?? stepNameAndStatusName.Item1;
                };

                var cols = DocumentClientExcelRequest.CreateDynamicColumnList(apps);

                var affiliateDisplayNameByName = NEnv.GetAffiliateModels().ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName);

                cols.Add(apps.Col(x => x.ApplicationDate.Date, ExcelType.Date, "Date"));
                cols.Add(apps.Col(x => x.ApplicationNr, ExcelType.Text, "Application nr"));
                cols.Add(apps.Col(x => Coalesce(
                    "Inactive",
                    Tuple.Create(x.IsFinalDecisionMade, "Loan created"),
                    Tuple.Create(x.IsCancelled, "Cancelled"),
                    Tuple.Create(x.IsRejected, "Rejected"),
                    Tuple.Create(x.IsActive, "Active")), ExcelType.Text, "Status"));
                cols.Add(apps.Col(x => getCategory(x.ListNames), ExcelType.Text, "Category"));
                cols.Add(apps.Col(x => affiliateDisplayNameByName.Opt(x.ProviderName) ?? x.ProviderName, ExcelType.Text, "Provider"));
                cols.Add(apps.Col(x => x.IsRejected ? string.Join(", ", x.RejectionReasons) : null, ExcelType.Text, "Rejection reasons"));
                cols.Add(apps.Col(x => x.IsFinalDecisionMade ? string.Join(", ", x.CreditNrs) : null, ExcelType.Text, "Created loans"));

                //Print out all the campaign parameters that exist like utm_campagin, utm_source, campaignCode and similar.
                //Note that this solution is unlikely to be the final one as there is likely need for an abstraction layer
                //between here where you can filter out some parameters that you dont want shown and set the column names and such
                //This is also very inefficient but should work fine for at least couple of years.
                var allCampaignParameters = apps.SelectMany(x => x.CampaignParameters.Select(y => y.ItemName)).ToHashSet();
                foreach (var campaignParameter in allCampaignParameters)
                {
                    cols.Add(apps.Col(x => x.CampaignParameters.FirstOrDefault(y => y.ItemName == campaignParameter)?.ItemValue, ExcelType.Text, campaignParameter));
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
                {
                    filters.Add(Tuple.Create("Provider", request.ProviderName));
                }

                if (hasCampaignParameterFilter)
                {
                    filters.Add(Tuple.Create("Campaign Parameter Name", request.CampaignParameterName));
                    filters.Add(Tuple.Create("Campaign Parameter Value", request.CampaignParameterValue));
                }

                excelRequest.Sheets[1].SetColumnsAndData(filters,
                    filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                    filters.Col(x => x.Item2, ExcelType.Text, "Value"));
            }

            var client = new nDocumentClient();
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"Applications-{request.FromApplicationDate.Value.ToString("yyyy-MM-dd")}-{request.ToApplicationDate.Value.ToString("yyyy-MM-dd")}.xlsx");
        }

        private string Coalesce(string fallback, params Tuple<bool, string>[] options)
        {
            foreach (var option in options)
            {
                if (option.Item1)
                    return option.Item2;
            }
            return fallback;
        }

        public class Request
        {
            public DateTime? FromApplicationDate { get; set; }
            public DateTime? ToApplicationDate { get; set; }
            public string PeriodName { get; set; }
            public string ProviderName { get; set; }
            public string CampaignParameterName { get; set; }
            public string CampaignParameterValue { get; set; }
        }
    }
}