using nCustomer.Services.EidSignatures.Mock;
using nCustomer.Services.EidSignatures.Assently;
using nCustomer.Services.EidSignatures.Scrive;
using NTech.Legacy.Module.Shared.Infrastructure;
using System;
using NTech.Core.Customer.Shared.Database;
using nCustomer.DbModel;
using NTech.Services.Infrastructure;
using nCustomer.Services.EidSignatures.Signicat2;

namespace nCustomer.Services.EidSignatures
{
    public static class ProviderSignatureServiceFactory
    {
        private static Lazy<CustomerContextFactory> customerContextFactory = new Lazy<CustomerContextFactory>(() =>
        {
            var serviceUser = NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword);
            return new CustomerContextFactory(() => new CustomersContextExtended(serviceUser.GetUserMetadata()));
        });

        public static ProviderSignatureService CreateService()
        {
            Lazy<SignatureSessionService> sessionService = new Lazy<SignatureSessionService>(() => 
                new SignatureSessionService(CoreClock.SharedInstance, customerContextFactory.Value));
            var provider = NEnv.SignatureProvider;

            if (provider == MockSignatureService.SharedProviderName)
                return new MockSignatureService(sessionService.Value);
            else if (provider == ScriveSignatureService.SharedProviderName)
                return new ScriveSignatureService(sessionService.Value);
            else if (provider == AssentlySignatureService.SharedProviderName)
                return new AssentlySignatureService(sessionService.Value);
            else if (provider == Signicat2SignatureService.SharedProviderName)
            {
                if(NEnv.Signicat2SignatureSettings.OptBool("useLocalMock"))
                    return new MockSignatureService(sessionService.Value, providerName: Signicat2SignatureService.SharedProviderName);
                else
                    return new Signicat2SignatureService(sessionService.Value);               
            }                
            else
                throw new NotImplementedException();
        }

        public static bool IsProviderHandled(string providerName)
        {
            return providerName.IsOneOf(MockSignatureService.SharedProviderName, ScriveSignatureService.SharedProviderName, AssentlySignatureService.SharedProviderName, Signicat2SignatureService.SharedProviderName);
        }

        public static bool IsSignicat(string providerName) =>
            Code.Services.EidSignatures.Signicat.SignicatSignatureService.ProviderNameShared == providerName;

        public static Code.Services.EidSignatures.Signicat.SignicatSignatureService CreateSignicatService() =>
            new Code.Services.EidSignatures.Signicat.SignicatSignatureService(CoreClock.SharedInstance, customerContextFactory.Value);
    }
}