using NTech.Services.Infrastructure.ElectronicAuthentication;
using System;

namespace nCustomer.Code.Services.EidAuthentication
{
    public class ReturnUrlModel
    {
        private readonly string returnUrl;

        public ReturnUrlModel(string returnUrl)
        {
            this.returnUrl = returnUrl;
        }

        public Uri GetReturnUrl(CommonElectronicAuthenticationSession session)
        {
            //NOTE: Dont add session.ProviderSessionId support here since this is used before that is created so it would cause inception.
            return new Uri(returnUrl.Replace("{localSessionId}", session.LocalSessionId));
        }
    }
}