using Newtonsoft.Json;
using nPreCredit.Code;
using nPreCredit.Code.ElectronicSignatures;
using nPreCredit.Code.Services;
using NTech;
using NTech.Banking.BankAccounts;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Services.Infrastructure.CreditStandard;
using NTech.Services.Infrastructure.NTechWs;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PreCreditCustomerClient = nPreCredit.Code.PreCreditCustomerClient;

namespace nPreCredit.WebserviceMethods.UnsecuredLoansStandard
{
    public class FetchUnsecuredLoanApplicationForCustomerPagesMethod : TypedWebserviceMethod<FetchUnsecuredLoanApplicationForCustomerPagesMethod.Request, FetchUnsecuredLoanApplicationForCustomerPagesMethod.Response>
    {
        public override string Path => "UnsecuredLoanStandard/CustomerPages/Fetch-Application";

        public override bool IsEnabled => NEnv.IsStandardUnsecuredLoansEnabled;

        protected override Response DoExecuteTyped(NTechWebserviceMethodRequestContext requestContext, Request request)
        {
            ValidateUsingAnnotations(request);

            var customerClient = new PreCreditCustomerClient();
            var settings = customerClient.LoadSettings("enableDisableSecureMessages");

            using (var context = new PreCreditContext())
            {
                var customerId = request.CustomerId.Value;

                var complexListsToGet = new string[] { "AgreementSignatureSession", "LoansToSettle", "Application", "DirectDebitSigningSession" };
                var signedAgreementDocumentType = CreditApplicationDocumentTypeCode.SignedAgreement.ToString();

                var application = context
                    .CreditApplicationHeaders
                    .Where(x => x.ApplicationNr == request.ApplicationNr && x.CustomerListMemberships.Any(y => y.ListName == "Applicant" && y.CustomerId == customerId) && !x.ArchivedDate.HasValue)
                    .OrderBy(x => x.ApplicationDate)
                    .ThenBy(x => x.ApplicationNr)
                    .Select(x => new
                    {
                        CreditNr = x
                            .ComplexApplicationListItems
                            .Where(y => y.ListName == "Application" && y.ItemName == "creditNr" && !y.IsRepeatable && y.Nr == 1 && x.IsFinalDecisionMade)
                            .Select(y => y.ItemValue)
                            .FirstOrDefault(),
                        CurrentCreditDecisionItems = x.CurrentCreditDecision.DecisionItems,
                        ComplexApplicationListItems = x.ComplexApplicationListItems.Where(y => complexListsToGet.Contains(y.ListName)),
                        SignedAgreementArchiveKey = x.Documents
                            .Where(y => !y.RemovedByUserId.HasValue && y.DocumentType == signedAgreementDocumentType)
                            .OrderByDescending(y => y.Id)
                            .Select(y => y.DocumentArchiveKey)
                            .FirstOrDefault()
                    })
                    .SingleOrDefault();

                if (application == null)
                    return Error("Not found", errorCode: "noSuchApplicationExists");

                var applicationInfoService = requestContext.Resolver().Resolve<ApplicationInfoService>();

                var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr);
                Lazy<ApplicationApplicantsModel> applicants = new Lazy<ApplicationApplicantsModel>(() => applicationInfoService.GetApplicationApplicants(request.ApplicationNr));

                var workflowService = requestContext.Resolver().Resolve<UnsecuredLoanStandardWorkflowService>();

                var response = new Response.ApplicationModel
                {
                    ApplicationNr = ai.ApplicationNr,
                    ApplicationDate = ai.ApplicationDate,
                    IsActive = ai.IsActive,
                    CreditNr = application.CreditNr,
                    IsCancelled = ai.IsCancelled,
                    IsFinalDecisionMade = ai.IsFinalDecisionMade,
                    IsRejected = ai.IsRejected
                };

                response.CurrentOffer = HandleOffer(workflowService, ai, CreditDecisionItemsToRow(application.CurrentCreditDecisionItems));
                if (response.CurrentOffer == null)
                {
                    response.IsFutureOfferPossible = ai.IsActive;
                }

                response.BankAccountsTask = CreateBankAccountsResponse(ai,
                    application.ComplexApplicationListItems.ToList(), application.CurrentCreditDecisionItems);

                response.KycTask = HandleKyc(ai, customerClient, applicants, application.CurrentCreditDecisionItems, workflowService);

                response.AgreementTask = HandleAgreement(workflowService, ai, customerClient, applicants,
                    application.ComplexApplicationListItems.ToList(), application.SignedAgreementArchiveKey, requestContext.Clock());

                response.DirectDebitTask = GetDirectDebitAccountTaskModel(applicants, application.ComplexApplicationListItems.ToList(), NEnv.ClientCfgCore);

                response.Enums = CreditStandardEnumService.Instance.GetApiEnums(language: NEnv.ClientCfg.Country.GetBaseLanguage());

                response.IsInactiveMessagingAllowed = settings["isInactiveMessagingAllowed"] == "true"; ;

                return new Response
                {
                    Application = response
                };
            }
        }

