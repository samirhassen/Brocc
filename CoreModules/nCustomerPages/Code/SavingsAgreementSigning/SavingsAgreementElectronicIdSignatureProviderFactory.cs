using System;
using System.Web.Mvc;

namespace nCustomerPages.Code.ElectronicIdSignature
{
    public static class SavingsAgreementElectronicIdSignatureProviderFactory
    {

        public delegate Uri GetExternalLink(string action, string controller, object routeValues);
        public static ISavingsAgreementElectronicIdSignatureProvider Create(GetExternalLink getExternalLink, UrlHelper urlHelper)
        {
            switch (NEnv.SignatureElectronicIdProviderCode)
            {
                case ElectronicIdProviderCode.Signicat:
                case ElectronicIdProviderCode.Signicat2:
                    return new SavingsAgreementSignicatElectronicIdSignatureProvider(getExternalLink, NEnv.SignatureElectronicIdProviderCode);
                default:
                    throw new Exception("Missing active signature provider");
            }
        }
    }
}
