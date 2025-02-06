using nCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nCredit.WebserviceMethods.Reports
{
    public class SwedishQuarterlyF818ReportMethod : FileStreamWebserviceMethod<SwedishQuarterlyF818ReportMethod.Request>
    {
        public override string Path => "Reports/GetSwedishQuarterlyF818";

        public override bool IsEnabled => IsReportEnabled;
        public static bool IsReportEnabled => NEnv.IsStandardUnsecuredLoansEnabled && NEnv.IsStandardUnsecuredLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "SE";
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Service();
            var service = new SwedishQuarterlyF818ReportService(
                resolver.DocumentClientHttpContext, resolver.ContextFactory);

            var result = service.CreateReport(Quarter.ContainingDate(request.QuarterEndDate.Value));

            return ExcelFile(result.ReportData, downloadFileName: result.DownloadFilename);
        }

        public class Request
        {
            [Required]
            public DateTime? QuarterEndDate { get; set; }
        }
    }

}
