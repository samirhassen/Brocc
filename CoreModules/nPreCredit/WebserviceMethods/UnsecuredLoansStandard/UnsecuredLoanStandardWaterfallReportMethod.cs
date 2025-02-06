using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans.Waterfall;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardWaterfallReportMethod : FileStreamWebserviceMethod<UnsecuredLoanStandardWaterfallReportMethod.Request>
    {
        public override string Path => "UnsecuredLoanStandard/Reports/Waterfall";

        public static bool IsReportEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override bool IsEnabled => IsReportEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var periodType = Enums.Parse<WaterfallPeriodTypeCode>(request.PeriodType, ignoreCase: true);
            if (!periodType.HasValue)
            {
                return Error($"PeriodType must be one of {string.Join(", ", Enums.GetAllValues<WaterfallPeriodTypeCode>())}", errorCode: "missingOrInvalidPeriodType");
            }

            var service = new UnsecuredLoanStandardWaterfallService();

            var workflowService = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

            var providerName = request.ProviderName.NormalizeNullOrWhitespace();
            var applicationsResult = service.GetApplications(request.FromInPeriodDate.Value, request.ToInPeriodDate.Value, periodType.Value, providerName: providerName);
            var applications = applicationsResult.Applications;

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new[]
                {
                    CreateWaterfallSheet(applications, service, periodType.Value),
                    CreateFilterSheet(applicationsResult.FromDate, applicationsResult.ToDate, periodType.Value, providerName),
                    CreateDetailsSheet(applications, workflowService)
                }
            };

            var result = new nDocumentClient().CreateXlsx(excelRequest);
            return File(result, downloadFileName: "Waterfall.xlsx");
        }

        private DocumentClientExcelRequest.Sheet CreateWaterfallSheet(
            List<WaterfallApplicationModel> applications,
            UnsecuredLoanStandardWaterfallService waterfallService,
            WaterfallPeriodTypeCode periodTypeCode)
        {
            var sheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Waterfall"
            };

            var (dates, rows) = waterfallService.ComputeAggregates(applications, periodTypeCode);

            string GetCategoryName(DateTime d)
            {
                switch (periodTypeCode)
                {
                    case WaterfallPeriodTypeCode.Yearly:
                        return d.Year.ToString();
                    case WaterfallPeriodTypeCode.Monthly:
                        return $"{d.Year}-{d.Month.ToString().PadLeft(2, '0')}";
                    case WaterfallPeriodTypeCode.Quarterly:
                        return $"{d.Year} Q{Quarter.ContainingDate(d).InYearOrdinalNr}";
                    default:
                        throw new NotImplementedException();
                }
            }

            var waterfallCols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);
            waterfallCols.Add(rows.Col(x => x.Description, ExcelType.Text, "Category"));
            foreach (var d in dates.OrderBy(x => x))
            {
                var localD = d;
                waterfallCols.Add(rows.Col(x => x.GetValue(localD), ExcelType.Number, GetCategoryName(d), nrOfDecimals: 0, overrideRowStyle: x => x.StyleOverride));
            }
            sheet.SetColumnsAndData(rows, waterfallCols.ToArray());

            return sheet;
        }

        private DocumentClientExcelRequest.Sheet CreateFilterSheet(DateTime fromDate, DateTime toDate, WaterfallPeriodTypeCode periodTypeCode, string providerName)
        {
            var filters = new List<Tuple<string, string>>();
            void AddFilter(string x, string y) => filters.Add(Tuple.Create(x, y));

            AddFilter("From date", fromDate.ToString("yyyy-MM-dd"));
            AddFilter("To date", toDate.ToString("yyyy-MM-dd"));
            AddFilter("Period", periodTypeCode.ToString());
            AddFilter("Provider", string.IsNullOrWhiteSpace(providerName) ? "all" : providerName);

            var sheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Filters"
            };

            sheet.SetColumnsAndData(filters,
                filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                filters.Col(x => x.Item2, ExcelType.Text, "Value"));

            return sheet;
        }

        private DocumentClientExcelRequest.Sheet CreateDetailsSheet(List<WaterfallApplicationModel> applications, WorkflowServiceReadBase workflowService)
        {
            var detailsCols = DocumentClientExcelRequest.CreateDynamicColumnList(applications);

            detailsCols.Add(applications.Col(x => x.ApplicationNr, ExcelType.Text, "ApplicationNr"));
            detailsCols.Add(applications.Col(x => x.ProviderName, ExcelType.Text, "ProviderName"));
            detailsCols.Add(applications.Col(x => x.GetPeriodStartDate(WaterfallPeriodTypeCode.Monthly), ExcelType.Date, "PeriodMonthDate"));
            detailsCols.Add(applications.Col(x => x.GetPeriodStartDate(WaterfallPeriodTypeCode.Quarterly), ExcelType.Date, "PeriodQuarterDate"));
            detailsCols.Add(applications.Col(x => x.GetPeriodStartDate(WaterfallPeriodTypeCode.Yearly), ExcelType.Date, "PeriodYearDate"));
            detailsCols.Add(applications.Col(x => x.GetPaidOutAmount(), ExcelType.Number, "PaidOutAmount", includeSum: true));
            bool hasPassedCreditCheck = false;
            foreach (var stepName in workflowService.GetStepOrder())
            {
                detailsCols.Add(applications.Col(x => x.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Current ? 1 : new int?(), ExcelType.Number, $"Current {stepName}", nrOfDecimals: 0, includeSum: true));
                if (!hasPassedCreditCheck) //There are no rejections after this so we hide all the zeroes. This can be safely removed. Its just to reduce clutter.
                {
                    detailsCols.Add(applications.Col(x => x.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Rejected ? 1 : new int?(), ExcelType.Number, $"Rejected {stepName}", nrOfDecimals: 0, includeSum: true));
                }
                detailsCols.Add(applications.Col(x => x.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Cancelled ? 1 : new int?(), ExcelType.Number, $"Cancelled {stepName}", nrOfDecimals: 0, includeSum: true));
                detailsCols.Add(applications.Col(x => x.GetWaterfallState(workflowService, stepName) == StepWaterfallStateCode.Accepted ? 1 : new int?(), ExcelType.Number, $"Approved {stepName}", nrOfDecimals: 0, includeSum: true));

                if (stepName == UnsecuredLoanStandardWorkflowService.CreditCheckStep.Name)
                    hasPassedCreditCheck = true;
            }
            var sheet = new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Details"
            };
            sheet.SetColumnsAndData(applications, detailsCols.ToArray());
            return sheet;
        }

        public class Request
        {
            [Required]
            public DateTime? FromInPeriodDate { get; set; }

            [Required]
            public DateTime? ToInPeriodDate { get; set; }

            public string PeriodType { get; set; }

            public string ProviderName { get; set; }
        }
    }
}