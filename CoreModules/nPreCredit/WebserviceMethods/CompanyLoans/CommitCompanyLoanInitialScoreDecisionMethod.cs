using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Services.Infrastructure.NTechWs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{

    public class CommitCompanyLoanInitialScoreDecisionMethod : TypedWebserviceMethod<CommitCompanyLoanInitialScoreDecisionMethod.Request, CommitCompanyLoanInitialScoreDecisionMethod.Response>
    {
        public override string Path => "CompanyLoan/Commit-InitialScore-Decision";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            if (request.IsAccepted.Value && request.AcceptedOffer == null)
                return Error("Missing AcceptedOffer which is needed when IsAccepted = true", errorCode: "missingOffer");
            if (!request.IsAccepted.Value && request.RejectionReasons == null)
                return Error("Missing RejectionReasons which is needed when IsAccepted = false", errorCode: "missingRejectionReasons");

            var resolver = requestContext.Resolver();

            if (!resolver.Resolve<IEncryptedTemporaryStorageService>().TryGetString(request.ScoreResultStorageKey, out var storedData))
                return Error("Stored scoring result expired", errorCode: "scoringResultExpired");

            var storedRecommendation = JsonConvert.DeserializeObject<CompanyLoanInitialCreditDecisionRecommendationModel>(storedData);

            if (request.ApplicationNr != storedRecommendation.ApplicationNr)
            {
                return Error("Stored scoring result is for another application", errorCode: "scoringResultWrongApplication");
            }

            var cs = resolver.Resolve<ICompanyLoanCreditCheckService>();
            var decision = request.IsAccepted.Value
                ? cs.AcceptInitialCreditDecision(storedRecommendation, request.AcceptedOffer, request.WasAutomated)
                : cs.RejectInitialCreditDecision(storedRecommendation, request.RejectionReasons, request.WasAutomated, request.SupressUserNotification);

            return new Response
            {
                CreditDecisionId = decision.Id
            };
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public string ScoreResultStorageKey { get; set; }

            [Required]
            public bool? IsAccepted { get; set; }

            public CompanyLoanInitialCreditDecisionRecommendationModel.OfferModel AcceptedOffer { get; set; }

            public List<string> RejectionReasons { get; set; }

            public bool WasAutomated { get; set; }
            public bool SupressUserNotification { get; set; }
        }

        public class Response
        {
            public int CreditDecisionId { get; set; }
        }
    }
}