        private static ComplexApplicationList.Row CreditDecisionItemsToRow(IEnumerable<CreditDecisionItem> creditDecisionItems) =>
            ComplexApplicationList.CreateListFromFlattenedItems("FakeDecisionList", creditDecisionItems.Select(x => new ComplexApplicationListItemBase
            {
                ListName = "FakeDecisionList",
                Nr = 1,
                IsRepeatable = x.IsRepeatable,
                ItemName = x.ItemName,
                ItemValue = x.Value
            }).ToList()).GetRow(1, true);

        private Response.CurrentOfferModel HandleOffer(UnsecuredLoanStandardWorkflowService workflowService, ApplicationInfoModel ai,
            ComplexApplicationList.Row currentCreditDecisionRow)
        {
            if (currentCreditDecisionRow.GetUniqueItemNames().Any())
            {
                if (currentCreditDecisionRow.GetUniqueItemBoolean("isOffer") == true)
                {
                    var offerInitialListName = workflowService.GetListName(UnsecuredLoanStandardWorkflowService.CustomerOfferDecisionStep.Name, workflowService.InitialStatusName);
                    var currentStepName = workflowService.GetCurrentListName(ai.ListNames);
                    var isPossibleToDecide = ai.IsActive
                        && currentStepName == offerInitialListName
                        && currentCreditDecisionRow.GetUniqueItem("customerDecisionCode") == "initial";
                    return new Response.CurrentOfferModel
                    {
                        OfferItems = currentCreditDecisionRow.GetUniqueItemNames().ToDictionary(x => x, x => currentCreditDecisionRow.GetUniqueItem(x)),
                        IsPossibleToDecide = isPossibleToDecide
                    };
                }
            }
            return null;
        }

        private Response.KycModel HandleKyc(ApplicationInfoModel ai, PreCreditCustomerClient customerClient, Lazy<ApplicationApplicantsModel> applicants, IEnumerable<CreditDecisionItem> creditDecisionItems, UnsecuredLoanStandardWorkflowService workflowService)
        {
            var currentCreditDecisionRow = CreditDecisionItemsToRow(creditDecisionItems);
            var hasAcceptedOffer = currentCreditDecisionRow.GetUniqueItem("customerDecisionCode") == "accepted";

            if (!hasAcceptedOffer)
                return new Response.KycModel { IsActive = false };

            else
            {
                var documentClient = new nDocumentClient();
                var kycStatusByCustomerId = customerClient.FetchCustomerOnboardingStatuses(applicants.Value.CustomerIdByApplicantNr.Values.ToHashSet(), "UnsecuredLoanApplication", ai.ApplicationNr, true);
                var isAnswersApproved = kycStatusByCustomerId.Values.All(x => x.LatestKycQuestionsAnswerDate.HasValue);
                var isAccepted = isAnswersApproved || workflowService.IsStepStatusAccepted(UnsecuredLoanStandardWorkflowService.KycStep.Name, ai.ListNames);

                return new Response.KycModel
                {
                    IsActive = !isAccepted,
                    IsAccepted = ToTriStateBool(isAccepted, false),
                    IsPossibleToAnswer = !isAnswersApproved && IsPossibleToAnswerKycQuestions(ai, applicants.Value, workflowService),
                    IsAnswersApproved = ToTriStateBool(isAnswersApproved, false),
                };
            }
        }

