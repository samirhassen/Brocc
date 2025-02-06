using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Services.EidSignatures.Mock
{
    public class MockSignatureService : ProviderSignatureService
    {
        private readonly string providerName;

        public MockSignatureService(SignatureSessionService sessionService, string providerName = SharedProviderName) : base(sessionService)
        {
            this.providerName = providerName;
        }

        public override string ProviderName => providerName;

        public const string SharedProviderName = "mock";

        protected override ProviderSessionData CreateProviderSession(CommonElectronicIdSignatureSession session)
        {
            var data = new ProviderSessionData
            {
                ProviderSessionId = session.Id,
                SignerDataBySignerNr = session.SigningCustomersBySignerNr.ToDictionary(x => x.Key, x =>
                {
                    var signerToken = OneTimeTokenGenerator.SharedInstance.GenerateUniqueToken();
                    return new ProviderSignerData
                    {
                        CustomData = new Dictionary<string, string> { { "signerToken", signerToken } },
                        SignatureUrlBySignerNr = NEnv.ServiceRegistry.External.ServiceUrl("nCustomerPages", $"mock-eid/{signerToken}/sign")
                    };
                }),
                SessionCustomData = new Dictionary<string, string>
                {
                    { "isMock", "true" }
                }
            };
            data.SessionAlternateKeys = data
                .SignerDataBySignerNr
                .Select(x => Tuple.Create("signerToken", x.Value.CustomData["signerToken"]))
                .ToList();
            return data;
        }

        protected override CommonElectronicIdSignatureSession FindSessionByProviderEventData(Dictionary<string, string> providerEventData, ICustomerContextExtended customersContext)
        {
            var signerToken = providerEventData?.Opt("signerToken");
            if (signerToken == null)
                return null;
            var localSessionId = SessionService.GetSessionIdByAlternateKey("signerToken", signerToken, customersContext);
            if (localSessionId == null)
                return null;
            return SessionService.GetSession(localSessionId, customersContext);
        }

        protected override void CloseProviderSession(CommonElectronicIdSignatureSession localSession)
        {
            //Mock has no provider session
        }

        protected override void UpdateSessionFromProvider(CommonElectronicIdSignatureSession localSession, Dictionary<string, string> providerEventData)
        {
            //A little convoluted for mock since there is no provider session so we use the provider event data instead.
            //Basically where the user surfs determines what changes.
            bool? hasSigned = providerEventData?.Opt("hasSigned") == "true" ? true : (providerEventData?.Opt("hasSigned") == "false" ? false : new bool?());
            if (hasSigned.HasValue)
            {
                var signerToken = providerEventData?.Opt("signerToken");

                var customer = localSession.SigningCustomersBySignerNr.Single(x => x.Value.GetCustomDataOpt("signerToken") == signerToken).Value;
                if (hasSigned == true)
                {
                    UpdateSessionOnCustomerSigned(localSession, customer.SignerNr, SessionService.Clock.Now.DateTime);

                    if (localSession.HaveAllSigned())
                    {
                        UpdateSessionOnSignedDocumentReceived(localSession, new CommonElectronicIdSignatureSession.PdfModel
                        {
                            ArchiveKey = localSession.UnsignedPdf.ArchiveKey, //NOTE: Would be cool if we could append a page or some text here even in mock to see a different pdf.
                            FileName = "SignedVersion_" + localSession.UnsignedPdf.FileName
                        });
                    }
                }
            }
        }
    }
}