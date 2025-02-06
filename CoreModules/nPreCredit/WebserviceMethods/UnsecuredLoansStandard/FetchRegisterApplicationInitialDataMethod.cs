using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FetchRegisterApplicationInitialDataMethod : TypedWebserviceMethod<FetchRegisterApplicationInitialDataMethod.Request, FetchRegisterApplicationInitialDataMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/FetchRegisterApplicationInitialData";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            return new Response
            {
                Enums = CreditStandardEnumService.Instance.GetApiEnums(language: NEnv.ClientCfg.Country.GetBaseLanguage()),
                ProviderDisplayNameByName = NEnv.GetAffiliateModels().ToDictionary(x => x.ProviderName, x => x.DisplayToEnduserName)
            };
        }

        public class Response
        {
            public EnumsApiModel Enums { get; set; }
            public Dictionary<string, string> ProviderDisplayNameByName { get; set; }
        }

        public class Request
        {
            public string UiLanguage { get; set; }
        }
    }
}