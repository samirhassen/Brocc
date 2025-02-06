using nCustomer.DbModel;
using System;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly Func<CustomersContext> createContext;
        private readonly Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository;


        public CustomerService(
            Func<CustomersContext> createContext,
            Func<CustomersContext, NtechCurrentUserMetadata, CustomerWriteRepository> createCustomerRepository)
        {
            this.createContext = createContext;
            this.createCustomerRepository = createCustomerRepository;
        }

        public IDictionary<int, IList<CustomerPropertyModel>> BulkFetch(ISet<int> customerIds, ISet<string> propertyNames, NtechCurrentUserMetadata user)
        {
            using (var db = createContext())
            {
                var repo = createCustomerRepository(db, user);
                return repo.BulkFetch(customerIds, propertyNames: propertyNames, skipDecryptingEncryptedItems: false);
            }
        }
    }

    public interface ICustomerService
    {
        IDictionary<int, IList<CustomerPropertyModel>> BulkFetch(ISet<int> customerIds, ISet<string> propertyNames, NtechCurrentUserMetadata user);
    }
}