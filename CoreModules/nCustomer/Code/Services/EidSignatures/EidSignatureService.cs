using nCustomer.Code;
using nCustomer.DbModel;
using NTech.Core.Customer.Shared.Database;
using NTech.ElectronicSignatures;
using NTech.Services.Infrastructure;
using System;

namespace nCustomer.Services.EidSignatures
{
    public class EidSignatureService
    {
        private static Lazy<CustomerContextFactory> customerContextFactory = new Lazy<CustomerContextFactory>(() =>
        {
            var serviceUser = NTechSelfRefreshingBearerToken.CreateSystemUserBearerTokenWithUsernameAndPassword(NEnv.ServiceRegistry, NEnv.ApplicationAutomationUsernameAndPassword);
            return new CustomerContextFactory(() => new CustomersContextExtended(serviceUser.GetUserMetadata()));
        });

        public EidSignatureService()
        {

        }

        public CommonElectronicIdSignatureSession SynchronizeSessionWithProvider(string sessionIdOrCustomSearchTerm, string customSearchTermNameOrNullForSessionId, bool closeItFirst, Action<bool> observeWasClosed = null)
        {
            var providerName = NEnv.SignatureProvider?.ToLowerInvariant();

            CommonElectronicIdSignatureSession WithContext(Func<ICustomerContextExtended, CommonElectronicIdSignatureSession> a)
            {
                using (var context = customerContextFactory.Value.CreateContext())
                {
                    var result = a(context);

                    context.SaveChanges();

                    return result;
                }
            }

            return WithContext(context =>
            {
                if (ProviderSignatureServiceFactory.IsSignicat(providerName))
                {
                    var service = ProviderSignatureServiceFactory.CreateSignicatService();

                    return service.GetSession(sessionIdOrCustomSearchTerm, customSearchTermNameOrNullForSessionId, context);
                }
                else if (ProviderSignatureServiceFactory.IsProviderHandled(providerName))
                {
                    var service = ProviderSignatureServiceFactory.CreateService();
                    return service.SynchronizeSessionWithProvider(sessionIdOrCustomSearchTerm, closeItFirst, alternateKeyName: customSearchTermNameOrNullForSessionId);
                }
                else
                    return null;
            });
        }

        public CommonElectronicIdSignatureSession CreateSingleDocumentSignatureSession(SingleDocumentSignatureRequest request)
        {
            using (var context = new CustomersContext())
            {
                var providerName = NEnv.SignatureProvider?.ToLowerInvariant();
                if (ProviderSignatureServiceFactory.IsSignicat(providerName))
                {
                    var service = ProviderSignatureServiceFactory.CreateSignicatService();

                    return service.CreateSingleDocumentSignatureSession(request);
                }
                else if (ProviderSignatureServiceFactory.IsProviderHandled(providerName))
                {
                    var service = ProviderSignatureServiceFactory.CreateService();
                    return service.CreateNewSession(request);
                }
                else
                    throw new NotImplementedException();
            }
        }
    }
}