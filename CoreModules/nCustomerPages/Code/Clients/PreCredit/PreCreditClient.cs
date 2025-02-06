using Newtonsoft.Json;
using NTech.Services.Infrastructure;
using NTech.Services.Infrastructure.NTechWs;
using System;
using System.Collections.Generic;

namespace nCustomerPages.Code
{
    public class PreCreditClient : IPreCreditClient
    {
        private readonly Func<string> getBearerToken;
        public PreCreditClient(Func<string> getBearerToken)
        {
            this.getBearerToken = getBearerToken;
        }

        private NHttp.NHttpCall Begin(TimeSpan? timeout = null)
        {
            return NHttp
                .Begin(
                    NEnv.ServiceRegistry.Internal.ServiceRootUri("nPreCredit"),
                    getBearerToken(),
                    timeout ?? TimeSpan.FromSeconds(45));
        }

        public MortgageLoanAdditionalQuestionsStatusResponse GetMortgageLoanAdditionalQuestionsStatus(MortgageLoanAdditionalQuestionsStatusRequest request, bool? disableAutomation)
        {
            return Begin()
                .PostJson("api/mortageloan/fetch-additional-questions-status", new
                {
                    request = request,
                    disableAutomation = disableAutomation
                })
                .ParseJsonAs<MortgageLoanAdditionalQuestionsStatusResponse>();
        }

        public object GetMortgageLoanObject(string applicationNr)
        {
            return JsonConvert.DeserializeObject(Begin()
                .PostJson("api/MortgageLoan/Object/FetchInfo", new { applicationNr })
                .ParseAsRawJson());
        }

        public void AnswerMortgageLoanAdditionalQuestions(MortgageLoanAnswerAdditionalQuestionsRequest request, bool? disableAutomation)
        {
            Begin()
                .PostJson("api/mortageloan/answer-additional-questions", new
                {
                    request = request,
                    disableAutomation = disableAutomation
                })
                .EnsureSuccessStatusCode();
        }

        public CreditApplicationResponse CreateCreditApplication(CreditApplicationRequest request, bool? disableAutomation)
        {
            return Begin()
                .PostJson("api/creditapplication/create", new
                {
                    request = request,
                    disableAutomation = disableAutomation
                }).ParseJsonAs<CreditApplicationResponse>();
        }

        public MortgageLoanApplicationResponse CreateMortgageLoanApplication(MortgageLoanApplicationRequest request, bool? disableAutomation, bool skipDirectScoring)
        {
            return Begin()
                .PostJson("api/mortgageloan/create-application", new
                {
                    request = request,
                    disableAutomation = disableAutomation,
                    skipDirectScoring = skipDirectScoring
                }).HandlingApiError(x => new MortgageLoanApplicationResponse
                {
                    IsError = false,
                    SuccessData = x.ParseJsonAs<MortgageLoanApplicationResponse.SuccessResponse>()
                }, x => new MortgageLoanApplicationResponse
                {
                    IsError = true,
                    ErrorData = new MortgageLoanApplicationResponse.ErrorResponse
                    {
                        ErrorMessge = x.ErrorMessage,
                        IsDuplicateProviderApplicationId = x.ErrorCode == "duplicateProviderApplicationId"
                    }
                });
        }

        public MortgageLoanInitialScoringResponse ScoreMortgageLoanApplicationInitial(string applicationNr, bool skipProviderCallback, bool wasReportedDirectlyToProvider)
        {
            return Begin()
                .PostJson("api/MortgageLoan/CreditCheck/DoInitial", new { applicationNr = applicationNr, skipProviderCallback, wasReportedDirectlyToProvider })
                .ParseJsonAs<MortgageLoanInitialScoringResponse>();
        }

        public string StoreTemporarilyEncryptedData(string plaintextMessage, int? expireAfterHours)
        {
            return Begin().PostJson("Api/EncryptedTemporaryStorage/StoreString", new
            {
                plaintextMessage = plaintextMessage,
                expireAfterHours = expireAfterHours
            }).ParseJsonAsAnonymousType(new { compoundKey = (string)null })?.compoundKey;
        }

        public bool TryGetTemporarilyEncryptedData(string compoundKey, out string plaintextMessage, bool removeAfter = false)
        {
            var result = Begin().PostJson("Api/EncryptedTemporaryStorage/GetString", new
            {
                compoundKey = compoundKey,
                removeAfter = removeAfter
            }).ParseJsonAsAnonymousType(new { exists = (bool?)null, plaintextMessage = (string)null });

            if (result?.exists ?? false)
            {
                plaintextMessage = result?.plaintextMessage;
                return true;
            }
            else
            {
                plaintextMessage = null;
                return false;
            }
        }

        public ApplicationDocumentResponse GetApplicationDocument(string applicationNr, string externalId, string providerName, string documentType)
        {
            return Begin().PostJson("api/mortageloan/get-application-document", new
            {
                request = new
                {
                    applicationNr,
                    externalId,
                    providerName,
                    documentType
                }
            }).ParseJsonAs<ApplicationDocumentResponse>();
        }

        public MortgageLoanApplicationFinalCreditCheckStatusModel FetchMortgageLoanFinalStatus(string applicationNr)
        {
            return Begin().PostJson("api/MortgageLoan/CreditCheck/FetchFinalStatus", new { applicationNr }).ParseJsonAs<MortgageLoanApplicationFinalCreditCheckStatusModel>();
        }

        public CreateCompanyLoanApplicationResponse CreateCompanyLoanApplication(CreateCompanyLoanApplicationRequest request)
        {
            return Begin()
                .PostJson("api/CompanyLoan/Create-Application", request)
                .ParseJsonAs<CreateCompanyLoanApplicationResponse>();
        }

        public CompanyLoanStartAdditionalQuestionsStatusResponse GetCompanyLoanAdditionalQuestionsStatus(string applicationNr, string civicRegNr, bool returnCompanyInformation)
        {

            return Begin()
                .PostJson("api/CompanyLoan/Fetch-AdditionalQuestions-Status", new { applicationNr, verifyConnectedCivicRegNr = civicRegNr, returnCompanyInformation = returnCompanyInformation })
                .HandlingApiError(
                x => x.ParseJsonAs<CompanyLoanStartAdditionalQuestionsStatusResponse>(),
                x => { throw new NTechWebserviceMethodException(x.ErrorMessage) { ErrorCode = x.ErrorCode }; });
        }

        public void SubmitCompanyLoanAdditionalQuestions(Newtonsoft.Json.Linq.JObject request)
        {
            Begin()
                .PostJsonRaw("api/CompanyLoan/Submit-AdditionalQuestions", request.ToString())
                .EnsureSuccessStatusCode();
        }

        public class SubmitCompanyLoanAdditionalQuestionsRequest
        {
            public string ApplicationNr { get; set; }
            public string BankAccountNr { get; set; }
            public string BankAccountNrType { get; set; }
        }


        public List<Application> GetApplications(int customerId) =>
            Begin()
                .PostJson("api/LoanStandard/Applications/List-Active", new { customerId })
                .ParseJsonAsAnonymousType(new { Applications = new List<Application>() })
                ?.Applications ?? new List<Application>();

        public class Application
        {
            public string ApplicationNr { get; set; }
            public DateTimeOffset ApplicationDate { get; set; }
        }
    }
}