        private Response.AgreementModel HandleAgreement(UnsecuredLoanStandardWorkflowService workflowService, ApplicationInfoModel ai, PreCreditCustomerClient customerClient, Lazy<ApplicationApplicantsModel> applicants, List<ComplexApplicationListItem> complexApplicationListItems, string signedAgreementArchiveKey, IClock clock)
        {
            if (!workflowService.AreAllStepsBeforeComplete(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, ai.ListNames))
            {
                return new Response.AgreementModel { IsActive = false };
            }
            else
            {
                var agreementInitialListName = workflowService.GetListName(UnsecuredLoanStandardWorkflowService.AgreementStep.Name, workflowService.InitialStatusName);
                var signatureRow = ComplexApplicationList.CreateListFromFlattenedItems("AgreementSignatureSession", complexApplicationListItems).GetRow(1, true);
                var isSessionFailed = signatureRow.GetUniqueItemBoolean("IsSessionFailed").GetValueOrDefault();
                var signedByApplicantNrs = signatureRow.GetRepeatedItems("SignedByApplicantNr").ToHashSet();
                var applicantResponse = new Dictionary<int, Response.AgreementApplicantModel>();

                Lazy<Dictionary<int, string>> signatureUrlsForActiveSession =
                    new Lazy<Dictionary<int, string>>(() => GetSignatureUrlsForActiveSession(ai.NrOfApplicants, signatureRow, clock));

                var customerPropertiesByCustomerId = Code.Services.CompanyLoans.CompanyLoanAgreementSignatureService
                    .GetPersonFirstNameAndBirthDatesByCustomerIds(applicants.Value.CustomerIdByApplicantNr.Values.ToHashSet(), customerClient);

                foreach (var applicantNr in Enumerable.Range(1, applicants.Value.NrOfApplicants))
                {
                    var applicantCustomerId = applicants.Value.CustomerIdByApplicantNr[applicantNr];
                    var isPossibleToSign = ai.IsActive
                            && workflowService.GetCurrentListName(ai.ListNames) == agreementInitialListName
                            && signatureRow.GetUniqueItemBoolean("IsSessionActive").GetValueOrDefault()
                            && signatureUrlsForActiveSession.Value.ContainsKey(applicantNr)
                            && signedAgreementArchiveKey == null;

                    applicantResponse[applicantNr] = new Response.AgreementApplicantModel
                    {
                        CustomerBirthDate = customerPropertiesByCustomerId[applicantCustomerId].BirthDate?.ToString("yyyy-MM-dd"),
                        CustomerShortName = customerPropertiesByCustomerId[applicantCustomerId].FirstName,
                        IsPossibleToSign = isPossibleToSign,
                        HasSigned = signedByApplicantNrs.Contains(applicantNr.ToString()) || signedAgreementArchiveKey != null,
                        SignatureUrl = isPossibleToSign ? signatureUrlsForActiveSession.Value[applicantNr] : null
                    };
                }

                return new Response.AgreementModel
                {
                    IsAccepted = ToTriStateBool(signedAgreementArchiveKey != null, isSessionFailed),
                    IsActive = applicantResponse.Any(x => x.Value.IsPossibleToSign || x.Value.HasSigned) || signedAgreementArchiveKey != null,
                    SignedAgreementArchiveKey = signedAgreementArchiveKey,
                    UnsignedAgreementArchiveKey = signatureRow.GetUniqueItem("UnsignedAgreementPdfArchiveKey"),
                    ApplicantStatusByApplicantNr = applicantResponse,
                };
            }
        }

        private static bool? ToTriStateBool(bool isAccepted, bool isRejected) => isAccepted ? true : (isRejected ? false : new bool?());

