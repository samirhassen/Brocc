using nCustomer.Code;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;

namespace NTech.Core.Customer.Shared.Services
{
    public class MergeCustomerRelationsService
    {
        private readonly Lazy<string> connectionString;

        public MergeCustomerRelationsService(Lazy<string> connectionString)
        {
            this.connectionString = connectionString;
        }

        public MergeCustomerRelationsResponse MergeCustomerRelation(MergeCustomerRelationsRequest request)
        {
            if (request.Relations != null && request.Relations.Count > 0)
            {
                var m = new DatabaseMergeCommand<CustomerClientCustomerRelation>("nCustomer", connectionString.Value);

                string failedMessage;
                if (!m.TryMergeTable("CustomerRelation", request.Relations, out failedMessage))
                    throw new NTechCoreWebserviceException(failedMessage) { ErrorCode = "mergeFailed", ErrorHttpStatusCode = 400, IsUserFacing = true };
            }
            return new MergeCustomerRelationsResponse();
        }
    }

    public class MergeCustomerRelationsRequest
    {
        public List<CustomerClientCustomerRelation> Relations { get; set; }
    }

    public class MergeCustomerRelationsResponse
    {

    }
}
