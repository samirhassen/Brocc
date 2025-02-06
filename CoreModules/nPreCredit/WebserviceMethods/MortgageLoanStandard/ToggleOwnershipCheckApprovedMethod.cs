using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class ToggleOwnershipCheckApprovedMethod : TypedWebserviceMethod<ToggleOwnershipCheckApprovedMethod.Request, ToggleOwnershipCheckApprovedMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/OwnershipCheck/Set-Approved";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var ai = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>()
                .GetApplicationInfo(request.ApplicationNr);

            if (!ai.IsActive)
                return Error("Application is not active");

            if (ai.IsFinalDecisionMade)
                return Error("Application cannot be changed");

            var step = MortgageLoanStandardWorkflowService.CollateralStep;
            var wf = requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>();

            var isStepCurrent =
                wf.IsStepStatusInitial(step.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(step.Name, ai.ListNames);

            if (!isStepCurrent)
                return Error("Can only be changed in the collateral step");


            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                ComplexApplicationListService.SetSingleUniqueItem(
                    request.ApplicationNr,
                    "Application",
                    "isOwnershipCheckApproved", 1,
                    request.IsApproved.Value ? "true" : "false",
                    context);

                context.SaveChanges();
            }

            return new Response();
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public bool? IsApproved { get; set; }
        }

        public class Response
        {

        }
    }
}