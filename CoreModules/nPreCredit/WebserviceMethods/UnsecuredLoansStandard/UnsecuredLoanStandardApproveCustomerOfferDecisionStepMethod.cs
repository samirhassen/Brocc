using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardApproveCustomerOfferDecisionStepMethod : TypedWebserviceMethod<UnsecuredLoanStandardApproveCustomerOfferDecisionStepMethod.Request, UnsecuredLoanStandardApproveCustomerOfferDecisionStepMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerOfferDecision/Set-Approved-Step";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
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
            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

            var isDecisionStepCurrent =
                wf.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, ai.ListNames);

            if (!isDecisionStepCurrent)
                return Error("Only the current step can be approved");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var currentOfferIsAccepted = context
                    .CreditDecisionItems
                    .Any(x =>
                        x.Decision.ApplicationNr == ai.ApplicationNr
                        && x.Decision.CreditApplication.CurrentCreditDecisionId == x.CreditDecisionId
                        && x.ItemName == "customerDecisionCode" && x.Value == "accepted"
                        && !x.IsRepeatable);
                if (!currentOfferIsAccepted)
                    return Error("Current credit decision has not been accepted by the customer");

                wf.ChangeStepStatusComposable(context, UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, wf.AcceptedStatusName, applicationNr: ai.ApplicationNr);

                context.CreateAndAddComment("Waiting for customer to accept offer approved", "CustomerOfferDecisionStepApproved", applicationNr: ai.ApplicationNr);

                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        private Response HandleRevert(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai)
        {
            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

            var currentStepName = UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name;

            if (!WorkflowServiceBase.IsRevertAllowed(ai, wf, currentStepName, out var revertNotAllowedMessage))
                return Error(revertNotAllowedMessage);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, currentStepName, wf.InitialStatusName, applicationNr: ai.ApplicationNr);
                context.CreateAndAddComment("Waiting for customer to accept offer reverted", "CustomerOfferDecisionStepReverted", applicationNr: ai.ApplicationNr);
                context.SaveChanges();
            }

            return null;
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