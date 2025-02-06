using nPreCredit.Code.Services.CompanyLoans;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class SetCompanyLoanWorkflowStatusMethod : TypedWebserviceMethod<SetCompanyLoanWorkflowStatusMethod.Request, SetCompanyLoanWorkflowStatusMethod.Response>
    {
        public override string Path => "CompanyLoan/Set-WorkflowStatus";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var c = requestContext.Resolver().Resolve<ICompanyLoanWorkflowService>();

            int? eventId = null;
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

                c.ChangeStepStatusComposable(context, request.StepName, request.StatusName, applicationNr: request.ApplicationNr, evt: evt);

                if (!string.IsNullOrWhiteSpace(request.CommentText))
                {
                    context.CreateAndAddComment(request.CommentText?.Trim(), evt?.EventType ?? "SetCompanyLoanWorkflowStatus", applicationNr: request.ApplicationNr);
                }

                if ((request.CompanionOperation ?? "").Equals("AcceptCustomerCheck", StringComparison.OrdinalIgnoreCase))
                {
                    var h = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == request.ApplicationNr);
                    h.CustomerCheckStatus = "Accepted";
                }
                else if ((request.CompanionOperation ?? "").Equals("AcceptFraudCheck", StringComparison.OrdinalIgnoreCase))
                {
                    var h = context.CreditApplicationHeaders.Single(x => x.ApplicationNr == request.ApplicationNr);
                    h.FraudCheckStatus = "Accepted";
                }

                context.SaveChanges();

                eventId = evt?.Id;
            }

            return new Response
            {
                EventId = eventId
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
        }
    }
}