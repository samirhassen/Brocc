using nCredit.DbModel.Repository;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class CustomerCreditHistoryBatchMethod : TypedWebserviceMethod<CustomerCreditHistoryBatchMethod.Request, CustomerCreditHistoryBatchMethod.Response>
    {
        public override string Path => "CustomerCreditHistoryBatch";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var repo = requestContext.Service().CustomerCreditHistory;
            return new Response { credits = repo.GetCustomerCreditHistory(request.CustomerIds, null) };
        }

        public class Request
        {
            public List<int> CustomerIds { get; set; }
        }

        public class Response
        {
            public List<CustomerCreditHistoryRepository.Credit> credits { get; set; } //[!] lowercase h since to preserve backward compatibility
        }
    }
}