using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code
{
    public class PreCreditCustomerClient : NTech.Core.Module.Shared.Clients.CustomerClient
    {
        public PreCreditCustomerClient() :
            base(LegacyHttpServiceHttpContextUser.SharedInstance, LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry))
        {
        }

        //TODO: Get rid of this entirely
        public IDictionary<string, string> GetCustomerCardItems(int customerId, params string[] names) =>
            BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, names)?.Values?.FirstOrDefault();

        public Dictionary<int, Dictionary<string, string>> BulkFetchPropertiesByCustomerIdsSimple(ISet<int> customerIds, params string[] propertyNames) =>
            BulkFetchPropertiesByCustomerIdsD(customerIds, propertyNames);

        public static string GetCustomerCardUrl(int customerId, NTechNavigationTarget back, bool forceLegacyUi = false)
        {
            return GetCustomerCardUrl(customerId.ToString(), back, forceLegacyUi: forceLegacyUi);
        }

        public static string GetCustomerCardUrl(string customerId, NTechNavigationTarget back, bool forceLegacyUi = false)
        {
            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomer", "Customer/CustomerCard",
                    Tuple.Create("customerId", customerId),
                    Tuple.Create("backTarget", back?.GetBackTargetOrNull()),
                    Tuple.Create("forceLegacyUi", forceLegacyUi ? "true" : null)
                ).ToString();
        }

        public static string GetCustomerFatcaCrsUrl(int customerId, NTechNavigationTarget back)
        {
            var backTarget = back?.GetBackTargetOrNull();
            return NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/KycManagement/FatcaCrs",
                        Tuple.Create("customerId", customerId.ToString()),
                        backTarget == null ? null : Tuple.Create("backTarget", backTarget)).ToString();
        }

        public static string GetCustomerPepKycUrl(int customerId, NTechNavigationTarget back)
        {
            var backTarget = back?.GetBackTargetOrNull();
            return NEnv.ServiceRegistry.Internal.ServiceUrl("nCustomer", "Ui/KycManagement/Manage",
                        Tuple.Create("customerId", customerId.ToString()),
                        backTarget == null ? null : Tuple.Create("backTarget", backTarget)).ToString();
        }
    }
}