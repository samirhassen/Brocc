using nCustomer.Code;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nCustomer.Services.EidSignatures
{
    public abstract class ProviderSignatureService
    {
        private readonly SignatureSessionService sessionService;

        public abstract string ProviderName { get; }

        public ProviderSignatureService(SignatureSessionService sessionService)
        {
            this.sessionService = sessionService;
        }

        protected abstract ProviderSessionData CreateProviderSession(CommonElectronicIdSignatureSession session);

        public SignatureSessionService SessionService => sessionService;

        protected class ProviderSessionData
        {
            public string ProviderSessionId { get; set; }
            public Dictionary<string, string> SessionCustomData { get; set; }
            public Dictionary<int, ProviderSignerData> SignerDataBySignerNr { get; set; }
            public List<Tuple<string, string>> SessionAlternateKeys { get; set; }
        }

        protected class ProviderSignerData
        {
            public Uri SignatureUrlBySignerNr { get; set; }
            public Dictionary<string, string> CustomData { get; set; }
        }

        /// <summary>
        /// Synchronize the session with the provider
        /// </summary>
        /// <param name="closeFirst">If you for instance are just trying to get rid of the local session but want to know the current state then set this to true to avoid synchronizing with the provider first.</param>
        /// <returns></returns>
        public CommonElectronicIdSignatureSession SynchronizeSessionWithProvider(string sessionIdOrAlternateKey, bool closeFirst, string alternateKeyName = null, bool supressCallback = false)
        {
            using (var context = sessionService.ContextFactory.CreateContext())
            {
                var sessionId = string.IsNullOrWhiteSpace(alternateKeyName)
                    ? sessionIdOrAlternateKey
                    : SessionService.GetSessionIdByAlternateKey(alternateKeyName, sessionIdOrAlternateKey);
                if (sessionId == null)
                    return null;

                var localSession = SessionService.GetSession(sessionId, context);

                if (localSession == null)
                    return null;

                var wasOpenAndUnsigned = !localSession.ClosedDate.HasValue && localSession.SignedPdf == null;

                if (closeFirst)
                    CloseProviderSession(localSession);
                else
                    UpdateSessionFromProvider(localSession, null);

                SessionService.StoreSession(localSession, context);

                context.SaveChanges();

                if (!supressCallback && wasOpenAndUnsigned && localSession.SignedPdf != null)
                    SendSuccessPostback(localSession);

                return localSession;
            }
        }

        public CommonElectronicIdSignatureSession CreateNewSession(SingleDocumentSignatureRequest request)
        {
            var localSession = SignatureSessionService.CreateLocalSessionFromRequest(request.ToUnvalidated(), ProviderName, NEnv.ClientCfgCore);

            var providerSession = CreateProviderSession(localSession);

            localSession.ProviderSessionId = providerSession.ProviderSessionId;

            foreach (var kvp in providerSession.SignerDataBySignerNr)
            {
                var signer = localSession.SigningCustomersBySignerNr[kvp.Key];
                signer.SignatureUrl = kvp.Value.SignatureUrlBySignerNr.ToString();
                if (kvp.Value.CustomData != null)
                    foreach (var kvp2 in kvp.Value.CustomData)
                        signer.SetCustomData(kvp2.Key, kvp2.Value);
            }

            if (providerSession.SessionCustomData != null)
                foreach (var kvp in providerSession.SessionCustomData)
                    localSession.SetCustomData(kvp.Key, kvp.Value);

            using (var context = sessionService.ContextFactory.CreateContext())
            {
                sessionService.StoreSession(localSession, context);
                foreach (var alternateKey in providerSession.SessionAlternateKeys ?? Enumerable.Empty<Tuple<string, string>>())
                {
                    sessionService.SetAlternateKey(alternateKey.Item1, alternateKey.Item2, localSession.Id, context);
                }

                context.SaveChanges();

                return localSession;
            }
        }

        protected abstract CommonElectronicIdSignatureSession FindSessionByProviderEventData(Dictionary<string, string> providerEventData, ICustomerContextExtended customersContext);
        protected abstract void CloseProviderSession(CommonElectronicIdSignatureSession localSession);
        protected abstract void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, Dictionary<string, string> providerEventData);

        protected void UpdateSessionOnCustomerSigned(CommonElectronicIdSignatureSession session, int signerNr, DateTime signedDate)
        {
            var customer = session.SigningCustomersBySignerNr[signerNr];
            if (customer.SignedDateUtc.HasValue)
                return;

            customer.SignedDateUtc = signedDate.ToUniversalTime();
        }

        protected void UpdateSessionOnSignedDocumentReceived(CommonElectronicIdSignatureSession session, CommonElectronicIdSignatureSession.PdfModel signedDocument)
        {
            if (session.SignedPdf != null)
                return;

            foreach (var customer in session.SigningCustomersBySignerNr.Values.Where(x => !x.SignedDateUtc.HasValue))
            {
                customer.SignedDateUtc = sessionService.Clock.Now.DateTime.ToUniversalTime();
            }

            session.SignedPdf = signedDocument;
            session.ClosedDate = sessionService.Clock.Now.DateTime;
            var nowUtc = DateTime.UtcNow;
        }

        public CommonElectronicIdSignatureSession HandleSignatureEvent(Dictionary<string, string> providerEventData)
        {
            using (var context = sessionService.ContextFactory.CreateContext())
            {
                var localSession = FindSessionByProviderEventData(providerEventData, context);
                if (localSession == null)
                    return null;

                if (localSession.ClosedDate.HasValue || localSession.SignedPdf != null)
                    return localSession;

                UpdateSessionFromProvider(localSession, providerEventData);

                SessionService.StoreSession(localSession, context);

                context.SaveChanges();

                if (localSession.SignedPdf != null)
                    SendSuccessPostback(localSession);

                return localSession;
            }
        }

        /// <summary>
        /// Make sure to save changes before doing this since the recipient may look up the session
        /// </summary>
        protected void SendSuccessPostback(CommonElectronicIdSignatureSession session)
        {
            if (session.ServerToServerCallbackUrl != null)
            {
                Postback(session.ServerToServerCallbackUrl, new
                {
                    sessionId = session.Id,
                    providerName = ProviderName,
                    eventName = "Success",
                });
            }
        }

        private bool Postback<T>(string url, T data)
        {
            var result = NHttp
                .Begin(new Uri(url), null, timeout: TimeSpan.FromMinutes(1))
                .PostJson("", data);
            return result.IsSuccessStatusCode;
        }

        protected T RunSync<T>(Task<T> asyncCall) => Task.Run(() => asyncCall).GetAwaiter().GetResult();
    }
}