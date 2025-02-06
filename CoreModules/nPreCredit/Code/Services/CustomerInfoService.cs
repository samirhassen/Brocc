using NTech.Core.Module.Shared.Clients;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.Code.Services
{
    public class CustomerInfoService : ICustomerInfoService
    {
        private readonly ICustomerClient customerClient;

        public CustomerInfoService(ICustomerClient customerClient)
        {
            this.customerClient = customerClient;
        }

        public CustomerContactInfoModel GetContactInfoByCustomerId(int customerId)
        {
            return GetContactInfoByCustomerIds(new HashSet<int> { customerId }).Opt(customerId);
        }

        public Dictionary<int, CustomerContactInfoModel> GetContactInfoByCustomerIds(ISet<int> customerIds)
        {
            return customerClient.BulkFetchPropertiesByCustomerIdsD(
                customerIds,
                "civicRegNr", "addressCity", "addressStreet", "addressZipcode", "addressCountry", "firstName", "lastName", "email", "phone")
                    .ToDictionary(x => x.Key, x => new CustomerContactInfoModel
                    {
                        CivicRegNr = x.Value?.Opt("civicRegNr"),
                        AddressCity = x.Value?.Opt("addressCity"),
                        AddressStreet = x.Value?.Opt("addressStreet"),
                        AddressZipcode = x.Value?.Opt("addressZipcode"),
                        AddressCountry = x.Value?.Opt("addressCountry"),
                        FirstName = x.Value?.Opt("firstName"),
                        LastName = x.Value?.Opt("lastName"),
                        Email = x.Value?.Opt("email"),
                        Phone = x.Value?.Opt("phone"),
                    });
        }
    }

    public interface ICustomerInfoService
    {
        Dictionary<int, CustomerContactInfoModel> GetContactInfoByCustomerIds(ISet<int> customerIds);
        CustomerContactInfoModel GetContactInfoByCustomerId(int customerId);
    }

    public class CustomerContactInfoModel
    {
        public string CivicRegNr { get; set; }
        public string AddressCity { get; set; }
        public string AddressStreet { get; set; }
        public string AddressZipcode { get; set; }
        public string AddressCountry { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}