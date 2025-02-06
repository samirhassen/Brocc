using Newtonsoft.Json;
using nPreCredit.Code.Services;
using nPreCredit.Code.Services.MortgageLoans;
using NTech.Banking.CivicRegNumbers;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace nPreCredit.WebserviceMethods.MortgageLoans
{
    public class FetchMortgageLoanApplicationAdditionalQuestionsStatusMethod : TypedWebserviceMethod<FetchMortgageLoanApplicationAdditionalQuestionsStatusMethod.Request, FetchMortgageLoanApplicationAdditionalQuestionsStatusMethod.Response>
    {
        public override string Path => "MortgageLoan/Fetch-AdditionalQuestions-Status";

        public override bool IsEnabled => NEnv.IsOnlyNonStandardMortgageLoansEnabled;

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
                ApplicationFields = new List<string> { "additionalQuestionsAnswerDate" },
                ApplicantFields = new List<string> { "customerId" },
                ErrorIfGetNonLoadedField = true
            });

            Lazy<ICustomerClient> cc = new Lazy<ICustomerClient>(resolver.Resolve<ICustomerClient>);
            var customerIdByApplicantNr = new Dictionary<int, int>();
            app.DoForEachApplicant(applicantNr =>
            {
                customerIdByApplicantNr[applicantNr] = app.Applicant(applicantNr).Get("customerId").IntValue.Required;
            });

            if (!string.IsNullOrWhiteSpace(request.VerifyConnectedCivicRegNr))
            {
                if (!resolver.Resolve<CivicRegNumberParser>().TryParse(request.VerifyConnectedCivicRegNr, out var cn))
                    return Error("Invalid civic regnr", errorCode: "invalidCivicRegNr");
                var customerId = cc.Value.GetCustomerId(cn);

                if (!customerIdByApplicantNr.Values.Contains(customerId))
                    return Error("No such application", errorCode: "notFound"); //Dont leak anything about this existing since the person logging in is not part of the application
            }

            var additionalQuestionsAnswerDate = app.Application.Get("additionalQuestionsAnswerDate").StringValue.Optional ?? "pending";

            Dictionary<int, MortgageLoanDualAgreementService.CustomerModel> customers;
            using (var context = new PreCreditContext())
            {
                customers = MortgageLoanDualAgreementService.GetCustomersComposable(context, ai, customerIdByApplicantNr, resolver.Resolve<ICustomerClient>());
            }

            //Ordering here is just to put the applicants first in order and then the rest in basically any predictable order.
            //This is to reduce the risk of the consumer forgetting to think about the order and just assuming the first two are the applicants in order.
            var customersData = customers.Values.OrderBy(x => x.IsApplicant ? x.ApplicantNr.Value : (1000 + x.CustomerId)).Select(x => new Response.CustomerModel
            {
                IsApplicant = x.IsApplicant,
                ApplicantNr = x.ApplicantNr,
                IsMainApplicationObjectOwner = x.IsMainApplicationObjectOwner,
                IsOtherApplicationObjectOwner = x.IsOtherApplicationObjectOwner,
                CustomerId = x.CustomerId,
                FirstName = x.FirstName,
                BirthDate = x.BirthDate?.ToString("yyyy-MM-dd"),
                HasSignedApplication = x.HasSignedApplicationAndPoaDocument,
                OwnedCollaterals = x.OwnedCollateralsByNr
                       .Values
                       .OrderBy(y => y.Nr)
                       .Select(y => new Response.CollateralModel
                       {
                           Nr = y.Nr,
                           IsEstate = y.IsEstate,
                           IsHousingCompany = y.IsHousingCompany,
                           IsMainCollateral = y.IsMainCollateral,
                           CustomerIds = y.CustomerIds.ToList()
                       })
                       .ToList()
            }).ToList();

            var response = new Response
            {
                ApplicationNr = request.ApplicationNr,
                AnsweredDate = additionalQuestionsAnswerDate != "pending" ? DateTime.ParseExact(additionalQuestionsAnswerDate, "o", CultureInfo.InvariantCulture) : new DateTime?(),
                Customers = customersData
            };

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), requestContext.Clock()))
            {
                var h = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        HasDecision = x.CurrentCreditDecisionId.HasValue,
                        IsAccepted = (x.CurrentCreditDecision as AcceptedCreditDecision) != null,
                        CurrentCreditDecisionItems = x
                            .CurrentCreditDecision
                            .DecisionItems
                            .Select(y => new
                            {
                                y.Id,
                                y.IsRepeatable,
                                y.ItemName,
                                y.Value
                            }),
                        ConsumerBankAccountNr = x.Items.Where(y => y.GroupName == "application" && y.Name == "ConsumerBankAccountNr" && !y.IsEncrypted)
                    })
                    .SingleOrDefault();

                if (h.HasDecision && h.IsAccepted)
                {
                    response.AcceptedDecision = new Response.AcceptedDecisionModel
                    {
                        UniqueItems = h
                            .CurrentCreditDecisionItems
                            .Where(x => !x.IsRepeatable)
                            .GroupBy(x => x.ItemName)
                            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Id).Select(y => y.Value).First()),
                        RepeatableItems = h
                            .CurrentCreditDecisionItems
                            .Where(x => x.IsRepeatable)
                            .GroupBy(x => x.ItemName)
                            .ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList()),
                    };

                    var couldBePending = ai.IsActive
                        && h.IsAccepted
                        && h.HasDecision;

                    response.IsPendingAnswers = couldBePending
                        && additionalQuestionsAnswerDate == "pending";

                    response.IsPendingSignatures = couldBePending && customersData.Any(x => x.IsApplicant && !x.HasSignedApplication);

                    if (response.IsPendingSignatures)
                    {
                        var applicantCustomers = response.Customers.Where(x => x.IsApplicant).ToList();
                        if (applicantCustomers.Any(x => !x.HasSignedApplication))
                        {
                            var model = FetchMortgageLoanDualAgreementSignatureStatusMethod.EnsureTokens(request.ApplicationNr, ApplicationSignatureTokenListName, applicantCustomers.Select(x => x.CustomerId).ToHashSet(), context);
                            foreach (var a in applicantCustomers)
                            {
                                a.SignApplicationToken = model.SignatureTokenByCustomerId[a.CustomerId];
                            }
                        }
                    }
                }
            }

            return response;
        }

        public const string ApplicationSignatureTokenListName = "ApplicationAndPoaSignatureTokens";

        public class Request
        {
            [Required]
            public string ApplicationNr { get; set; }

            public string VerifyConnectedCivicRegNr { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }

            public bool IsPendingAnswers { get; set; }
            public bool IsPendingSignatures { get; set; }
            public string ConsumerBankAccountNr { get; set; }

            public DateTime? AnsweredDate { get; set; }

            public AcceptedDecisionModel AcceptedDecision { get; set; }
            public List<CustomerModel> Customers { get; set; }

            public class AcceptedDecisionModel
            {
                public Dictionary<string, string> UniqueItems { get; set; }
                public Dictionary<string, List<string>> RepeatableItems { get; set; }
            }

            public class CustomerModel
            {
                public bool IsApplicant { get; set; }
                public int? ApplicantNr { get; set; }
                public bool IsMainApplicationObjectOwner { get; set; }
                public bool IsOtherApplicationObjectOwner { get; set; }
                public int CustomerId { get; set; }
                public string FirstName { get; set; }
                public string BirthDate { get; set; }
                public bool HasSignedApplication { get; set; }
                public string SignApplicationToken { get; set; }
                internal List<CollateralModel> OwnedCollaterals { get; set; }
            }

            internal class CollateralModel
            {
                public int Nr { get; set; }
                public bool IsEstate { get; set; }
                public bool IsHousingCompany { get; set; }
                public bool IsMainCollateral { get; set; }
                public List<int> CustomerIds { get; set; }
            }
        }
    }
}