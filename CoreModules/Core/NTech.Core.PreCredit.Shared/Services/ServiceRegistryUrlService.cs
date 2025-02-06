using NTech.Banking.Conversion;
using NTech.Core.Module;
using System;

namespace nPreCredit.Code.Services
{
    public class ServiceRegistryUrlService : IServiceRegistryUrlService
    {
        private readonly IPreCreditEnvSettings envSettings;

        public ServiceRegistryUrlService(INTechServiceRegistry serviceRegistry, IPreCreditEnvSettings envSettings)
        {
            ServiceRegistry = serviceRegistry;
            this.envSettings = envSettings;
        }

        public INTechServiceRegistry ServiceRegistry { get; }

        public string CreditUrl(string creditNr)
        {
            return ServiceRegistry.ExternalServiceUrl("nCredit", "Ui/Credit", Tuple.Create("creditNr", creditNr)).ToString();
        }

        public Uri UnsignedMortgageLoanAgreementPublicUrl(string applicationNr, string documentArchivekey)
        {
            return ServiceRegistry.ExternalServiceUrl("nCustomerPages", "api/mortgageloan/document/unsigned-agreement",
                Tuple.Create("a", applicationNr),
                Tuple.Create("k", Hashes.Sha256(documentArchivekey)));
        }

        public Uri ArchiveDocumentUrl(string key)
        {
            if (key == null)
                return null;
            return ServiceRegistry.ExternalServiceUrl("nDocument", "Archive/Fetch", Tuple.Create("key", key));
        }

        public Uri LoggedInUserNavigationUrl(string relativeUrl, params Tuple<string, string>[] queryStringParameters)
        {
            return ServiceRegistry.ExternalServiceUrl(envSettings.CurrentServiceName, relativeUrl, queryStringParameters);
        }
    }

    public interface IServiceRegistryUrlService
    {
        INTechServiceRegistry ServiceRegistry { get; }
        string CreditUrl(string creditNr);
        Uri ArchiveDocumentUrl(string key);
        Uri UnsignedMortgageLoanAgreementPublicUrl(string applicationNr, string documentArchivekey);
        Uri LoggedInUserNavigationUrl(string relativeUrl, params Tuple<string, string>[] queryStringParameters);
    }
}