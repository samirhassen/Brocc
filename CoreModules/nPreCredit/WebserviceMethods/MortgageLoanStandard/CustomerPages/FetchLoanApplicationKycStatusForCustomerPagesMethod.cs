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
    public class FetchLoanApplicationKycStatusForCustomerPagesMethod : TypedWebserviceMethod<FetchLoanApplicationKycStatusForCustomerPagesMethod.Request, FetchLoanApplicationKycStatusForCustomerPagesMethod.Response>
    {
        public override string Path => "MortgageLoanStandard/CustomerPages/Fetch-Application-KycStatus";

        public override bool IsEnabled => NEnv.IsStandardMortgageLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var customerClient = new PreCreditCustomerClient();

            using (var context = new PreCreditContext())
            {
                var customerId = request.CustomerId.Value;

                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.CustomerListMemberships.Any(y => CustomerTypesThatShouldAnswerKycQuestions.Contains(y.ListName) && y.CustomerId == customerId) && !x.ArchivedDate.HasValue)
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => new
                    {
                        Customers = x.CustomerListMemberships.Where(y => CustomerTypesThatShouldAnswerKycQuestions.Contains(y.ListName)).Select(y => new { y.ListName, y.CustomerId }),
                    })
                    .SingleOrDefault();

                var customerIdsWithRoles = application
                    .Customers
                    .GroupBy(x => x.CustomerId)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.ListName).ToList());

                if (application == null)
                    return Error("Not found", errorCode: "noSuchApplicationExists");

                var applicationInfoService = requestContext.Resolver().Resolve<ApplicationInfoService>();

                var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr);
                var applicants = applicationInfoService.GetApplicationApplicants(request.ApplicationNr);

                var workflowService = requestContext.Resolver().Resolve<IMortgageLoanStandardWorkflowService>();

                return CreateResponse(ai, customerClient, customerIdsWithRoles, applicants, workflowService);
            }
        }

        private Response CreateResponse(ApplicationInfoModel ai, PreCreditCustomerClient customerClient, Dictionary<int, List<string>> customerIdsWithRoles, ApplicationApplicantsModel applicants, IMortgageLoanStandardWorkflowService workflowService)
        {
            if (!workflowService.AreAllStepsBeforeComplete(MortgageLoanStandardWorkflowService.WaitingForAdditionalInfoStep.Name, ai.ListNames))
                return new Response { IsActive = false };

            var documentClient = new nDocumentClient();
            var kycStatusByCustomerId = customerClient.FetchCustomerOnboardingStatuses(customerIdsWithRoles.Keys.ToHashSet(), "MortgageLoanApplication", ai.ApplicationNr, true);

            var customers = new List<Response.KycCustomerModel>();
            foreach (var customerId in customerIdsWithRoles.Keys)
            {
                var customerKycStatus = kycStatusByCustomerId[customerId];

                var customerApplicants = applicants.CustomerIdByApplicantNr.Where(x => x.Value == customerId);
                customers.Add(new Response.KycCustomerModel
                {
                    CustomerId = customerId,
                    ApplicantNr = customerApplicants.Any() ? customerApplicants.Single().Key : new int?(),
                    CustomerBirthDate = customerKycStatus.CustomerBirthDate,
                    CustomerShortName = customerKycStatus.CustomerShortName,
                    LatestKycQuestionsAnswerDate = customerKycStatus.LatestKycQuestionsAnswerDate,
                    LatestQuestions = customerKycStatus?.LatestKycQuestionsSet?.Items?.Select(y => new Response.KycQuestionModel
                    {
                        AnswerCode = y.AnswerCode,
                        QuestionCode = y.QuestionCode,
                        AnswerText = y.AnswerText,
                        QuestionText = y.QuestionText
                    }).ToList()
                });
            }
            var answersExist = customers.All(x => x.LatestKycQuestionsAnswerDate.HasValue);
            var isAccepted = answersExist || workflowService.IsStepStatusAccepted(MortgageLoanStandardWorkflowService.KycStep.Name, ai.ListNames);

            return new Response
            {
                IsActive = true,
                IsAccepted = ToTriStateBool(isAccepted, false),
                IsPossibleToAnswer = IsPossibleToAnswerKycQuestions(ai, answersExist, workflowService),
                IsAnswersApproved = ToTriStateBool(answersExist, false),
                Customers = customers
            };
        }

        private static bool? ToTriStateBool(bool isAccepted, bool isRejected) => isAccepted ? true : (isRejected ? false : new bool?());

        public static bool IsPossibleToAnswerKycQuestions(ApplicationInfoModel ai, bool answersExist, IMortgageLoanStandardWorkflowService workflowService)
        {
            if (!ai.IsActive)
                return false;

            if (workflowService.IsStepStatusAccepted(MortgageLoanStandardWorkflowService.KycStep.Name, ai.ListNames))
                return false;

            if (answersExist) //Dont allow edit answer. This could probably be relaxed if desired.
                return false;

            return true;
        }

        public static HashSet<string> CustomerTypesThatShouldAnswerKycQuestions = new HashSet<string> { "Applicant" };

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public string ApplicationNr { get; set; }
            public bool IsActive { get; set; }
            public bool? IsAccepted { get; set; }
            public bool IsPossibleToAnswer { get; set; }
            public bool? IsAnswersApproved { get; set; }
            public List<KycCustomerModel> Customers { get; set; }
            public class KycCustomerModel
            {
                public int CustomerId { get; set; }
                public int? ApplicantNr { get; set; }
                public string CustomerBirthDate { get; set; }
                public string CustomerShortName { get; set; }
                public DateTime? LatestKycQuestionsAnswerDate { get; set; }
                public List<KycQuestionModel> LatestQuestions { get; set; }
            }

            public class KycQuestionModel
            {
                public string QuestionCode { get; set; }
                public string AnswerCode { get; set; }
                public string QuestionText { get; set; }
                public string AnswerText { get; set; }
            }
        }
    }
}