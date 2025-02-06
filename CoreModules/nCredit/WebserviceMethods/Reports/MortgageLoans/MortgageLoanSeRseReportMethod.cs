using NTech.Core.Credit.Shared.Services;
using NTech.Legacy.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;

namespace nCredit.WebserviceMethods.Reports
{
    public class MortgageLoanSeRseReportMethod : ReportWebserviceMethod<RseForCreditRequest>
    {
        public override string ReportName => "MortgageLoanSeRse";

        public override bool IsEnabled => NEnv.IsMortgageLoansEnabled && NEnv.ClientCfg.Country.BaseCountry == "SE";

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, RseForCreditRequest request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Service();
            var s = new SwedishMortgageLoanRseService(resolver.ContextFactory, NEnv.NotificationProcessSettings, CoreClock.SharedInstance, NEnv.ClientCfgCore, NEnv.EnvSettings);
            var excelRequest = s.CreateRseReportForCredit(request);

            var documentClient = resolver.DocumentClientHttpContext;
            var report = documentClient.CreateXlsx(excelRequest);

            return ExcelFile(report, downloadFileName: $"RSE-{request.CreditNr}.xlsx");
        }
    }
}