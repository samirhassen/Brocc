using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.NewUnsecuredLoans;
using nPreCredit.Code.Services.UnsecuredLoans;
using nPreCredit.Code.StandardPolicyFilters.DataSources;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class NewCreditCheckMethod : TypedWebserviceMethod<NewCreditCheckMethod.Request, NewCreditCheckMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/New-CreditCheck";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var service = new NewCreditCheckUlStandardService(
                requestContext.Resolver().Resolve<ApplicationInfoService>(),
                requestContext.Resolver().Resolve<UnsecuredLoanLtlAndDbrService>(),
                requestContext.Resolver().Resolve<PreCreditContextFactoryService>(),
                requestContext.Resolver().Resolve<UnsecuredLoanStandardApplicationPolicyFilterDataSourceFactory>(),
                NEnv.ClientCfgCore);

            var recommendation = service.NewCreditCheck(request.ApplicationNr);

            var tempStorage = requestContext.Resolver().Resolve<IEncryptedTemporaryStorageService>();

            return new Response
            {
                Recommendation = recommendation,
                RecommendationTemporaryStorageKey = tempStorage.StoreString(JsonConvert.SerializeObject(recommendation), TimeSpan.FromHours(24))
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public UnsecuredLoanStandardCreditRecommendationModel Recommendation { get; set; }
            public string RecommendationTemporaryStorageKey { get; set; }
        }
    }
}