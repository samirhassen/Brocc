using NTech.Legacy.Module.Shared.Infrastructure;
using System;

namespace nCustomer.Code.Services.EidAuthentication
{
    public static class EidAuthenticationServiceFactory
    {
        public static IEidAuthenticationService CreateEidAuthenticationService()
        {
            var provider = NEnv.EidLoginProvider;

            if (provider == ScriveAuthenticationService.ProviderName)
                return new ScriveAuthenticationService(NEnv.ScriveAuthenticationSettings, CoreClock.SharedInstance);
            else if (provider == SignicatAuthenticationService.ProviderName)
                return new SignicatAuthenticationService(null, CoreClock.SharedInstance);
            else if (provider == Signicat2AuthenticationService.ProviderName) //New Signicat provider, use this one going forward
                return new Signicat2AuthenticationService(NEnv.Signicat2AuthenticationSettings, CoreClock.SharedInstance);
            else if (provider == MockAuthenticationService.ProviderName)
                return new MockAuthenticationService(CoreClock.SharedInstance);
            else
                throw new NotImplementedException();
        }
    }
}