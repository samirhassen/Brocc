using Newtonsoft.Json;
using nPreCredit.Code.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CustomerClient = nPreCredit.Code.PreCreditCustomerClient;

namespace nPreCredit.WebserviceMethods.MortgageLoanStandard
{
    public class AnswerKycQuestionsForCustomerPagesMethod : TypedWebserviceMethod<AnswerKycQuestionsForCustomerPagesMethod.Request, AnswerKycQuestionsForCustomerPagesMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/CustomerPages/Answer-Kyc-Questions";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            using (var context = new PreCreditContextExtended(requestContext.CurrentUserMetadata(), NTech.ClockFactory.SharedInstance))
            {
                var customerId = request.CustomerId.Value;

                var infoService = requestContext.Resolver().Resolve<ApplicationInfoService>();
                var ai = infoService.GetApplicationInfo(request.ApplicationNr, true);
                if (ai == null)
                    return Error("No such application exists");

                var complexListsToGet = new string[] { "Application" };
                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        ComplexApplicationListItems = x.ComplexApplicationListItems.Where(y => complexListsToGet.Contains(y.ListName)),
                        Customers = x.CustomerListMemberships.Where(y => FetchLoanApplicationKycStatusForCustomerPagesMethod.CustomerTypesThatShouldAnswerKycQuestions.Contains(y.ListName)).Select(y => new { y.ListName, y.CustomerId })
                    })
                    .Single();

                if (!application.Customers.Any(x => x.CustomerId == customerId))
                    return Error("No such application exists");

                if (request.Customers.Any(x => !application.Customers.Any(y => y.CustomerId == x.CustomerId)))
                    return Error("No such application exists");

                var applicationRow = ComplexApplicationList.CreateListFromFlattenedItems("Application", application.ComplexApplicationListItems.ToList()).GetRow(1, true);

                var hasAnsweredKycQuestions = applicationRow.GetUniqueItemBoolean("hasAnsweredKycQuestions") ?? false;

                if (!FetchLoanApplicationKycStatusForCustomerPagesMethod.IsPossibleToAnswerKycQuestions(ai, hasAnsweredKycQuestions, requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>()))
                    return Error("Answers cannot be changed at this time");

                var questionSets = new List<CustomerQuestionsSet>();
                foreach (var customer in request.Customers)
                {
                    questionSets.Add(new CustomerQuestionsSet
                    {
                        AnswerDate = requestContext.Clock().Now.DateTime,
                        CustomerId = customer.CustomerId,
                        Items = customer.Answers.Select(x => new CustomerQuestionsSetItem
                        {
                            AnswerCode = x.AnswerCode,
                            AnswerText = x.AnswerText,
                            QuestionCode = x.QuestionCode,
                            QuestionText = x.QuestionText
                        }).ToList()
                    });
                }
                if (questionSets.Count > 0)
                {
                    new CustomerClient().AddCustomerQuestionsSetsBatch(questionSets, "MortgageLoanApplication", request.ApplicationNr);
                    ComplexApplicationListService.SetSingleUniqueItem(request.ApplicationNr, "Application", "hasAnsweredKycQuestions", 1, "true", context);
                    context.SaveChanges();
                }

                return new Response
                {

                };
            }
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }

            [Required]
            public List<CustomerModel> Customers { get; set; }
            public class CustomerModel
            {
                public int CustomerId { get; set; }
                public List<QuestionModel> Answers { get; set; }
            }

            public class QuestionModel
            {
                public string QuestionCode { get; set; }
                public string AnswerCode { get; set; }
                public string QuestionText { get; set; }
                public string AnswerText { get; set; }
            }
        }

        public class Response
        {

        }
    }
}