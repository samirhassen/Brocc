using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Banking.Conversion;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class CompleteMortgageLoanLeadMethod : TypedWebserviceMethod<CompleteMortgageLoanLeadMethod.Request, CompleteMortgageLoanLeadMethod.Response>
    {
        public override string Path => "MortgageLoan/Complete-Lead";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var completionCode = Enums.Parse<CompletionCodeOption>(request.CompletionCode, ignoreCase: true);
            if (!completionCode.HasValue)
            {
                var options = Enums.GetAllValues<CompletionCodeOption>().Select(x => x.ToString()).ToArray();
                return Error($"Missing or invalid CompletionCode. Must be one of: {string.Join(", ", options)}", errorCode: "missingOrInvalidCompletionCode");
            }

            var r = requestContext.Resolver();

            var ai = r.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);

            if (!ai.IsActive || !ai.IsLead)
            {
                return Error("Application is not active or not a lead", errorCode: "notActiveOrNotALead");
            }

            var s = r.Resolve<IMortgageLoanLeadsWorkListService>();
            switch (completionCode)
            {
                case CompletionCodeOption.ChangeToQualifiedLead:
                    return new Response
                    {
                        WasChangedToQualifiedLead = s.TryChangeToQualifiedLead(ai)
                    };
                case CompletionCodeOption.Cancel:
                    return new Response
                    {
                        WasCancelled = s.TryCancelLead(ai)
                    };
                case CompletionCodeOption.Reject:
                    {
                        if (request.RejectionReasons == null || request.RejectionReasons.Count == 0)
                            return Error("Reject requires at least one rejection reason", errorCode: "rejectionReasonsMissing");
                        if (request.RejectionReasons.Contains("other") && string.IsNullOrWhiteSpace(request.RejectionReasonOtherText))
                            return Error("Rejection reason other requires RejectionReasonOtherText");
                        return new Response
                        {
                            WasRejected = s.TryRejectLead(ai, request.RejectionReasons, request.RejectionReasonOtherText)
                        };
                    }
                case CompletionCodeOption.TryLater:
                    {
                        if (!request.TryLaterDays.HasValue)
                            return Error("TryLater requires TryLaterDays", errorCode: "missingTryLaterDays");
                        return new Response
                        {
                            WasTryLaterScheduled = s.TryScheduleTryLater(ai, request.TryLaterDays.Value)
                        };
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public string CompletionCode { get; set; }

            public List<string> RejectionReasons { get; set; }
            public string RejectionReasonOtherText { get; set; }
            public int? TryLaterDays { get; set; }
        }

        public enum CompletionCodeOption
        {
            ChangeToQualifiedLead,
            Cancel,
            Reject,
            TryLater
        }

        public class Response
        {
            public bool WasChangedToQualifiedLead { get; set; }
            public bool WasCancelled { get; set; }
            public bool WasRejected { get; set; }
            public bool WasTryLaterScheduled { get; set; }
        }
    }
}