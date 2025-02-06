using System.Collections.Generic;

namespace nDataWarehouse.Code.Clients
{
    public interface ICustomerClient
    {
        IDictionary<int, GetPropertyCustomer> BulkFetchPropertiesByCustomerIds(ISet<int> customerIds, params string[] propertyNames);
    }

    public class GetPropertyCustomer
    {
        public int CustomerId { get; set; }
        public List<Property> Properties { get; set; }

        public class Property
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}