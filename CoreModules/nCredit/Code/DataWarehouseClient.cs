using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace nCredit.Code
{
    public class DataWarehouseClient : AbstractServiceClient
    {
        protected override string ServiceName => "nDataWarehouse";

        public void MergeDimension<T>(string dimensionName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeDimension", new { dimensionName = dimensionName, values = values })
                .EnsureSuccessStatusCode();
        }

        public void MergeFact<T>(string factName, List<T> values)
        {
            Begin()
                .PostJson("Api/MergeFact", new { factName = factName, values = values })
                .EnsureSuccessStatusCode();
        }

        public Stream CreateReport(string reportName, ExpandoObject parameters, TimeSpan? callTimeout = null)
        {
            var ms = new MemoryStream();
            Begin(timeout: callTimeout)
                .PostJson("Api/CreateReport", new { reportName, parameters })
                .CopyToStream(ms);
            ms.Position = 0;
            return ms;
        }

        public List<T> FetchReportData<T>(string reportName, ExpandoObject parameters)
        {
            return Begin()
                .PostJson("Api/FetchReportData", new { reportName, parameters })
                .ParseJsonAs<List<T>>();
        }

        public class ScheduledExcelExportedReportsResult
        {
            public List<Report> Reports { get; set; }
            public class Report
            {
                public string ReportName { get; set; }
                public string FriendlyName { get; set; }
                public DateTime? LatestReportDate { get; set; }
                public string LatestReportViewUrl { get; set; }
                public string AllReportsViewUrl { get; set; }
            }
        }

        public ScheduledExcelExportedReportsResult FetchScheduledExcelExportedReportsForCurrentUser(NTechNavigationTarget backTarget)
        {
            return Begin()
                .PostJson("Api/ScheduledExcelExports/FetchAvailableReportsForCurrentUser", new { backTarget = backTarget.GetBackTargetOrNull() })
                .ParseJsonAs<ScheduledExcelExportedReportsResult>();
        }
    }
}