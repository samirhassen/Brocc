using nPreCredit.Code.Services.NewUnsecuredLoans;
using NTech.Core.PreCredit.Shared.Models;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateUnsecuredLoanApplicationMethod : TypedWebserviceMethod<UlStandardApplicationRequest, UlStandardApplicationResponse>
    {
        public override string Path => "UnsecuredLoanStandard/Create-Application";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override UlStandardApplicationResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, UlStandardApplicationRequest request)
        {
            ValidateUsingAnnotations(request);

            var service = requestContext.Resolver().Resolve<CreateApplicationWithScoringUlStandardService>();

            return service.CreateApplicationWithAutomaticScoring(request, isFromInsecureSource: false, requestJson: NEnv.IsProduction ? null : requestContext.RequestJson);
        }
    }
}