using nCredit.DbModel.Repository;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
namespace nCredit.WebserviceMethods
{
    public class CustomerCreditHistoryByCreditNrsMethod : TypedWebserviceMethod<CustomerCreditHistoryByCreditNrsMethod.Request, CustomerCreditHistoryByCreditNrsMethod.Response>
    {
        public override string Path => "CustomerCreditHistoryByCreditNrs";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var repo =requestContext.Service().CustomerCreditHistory;
            return new Response { credits = repo.GetCustomerCreditHistory(null, request.CreditNrs) };
        }

        public class Request
        {
            public List<string> CreditNrs { get; set; }
        }

        public class Response
        {
            public List<CustomerCreditHistoryRepository.Credit> credits { get; set; } //[!] lowercase h since to preserve backward compatibility
        }
    }
}