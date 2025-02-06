using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class UnsecuredLoanStandardApproveAgreementStepMethod : TypedWebserviceMethod<UnsecuredLoanStandardApproveAgreementStepMethod.Request, UnsecuredLoanStandardApproveAgreementStepMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Agreement/Set-Approved-Step";

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

            if (ai.IsFinalDecisionMade)
                return Error("Application cannot be changed");

            if (request.IsApproved.Value)
                return HandleApprove(requestContext, ai, request.IsAutomatic ?? false);
            else
                return HandleRevert(requestContext, ai);
        }

        private Response HandleApprove(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai, bool isAutomatic = false)
        {
            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

            var isDecisionStepCurrent =
                wf.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames);

            if (!isDecisionStepCurrent)
                return Error("Only the current step can be approved");

            var documents = requestContext.Resolver().Resolve<IApplicationDocumentService>().FetchForApplication(ai.ApplicationNr, new List<string>
            {
                CreditApplicationDocumentTypeCode.SignedAgreement.ToString()
            });

            if (!documents.Any(x => x.DocumentType == CreditApplicationDocumentTypeCode.SignedAgreement.ToString()))
                return Error("Signed agreement missing");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, UnsecuredLoanStandardWorkflowService.AgreementStep.Name, wf.AcceptedStatusName, applicationNr: ai.ApplicationNr);
                var commentText = isAutomatic ? "Agreement approved automatically" : "Agreement approved";
                context.CreateAndAddComment(commentText, "AgreementStepApproved", applicationNr: ai.ApplicationNr);

                var listService = requestContext.Resolver().Resolve<IComplexApplicationListReadOnlyService>();
                var lists = listService.GetListsForApplication(ai.ApplicationNr, true, context, "Application");
                var applicationRow = lists["Application"].GetRow(1, true);

                UnsecuredLoanStandardCreateLoanMethod.EnsureCreditNr(ai, applicationRow, context);

                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        private Response HandleRevert(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai)
        {
            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

            var currentStepName = UnsecuredLoanStandardWorkflowService.AgreementStep.Name;

            if (!WorkflowServiceBase.IsRevertAllowed(ai, wf, currentStepName, out var revertNotAllowedMessage))
                return Error(revertNotAllowedMessage);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, currentStepName, wf.InitialStatusName, applicationNr: ai.ApplicationNr);
                context.CreateAndAddComment("Agreement reverted", "AgreementStepReverted", applicationNr: ai.ApplicationNr);
                context.SaveChanges();
            }

            return new Response
            {

            };
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