        public static Response.BankAccountsTaskModel CreateBankAccountsResponse(ApplicationInfoModel ai,
            List<ComplexApplicationListItem> complexApplicationListItems,
            IEnumerable<CreditDecisionItem> creditDecisionItems)
        {
            var currentCreditDecisionRow = CreditDecisionItemsToRow(creditDecisionItems);
            var hasAcceptedOffer = currentCreditDecisionRow.GetUniqueItem("customerDecisionCode") == "accepted";

            if (!hasAcceptedOffer)
                return new Response.BankAccountsTaskModel
                {
                    IsActive = false
                };

            var applicationRow = ComplexApplicationList.CreateListFromFlattenedItems("Application", complexApplicationListItems).GetRow(1, true);
            var loanToSettleList = ComplexApplicationList.CreateListFromFlattenedItems("LoansToSettle", complexApplicationListItems);

            var loansToSettle = loanToSettleList
                .GetRowNumbers()
                .Where(x => loanToSettleList.GetRow(x, true).GetUniqueItemBoolean("shouldBeSettled") == true)
                .Select(x =>
                {
                    var row = loanToSettleList.GetRow(x, true);
                    return new Response.BankAccountsTaskModel.LoanToSettleModel
                    {
                        Nr = row.Nr,
                        BankAccountNr = row.GetUniqueItem("bankAccountNr"),
                        BankAccountNrType = row.GetUniqueItem("bankAccountNrType"),
                        CurrentDebtAmount = row.GetUniqueItemInteger("currentDebtAmount"),
                        CurrentInterestRatePercent = row.GetUniqueItemDecimal("currentInterestRatePercent"),
                        MonthlyCostAmount = row.GetUniqueItemInteger("monthlyCostAmount"),
                        LoanType = row.GetUniqueItem("loanType"),
                        SettlementPaymentReference = row.GetUniqueItem("settlementPaymentReference"),
                        SettlementPaymentMessage = row.GetUniqueItem("settlementPaymentMessage")
                    };
                })
                .ToList();

            var hasBankAccountsBeenConfirmed = applicationRow.GetUniqueItem("confirmedBankAccountsCode") == "Approved";

            return new Response.BankAccountsTaskModel
            {
                IsActive = true,
                IsAccepted = ToTriStateBool(hasBankAccountsBeenConfirmed, false),
                IsPossibleToEditLoansToSettleBankAccounts = ai.IsActive && !hasBankAccountsBeenConfirmed && (loansToSettle?.Count ?? 0) > 0,
                IsPossibleToEditPaidToCustomerBankAccount = ai.IsActive && !hasBankAccountsBeenConfirmed,
                PaidToCustomer = new Response.BankAccountsTaskModel.PaidToCustomerModel
                {
                    Amount = currentCreditDecisionRow.GetUniqueItemInteger("paidToCustomerAmount"),
                    BankAccountNr = applicationRow.GetUniqueItem("paidToCustomerBankAccountNr"),
                    BankAccountNrType = applicationRow.GetUniqueItem("paidToCustomerBankAccountNrType")
                },
                LoansToSettle = loansToSettle
            };
        }

        private Dictionary<int, string> GetSignatureUrlsForActiveSession(int nrOfApplicants, ComplexApplicationList.Row agreementSignatureSession, IClock clock)
        {
            var emptyAnswer = new Dictionary<int, string>();
            if (!agreementSignatureSession.GetUniqueItemBoolean("IsSessionActive").GetValueOrDefault())
                return emptyAnswer;

            var providerName = agreementSignatureSession.GetUniqueItem("SignatureSessionProviderName");
            var sessionId = agreementSignatureSession.GetUniqueItem("SignatureSessionId");

            var provider = new ElectronicSignatureProvider(clock);
            var session = provider.GetCommonSignatureSession(sessionId, true);

            if (session == null)
                return emptyAnswer;

            try
            {
                return session.GetActiveSignatureUrlBySignerNr();
            }
            catch (Exception ex)
            {
                //We dont want signature problems to destroy the entire customer pages intercation since that will prevent the customer
                //from even accessing the message function to talk about the issue so better that they just cant click sign.
                NLog.Error(ex, "GetSignatureUrlsForActiveSession failed for session " + sessionId);
                return emptyAnswer;
            }
        }

