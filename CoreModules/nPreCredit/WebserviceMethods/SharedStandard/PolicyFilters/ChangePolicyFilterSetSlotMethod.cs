using Newtonsoft.Json;
using NTech.Core.PreCredit.Shared.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class ChangePolicyFilterSetSlotMethod : TypedWebserviceMethod<ChangePolicyFilterSetSlotMethod.Request, ChangePolicyFilterSetSlotMethod.Response>
    {
        public override string Path => "LoanStandard/PolicyFilters/Change-Slot";

        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("High");

        public override bool IsEnabled => PolicyFilterService.IsEnabled(NEnv.EnvSettings);

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var s = requestContext.Resolver().Resolve<PolicyFilterService>();

            var movedToInactiveId = s.ChangePolicyFilterSetSlot(request.Id.Value, request.SlotName);

            return new Response
            {
                MovedToInactiveId = movedToInactiveId
            };
        }

        public class Request
        {
            [Required]
            public int? Id { get; set; }

            public string SlotName { get; set; }
        }

        public class Response
        {
            public int? MovedToInactiveId { get; set; }
        }
    }
}