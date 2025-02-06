using nCustomer.DbModel;
using NTech.Banking.OrganisationNumbers;
using NTech.Core.Customer.Shared.Services;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public interface ICompanyCustomerService
    {
        int CreateOrUpdateCompany(IOrganisationNumber orgnr, IDictionary<string, string> properties, NtechCurrentUserMetadata currentUser, ISet<string> additionalSensitiveProperties = null, int? expectedCustomerId = null, string externalEventCode = null, ISet<string> forceUpdateProperties = null);
    }

    public class CompanyCustomerService : CustomerServiceBase, ICompanyCustomerService
    {
        private readonly Func<CustomersContext> createContext;
        private readonly Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository;

        public CompanyCustomerService(
            Func<CustomersContext> createContext,
            Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository)
        {
            this.createContext = createContext;
            this.createCustomerRepository = createCustomerRepository;
        }

        public int CreateOrUpdateCompany(IOrganisationNumber orgnr, IDictionary<string, string> properties, NtechCurrentUserMetadata currentUser, ISet<string> additionalSensitiveProperties = null, int? expectedCustomerId = null, string externalEventCode = null, ISet<string> forceUpdateProperties = null)
        {
            CheckForBannedProperties(properties);

            var actualCustomerId = CustomerIdSource.GetCustomerIdByOrgnr(orgnr);
            if (expectedCustomerId.HasValue && expectedCustomerId.Value != actualCustomerId)
                throw new Exception($"Expected customerid to be {expectedCustomerId.Value} but it was instead {actualCustomerId}");

            var items = new List<CustomerPropertyModel>();

            Action<string, string> add = (name, value) =>
                {
                    items.Add(CustomerPropertyModel.Create(
                        actualCustomerId,
                        name,
                        value,
                        false,
                        forceUpdate: (forceUpdateProperties?.Contains(name) ?? false),
                        forceSensetiveIfNoTemplate: additionalSensitiveProperties != null && additionalSensitiveProperties.Contains(name)));
                };

            add(CustomerProperty.Codes.orgnr.ToString(), orgnr.NormalizedValue);
            add(CustomerProperty.Codes.orgnr_country.ToString(), orgnr.Country);
            add(CustomerProperty.Codes.isCompany.ToString(), "true");

            foreach (var p in properties)
                add(p.Key, p.Value);

            using (var db = createContext())
            {
                using (var tr = db.Database.BeginTransaction())
                {
                    var repository = createCustomerRepository(db, currentUser);
                    repository.UpdateProperties(items, false, businessEventCode: externalEventCode == null ? null : $"E_{externalEventCode}");
                    db.SaveChanges();
                    tr.Commit();
                }
            }

            return actualCustomerId;
        }
    }
}