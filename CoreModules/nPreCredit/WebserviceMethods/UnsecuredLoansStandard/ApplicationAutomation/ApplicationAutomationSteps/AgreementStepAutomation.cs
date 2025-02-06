using nPreCredit.Code;
using nPreCredit.Code.Clients;
using nPreCredit.Code.Services;
using Serilog;
using System;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation
{
    public static class AgreementStepAutomation
    {
        internal static bool TryCreateAndSendAgreement(ApplicationInfoModel applicationInfoModel, UnsecuredLoanStandardAgreementService agreementService, nPreCreditClient client)
        {
            try
            {
                var documentClient = new nDocumentClient();
                var disableTemplateCache = NEnv.IsTemplateCacheDisabled;

                var printContext = agreementService.GetPrintContext(applicationInfoModel);
                var fileBytes = agreementService.CreateAgreementPdf(printContext, overrideTemplateName: null, disableTemplateCache).ToArray();
                var unsignedAgreementPdfArchiveKey = documentClient.ArchiveStore(fileBytes, "application/pdf", $"credit-agreement-{applicationInfoModel.ApplicationNr}.pdf");

                client.UnsecuredLoanStandardCreateAgreementSignatureSession(applicationInfoModel.ApplicationNr, unsignedAgreementPdfArchiveKey, isAutomatic: true);

                return true;
            }
            catch (Exception ex)
            {
                NLog.Error(ex, "Error when trying to create and send agreement.");
                return false;
            }
        }
    }
}