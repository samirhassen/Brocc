using nCustomer.Code;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Services.EidSignatures.Scrive
{
    public class ScriveSignatureService : ProviderSignatureService
    {
        public ScriveSignatureService(SignatureSessionService sessionService) : base(sessionService)
        {

        }

        public override string ProviderName => SharedProviderName;

        public static string SharedProviderName => "scrive";

        protected override ProviderSessionData CreateProviderSession(CommonElectronicIdSignatureSession session)
        {
            var client = new ScriveSignatureClient(NEnv.ScriveSignatureSettings);

            var documentBytes = new DocumentClient().FetchRawWithFilename(session.UnsignedPdf.ArchiveKey, out var _, out var filename);
            var document = client.NewDocument(documentBytes, filename);

            document.SetTitle($"{filename} {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}");
            document.SetLanguage("sv");

            document.ChangeAuthorFromSignatoryToViewer();
            foreach (var localSigner in session.SigningCustomersBySignerNr.OrderBy(x => x.Key))
            {
                var scriveSignerIndex = document.AddSignatory(
                    NEnv.BaseCivicRegNumberParser.Parse(localSigner.Value.CivicRegNr),
                    localSigner.Value.FirstName ?? "Signer",
                    localSigner.Value.LastName ?? localSigner.Key.ToString(),
                    NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"signature-result/{session.Id}/success"),
                    NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"signature-result/{session.Id}/failure"));
                localSigner.Value.SetCustomData("scriveSignerIndex", scriveSignerIndex.ToString());
            }

            document.SetApiStatusChangeCallbackUrl(NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"api/signature/{session.Id}/signal-event"));
            document = client.UpdateDocument(document);
            document = client.StartSignatureProcess(document.GetId());

            var result = new ProviderSessionData
            {
                ProviderSessionId = document.GetId(),
                SignerDataBySignerNr = new Dictionary<int, ProviderSignerData>()
            };
            foreach (var localSigner in session.SigningCustomersBySignerNr)
            {
                var status = document.GetSignatureStatusForLocalCustomer(client.ApiEndpoint, localSigner.Value);
                result.SignerDataBySignerNr[localSigner.Key] = new ProviderSignerData
                {
                    SignatureUrlBySignerNr = status.SignatureUrl
                };
            }

            return result;
        }

        protected override CommonElectronicIdSignatureSession FindSessionByProviderEventData(Dictionary<string, string> providerEventData, ICustomerContextExtended customersContext)
        {
            var localSessionId = providerEventData?.Opt("localSessionId");
            if (localSessionId == null)
                return null;
            return SessionService.GetSession(localSessionId, customersContext);
        }

        protected override void CloseProviderSession(CommonElectronicIdSignatureSession localSession)
        {
            if (localSession.ClosedDate.HasValue)
                return;

            try
            {
                var client = new ScriveSignatureClient(NEnv.ScriveSignatureSettings);
                var document = client.GetDocument(localSession.ProviderSessionId);

                if (document.IsActive())
                {
                    document = client.CancelDocument(localSession.ProviderSessionId);
                    UpdateSessionFromProvider(localSession, document, client);
                }
            }
            catch (Exception ex)
            {
                NLog.Error($"Failed to close local session from scrive for session provider Id = {localSession.ProviderSessionId}, Local Id = {localSession.Id}", ex);
            }
            finally
            {
                if (!localSession.ClosedDate.HasValue)
                {
                    localSession.ClosedDate = SessionService.Clock.Now.DateTime;
                    localSession.ClosedMessage = "Forced closed locally";
                }
            }
        }

        protected void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, EditableScriveDocument document, ScriveSignatureClient client)
        {
            foreach (var localSigner in localSession.SigningCustomersBySignerNr)
            {
                var status = document.GetSignatureStatusForLocalCustomer(client.ApiEndpoint, localSigner.Value);
                if (status.HasSigned)
                    UpdateSessionOnCustomerSigned(localSession, localSigner.Key, SessionService.Clock.Now.DateTime.ToUniversalTime());
            }

            if (localSession.SignedPdf == null && document.IsSignedByAll())
            {
                var pdfData = client.DownloadSignedPdf(localSession.ProviderSessionId);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(localSession.UnsignedPdf.FileName) + "_signed.pdf";
                var archiveKey = new DocumentClient().ArchiveStore(pdfData, "application/pdf", fileName);
                var pdf = new CommonElectronicIdSignatureSession.PdfModel
                {
                    FileName = fileName,
                    ArchiveKey = archiveKey
                };
                UpdateSessionOnSignedDocumentReceived(localSession, pdf);
            }
        }

        protected override void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, Dictionary<string, string> providerEventData)
        {
            if (localSession.ClosedDate.HasValue)
                return;

            try
            {
                var client = new ScriveSignatureClient(NEnv.ScriveSignatureSettings);
                var document = client.GetDocument(localSession.ProviderSessionId);
                UpdateSessionFromProvider(localSession, document, client);
            }
            catch (Exception ex)
            {
                NLog.Error($"Failed to get data from scrive for session {localSession.ProviderSessionId}", ex);
            }
        }
    }
}