        private Response.DirectDebitTaskModel GetDirectDebitAccountTaskModel(Lazy<ApplicationApplicantsModel> applicants, List<ComplexApplicationListItem> complexApplicationListItems, IClientConfigurationCore clientConfig)
        {
            if (!clientConfig.IsFeatureEnabled("ntech.feature.directdebitpaymentsenabled"))
                return null;

            var applicantDictionary = new Dictionary<int, Response.DirectDebitApplicantModel>();
            foreach (var applicantNr in Enumerable.Range(1, applicants.Value.NrOfApplicants))
            {
                var applicantData = applicants.Value.ApplicantInfoByApplicantNr[applicantNr];
                applicantDictionary.Add(applicantNr, new Response.DirectDebitApplicantModel
                {
                    BirthDate = applicantData.BirthDate,
                    FirstName = applicantData.FirstName,
                    CustomerId = applicants.Value.CustomerIdByApplicantNr[applicantNr]
                });
            }

            var applicationRow = ComplexApplicationList.CreateListFromFlattenedItems("Application", complexApplicationListItems).GetRow(1, true);
            var directDebitRow = ComplexApplicationList.CreateListFromFlattenedItems("DirectDebitSigningSession", complexApplicationListItems).GetRow(1, false);

            string signatureSessionUrl = null;
            var isSessionActive = directDebitRow?.GetUniqueItem("IsSessionActive");
            var signingSessionId = directDebitRow?.GetUniqueItem("SigningSessionid");
            if (isSessionActive != null && signingSessionId != null)
            {
                try
                {
                    var session = new CommonSignatureClient().GetSession(signingSessionId, false, false).Session;
                    var applicantNr = applicationRow.GetUniqueItemInteger("directDebitAccountOwnerApplicantNr");
                    signatureSessionUrl = session.GetActiveSignatureUrlBySignerNr().Opt(1);
                }
                catch (Exception ex)
                {
                    //We dont want signature problems to destroy the entire customer pages intercation since that will prevent the customer
                    //from even accessing the message function to talk about the issue so better that they just cant click sign.
                    NLog.Error(ex, "GetDirectDebitAccountTaskModel get signature urls failed for session " + signingSessionId);
                }
            }

            var signedArchiveKey = directDebitRow?.GetUniqueItem("SignedDirectDebitConsentFilePdfArchiveKey");

            return new Response.DirectDebitTaskModel
            {
                IsAccepted = signedArchiveKey != null ? true : new bool?(),
                IsActive = true,
                CustomerInfoByApplicantNr = applicantDictionary,
                AccountOwnerApplicantNr = applicationRow.GetUniqueItemInteger("directDebitAccountOwnerApplicantNr"),
                DirectDebitBankAccountNr = applicationRow.GetUniqueItem("directDebitBankAccountNr"),
                HasConfirmedAccountInfo = directDebitRow != null,
                UnsignedDirectDebitConsentFileArchiveKey = directDebitRow?.GetUniqueItem("UnsignedDirectDebitConsentFilePdfArchiveKey"),
                SignedDirectDebitConsentFileArchiveKey = signedArchiveKey,
                SignatureSessionUrl = signatureSessionUrl,
                PaidToCustomerBankAccountNr = GetPaidToCustomerBankAccountNr(applicationRow, NEnv.ClientCfgCore)?.FormatFor(null)
            };
        }

        private IBankAccountNumber GetPaidToCustomerBankAccountNr(ComplexApplicationList.Row applicationRow, IClientConfigurationCore clientConfiguration)
        {
            var paidToCustomerBankAccountNr = applicationRow.GetUniqueItem("paidToCustomerBankAccountNr");
            var paidToCustomerBankAccountNrType = applicationRow.GetUniqueItem("paidToCustomerBankAccountNrType");
            if (paidToCustomerBankAccountNr == null)
                return null;

            var parser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);
            if (parser.TryParseFromStringWithDefaults(paidToCustomerBankAccountNr, paidToCustomerBankAccountNrType, out var parsedAccount))
                return parsedAccount;

