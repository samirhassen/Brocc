using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.SharedStandard
{
    public class LoanStandardApproveKycStepMethod : TypedWebserviceMethod<LoanStandardApproveKycStepMethod.Request, LoanStandardApproveKycStepMethod.Response>
    {
        public override string Path => "LoanStandard/Kyc/Set-Approved-Step";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled || NEnv.IsStandardMortgageLoansEnabled;
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

        private Response HandleApprove(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai, bool isAutomatic)
        {
            var wf = requestContext.Resolver().Resolve<ISharedWorkflowService>();

            var isDecisionStepCurrent =
                wf.IsStepStatusInitial(KycStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(KycStep.Name, ai.ListNames);

            if (!isDecisionStepCurrent)
                return Error("Only the current step can be approved");

            var applicationService = requestContext.Resolver().Resolve<ApplicationInfoService>();
            var applicants = applicationService.GetApplicationApplicants(ai.ApplicationNr);
            var screenedCustomerIds = applicants
                .AllConnectedCustomerIdsWithRoles
                .Where(x => x.Value.Intersect(ScreenedRoles).Any()).Select(x => x.Key)
                .ToHashSet();

            var customerClient = new Code.PreCreditCustomerClient();
            var onboardingResults = customerClient.FetchCustomerOnboardingStatuses(screenedCustomerIds, ApplicationType, ai.ApplicationNr, false);

            if (onboardingResults.Values.Any(x => !x.IsPep.HasValue || !x.IsSanction.HasValue))
                return Error("Pep or sanction status unknown");

            if (onboardingResults.Values.Any(x => !x.LatestScreeningDate.HasValue))
                return Error("Screening not done");

            if (onboardingResults.Any(x => !x.Value.HasNameAndAddress))
                return Error("Customers missing name, email or address information");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, KycStep.Name, wf.AcceptedStatusName, applicationNr: ai.ApplicationNr);
                var isAutomaticText = isAutomatic ? "automatically" : "";
                context.CreateAndAddComment($"Kyc approved {isAutomaticText}", "KycStepApproved", applicationNr: ai.ApplicationNr);
                context.SaveChanges();
            }

            HandlePostAutomationEvents(ai, requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>(), requestContext.Resolver().Resolve<UnsecuredLoanStandardAgreementService>(), isAutomatic);

            return new Response
            {

            };
        }

        private void HandlePostAutomationEvents(ApplicationInfoModel ai, UnsecuredLoanStandardWorkflowService wf, UnsecuredLoanStandardAgreementService agreementService, bool isAutomatic)
        {
            if (!isAutomatic)
            {
                var automationHandler = new ApplicationAutomationHandler(ai, wf, agreementService);
                if (automationHandler.ShouldAutoApproveFraud())
                {
                    automationHandler.HandlePostAutomationEvents(StepWithAutomationName.Kyc);
                }
            }
        }

        private Response HandleRevert(NTechWebserviceMethodRequestContext requestContext, ApplicationInfoModel ai)
        {
            var wf = requestContext.Resolver().Resolve<ISharedWorkflowService>();

            var currentStepName = KycStep.Name;

            if (!WorkflowServiceBase.IsRevertAllowed(ai, wf, currentStepName, out var revertNotAllowedMessage))
                return Error(revertNotAllowedMessage);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                wf.ChangeStepStatusComposable(context, currentStepName, wf.InitialStatusName, applicationNr: ai.ApplicationNr);
                context.CreateAndAddComment("Kyc reverted", "KycStepReverted", applicationNr: ai.ApplicationNr);
                context.SaveChanges();
            }

            return new Response
            {

            };
        }

        private WorkflowModel.StepModel KycStep => NEnv.IsStandardMortgageLoansEnabled ? MortgageLoanStandardWorkflowService.KycStep : UnsecuredLoanStandardWorkflowService.KycStep;
        private string ApplicationType => NEnv.IsStandardMortgageLoansEnabled ? "MortgageLoanApplication" : "UnsecuredLoanApplication";
        private HashSet<string> ScreenedRoles => NEnv.IsStandardMortgageLoansEnabled ?
            new HashSet<string> { "Applicant", "mortgageLoanPropertyOwner", "mortgageLoanConsentingParty" } :
            new HashSet<string> { "Applicant" };

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