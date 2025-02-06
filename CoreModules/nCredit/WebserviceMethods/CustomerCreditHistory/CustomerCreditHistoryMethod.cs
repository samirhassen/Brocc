using nCredit.DbModel.Repository;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class CustomerCreditHistoryMethod : TypedWebserviceMethod<CustomerCreditHistoryMethod.Request, CustomerCreditHistoryMethod.Response>
    {
        public override string Path => "CustomerCreditHistory";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!request.CustomerId.HasValue)
                return Error("Missing customerId", errorCode: "missingCustomerId");

            var repo = requestContext.Service().CustomerCreditHistory;
            return new Response { credits = repo.GetCustomerCreditHistory(new List<int> { request.CustomerId.Value }, null) };
        }

        public class Request
        {
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public List<CustomerCreditHistoryRepository.Credit> credits { get; set; } //[!] lowercase h since to preserve backward compatibility
        }
    }
}