using nCustomer.WebserviceMethods.CustomerContactInfo;
using NTech.Core;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure.NTechWs;

namespace nCustomer.WebserviceMethods
{
    public class GetKycUpdateReportMethod : FileStreamWebserviceMethod<Request>
    {
        public override string Path => "Kyc-Reminders/Report";
        public override bool IsEnabled => NEnv.ClientCfgCore.IsFeatureEnabled("feature.customerpages.kyc");

        protected override ActionResult.FileStream DoExecuteFileStream(NTechWebserviceMethodRequestContext requestContext, CustomerContactInfo.Request request)
        {
            var resolver = requestContext.Service();
            var reportRequest = resolver.KycQuestionsUpdate.CreateCustomerKycStatusReport();
            var customerClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceSystemUser.SharedInstance, NEnv.ServiceRegistry);
            var reportStream = customerClient.CreateXlsx(reportRequest);
            ICoreClock clock = requestContext.Clock();
            return ExcelFile(
                reportStream,
                downloadFileName: $"KycReminderStatus-{clock.Now.ToString("yyyy-MM-dd")}.xlsx");
        }

        public class Request
        {

        }
    }
}