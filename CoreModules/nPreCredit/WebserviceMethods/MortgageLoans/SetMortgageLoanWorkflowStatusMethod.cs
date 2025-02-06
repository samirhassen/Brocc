using nPreCredit.Code.Services;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class SetMortgageLoanWorkflowStatusMethod : TypedWebserviceMethod<SetMortgageLoanWorkflowStatusMethod.Request, SetMortgageLoanWorkflowStatusMethod.Response>
    {
        public override string Path => "MortgageLoan/Set-WorkflowStatus";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();
            var c = resolver.Resolve<IMortgageLoanWorkflowService>();

            int? eventId = null;
            bool wasChanged = false;
            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                CreditApplicationEvent evt = null;
                if (!string.IsNullOrWhiteSpace(request.EventCode))
                {
                    var eventCode = Enums.Parse<CreditApplicationEventCode>(request.EventCode, ignoreCase: true);
                    if (!eventCode.HasValue)
                        return Error("Invalid EventCode", errorCode: "invalidEventCode");
                    evt = context.CreateAndAddEvent(eventCode.Value, applicationNr: request.ApplicationNr);
                }

                wasChanged = c.ChangeStepStatusComposable(context, request.StepName, request.StatusName, applicationNr: request.ApplicationNr, evt: evt);

                if (!string.IsNullOrWhiteSpace(request.CommentText))
                {
                    context.CreateAndAddComment(request.CommentText?.Trim(), evt?.EventType ?? "SetMortgageLoanWorkflowStatus", applicationNr: request.ApplicationNr);
                }

                if (request.CompanionOperation != null && (request.CompanionOperation.StartsWith("AddToApplicationList:") || request.CompanionOperation.StartsWith("RemoveFromApplicationList:")))
                {
                    var isMember = request.CompanionOperation.StartsWith("Add");
                    var listName = request.CompanionOperation.Substring(request.CompanionOperation.IndexOf(':') + 1);
                    var cs = resolver.Resolve<CreditApplicationListService>();
                    cs.SetMemberStatusComposable(context, listName, isMember, applicationNr: request.ApplicationNr, evt: evt);
                }

                //Special if sign application
                if (request.StepName == c.Model.GetSignApplicationStepIfAny()?.Name)
                {
                    ComplexApplicationListService.SetSingleUniqueItem(
                        request.ApplicationNr, "ApplicationCategory", "IsApplicationSigned", 1,
                        request.StatusName == c.AcceptedStatusName ? "true" : "false", context);
                }

                //Special if approve agreement
                if (request.StepName == c.Model.GetApproveAgreementStepIfAny()?.Name)
                {
                    var toStatusAccepted = request.StatusName == c.AcceptedStatusName ? "true" : "false";
                    ComplexApplicationListService.SetSingleUniqueItem(
                        request.ApplicationNr, "ApplicationCategory", "IsAgreementApproved", 1,
                        toStatusAccepted, context);

                    // Cancel approve agreement
                    if (toStatusAccepted == "false")
                    {
                        var appDocumentService = resolver.Resolve<IApplicationDocumentService>();

                        var documentTypes = new List<string>
                            {CreditApplicationDocumentTypeCode.SignedAgreement.ToString()};

                        var existingSignedAgreements = appDocumentService.FetchForApplication(request.ApplicationNr, documentTypes);

                        foreach (var agreement in existingSignedAgreements)
                        {
                            appDocumentService.RemoveDocument(request.ApplicationNr, agreement.DocumentId);
                        }
                    }
                }

                context.SaveChanges();

                eventId = evt?.Id;
            }

            return new Response
            {
                EventId = eventId,
                WasChanged = wasChanged
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }
            [Required]
            public string StepName { get; set; }
            [Required]
            public string StatusName { get; set; }
            public string CommentText { get; set; }
            public string EventCode { get; set; }
            public string CompanionOperation { get; set; }
        }

        public class Response
        {
            public int? EventId { get; set; }
            public bool WasChanged { get; set; }
        }
    }
}