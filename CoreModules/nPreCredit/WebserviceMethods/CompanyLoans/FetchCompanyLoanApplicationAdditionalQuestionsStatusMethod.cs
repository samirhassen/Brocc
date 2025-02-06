using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.CompanyLoans;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.CompanyLoans
{
    public class FetchCompanyLoanApplicationAdditionalQuestionsStatusMethod : TypedWebserviceMethod<FetchCompanyLoanApplicationAdditionalQuestionsStatusMethod.Request, FetchCompanyLoanApplicationAdditionalQuestionsStatusMethod.Response>
    {
        public override string Path => "CompanyLoan/Fetch-AdditionalQuestions-Status";

        public override bool IsEnabled => NEnv.IsCompanyLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var resolver = requestContext.Resolver();

            var ai = resolver.Resolve<ApplicationInfoService>().GetApplicationInfo(request.ApplicationNr);

            if (ai == null)
                return Error("No such application", errorCode: "notFound");

            var repo = resolver.Resolve<IPartialCreditApplicationModelRepository>();

            var app = repo.Get(request.ApplicationNr, new PartialCreditApplicationModelRequest
            {
                ApplicationFields = new List<string> { "applicantCustomerId", "companyCustomerId", "additionalQuestionsAnswerDate" },
                ErrorIfGetNonLoadedField = true
            });

            Lazy<ICustomerClient> cc = new Lazy<ICustomerClient>(resolver.Resolve<ICustomerClient>);

            if (!string.IsNullOrWhiteSpace(request.VerifyConnectedCivicRegNr))
            {
                if (!resolver.Resolve<CivicRegNumberParser>().TryParse(request.VerifyConnectedCivicRegNr, out var cn))
                    return Error("Invalid civic regnr", errorCode: "invalidCivicRegNr");

                var customerId = cc.Value.GetCustomerId(cn);

                if (customerId != app.Application.Get("applicantCustomerId").IntValue.Required)
                    return Error("No such application", errorCode: "notFound"); //Dont leak anything about this existing since the person logging in is not part of the application
            }

            var additionalQuestionsAnswerDate = app.Application.Get("additionalQuestionsAnswerDate").StringValue.Optional ?? "pending";

            var isAllowedToAnswer = ai.IsActive
                && ai.CreditCheckStatus == CreditApplicationMarkerStatusName.Accepted
                && ai.AgreementStatus != CreditApplicationMarkerStatusName.Accepted
                && additionalQuestionsAnswerDate == "pending";

            Response.CompanyCompanyInformationModel companyInformationModel = null;
            if (request.ReturnCompanyInformation.GetValueOrDefault())
            {
                var companyCustomerId = app.Application.Get("companyCustomerId").IntValue.Required;
                var companyInfo = cc.Value.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { companyCustomerId }, "companyName", "orgnr")?.Opt(companyCustomerId);
                companyInformationModel = new Response.CompanyCompanyInformationModel
                {
                    Name = companyInfo?.Opt("companyName"),
                    Orgnr = companyInfo?.Opt("orgnr")
                };
            }

            var response = new Response
            {
                ApplicationNr = request.ApplicationNr,
                IsPendingAnswers = isAllowedToAnswer,
                CompanyInformation = companyInformationModel,
                AnsweredDate = additionalQuestionsAnswerDate != "pending" ? DateTime.ParseExact(additionalQuestionsAnswerDate, "o", CultureInfo.InvariantCulture) : new DateTime?()
            };

            using (var context = new PreCreditContext())
            {
                var h = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        x.CurrentCreditDecision
                    })
                    .SingleOrDefault();
                if (h != null)
                {
                    var acceptedOffer = h.CurrentCreditDecision as AcceptedCreditDecision;
                    if (acceptedOffer != null)
                    {
                        var decisionModel = CreditDecisionModelParser.ParseCompanyLoanCreditDecision(acceptedOffer.AcceptedDecisionModel);

                        response.AnswerableSinceDate = h.CurrentCreditDecision.DecisionDate.DateTime;
                        var sr = resolver.Resolve<IServiceRegistryUrlService>();

                        if (isAllowedToAnswer)
                            response.AdditionalQuestionsLink = sr.ServiceRegistry.ExternalServiceUrl("nCustomerPages", $"a/#/q-eid-login/{request.ApplicationNr}/start").ToString();

                        response.Offer = decisionModel?.GetExtendedOfferModel(NEnv.EnvSettings);
                    }
                }
            }

            return response;
        }

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public string VerifyConnectedCivicRegNr { get; set; }

            public bool? ReturnCompanyInformation { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }

            public bool IsPendingAnswers { get; set; }

            public DateTime? AnswerableSinceDate { get; set; }

            public string AdditionalQuestionsLink { get; set; }

            public DateTime? AnsweredDate { get; set; }

            public string AnswersDocumentKey { get; set; }

            public CompanyLoanExtendedOfferModel Offer { get; set; }

            public CompanyCompanyInformationModel CompanyInformation { get; set; }

            public class CompanyCompanyInformationModel
            {
                public string Orgnr { get; set; }
                public string Name { get; set; }
            }
        }
    }
}