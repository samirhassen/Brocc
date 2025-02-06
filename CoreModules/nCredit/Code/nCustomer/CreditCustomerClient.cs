using NTech.Core.Module.Shared.Clients;
using NTech.Legacy.Module.Shared.Infrastructure.HttpClient;
using NTech.Services.Infrastructure;
using System;
using System.Collections.Generic;

namespace nCredit.Code
{
    public class CreditCustomerClient : CustomerClient
    {
        public CreditCustomerClient() : base(LegacyHttpServiceHttpContextUser.SharedInstance, LegacyServiceClientFactory.CreateClientFactory(NEnv.ServiceRegistry))
        {

        }

        public static Uri GetCustomerCardUri(int customerId, string backTarget = null)
        {
            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomer", "Customer/CustomerCard", GetCustomerCardArgsTupleArr(customerId, backTarget));
        }

        public static string GetCustomerCardUrl(int customerId, NTechNavigationTarget backTarget)
        {
            return GetCustomerCardUrl(customerId.ToString(), backTarget);
        }

        public static string GetCustomerCardUrl(string customerId, NTechNavigationTarget backTarget)
        {
            return NEnv.ServiceRegistry.External.ServiceUrl("nCustomer", "Customer/CustomerCard",
                    Tuple.Create("backTarget", backTarget.GetBackTargetOrNull()),
                    Tuple.Create("customerId", customerId)).ToString();
        }

        public static Tuple<string, string>[] GetCustomerCardArgsTupleArr(int customerId, string backTarget = null)
        {
            var args = new List<Tuple<string, string>>();
            args.Add(Tuple.Create("customerId", customerId.ToString()));

            if (!string.IsNullOrEmpty(backTarget))
                args.Add(Tuple.Create("backTarget", backTarget));

            return args.ToArray();
        }

        public IDictionary<string, string> GetCustomerCardItems(int customerId, params string[] names)
        {
            return this.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, names).Opt(customerId);
        }
    }
}