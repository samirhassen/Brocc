using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.WebserviceMethods.UnsecuredLoansStandard.ApplicationAutomation;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class SetUnsecuredLoanStandardCustomerCreditDecisionMethod : TypedWebserviceMethod<SetUnsecuredLoanStandardCustomerCreditDecisionMethod.Request, SetUnsecuredLoanStandardCustomerCreditDecisionMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Set-Customer-CreditDecisionCode";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (requestContext.IsForwardedCustomerPagesApiCall() && !request.CustomerId.HasValue)
            {
                return Error("CustomerId required");
            }

            var applicationInfoService = requestContext
                .Resolver()
                .Resolve<ApplicationInfoService>();

            var applicationCancellationService = requestContext
                   .Resolver()
                   .Resolve<IApplicationCancellationService>();

            var ai = applicationInfoService
                .GetApplicationInfo(request.ApplicationNr, true);

            if (ai == null)
                return Error("Not found");

            int? changedByApplicantNr = null;
            if (request.CustomerId.HasValue)
            {
                var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);
                changedByApplicantNr = applicants
                    .CustomerIdByApplicantNr
                    .Select(x => new { ApplicantNr = x.Key, CustomerId = x.Value })
                    .FirstOrDefault(x => x.CustomerId == request.CustomerId.Value)
                    ?.ApplicantNr;
                if (!changedByApplicantNr.HasValue)
                    return Error("Not found");
            }

            if (!ai.IsActive)
                return Error("Application is not active");

            var wf = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();
            var isDecisionStepCurrent =
                wf.IsStepStatusInitial(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, ai.ListNames)
                &&
                wf.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, ai.ListNames);

            if (ai.IsFinalDecisionMade || !isDecisionStepCurrent)
                return Error("Application cannot be changed");

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var customerDecisionCodeItem = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .SelectMany(x => x.CurrentCreditDecision.DecisionItems.Where(y => y.ItemName == "customerDecisionCode" && !y.IsRepeatable))
                    .FirstOrDefault();

                if (customerDecisionCodeItem == null)
                    return Error("Application does not have a current offer");

                customerDecisionCodeItem.Value = request.CustomerDecisionCode;

                var whoChanged = changedByApplicantNr.HasValue ? $"Applicant {changedByApplicantNr.Value}" : "Handler";
                var cd = request.CustomerDecisionCode.ToLower();

                if (cd == "accepted" || cd == "rejected")
                    context.CreateAndAddComment($"{whoChanged} has {cd} offer", "CustomerDecisionCodeChanged", applicationNr: request.ApplicationNr);
                else
                    context.CreateAndAddComment($"{whoChanged} changed customers offer decision to {cd}", "CustomerDecisionCodeChanged", applicationNr: request.ApplicationNr);

                context.SaveChanges();

                var autoHandler = new ApplicationAutomationHandler(ai, wf, requestContext.Resolver().Resolve<UnsecuredLoanStandardAgreementService>());
                autoHandler.HandleCustomerDecisionAutomation(cd, context, whoChanged, applicationCancellationService);
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
            [EnumCode(EnumType = typeof(AllowedCode))]
            public string CustomerDecisionCode { get; set; }

            public int? CustomerId { get; set; }

            private enum AllowedCode
            {
                initial,
                accepted,
                rejected
            }
        }

        public class Response
        {

        }
    }
}