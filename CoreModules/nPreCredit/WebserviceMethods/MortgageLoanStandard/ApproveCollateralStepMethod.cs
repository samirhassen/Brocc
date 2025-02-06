using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class ApproveCollateralStepMethod : TypedWebserviceMethod<ApproveCollateralStepMethod.Request, ApproveCollateralStepMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Collateral/Set-Approved-Step";

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

            if (request.IsApproved.Value)
                return HandleApprove(requestContext, ai);
            else
                return HandleRevert(requestContext, ai);
        }

        private Response HandleApprove(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai)
        {
            var wf = requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>();

            var isStepCurrent =
                wf.IsStepStatusInitial(CurrentStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(CurrentStep.Name, ai.ListNames);

            if (!isStepCurrent)
                return Error("Only the current step can be approved");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, CurrentStep.Name, wf.AcceptedStatusName, applicationNr: ai.ApplicationNr);

                context.CreateAndAddComment($"{CurrentStep.DisplayName} approved", $"{CurrentStep.Name}Approved", applicationNr: ai.ApplicationNr);

                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        private Response HandleRevert(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai)
        {
            var wf = requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>();


            if (!MortgageLoanStandardWorkflowService.IsRevertAllowed(ai, wf, CurrentStep.Name, out var revertNotAllowedMessage))
                return Error(revertNotAllowedMessage);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, CurrentStep.Name, wf.InitialStatusName, applicationNr: ai.ApplicationNr);

                context.CreateAndAddComment($"{CurrentStep.DisplayName} reverted", $"{CurrentStep.Name}StepReverted", applicationNr: ai.ApplicationNr);
                context.SaveChanges();
            }

            return null;
        }

        private WorkflowModel.StepModel CurrentStep => MortgageLoanStandardWorkflowService.CollateralStep;

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