using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchCreditReportProvidersMethod : TypedWebserviceMethod<FetchCreditReportProvidersMethod.Request, FetchCreditReportProvidersMethod.Response>
    {
        public override string Path => "CreditReportProviders/FetchAll";

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            var creditReportProviderName = NEnv.CreditReportProviderName;
            var allProviders = new HashSet<string>();
            allProviders.Add(creditReportProviderName);
            NEnv.ListCreditReportProviders.ToList().ForEach(x => allProviders.Add(x));
            return new Response
            {
                CurrentProviderName = creditReportProviderName,
                AllProviderNames = allProviders.ToList()
            };
        }

        public class Request
        {

        }

        public class Response
        {
            public string CurrentProviderName { get; set; }
            public List<string> AllProviderNames { get; set; }
        }
    }
}