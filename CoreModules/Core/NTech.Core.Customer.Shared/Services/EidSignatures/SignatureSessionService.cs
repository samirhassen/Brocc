using nCustomer.Code;
using nCustomer.Code.Services;
using Newtonsoft.Json;
using NTech.Banking.CivicRegNumbers;
using NTech.Core;
using NTech.Core.Customer.Shared.Database;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using NTech.ElectronicSignatures;
using System;
using System.Linq;

namespace nCustomer.Services.EidSignatures
{
    public class SignatureSessionService
    {
        public const string SessionKeySpace = "SignatureSessionServiceV1";
        public const string AlternateKeyKeySpace = "AlternateSignatureSessionKeyV";


        public SignatureSessionService(ICoreClock clock, CustomerContextFactory contextFactory)
        {
            this.clock = clock;
            this.contextFactory = contextFactory;
        }

        private readonly ICoreClock clock;
        private readonly CustomerContextFactory contextFactory;

        public ICoreClock Clock => clock;
        public CustomerContextFactory ContextFactory => contextFactory;

        public void StoreSession(CommonElectronicIdSignatureSession session, ICustomerContextExtended context)
        {
            KeyValueStoreService.SetValueComposable(context, session.Id, SessionKeySpace, JsonConvert.SerializeObject(session), context.CurrentUser, context.CoreClock);
        }

        public void SetAlternateKey(string keyName, string alternateKey, string sessionId, ICustomerContextExtended context)
        {
            KeyValueStoreService.SetValueComposable(context, $"{keyName}#{alternateKey}", AlternateKeyKeySpace, sessionId, context.CurrentUser, context.CoreClock);
        }

        public CommonElectronicIdSignatureSession GetSession(string sessionId, ICustomerContextExtended context)
        {
            var raw = KeyValueStoreService.GetValueComposable(context, sessionId, SessionKeySpace);
            return raw == null ? null : JsonConvert.DeserializeObject<CommonElectronicIdSignatureSession>(raw);
        }

        public string GetSessionIdByAlternateKey(string keyName, string alternateKey)
        {
            using (var context = contextFactory.CreateContext())
            {
                return GetSessionIdByAlternateKey(keyName, alternateKey, context);
            }
        }

        public string GetSessionIdByAlternateKey(string keyName, string alternateKey, ICustomerContextExtended context)
        {
            return KeyValueStoreService.GetValueComposable(context, $"{keyName}#{alternateKey}", AlternateKeyKeySpace);
        }

        public CommonElectronicIdSignatureSession GetSession(string sessionId, bool closeItFirst, Action<bool> observeWasClosed = null)
        {
            using (var context = contextFactory.CreateContext())
            {
                var session = GetSession(sessionId, context);
                if (closeItFirst && !session.ClosedDate.HasValue)
                {
                    session.ClosedDate = context.CoreClock.Now.DateTime;
                    StoreSession(session, context);
                    context.SaveChanges();
                    observeWasClosed?.Invoke(true);
                }
                else
                    observeWasClosed?.Invoke(false);
                return session;
            }
        }

        public static CommonElectronicIdSignatureSession CreateLocalSessionFromRequest(SingleDocumentSignatureRequestUnvalidated request, string providerName, IClientConfigurationCore clientConfiguration)
        {            
            return new CommonElectronicIdSignatureSession
            {
                Id = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken(),
                SignatureProviderName = providerName,
                RedirectAfterFailedUrl = request.RedirectAfterFailedUrl,
                RedirectAfterSuccessUrl = request.RedirectAfterSuccessUrl,
                ServerToServerCallbackUrl = request.ServerToServerCallbackUrl,
                UnsignedPdf = new CommonElectronicIdSignatureSession.PdfModel
                {
                    ArchiveKey = request.DocumentToSignArchiveKey,
                    FileName = request.DocumentToSignFileName
                },
                SigningCustomersBySignerNr = request.SigningCustomers.ToDictionary(x => x.SignerNr.Value, x =>
                {
                    var signerToken = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
                    return new CommonElectronicIdSignatureSession.SigningCustomer
                    {
                        SignerNr = x.SignerNr.Value,
                        CivicRegNr = new CivicRegNumberParser(clientConfiguration.Country.BaseCountry).Parse(x.CivicRegNr).NormalizedValue,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                    };
                }),
                CustomData = request.CustomData
            };
        }
    }
}