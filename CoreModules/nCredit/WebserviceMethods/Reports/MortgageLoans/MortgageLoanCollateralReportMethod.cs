using NTech.Core.Credit.Shared.Services.SwedishMortgageLoans;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanCollateralMethod : FileStreamWebserviceMethod<SwedishMortgageLoanCollateralReportRequest>
    {
        public override string Path => "Reports/MortgageLoanCollateral";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled && NEnv.ClientCfgCore.Country.BaseCountry == "SE";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, SwedishMortgageLoanCollateralReportRequest request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Service();
            var reportService = new SwedishMortgageLoanCollateralReportService(resolver.ContextFactory);
            var reportRequest = reportService.CreateReportRequest(request);
            var result = resolver.DocumentClientHttpContext.CreateXlsx(reportRequest);

            return ExcelFile(result, downloadFileName: $"Collateral-{request.Date.Value.ToString("yyyy-MM-dd")}.xlsx");
        }
    }
}