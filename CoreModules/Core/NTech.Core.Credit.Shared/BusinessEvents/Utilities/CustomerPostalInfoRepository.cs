using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class CustomerPostalInfoRepository : ICustomerPostalInfoRepository
    {
        public CustomerPostalInfoRepository(bool allowMissingAddress, ICustomerClient customerClient, IClientConfigurationCore clientConfigurationCore)
        {
            this.allowMissingAddress = allowMissingAddress;
            this.customerClient = customerClient;
            this.clientConfigurationCore = clientConfigurationCore;
        }

        private readonly Dictionary<int, SharedCustomerPostalInfo> customerInfos = new Dictionary<int, SharedCustomerPostalInfo>();
        private readonly bool allowMissingAddress;
        private readonly ICustomerClient customerClient;
        private readonly IClientConfigurationCore clientConfigurationCore;

        public SharedCustomerPostalInfo GetCustomerPostalInfo(int customerId)
        {
            if (!customerInfos.ContainsKey(customerId))
            {
                PreFetchCustomerPostalInfo(new HashSet<int>() { customerId });
            }
            if (!customerInfos.ContainsKey(customerId))
                throw new Exception($"Missing customerInfo for customerId {customerId}");
            return customerInfos[customerId];
        }

        public void PreFetchCustomerPostalInfo(ISet<int> customerIds)
        {
            Func<IDictionary<string, string>, string, string> opt = (items, name) =>
            {
                if (!items.ContainsKey(name))
                    return null;
                return items[name];
            };
            Func<IDictionary<string, string>, string, int, string> req = (items, name, customerId) =>
            {
                if (!items.ContainsKey(name))
                    throw new Exception($"CustomerCard: Missing '{name}' on customer '{customerId}'");
                return items[name];
            };

            customerIds.ExceptWith(customerInfos.Keys);
            if (!customerIds.Any())
                return;

            var properties = new string[] { "addressStreet", "addressZipcode", "addressCity", "addressCountry", "firstName", "lastName", "companyName", "isCompany" };

            var customerItems = customerClient.BulkFetchPropertiesByCustomerIdsD(customerIds, properties);
            foreach (var c in customerItems)
            {
                var customerId = c.Key;
                var items = c.Value;
                var isCompany = opt(items, "isCompany") == "true";
                SharedCustomerPostalInfo card;

                if (isCompany)
                    card = new CompanyCustomerPostalInfo { CompanyName = req(items, "companyName", customerId) };
                else
                    card = new PersonCustomerPostalInfo { FullName = $"{req(items, "firstName", customerId)} {opt(items, "lastName")}".Trim() };

                card.CustomerId = customerId;
                card.StreetAddress = !allowMissingAddress ? req(items, "addressStreet", customerId) : opt(items, "addressStreet");
                card.PostArea = opt(items, "addressCity");
                card.ZipCode = opt(items, "addressZipcode");
                card.AddressCountry = opt(items, "addressCountry") ?? clientConfigurationCore.Country.BaseCountry;

                customerInfos[card.CustomerId] = card;
            }
        }
    }
}