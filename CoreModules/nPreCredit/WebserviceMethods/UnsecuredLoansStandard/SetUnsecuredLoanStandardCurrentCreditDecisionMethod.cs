using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class SetUnsecuredLoanStandardCurrentCreditDecisionMethod : TypedWebserviceMethod<SetUnsecuredLoanStandardCurrentCreditDecisionMethod.Request, SetUnsecuredLoanStandardCurrentCreditDecisionMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/Set-Current-CreditDecision";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if ((request.Offer == null) == (request.Rejection == null))
                return Error("Exactly one of Offer and Rejection must be set", errorCode: "missingOfferOrRejection");

            var resolver = requestContext.Resolver();

            var cs = resolver.Resolve<CreditRecommendationUlStandardService>();

            if (cs.HasActiveCustomerDecision(request.ApplicationNr))
                return Error("customerDecisionCode is accepted or rejected. Use api/UnsecuredLoanStandard/Set-Customer-CreditDecisionCode to change back to initial first.", errorCode: "customerDecisionMustBeInitial", httpStatusCode: 400);

            UnsecuredLoanStandardCreditRecommendationModel recommendation = null;
            if (!string.IsNullOrWhiteSpace(request.RecommendationTemporaryStorageKey))
            {
                var tempStorage = resolver.Resolve<IEncryptedTemporaryStorageService>();
                if (!tempStorage.TryGetString(request.RecommendationTemporaryStorageKey, out var recommendationRaw))
                    return Error("Recommendation does not exist", errorCode: "missingRecommendation", httpStatusCode: 400);
                recommendation = UnsecuredLoanStandardCreditRecommendationModel.ParseJson(recommendationRaw);
            }

            var decision = request.Offer != null
                ? cs.AcceptInitialCreditDecision(request.ApplicationNr, request.Offer, request.WasAutomated, request.SupressUserNotification, recommendation)
                : cs.RejectInitialCreditDecision(request.ApplicationNr, request.Rejection, request.WasAutomated, request.SupressUserNotification, recommendation);

            return new Response
            {
                CreditDecisionId = decision.Id
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public UnsecuredLoanStandardCurrentCreditDecisionOfferModel Offer { get; set; }
            public UnsecuredLoanStandardCurrentCreditDecisionRejectionModel Rejection { get; set; }

            public bool WasAutomated { get; set; }
            public bool SupressUserNotification { get; set; }
            public string RecommendationTemporaryStorageKey { get; set; }
        }

        public class Response
        {
            public int CreditDecisionId { get; set; }
        }
    }
}