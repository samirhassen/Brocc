using nCredit.Code;
using NTech.Core.Credit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods.Reports
{
    public class ExtraAmortizationsReportMethod : FileStreamWebserviceMethod<ExtraAmortizationsReportRequest>
    {
        public override string Path => "Reports/Extra-Amortizations";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, ExtraAmortizationsReportRequest request)
        {
            ValidateUsingAnnotations(request);

            var service = new ExtraAmortizationsReportService(requestContext.Service().ContextFactory);

            var excelRequest = service.CreateReportExcelRequest(request);

            var client = requestContext.Service().DocumentClientHttpContext;
            var result = client.CreateXlsx(excelRequest);

            return ExcelFile(result, downloadFileName: $"Extra-Amortizations-{request.Date1.Value.ToString("yyyy-MM-dd")}-{request.Date2.Value.ToString("yyyy-MM-dd")}.xlsx");
        }
    }
}