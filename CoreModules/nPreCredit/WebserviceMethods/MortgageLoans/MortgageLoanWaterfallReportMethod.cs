using nPreCredit.Code;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class MortgageLoanWaterfallReportMethod : FileStreamWebserviceMethod<MortgageLoanWaterfallReportMethod.Request>
    {
        public override string Path => "MortgageLoan/Reports/Waterfall";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var periodType = Enums.Parse<PeriodTypeCode>(request.PeriodType, ignoreCase: true);
            if (!periodType.HasValue)
            {
                return Error($"PeriodType must be one of {string.Join(", ", Enums.GetAllValues<PeriodTypeCode>())}", errorCode: "missingOrInvalidPeriodType");
            }

            (DateTime, DateTime)? filterDates = (request.FromInPeriodDate.Value, request.ToInPeriodDate.Value);

            var s = new MortgageLoanWaterfallDataService();
            (string ParameterName, string ParameterValue)? filterByCampaignParameter = null;
            if (!string.IsNullOrWhiteSpace(request.CampaignParameterName) && !string.IsNullOrWhiteSpace(request.CampaignParameterValue))
            {
                filterByCampaignParameter = (request.CampaignParameterName, request.CampaignParameterValue);
            }
            var details = s.GetApplicationModels(
                filterByProviderName: request.ProviderName,
                filterByMonthDates: periodType == PeriodTypeCode.Monthly ? filterDates : null,
                filterByQuarterDates: periodType == PeriodTypeCode.Quarterly ? filterDates : null,
                filterByYearDates: periodType == PeriodTypeCode.Yearly ? filterDates : null,
                filterByCampaignParameter: filterByCampaignParameter);

            var sheets = new List<DocumentClientExcelRequest.Sheet>();

            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Waterfall"
            });
            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = "Filters"
            });
            sheets.Add(new DocumentClientExcelRequest.Sheet
            {
                AutoSizeColumns = true,
                Title = $"Details"
            });

            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = sheets.ToArray()
            };

            var (dates, rows) = ComputeAggregates(details, x =>
            {
                switch (periodType)
                {
                    case PeriodTypeCode.Monthly: return x.PeriodMonthDate.Value;
                    case PeriodTypeCode.Quarterly: return x.PeriodQuarterDate.Value;
                    case PeriodTypeCode.Yearly: return x.PeriodYearDate.Value;
                    default: throw new NotImplementedException();
                }
            });

            string GetCategoryName(DateTime d)
            {
                switch (periodType)
                {
                    case PeriodTypeCode.Yearly:
                        return d.Year.ToString();
                    case PeriodTypeCode.Monthly:
                        return $"{d.Year}-{d.Month.ToString().PadLeft(2, '0')}";
                    case PeriodTypeCode.Quarterly:
                        return $"{d.Year} Q{Quarter.ContainingDate(d).InYearOrdinalNr}";
                    default:
                        throw new NotImplementedException();
                }
            }

            var cols = DocumentClientExcelRequest.CreateDynamicColumnList(rows);
            cols.Add(rows.Col(x => x.Description, ExcelType.Text, "Category"));
            foreach (var d in dates.OrderBy(x => x))
            {
                var localD = d;
                cols.Add(rows.Col(x => x.GetValue(localD), ExcelType.Number, GetCategoryName(d), nrOfDecimals: 0, overrideRowStyle: x => x.StyleOverride));
            }
            excelRequest.Sheets[0].SetColumnsAndData(rows, cols.ToArray());

            // Filters
            var filters = new List<Tuple<string, string>>();
            void AddFilter(string x, string y) => filters.Add(Tuple.Create(x, y));

            AddFilter("From month", request.FromInPeriodDate.Value.ToString("yyyy-MM"));
            AddFilter("To month", request.ToInPeriodDate.Value.ToString("yyyy-MM"));
            AddFilter("Period", periodType.ToString());
            AddFilter("Provider", string.IsNullOrWhiteSpace(request.ProviderName) ? "all" : request.ProviderName);
            AddFilter("Campaign", filterByCampaignParameter == null ? "all" : $"{filterByCampaignParameter.Value.ParameterName}={filterByCampaignParameter.Value.ParameterValue}");

            excelRequest.Sheets[1].SetColumnsAndData(filters,
                filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                filters.Col(x => x.Item2, ExcelType.Text, "Value"));

            // Details
            Func<bool?, int?> b = x => x.HasValue ? (x.Value ? 1 : 0) : new int?();
            excelRequest.Sheets[2].SetColumnsAndData(details,
                details.Col(x => x.ApplicationNr, ExcelType.Text, "ApplicationNr"),
                details.Col(x => x.ProviderName, ExcelType.Text, "ProviderName"),
                details.Col(x => x.PeriodMonthDate, ExcelType.Date, "PeriodMonthDate"),
                details.Col(x => x.PeriodQuarterDate, ExcelType.Date, "PeriodQuarterDate"),
                details.Col(x => x.PeriodYearDate, ExcelType.Date, "PeriodYearDate"),
                details.Col(x => b(x.WasChangedToQualifiedLead), ExcelType.Number, "WasChangedToQualifiedLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsOrWasLead), ExcelType.Number, "IsOrWasLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsLead), ExcelType.Number, "IsLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsActive), ExcelType.Number, "IsActive", nrOfDecimals: 0),
                details.Col(x => b(x.IsCurrentLead), ExcelType.Number, "IsCurrentLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsQualifiedLead), ExcelType.Number, "IsQualifiedLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsRejected), ExcelType.Number, "IsRejected", nrOfDecimals: 0),
                details.Col(x => b(x.IsCancelled), ExcelType.Number, "IsCancelled", nrOfDecimals: 0),
                details.Col(x => b(x.IsApplicationSigned), ExcelType.Number, "IsApplicationSigned", nrOfDecimals: 0),
                details.Col(x => b(x.IsAgreementApproved), ExcelType.Number, "IsAgreementApproved", nrOfDecimals: 0),
                details.Col(x => b(x.IsLastCategoryPaidOut), ExcelType.Number, "IsLastCategoryPaidOut", nrOfDecimals: 0),
                details.Col(x => x.InitialCapitalDebt, ExcelType.Number, "InitialCapitalDebt"),
                details.Col(x => b(x.IsLastCategoryQualifiedLead), ExcelType.Number, "IsLastCategoryQualifiedLead", nrOfDecimals: 0),
                details.Col(x => b(x.IsLastCategorySignedApplication), ExcelType.Number, "IsLastCategorySignedApplication  ", nrOfDecimals: 0),
                details.Col(x => b(x.IsLastCategoryAgreementSent), ExcelType.Number, "IsLastCategoryAgreementSent  ", nrOfDecimals: 0)
            );

            var client = new nDocumentClient();
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"Waterfall.xlsx");
        }

        private (List<DateTime> PeriodDates, List<ModelRow> Rows) ComputeAggregates(List<MortgageLoanWaterfallApplicationModel> models, Func<MortgageLoanWaterfallApplicationModel, DateTime> getAggregationDate)
        {
            var aggregatesByDate = models
                .GroupBy(getAggregationDate)
                .ToDictionary(x => x.Key, x => x.ToList());

            var rows = new List<ModelRow>();

            void AddRow(string desc, Func<List<MortgageLoanWaterfallApplicationModel>, decimal> getValue, DocumentClientExcelRequest.StyleData style = null)
            {
                rows.Add(new ModelRow { Description = desc, GetValue = x => getValue(aggregatesByDate[x]), StyleOverride = style });
            }

            decimal Fraction(int x, int y) => (y == 0 ? 0m : Math.Round((decimal)x / (decimal)y, 4));

            AddRow("Total applications", x => x.Count());
            AddRow("Total leads", x => x.Count(y => y.IsOrWasLead));
            AddRow("Current leads", x => x.Count(y => y.IsCurrentLead));
            AddRow("Rejected leads", x => x.Count(y => y.IsLead && y.IsRejected));
            AddRow("Cancelled leads", x => x.Count(y => y.IsLead && y.IsCancelled));
            AddRow("Total qualified applications", x => x.Count(y => y.IsQualifiedLead));
            AddRow("Current qualified applications", x => x.Count(y => y.IsLastCategoryQualifiedLead && y.IsActive));
            AddRow("Rejected qualified applications", x => x.Count(y => y.IsLastCategoryQualifiedLead && y.IsRejected));
            AddRow("Cancelled qualified applications", x => x.Count(y => y.IsLastCategoryQualifiedLead && y.IsCancelled));
            AddRow("Total signed applications", x => x.Count(y => y.IsApplicationSigned));
            AddRow("Current signed applications", x => x.Count(y => y.IsLastCategorySignedApplication && y.IsActive));
            AddRow("Rejected signed applications", x => x.Count(y => y.IsLastCategorySignedApplication && y.IsRejected));
            AddRow("Cancelled signed applications", x => x.Count(y => y.IsLastCategorySignedApplication && y.IsCancelled));
            AddRow("Total sent agreement", x => x.Count(y => y.IsAgreementApproved));
            AddRow("Current sent agreement", x => x.Count(y => y.IsLastCategoryAgreementSent && y.IsActive));
            AddRow("Rejected sent agreement", x => x.Count(y => y.IsLastCategoryAgreementSent && y.IsRejected));
            AddRow("Cancelled sent agreement", x => x.Count(y => y.IsLastCategoryAgreementSent && y.IsCancelled));
            AddRow("Total rejected", x => x.Count(y => y.IsRejected));
            AddRow("Total cancelled", x => x.Count(y => y.IsCancelled));
            AddRow("Paid out loans", x => x.Count(y => y.IsLastCategoryPaidOut));
            AddRow("Paid out loans amount", x => x.Sum(y => y.InitialCapitalDebt.GetValueOrDefault()));
            AddRow("Signed applications %", x => Fraction(x.Count(y => y.IsApplicationSigned), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Sent agreement %", x => Fraction(x.Count(y => y.IsAgreementApproved), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            //NOTE: IsApplicationSigned as divisor here only is intentional
            AddRow("Take up rate signed applications %", x => Fraction(x.Count(y => y.IsLastCategoryPaidOut), x.Count(y => y.IsApplicationSigned)), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });
            AddRow("Take up rate total applications %", x => Fraction(x.Count(y => y.IsLastCategoryPaidOut), x.Count()), style: new DocumentClientExcelRequest.Column
            {
                IsPercent = true,
                NrOfDecimals = 2
            });

            return (PeriodDates: aggregatesByDate.Keys.ToList(), rows);
        }

        private class ModelRow
        {
            public string Description { get; set; }
            public Func<DateTime, decimal> GetValue { get; set; }
            public DocumentClientExcelRequest.StyleData StyleOverride { get; set; }
        }

        private enum PeriodTypeCode
        {
            Monthly,
            Quarterly,
            Yearly
        }

        public class Request
        {
            [Required]
            public DateTime? FromInPeriodDate { get; set; }

            [Required]
            public DateTime? ToInPeriodDate { get; set; }

            public string PeriodType { get; set; }

            public string ProviderName { get; set; }

            public string CampaignParameterName { get; set; }

            public string CampaignParameterValue { get; set; }
        }
    }
}