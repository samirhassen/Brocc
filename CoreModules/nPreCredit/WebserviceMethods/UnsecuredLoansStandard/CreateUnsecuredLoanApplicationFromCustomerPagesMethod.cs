using nPreCredit.Code.Services.NewUnsecuredLoans;
using NTech.Core.PreCredit.Shared.Models;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateUnsecuredLoanApplicationFromCustomerPagesMethod : TypedWebserviceMethod<UlStandardApplicationRequest, CreateUnsecuredLoanApplicationFromCustomerPagesMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Create-Application-From-CustomerPages";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, UlStandardApplicationRequest request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Resolver().Resolve<CreateApplicationWithScoringUlStandardService>();

            var result = service.CreateApplicationWithAutomaticScoring(request, isFromInsecureSource: true, requestJson: NEnv.IsProduction ? null : requestContext.RequestJson);

            //NOTE: Do NOT add the credit decision here as it will leak it directly to the customer.
            return new Response
            {
                ApplicationNr = result.ApplicationNr
            };
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
        }
    }
}