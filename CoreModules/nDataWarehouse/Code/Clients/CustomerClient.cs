using System;
using System.Collections.Generic;
using System.Linq;

namespace nDataWarehouse.Code.Clients
{
    public class CustomerClient : AbstractServiceClient, ICustomerClient
    {
        public CustomerClient(Func<string> getBearerToken) : base(getBearerToken)
        {

        }

        protected override string ServiceName => "nCustomer";

        public IDictionary<int, GetPropertyCustomer> BulkFetchPropertiesByCustomerIds(ISet<int> customerIds, params string[] propertyNames)
        {
            return Begin()
                .PostJson("Customer/BulkFetchPropertiesByCustomerIds", new
                {
                    propertyNames = propertyNames,
                    customerIds = customerIds
                })
                .ParseJsonAs<GetPropertyResult>()
                .Customers
                .ToDictionary(x => x.CustomerId, x => x);
        }
        private class GetPropertyResult
        {
            public List<GetPropertyCustomer> Customers { get; set; }
        }
    }
}