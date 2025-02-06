using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation;
using NTech.Core.PreCredit.Shared;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardApproveFraudStepMethod : TypedWebserviceMethod<UnsecuredLoanStandardApproveFraudStepMethod.Request, UnsecuredLoanStandardApproveFraudStepMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Fraud/Approve-Step";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var applicationService = resolver.Resolve<ApplicationInfoService>();

            var ai = applicationService.GetApplicationInfo(request.ApplicationNr);

            if (!ai.IsActive)
                return Error("Application is not active");

            var wfService = resolver.Resolve<UnsecuredLoanStandardWorkflowService>();

            if (request.IsApproved == null)
                return Error("IsApproved must be set on the request. ");

            if (ai.IsFinalDecisionMade)
                return Error("Application cannot be changed. ");

            return request.IsApproved.Value
                ? HandleApprove(wfService, requestContext, ai, request.IsAutomatic ?? false)
                : HandleRevert(wfService, requestContext, ai);

        }

        private Response HandleApprove(UnsecuredLoanStandardWorkflowService wfService, NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel aiModel, bool isAutomatic = false)
        {
            var isDecisionStepCurrent =
                wfService.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.FraudStep.Name, aiModel.ListNames)
                &&
                wfService.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.FraudStep.Name, aiModel.ListNames);

            if (!isAutomatic && !isDecisionStepCurrent)
                return Error("Only the current step can be approved");

            var currentStepName = UnsecuredLoanStandardWorkflowService.FraudStep.Name;
            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var existingFraudControl = context.FraudControls
                    .SingleOrDefault(control => control.ApplicationNr == aiModel.ApplicationNr && control.IsCurrentData);
                if (existingFraudControl != null && existingFraudControl.FraudControlItems.Any(x => !x.Status.Equals(FraudCheckStatusCode.Approved)))
                {
                    return Error("All fraud controls must be approved before the step can be approved. ");
                }

                wfService.ChangeStepStatusComposable(context, currentStepName, wfService.AcceptedStatusName, applicationNr: aiModel.ApplicationNr);
                var approvedCommentText = isAutomatic ? "Fraud step approved automatically" : "Fraud step approved";
                context.CreateAndAddComment(approvedCommentText, "FraudStepApproved", applicationNr: aiModel.ApplicationNr);
                context.SaveChanges();
            }

            HandlePostAutomationEvents(aiModel, wfService, requestContext.Resolver().Resolve<UnsecuredLoanStandardAgreementService>(), isAutomatic);

            return new Response();
        }

        private void HandlePostAutomationEvents(ApplicationInfoModel aiModel, UnsecuredLoanStandardWorkflowService wf, UnsecuredLoanStandardAgreementService agreementService, bool isAutomatic)
        {
            if (!isAutomatic)
            {
                var automationHandler = new ApplicationAutomationHandler(aiModel, wf, agreementService);
                if (automationHandler.ShouldAutoSendAgreement())
                {
                    automationHandler.HandlePostAutomationEvents(StepWithAutomationName.Fraud);
                }
            }
        }

        private Response HandleRevert(UnsecuredLoanStandardWorkflowService wfService, NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel aiModel)
        {
            var currentStepName = UnsecuredLoanStandardWorkflowService.FraudStep.Name;

            if (!WorkflowServiceBase.IsRevertAllowed(aiModel, wfService, currentStepName, out var revertNotAllowedMessage))
                return Error(revertNotAllowedMessage);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wfService.ChangeStepStatusComposable(context, currentStepName, wfService.InitialStatusName,
                    applicationNr: aiModel.ApplicationNr);
                context.CreateAndAddComment("Fraud reverted", "FraudStepReverted", applicationNr: aiModel.ApplicationNr);
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

            public bool? IsAutomatic { get; set; }
        }

        public class Response
        {

        }
    }
}