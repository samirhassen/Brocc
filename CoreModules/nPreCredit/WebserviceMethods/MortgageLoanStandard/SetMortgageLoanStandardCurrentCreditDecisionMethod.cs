using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class SetMortgageLoanStandardCurrentCreditDecisionMethod : TypedWebserviceMethod<SetMortgageLoanStandardCurrentCreditDecisionMethod.Request, SetMortgageLoanStandardCurrentCreditDecisionMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/Set-Current-CreditDecision";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;
        public override IEnumerable<string> LimitAccessToGroupNames => Enumerables.Singleton("Middle");

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.InitialOffer == null && request.Rejection == null && request.FinalOffer == null)
                return Error("Exactly one of Offer and Rejection must be set", errorCode: "missingOfferOrRejection");

            var resolver = requestContext.Resolver();

            var cs = resolver.Resolve<MortgageLoanStandardCreditCheckService>();

            MortgageLoanStandardCreditRecommendationModel recommendation = null;
            if (!string.IsNullOrWhiteSpace(request.RecommendationTemporaryStorageKey))
            {
                var tempStorage = resolver.Resolve<IEncryptedTemporaryStorageService>();
                if (!tempStorage.TryGetString(request.RecommendationTemporaryStorageKey, out var recommendationRaw))
                    return Error("Recommendation does not exist", errorCode: "missingRecommendation", httpStatusCode: 400);
                recommendation = MortgageLoanStandardCreditRecommendationModel.ParseJson(recommendationRaw);
            }
            int decisionId;
            if (request.Rejection != null)
            {
                decisionId = cs.RejectCreditDecision(request.ApplicationNr, request.Rejection, request.WasAutomated, request.SupressUserNotification, recommendation).Id;
            }
            else if (request.InitialOffer != null)
            {
                decisionId = cs.AcceptInitialCreditDecision(request.ApplicationNr, request.InitialOffer, request.WasAutomated, request.SupressUserNotification, recommendation).Id;
            }
            else if (request.FinalOffer != null)
            {
                decisionId = cs.AcceptFinalCreditDecision(request.ApplicationNr, request.FinalOffer, request.WasAutomated, request.SupressUserNotification, recommendation).Id;
            }
            else
                throw new NotImplementedException();

            return new Response
            {
                CreditDecisionId = decisionId
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public InitialOfferModel InitialOffer { get; set; }
            public RejectionModel Rejection { get; set; }
            public FinalOfferModel FinalOffer { get; set; }

            public bool WasAutomated { get; set; }
            public bool SupressUserNotification { get; set; }
            public string RecommendationTemporaryStorageKey { get; set; }

            public class InitialOfferModel
            {
                public bool? IsPurchase { get; set; }
                public decimal? ObjectPriceAmount { get; set; }
                public decimal? PaidToCustomerAmount { get; set; }
                public decimal? OwnSavingsAmount { get; set; }
                public decimal? SettlementAmount { get; set; }
            }
            public class FinalOfferModel
            {
            }

            public class RejectionModel
            {
                [Required]
                public List<RejectionReasonModel> RejectionReasons { get; set; }
                public string OtherText { get; set; }
                public bool IsFinal { get; set; }
            }

            public class RejectionReasonModel
            {
                public string Code { get; set; }
                public string DisplayName { get; set; }
            }
        }

        public class Response
        {
            public int CreditDecisionId { get; set; }
        }
    }
}