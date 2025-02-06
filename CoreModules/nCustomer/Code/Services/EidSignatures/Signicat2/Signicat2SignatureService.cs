using nCustomer.Code;
using nCustomer.Code.Services.EidSignatures.Signicat2;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.ElectronicSignatures;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nCustomer.Services.EidSignatures.Signicat2
{
    public class Signicat2SignatureService : ProviderSignatureService
    {
        public Signicat2SignatureService(SignatureSessionService sessionService) : base(sessionService) { }

        public override string ProviderName => SharedProviderName;

        public static string SharedProviderName => "signicat2";

        protected override ProviderSessionData CreateProviderSession(CommonElectronicIdSignatureSession session)
        {
            var client = new Signicat2SignatureClient(NEnv.Signicat2SignatureSettings);

            var documentBytes = new DocumentClient().FetchRawWithFilename(session.UnsignedPdf.ArchiveKey, out var _, out var filename);
            var token = client.ToSync(() => client.GetTokenAsync());
            var documentId = client.ToSync(() => client.SendDocumentAsync(token, documentBytes));

            var caseResult = client.ToSync(() => client.CreateCaseAsync(token, session.Id, documentId, session.RedirectAfterSuccessUrl, session.RedirectAfterFailedUrl, session.SigningCustomersBySignerNr));

            var result = new ProviderSessionData
            {
                ProviderSessionId = caseResult.id,
                SignerDataBySignerNr = new Dictionary<int, ProviderSignerData>()
            };

            foreach (var localSigner in session.SigningCustomersBySignerNr)
            {
                var task = caseResult.tasks[localSigner.Key - 1];
                result.SignerDataBySignerNr[localSigner.Key] = new ProviderSignerData
                {
                    SignatureUrlBySignerNr = new Uri(task.signingUrl)
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
           //We don't do anything with external provider session
           //As it is deleted after 7 days anyway
        }

        private async Task<bool> UpdateSessionFromProviderOrRetry(Signicat2SignatureClient client, string signicatAccessToken, CommonElectronicIdSignatureSession localSession, 
            Dictionary<string, string> providerEventData, bool isLastTry)
        {
            var state = await client.GetOrderState(signicatAccessToken, localSession.ProviderSessionId, localSession.SigningCustomersBySignerNr.Keys.ToList());

            var status = state.Status;

            ICoreClock clock = CoreClock.SharedInstance;

            if (status == OrderStateCode.CompletedSuccessfully)
            {
                if (localSession.SignedPdf == null)
                {
                    var pdf = await client.DownloadPackagingTaskResultDocumentAsync(signicatAccessToken, localSession.ProviderSessionId, Signicat2SignatureClient.PackageTaskName);
                    var fileName = "signed_agreement.pdf";
                    var archiveKey = new DocumentClient().ArchiveStore(pdf, "application/pdf", fileName);

                    localSession.SignedPdf = new CommonElectronicIdSignatureSession.PdfModel
                    {
                        ArchiveKey = archiveKey,
                        FileName = fileName
                    };
                }
            }

            else if (status == OrderStateCode.Failed)
            {
                localSession.ClosedDate = clock.Now.DateTime;
                localSession.ClosedMessage = OrderStateCode.Failed.ToString();
            }
            else if (status == OrderStateCode.WaitingForSignatures)
            {
                //Do nothing
            }

            else if (status == OrderStateCode.WaitingForPackaging)
            {
                if (isLastTry)
                {
                    localSession.ClosedDate = clock.Now.DateTime;
                    localSession.ClosedMessage = "WaitingForPackaging: Gave up after retries";
                }
                else
                    return true; //Retry
            }

            foreach (var signedByNr in state.SignedByNrs)
            {
                UpdateSessionOnCustomerSigned(localSession, signedByNr, clock.Now.DateTime);
            }

            return false;
        }

        protected override void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, Dictionary<string, string> providerEventData)
        {
            var settings = NEnv.Signicat2SignatureSettings;
            var client = new Signicat2SignatureClient(settings);

            client.ToSync<object>(async () =>
            {
                var token = await client.GetTokenAsync();

                await Signicat2SignaturePackagingErrorHandler.RetryOnPackagingError(
                    isLastTry => UpdateSessionFromProviderOrRetry(client, token, localSession, providerEventData, isLastTry), settings, localSession.Id);

                return null;
            });
        }
    }

    public enum OrderStateCode
    {
        WaitingForSignatures,
        WaitingForPackaging,
        CompletedSuccessfully,
        Failed
    }
}