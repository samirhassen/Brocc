using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nPreCredit.WebserviceMethods
{
    public class FetchAdditionalQuestionsApplicationStatusMethod : TypedWebserviceMethod<FetchAdditionalQuestionsApplicationStatusMethod.Request, FetchAdditionalQuestionsApplicationStatusMethod.Response>
    {
        public override string Path => "AdditionalQuestions/FetchApplicationStatus";

        public override bool IsEnabled => !NEnv.IsMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            Validate(request, r =>
            {
                r.Require(x => x.ApplicationNr);
            });

            if (!NEnv.IsUnsecuredLoansEnabled)
                throw new Exception("Unsecured loans are not enabled"); //Not 404 like we usually do since we are highly likely to hit this alot during refactoring.

            Func<IEnumerable<CreditApplicationOneTimeToken>, bool, DateTimeOffset?, dynamic> additionalQuestionsStatus = (tokens, canSkipAdditionalQuestions, latestAnswerDate) =>
            {
                var t = tokens
                   ?.OrderByDescending(x => x.CreationDate)
                   .FirstOrDefault();
                return OneTimeTokenToAdditionlQuestionsStatus(t, canSkipAdditionalQuestions, latestAnswerDate);
            };

            using (var context = new PreCreditContext())
            {
                IList<CreditApplicationOneTimeToken> currentlyPendingSignAgreementTokens = null;

                var app = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        x.CanSkipAdditionalQuestions,
                        AdditionalQuestionTokens = x.OneTimeTokens.Where(y => y.TokenType == "AdditionalQuestions"),
                        LatestAdditionalQuestionsAnswerDate = x
                            .Items
                            .Where(y => y.AddedInStepName == "AdditionalQuestions")
                            .OrderByDescending(y => y.ChangedDate)
                            .Select(y => (DateTimeOffset?)y.ChangedDate)
                            .FirstOrDefault(),
                        SignInitialCreditAgreements = x.OneTimeTokens.Where(y => y.TokenType == "SignInitialCreditAgreement" && !y.RemovedBy.HasValue),
                    })
                    .ToList()
                    .SingleOrDefault();

                if (app == null)
                    return Error("No such application exists", httpStatusCode: 400, errorCode: "notFound");

                var applicationInfo = requestContext.Resolver().Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);

                var handler = DependancyInjection.Services.Resolve<ICreditApplicationTypeHandler>();

                return new Response
                {
                    ApplicationNr = request.ApplicationNr,
                    AdditionalQuestionsStatus = additionalQuestionsStatus(app.AdditionalQuestionTokens, app.CanSkipAdditionalQuestions, app.LatestAdditionalQuestionsAnswerDate),
                    AgreementSigningStatus = handler.GetAgreementSigningStatusWithPending(request.ApplicationNr, app.SignInitialCreditAgreements, out currentlyPendingSignAgreementTokens)
                };
            }
        }

        public class AdditionalQuestionsStatusModel
        {
            //NOTE: Lowercase names to allow it to work with legacy code
            public DateTimeOffset? sentDate { get; set; }
            public bool hasAnswered { get; set; }
            public DateTimeOffset? latestAnswerDate { get; set; }
            public bool canSkipAdditionalQuestions { get; set; }
        }

        private AdditionalQuestionsStatusModel OneTimeTokenToAdditionlQuestionsStatus(CreditApplicationOneTimeToken token, bool canSkipAdditionalQuestions, DateTimeOffset? latestAnswerDate)
        {
            var extraData = token == null || token.TokenExtraData == null ? null : JsonConvert.DeserializeAnonymousType(token.TokenExtraData, new { hasAnswered = (bool?)null });
            return new AdditionalQuestionsStatusModel
            {
                sentDate = token?.CreationDate,
                hasAnswered = latestAnswerDate.HasValue || (extraData?.hasAnswered ?? false),
                latestAnswerDate = latestAnswerDate,
                canSkipAdditionalQuestions = canSkipAdditionalQuestions
            };
        }

        public class Request
        {
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public AgreementSigningStatusWithPending AgreementSigningStatus { get; set; }
            public AdditionalQuestionsStatusModel AdditionalQuestionsStatus { get; set; }
        }
    }
}