            return null;
        }

        public static bool IsPossibleToAnswerKycQuestions(ApplicationInfoModel ai, ApplicationApplicantsModel applicants, UnsecuredLoanStandardWorkflowService workflowService)
        {
            if (!ai.IsActive)
                return false;

            if (workflowService.IsStepStatusAccepted(UnsecuredLoanStandardWorkflowService.KycStep.Name, ai.ListNames))
                return false;

            var kycStatusByCustomerId = new PreCreditCustomerClient().FetchCustomerOnboardingStatuses(applicants.CustomerIdByApplicantNr.Values.ToHashSet(), "UnsecuredLoanApplication", ai.ApplicationNr, true);
            foreach (var applicantNr in Enumerable.Range(1, ai.NrOfApplicants))
            {
                if (kycStatusByCustomerId[applicants.CustomerIdByApplicantNr[applicantNr]]?.LatestKycQuestionsSet != null)
                    return false;
            }

            return true;
        }

        public class Request
        {
            [Required]
            public int? CustomerId { get; set; }

            [Required]
            public string ApplicationNr { get; set; }
        }

        public class Response
        {
            public ApplicationModel Application { get; set; }
            public class ApplicationModel
            {
                public string ApplicationNr { get; set; }
                public EnumsApiModel Enums { get; set; }
                public bool IsActive { get; set; }
                public DateTimeOffset ApplicationDate { get; set; }
                public bool IsCancelled { get; set; }
                public bool IsRejected { get; set; }
                public bool IsFinalDecisionMade { get; set; }
                public string CreditNr { get; set; }
                public CurrentOfferModel CurrentOffer { get; set; }
                public bool? IsFutureOfferPossible { get; set; }
                public bool? IsInactiveMessagingAllowed { get; set; }
                public KycModel KycTask { get; set; }
                public DirectDebitTaskModel DirectDebitTask { get; set; }
                public AgreementModel AgreementTask { get; set; }
                public BankAccountsTaskModel BankAccountsTask { get; set; }
            }
            public class CurrentOfferModel
            {
                public bool IsPossibleToDecide { get; set; }
                public Dictionary<string, string> OfferItems { get; set; }
            }
            public interface ICustomerTask
            {
                bool IsActive { get; }
                bool? IsAccepted { get; }
            }
            public class KycModel : ICustomerTask
            {
                public bool IsActive { get; set; }
                public bool? IsAccepted { get; set; }
                public bool IsPossibleToAnswer { get; set; }
                public bool? IsAnswersApproved { get; set; }
            }
            public class DirectDebitTaskModel : ICustomerTask
            {
                public bool IsActive { get; set; }
                public bool? IsAccepted { get; set; }
                public bool HasConfirmedAccountInfo { get; set; }
                public Dictionary<int, DirectDebitApplicantModel> CustomerInfoByApplicantNr { get; set; }
                public string DirectDebitBankAccountNr { get; set; }
                public int? AccountOwnerApplicantNr { get; set; }
                public string UnsignedDirectDebitConsentFileArchiveKey { get; set; }
                public string SignedDirectDebitConsentFileArchiveKey { get; set; }
                public string SignatureSessionUrl { get; set; }
                public string PaidToCustomerBankAccountNr { get; set; }
            }
            public class DirectDebitApplicantModel
            {
                public int CustomerId { get; set; }
                public string FirstName { get; set; }
                public string BirthDate { get; set; }
            }
            public class KycApplicantModel
            {
                public string CustomerBirthDate { get; set; }
                public string CustomerShortName { get; set; }
                public DateTime? LatestKycQuestionsAnswerDate { get; set; }
                public List<UnsecuredLoanStandardCustomerPagesKycQuestionModel> LatestQuestions { get; set; }
            }
            public class AgreementModel : ICustomerTask
            {
                public bool IsActive { get; set; }
                public bool? IsAccepted { get; set; }
                public string UnsignedAgreementArchiveKey { get; set; }
                public string SignedAgreementArchiveKey { get; set; }
                public Dictionary<int, AgreementApplicantModel> ApplicantStatusByApplicantNr { get; set; }
            }
            public class AgreementApplicantModel
            {
                public string CustomerBirthDate { get; set; }
                public string CustomerShortName { get; set; }
                public bool HasSigned { get; set; }
                public string SignatureUrl { get; set; }
                public bool IsPossibleToSign { get; set; }
            }
            public class BankAccountsTaskModel : ICustomerTask
            {
                public bool IsActive { get; set; }
                public bool? IsAccepted { get; set; }
                public bool IsPossibleToEditPaidToCustomerBankAccount { get; set; }
                public bool IsPossibleToEditLoansToSettleBankAccounts { get; set; }
                public PaidToCustomerModel PaidToCustomer { get; set; }
                public List<LoanToSettleModel> LoansToSettle { get; set; }
                public class PaidToCustomerModel
                {
                    public int? Amount { get; set; }
                    public string BankAccountNrType { get; set; }
                    public string BankAccountNr { get; set; }
                }
                public class LoanToSettleModel
                {
                    public int Nr { get; set; }
                    public int? CurrentDebtAmount { get; set; }
                    public int? MonthlyCostAmount { get; set; }
                    public decimal? CurrentInterestRatePercent { get; set; }
                    public string LoanType { get; set; }
                    public string BankAccountNrType { get; set; }
                    public string BankAccountNr { get; set; }
                    public string SettlementPaymentReference { get; set; }
                    public string SettlementPaymentMessage { get; set; }
                }
            }
        }
    }

    public class UnsecuredLoanStandardCustomerPagesKycQuestionModel
    {
        public string QuestionCode { get; set; }
        public string AnswerCode { get; set; }
        public string QuestionText { get; set; }
        public string AnswerText { get; set; }
    }
}