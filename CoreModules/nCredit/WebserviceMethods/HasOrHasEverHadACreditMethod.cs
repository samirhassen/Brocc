using NTech.Services.Infrastructure.NTechWs;
using System.Linq;
namespace nCredit.WebserviceMethods
{
    public class HasOrHasEverHadACreditMethod : TypedWebserviceMethod<HasOrHasEverHadACreditMethod.Request, HasOrHasEverHadACreditMethod.Response>
    {
        public override string Path => "CustomerPages/HasOrHasEverHadACredit";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            if (!request.CustomerId.HasValue)
                return Error("Missing customerId", errorCode: "missingCustomerId");

            using (var context = new CreditContext())
            {
                var statuses = context
                    .CreditHeaders
                    .Where(x => x.CreditCustomers.Any(y => y.CustomerId == request.CustomerId.Value))
                    .Select(x => x.Status)
                    .ToList();
                return new Response { hasOrHasEverHadACredit = statuses.Any(), hasActiveCredit = statuses.Contains("Normal") };
            }
        }

        public class Request
        {
            public int? CustomerId { get; set; }
        }

        public class Response
        {
            public bool? hasOrHasEverHadACredit { get; set; } //[!] lowercase h since to preserve backward compatibility
            public bool? hasActiveCredit { get; set; }
        }
    }
}