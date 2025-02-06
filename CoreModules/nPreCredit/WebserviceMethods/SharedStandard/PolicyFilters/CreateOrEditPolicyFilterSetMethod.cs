using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class CreateOrEditPolicyFilterSetMethod : TypedWebserviceMethod<CreateOrEditPolicyFilterSetRequest, CreateOrEditPolicyFilterSetResponse>
    {
        public override string Path => "LoanStandard/PolicyFilters/CreateOrEdit-Set";

        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        public override bool IsEnabled => PolicyFilterService.IsEnabled(NEnv.EnvSettings);

        protected override CreateOrEditPolicyFilterSetResponse DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, CreateOrEditPolicyFilterSetRequest request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<PolicyFilterService>();

            return s.CreateOrEditPolicyFilterSet(request);
        }
    }
}