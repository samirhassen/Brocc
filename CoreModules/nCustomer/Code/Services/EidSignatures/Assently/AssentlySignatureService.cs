using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using Serilog;
using System;
using System.Collections.Generic;
using static nCustomer.Services.EidSignatures.Assently.AssentlySignatureClientBase;

namespace nCustomer.Services.EidSignatures.Assently
{
    public class AssentlySignatureService : ProviderSignatureService
    {
        public AssentlySignatureService(SignatureSessionService sessionService) : base(sessionService)
        {

        }

        public override string ProviderName => SharedProviderName;

        public static string SharedProviderName => "assently";

        protected override ProviderSessionData CreateProviderSession(CommonElectronicIdSignatureSession session)
        {
            var client = new AssentlySignatureClient(NEnv.AssentlySignatureSettings);
            var callbackUri = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"api/signature/{session.Id}/signal-event");

            var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
            var (IsSuccess, ContentType, FileName, FileData) = documentClient.TryFetchRaw(session.UnsignedPdf.ArchiveKey);

            var providerCase = client.ToSync(() => client.GetCaseAsync(session.SigningCustomersBySignerNr, FileData, FileName, callbackUri));

            var result = new ProviderSessionData
            {
                ProviderSessionId = providerCase.CaseId, 
                SignerDataBySignerNr = new Dictionary<int, ProviderSignerData>()
            };

            foreach (var localSigner in session.SigningCustomersBySignerNr)
            {
                var status = providerCase.SignatureStatus[localSigner.Key];
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
                var client = new AssentlySignatureClient(NEnv.AssentlySignatureSettings);
                client.ToSync(() => client.CancelDocumentAsync(localSession.ProviderSessionId));  

                UpdateSessionFromProvider(localSession, client);
            }
            catch (Exception ex)
            {
                NLog.Error($"Failed to close local session from Assently for session provider Id = {localSession.ProviderSessionId}, Local Id = {localSession.Id}", ex);
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

        protected void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, AssentlySignatureClient client)
        {
            var status = client.ToSync(() => client.GetSignatureStatusBySignerNrAsync(localSession.ProviderSessionId, null));
            bool allPartiesSigned = true;

            foreach (var localSigner in localSession.SigningCustomersBySignerNr)
            {
                if (status[localSigner.Key].HasSigned)
                    UpdateSessionOnCustomerSigned(localSession, localSigner.Key, SessionService.Clock.Now.DateTime.ToUniversalTime());

                if (!status[localSigner.Key].HasSigned)
                    allPartiesSigned = false;
            }

            if (localSession.SignedPdf == null && allPartiesSigned)
            {
                var (FileName, FileData) = client.ToSync(() => client.GetSignedDocumentAsync(localSession.ProviderSessionId));

                if (FileData == null)
                {
                    throw new AssentlySignatureClientException("Could not fetch signed document from Assently."); 
                }

                var documentClient = LegacyServiceClientFactory.CreateDocumentClient(LegacyHttpServiceHttpContextUser.SharedInstance, NEnv.ServiceRegistry);
                var archiveKey = documentClient.ArchiveStore(FileData, "application/pdf", FileName);
                var pdf = new CommonElectronicIdSignatureSession.PdfModel
                {
                    FileName = FileName,
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
                var client = new AssentlySignatureClient(NEnv.AssentlySignatureSettings);
                UpdateSessionFromProvider(localSession, client); 
            }
            catch (Exception ex)
            {
                NLog.Error($"Failed to get data from Assently for session {localSession.ProviderSessionId}", ex);
            }
        }
    }
}