using nCredit.Code;
using nCredit.Excel;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class WaterfallReportMethod : FileStreamWebserviceMethod<WaterfallReportMethod.Request>
    {
        public override string Path => "Reports/GetApplicationWaterfall";

        public override bool IsEnabled => NEnv.ServiceRegistry.ContainsService("nPreCredit") && !NEnv.IsStandardUnsecuredLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var c = new DataWarehouseClient();

            ValidateRequestAndSetupDefaults(request, c);

            var p = new ExpandoObject();
            p.SetValues(d =>
            {
                d["fromMonthDate"] = request.FromMonthDate.Value;
                d["toMonthDate"] = request.ToMonthDate.Value;
                d["providerName"] = request.ProviderName;
                d["campaignCode"] = request.CampaignCode;
                d["scoreGroup"] = request.ScoreGroup;
            });

            var items = c.FetchReportData<ApplicationWaterfallMonth>("WaterfallReportData1", p);

            var groupType = request.GroupPeriod?.ToLowerInvariant() == "yearly"
                ? GroupType.Year
                : (request.GroupPeriod?.ToLowerInvariant() == "quarterly"
                    ? GroupType.Quarter
                    : GroupType.Month);

            Dictionary<DateTime, ApplicationWaterfallMonth> actualItems;

            Func<DateTime, IEnumerable<ApplicationWaterfallMonth>, ApplicationWaterfallMonth> combineMonths = (d, ii) =>
                {
                    return new ApplicationWaterfallMonth
                    {
                        MonthDate = d,
                        AcceptedAmount = ii.Sum(x => x.AcceptedAmount),
                        AcceptedCount = ii.Sum(x => x.AcceptedCount),
                        ApplicationsAmount = ii.Sum(x => x.ApplicationsAmount),
                        ApplicationsCount = ii.Sum(x => x.ApplicationsCount),
                        CancelledAmount = ii.Sum(x => x.CancelledAmount),
                        CancelledCount = ii.Sum(x => x.CancelledCount),
                        PaidOutAmount = ii.Sum(x => x.PaidOutAmount),
                        PaidOutCount = ii.Sum(x => x.PaidOutCount),
                        RejectedAmount = ii.Sum(x => x.RejectedAmount),
                        RejectedCount = ii.Sum(x => x.RejectedCount)
                    };
                };

            if (groupType == GroupType.Month)
                actualItems = items.ToDictionary(x => x.MonthDate, x => x);
            else if (groupType == GroupType.Quarter)
                actualItems = items
                    .GroupBy(x => new { x.MonthDate.Year, Quarter = MonthToQuarterMapping[x.MonthDate.Month] })
                    .ToDictionary(x => QuarterToDate(x.Key.Year, x.Key.Quarter), x => combineMonths(QuarterToDate(x.Key.Year, x.Key.Quarter), x));
            else if (groupType == GroupType.Year)
                actualItems = items
                    .GroupBy(x => x.MonthDate.Year)
                    .ToDictionary(x => new DateTime(x.Key, 1, 1), x => combineMonths(new DateTime(x.Key, 1, 1), x));
            else
                throw new NotImplementedException();

            Func<DateTime, string> getCategoryName = d =>
            {
                switch (groupType)
                {
                    case GroupType.Year:
                        return d.Year.ToString();
                    case GroupType.Month:
                        return $"{d.Year}-{d.Month}";
                    case GroupType.Quarter:
                        return $"{d.Year} Q{MonthToQuarterMapping[d.Month]}";
                    default:
                        throw new NotImplementedException();
                }
            };

            Action<bool, DocumentClientExcelRequest.Sheet> setupSheet = (isCount, sheet) =>
               {
                   var rows = new List<ModelRow>();
                   rows.Add(new ModelRow
                   {
                       Description = "Applications",
                       GetValue = x => isCount ? actualItems[x].ApplicationsCount : actualItems[x].ApplicationsAmount
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Rejected",
                       GetValue = x => isCount ? actualItems[x].RejectedCount : actualItems[x].RejectedAmount
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Cancelled",
                       GetValue = x => isCount ? actualItems[x].CancelledCount : actualItems[x].CancelledAmount
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Accepted",
                       GetValue = x => isCount ? actualItems[x].AcceptedCount : actualItems[x].AcceptedAmount
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Paid out",
                       GetValue = x => isCount ? actualItems[x].PaidOutCount : actualItems[x].PaidOutAmount
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Accepted %",
                       GetValue = x =>
                        isCount
                        ? (actualItems[x].ApplicationsCount == 0 ? 0m : Math.Round((decimal)actualItems[x].AcceptedCount / (decimal)actualItems[x].ApplicationsCount, 4))
                        : (actualItems[x].ApplicationsAmount == 0 ? 0m : Math.Round((decimal)actualItems[x].AcceptedAmount / (decimal)actualItems[x].ApplicationsAmount, 4)),
                       StyleOverride = new DocumentClientExcelRequest.StyleData
                       {
                           IsPercent = true,
                           NrOfDecimals = 2
                       }
                   });
                   rows.Add(new ModelRow
                   {
                       Description = "Converted %",
                       GetValue = x =>
                        isCount
                        ? (actualItems[x].AcceptedCount == 0 ? 0m : Math.Round((decimal)actualItems[x].PaidOutCount / (decimal)actualItems[x].AcceptedCount, 4))
                        : (actualItems[x].AcceptedAmount == 0 ? 0m : Math.Round((decimal)actualItems[x].PaidOutAmount / (decimal)actualItems[x].AcceptedAmount, 4)),
                       StyleOverride = new DocumentClientExcelRequest.StyleData
                       {
                           IsPercent = true,
                           NrOfDecimals = 2
                       }
                   });

                   var cols = Excel.DocumentClientExcelRequest.CreateDynamicColumnList(rows);
                   cols.Add(rows.Col(x => x.Description, ExcelType.Text, "Category"));
                   foreach (var d in actualItems.Keys.OrderBy(x => x))
                   {
                       var localD = d;
                       cols.Add(rows.Col(x => x.GetValue(localD), ExcelType.Number, getCategoryName(d), nrOfDecimals: 0, overrideRowStyle: x => x.StyleOverride));
                   }
                   sheet.SetColumnsAndData(rows, cols.ToArray());
               };


            var excelRequest = new DocumentClientExcelRequest
            {
                Sheets = new DocumentClientExcelRequest.Sheet[]
                {
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Count"
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Balance"
                        },
                        new DocumentClientExcelRequest.Sheet
                        {
                            AutoSizeColumns = true,
                            Title = "Filter"
                        }
                }
            };
            setupSheet(true, excelRequest.Sheets[0]);
            setupSheet(false, excelRequest.Sheets[1]);

            var filters = new List<Tuple<string, string>>();
            Action<string, string> f = (a, b) => filters.Add(Tuple.Create(a, b));
            f("From month", request.FromMonthDate.Value.ToString("yyyy-MM"));
            f("To month", request.ToMonthDate.Value.ToString("yyyy-MM"));
            f("Period", groupType.ToString());
            f("Provider", ProviderDisplayNames.GetProviderDisplayName(request.ProviderName));
            f("Risk group", request.ScoreGroup);
            f("Campaign code", request.CampaignCode);

            excelRequest.Sheets[2].SetColumnsAndData(filters,
                filters.Col(x => x.Item1, ExcelType.Text, "Name"),
                filters.Col(x => x.Item2, ExcelType.Text, "Value"));

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return File(result, downloadFileName: $"Application-Waterfall-{request.FromMonthDate.Value.ToString("yyyyMM")}-{request.FromMonthDate.Value.ToString("yyyyMM")}.xlsx");
        }

        private void ValidateRequestAndSetupDefaults(Request request, DataWarehouseClient c)
        {
            Func<DateTime, DateTime> firstOfMonth = d => new DateTime(d.Year, d.Month, 1);

            if (!request.FromMonthDate.HasValue || !request.ToMonthDate.HasValue)
            {
                var stats = c.FetchReportData<ApplicationStats>("applicationStats1", new ExpandoObject()).Single();
                request.FromMonthDate = request.FromMonthDate ?? stats?.MinApplicationDate;
                request.ToMonthDate = request.ToMonthDate ?? stats?.MaxApplicationDate;
            }

            request.ProviderName = string.IsNullOrWhiteSpace(request.ProviderName) ? "all" : request.ProviderName.Trim();
            request.ScoreGroup = string.IsNullOrWhiteSpace(request.ScoreGroup) ? "all" : request.ScoreGroup.Trim();
            request.CampaignCode = string.IsNullOrWhiteSpace(request.CampaignCode) ? "all" : request.CampaignCode.Trim();

            Validate(request, x =>
            {
                x.Require(r => r.FromMonthDate);
                x.Require(r => r.ToMonthDate);
            });
        }

        public class Request
        {
            public DateTime? FromMonthDate { get; set; }
            public DateTime? ToMonthDate { get; set; }
            public string GroupPeriod { get; set; }
            public string ProviderName { get; set; }
            public string CampaignCode { get; set; }
            public string ScoreGroup { get; set; }
        }

        private class ApplicationWaterfallMonth
        {
            public DateTime MonthDate { get; set; }
            public int ApplicationsCount { get; set; }
            public int AcceptedCount { get; set; }
            public int RejectedCount { get; set; }
            public int CancelledCount { get; set; }
            public int PaidOutCount { get; set; }
            public decimal ApplicationsAmount { get; set; }
            public decimal AcceptedAmount { get; set; }
            public decimal RejectedAmount { get; set; }
            public decimal CancelledAmount { get; set; }
            public decimal PaidOutAmount { get; set; }
        }

        private class ApplicationStats
        {
            public DateTime? MinApplicationDate { get; set; }
            public DateTime? MaxApplicationDate { get; set; }
            public int ApplicationCount { get; set; }
        }

        private class ModelRow
        {
            public string Description { get; set; }
            public Func<DateTime, decimal> GetValue { get; set; }
            public Func<DateTime, bool> IsYear { get; set; }
            public DocumentClientExcelRequest.StyleData StyleOverride { get; set; }
        }

        private enum GroupType { Month, Quarter, Year }
        public static Dictionary<int, int> MonthToQuarterMapping = new Dictionary<int, int>
        {
            {1, 1},{2, 1},{3, 1},
            {4, 2},{5, 2},{6, 2},
            {7, 3},{8, 3},{9, 3},
            {10, 4},{11, 4},{12, 4},
        };
        private DateTime QuarterToDate(int year, int quarter)
        {
            var firstMonthInQuarter = MonthToQuarterMapping.Where(x => x.Value == quarter).Min(x => x.Key);
            return new DateTime(year, firstMonthInQuarter, 1);
        }
    }
}