using NTech.Core.Customer.Shared.Database;
using System.Collections.Generic;
using System.Linq;

namespace nCustomer.Code.Services
{
    public class CustomerPropertyStatusService
    {
        private readonly CustomerContextFactory customerContextFactory;

        public CustomerPropertyStatusService(CustomerContextFactory customerContextFactory)
        {
            this.customerContextFactory = customerContextFactory;
        }

        public class CheckPropertyStatusResult
        {
            public List<string> MissingPropertyNames { get; set; }
        }

        public bool TryCheckPropertyStatus(int customerId, List<string> propertyNames, out string failedMessage, out CheckPropertyStatusResult result)
        {
            result = null;

            if (customerId == 0)
            {
                failedMessage = "Missing customerId";
                return false;
            }
            if (propertyNames == null || propertyNames.Count == 0)
            {
                failedMessage = "Missing propertyNames";
                return false;
            }

            var ps = new HashSet<string>(propertyNames);
            using (var db = customerContextFactory.CreateContext())
            {
                var existingNames = new HashSet<string>(db.CustomerPropertiesQueryable.Where(x => x.CustomerId == customerId && propertyNames.Contains(x.Name) && x.IsCurrentData).Select(x => x.Name).ToList());
                var missingNames = ps.Except(existingNames);

                failedMessage = null;
                result = new CheckPropertyStatusResult
                {
                    MissingPropertyNames = missingNames?.ToList()
                };
                return true;
            }
        }
    }
}