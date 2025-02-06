using NTech.Core.Module.Shared.Services;
using System.Collections.Generic;

namespace nCustomer.Code.Services
{
    public class CustomerCustomerSearchSourceService : ICustomerSearchSourceService
    {
        private readonly CustomerSearchService customerSearchService;

        public CustomerCustomerSearchSourceService(CustomerSearchService customerSearchService)
        {
            this.customerSearchService = customerSearchService;
        }

        public ISet<int> FindCustomers(string searchQuery) => customerSearchService.FindCustomersByOmniQuery(searchQuery);

        public List<CustomerSearchEntity> GetCustomerEntities(int customerId) => new List<CustomerSearchEntity>();
    }
}