using nCredit.DbModel.BusinessEvents.NewCredit;
using nPreCredit;
using nPreCredit.Code.Services;
using NTech.Banking.BankAccounts;
using NTech.Banking.Conversion;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.CreditStandard;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace NTech.Core.PreCredit.Shared.Services.UlStandard
{
    public class CreateLoanFromApplicationUlStandardService
    {
        private readonly ApplicationInfoService applicationInfoService;
        private readonly UnsecuredLoanStandardWorkflowService workflowService;
        private readonly IPreCreditContextFactoryService contextFactoryService;
        private readonly IComplexApplicationListReadOnlyService listService;
        private readonly ICreditClient creditClient;
        private readonly IClientConfigurationCore clientConfiguration;
        private readonly IApplicationDocumentService applicationDocumentService;
        private readonly ICustomerClient customerClient;

        public CreateLoanFromApplicationUlStandardService(ApplicationInfoService applicationInfoService, UnsecuredLoanStandardWorkflowService workflowService, 
            IPreCreditContextFactoryService contextFactoryService, IComplexApplicationListReadOnlyService listService, ICreditClient creditClient,
            IClientConfigurationCore clientConfiguration, IApplicationDocumentService applicationDocumentService, ICustomerClient customerClient)
        {
            this.applicationInfoService = applicationInfoService;
            this.workflowService = workflowService;
            this.contextFactoryService = contextFactoryService;
            this.listService = listService;
            this.creditClient = creditClient;
            this.clientConfiguration = clientConfiguration;
            this.applicationDocumentService = applicationDocumentService;
            this.customerClient = customerClient;
        }

        public CreateLoanFromApplicationUlStandardResponse CreateLoan(CreateLoanFromApplicationUlStandardRequest request)
        {
            var ai = applicationInfoService.GetApplicationInfo(request.ApplicationNr, true);

            if (ai == null)
                throw new NTechCoreWebserviceException("No such application exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (!ai.IsActive)
                throw new NTechCoreWebserviceException("Application is not active") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (ai.IsFinalDecisionMade)
                throw new NTechCoreWebserviceException("Loan already created") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

            if (workflowService.GetCurrentListName(ai.ListNames) != workflowService.GetListName(UnsecuredLoanStandardWorkflowService.PaymentStep.Name, workflowService.InitialStatusName))
                throw new NTechCoreWebserviceException("Application is not on the payment step") { IsUserFacing = true, ErrorHttpStatusCode = 400 };            

            using (var context = contextFactoryService.CreateExtended())
            {
                var decisionResult = context
                    .CreditApplicationHeadersQueryable
                    .Where(x => x.ApplicationNr == request.ApplicationNr)
                    .Select(x => new
                    {
                        x.CurrentCreditDecisionId,
                        CurrentDecisionItems = x.CurrentCreditDecision.DecisionItems.Select(y => new { ItemName = y.ItemName, ItemValue = y.Value })
                    })
                    .Single();

                if (!decisionResult.CurrentCreditDecisionId.HasValue)
                    throw new NTechCoreWebserviceException("No credit decision exists") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                //We fake a complex list here just so we access as much of the different data sources as possible in the same way
                var currentDecisionList = ComplexApplicationList.CreateEmpty("DecisionTemp");
                var currentDecision = currentDecisionList.AddRow(
                    initialUniqueItems: decisionResult.CurrentDecisionItems.ToDictionary(x => x.ItemName, x => x.ItemValue));

                if (currentDecision.GetUniqueItemBoolean("isOffer") != true)
                {
                    throw new NTechCoreWebserviceException("Current decision is not an offer") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                }

                var lists = listService.GetListsForApplication(request.ApplicationNr, true, context, "Application", "DirectDebitLoanTerms", "LoansToSettle");

                var applicationRow = lists["Application"].GetRow(1, true);
                var loansToSettleList = lists.Opt("LoansToSettle") ?? ComplexApplicationList.CreateEmpty("LoansToSettle");
                var directDebitLoanTerms = lists["DirectDebitLoanTerms"].GetRow(1, true);

                var creditNr = applicationRow.GetUniqueItem("creditNr");
                var creationResult = EnsureCreditNr(ai, creditClient, applicationRow, context);
                if (creationResult.WasCreated)
                {
                    creditNr = creationResult.CreditNr;
                    context.SaveChanges();
                }

                IBankAccountNumber paidToCustomerBankAccountNr;

                var bankAccountNrParser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);

                var paidToCustomerAmount = currentDecision.GetUniqueItemDecimal("paidToCustomerAmount", require: true).Value;

                if (paidToCustomerAmount > 0)
                {
                    var paidToCustomerBankAccountNrRaw = applicationRow.GetUniqueItem("paidToCustomerBankAccountNr", require: true);
                    var paidToCustomerBankAccountNrType = applicationRow.GetUniqueItem("paidToCustomerBankAccountNrType", require: true);

                    if (!bankAccountNrParser.TryParseFromStringWithDefaults(paidToCustomerBankAccountNrRaw, paidToCustomerBankAccountNrType, out paidToCustomerBankAccountNr))
                    {
                        throw new NTechCoreWebserviceException("Invalid paid to customer bank account nr") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                    }
                }
                else
                {
                    paidToCustomerBankAccountNr = null;
                }

                var decisionLoansToSettleAmount = currentDecision.GetUniqueItemDecimal("loansToSettleAmount", require: true).Value;
                var loansToSettle = loansToSettleList.GetRows().Where(x => x.GetUniqueItemBoolean("shouldBeSettled") == true).Select(x => new
                {
                    CurrentDebtAmount = x.GetUniqueItemDecimal("currentDebtAmount", require: true).Value,
                    BankAccountNrType = x.GetUniqueItem("bankAccountNrType", require: true),
                    BankAccountNr = x.GetUniqueItem("bankAccountNr", require: true),
                    PaymentReference = x.GetUniqueItem("settlementPaymentReference"),
                    PaymentMessage = x.GetUniqueItem("settlementPaymentMessage")
                }).Where(x => x.CurrentDebtAmount > 0m).ToList();

                if (loansToSettle.Sum(x => x.CurrentDebtAmount) != decisionLoansToSettleAmount)
                    //This could be relaxed to allowing the actual to be less than the decision but then the annuity needs to be recalculated
                    throw new NTechCoreWebserviceException("Decision loans to settle amount != actual loans to settle amount") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var applicants = applicationInfoService
                    .GetApplicationApplicants(request.ApplicationNr);

                var signedAgreementDocuments = applicationDocumentService.FetchForApplication(request.ApplicationNr, new List<string> { CreditApplicationDocumentTypeCode.SignedAgreement.ToString() });

                if (signedAgreementDocuments.Count != 1)
                    throw new NTechCoreWebserviceException("There should be exactly one document of type SignedAgreement but instead there are " + signedAgreementDocuments.Count) { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                var initialFeeWithheldAmount = currentDecision.GetUniqueItemInteger("initialFeeWithheldAmount") ?? 0;

                var createRequest = new NewCreditRequest
                {
                    ApplicationNr = request.ApplicationNr,
                    CreditNr = creditNr,
                    SinglePaymentLoanRepaymentTimeInDays = currentDecision.GetUniqueItemInteger("singlePaymentLoanRepaymentTimeInDays"),
                    RepaymentTimeInMonths = currentDecision.GetUniqueItemInteger("repaymentTimeInMonths"),
                    SharedAgreementPdfArchiveKey = signedAgreementDocuments.Single().DocumentArchiveKey,
                    //Used mainly for the ui here so a handler can use it to pay back overpayments and such. The actual settlement payments on loan creations do not use this
                    BankAccountNr = paidToCustomerBankAccountNr != null && IsRegularBankAccount(paidToCustomerBankAccountNr) ? paidToCustomerBankAccountNr.FormatFor(null) : null,
                    CapitalizedInitialFeeAmount = currentDecision.GetUniqueItemInteger("initialFeeCapitalizedAmount") ?? 0,
                    NrOfApplicants = ai.NrOfApplicants,
                    ProviderApplicationId = applicationRow.GetUniqueItem("providerApplicationId"),
                    CampaignCode = applicationRow.GetUniqueItem("campaignCode"),
                    Iban = paidToCustomerBankAccountNr != null && IsRegularIban(paidToCustomerBankAccountNr) ? paidToCustomerBankAccountNr.FormatFor(null) : null,
                    MarginInterestRatePercent = currentDecision.GetUniqueItemDecimal("marginInterestRatePercent", require: true).Value,
                    NotificationFee = currentDecision.GetUniqueItemDecimal("notificationFeeAmount", require: true).Value,
                    ProviderName = ai.ProviderName,
                    Applicants = Enumerable.Range(1, applicants.NrOfApplicants).Select(applicantNr =>
                    {
                        return new NewCreditRequest.Applicant
                        {
                            ApplicantNr = applicantNr,
                            CustomerId = applicants.CustomerIdByApplicantNr[applicantNr],
                            AgreementPdfArchiveKey = null //Uses shared agreement instead
                        };
                    }).ToList(),
                    CreditAmountParts = new List<NewCreditRequest.CreditAmountPartModel>(),
                    DirectDebitDetails = new NewCreditRequest.DirectDebitDetailsModel(),
                    FirstNotificationCosts = currentDecision.GetUniqueItemNames().Where(CreditRecommendationUlStandardService.IsFirstNotificationCostAmountDecisionItem).Select(x => new NewCreditRequest.FirstNotificationCostItem
                    {
                        CostAmount = currentDecision.GetUniqueItemInteger(x, require: true).Value,
                        CostCode = CreditRecommendationUlStandardService.GetFirstNotificationCostAmountDecisionItemCode(x)
                    }).ToList()
                };

                foreach (var payment in loansToSettle)
                {
                    if (string.IsNullOrWhiteSpace(payment.PaymentMessage) && string.IsNullOrWhiteSpace(payment.PaymentReference))
                        throw new NTechCoreWebserviceException("At least one of payment message and payment reference is required on all settlement payments") { ErrorCode = "loanToSettleMissingPaymentReference", ErrorHttpStatusCode = 400, IsUserFacing = true };

                    createRequest.CreditAmountParts.Add(new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = payment.CurrentDebtAmount,
                        IsDirectToCustomerPayment = false,
                        IsSettlingOtherLoan = true,
                        ShouldBePaidOut = true,
                        PaymentBankAccountNrType = payment.BankAccountNrType,
                        PaymentBankAccountNr = payment.BankAccountNr,
                        PaymentReference = payment.PaymentReference,
                        PaymentMessage = payment.PaymentMessage,
                        SubAccountCode = CreditStandardSubAccountCode.SettledLoanPartCode
                    });
                }

                if (paidToCustomerAmount > 0m)
                {
                    createRequest.CreditAmountParts.Add(new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = paidToCustomerAmount,
                        IsDirectToCustomerPayment = true,
                        IsSettlingOtherLoan = false,
                        ShouldBePaidOut = true,
                        PaymentBankAccountNrType = paidToCustomerBankAccountNr.AccountType.ToString(),
                        PaymentBankAccountNr = paidToCustomerBankAccountNr.FormatFor(null),
                        SubAccountCode = CreditStandardSubAccountCode.PaidToCustomerPartCode
                    });
                }

                if (initialFeeWithheldAmount > 0)
                {
                    createRequest.CreditAmountParts.Add(new NewCreditRequest.CreditAmountPartModel
                    {
                        Amount = initialFeeWithheldAmount,
                        ShouldBePaidOut = false,
                        IsDirectToCustomerPayment = false,
                        IsSettlingOtherLoan = false,
                        IsCoveringInitialFeeDrawnFromLoan = true,
                        SubAccountCode = CreditStandardSubAccountCode.WithheldInitialFeeCode
                    });
                }

                if (directDebitLoanTerms.GetUniqueItem("isPending") == "false")
                {
                    var isActive = directDebitLoanTerms.GetUniqueItemBoolean("isActive", require: true).Value;
                    string accountNr = null;
                    int? accountOwner = null;
                    string consentArchiveKey = null;

                    if (isActive)
                    {
                        if (!bankAccountNrParser.TryParseBankAccount(
                            directDebitLoanTerms.GetUniqueItem("bankAccountNr", require: true),
                            Enums.Parse<BankAccountNumberTypeCode>(directDebitLoanTerms.GetUniqueItem("bankAccountNrType", require: true), ignoreCase: true).Value, out var parsedNr))
                        {
                            throw new NTechCoreWebserviceException("Invalid direct debit bank account nr") { ErrorHttpStatusCode = 400, IsUserFacing = true };
                        }
                        accountNr = parsedNr.FormatFor(null);
                        accountOwner = directDebitLoanTerms.GetUniqueItemInteger("accountOwnerApplicantNr", require: true);
                        consentArchiveKey = directDebitLoanTerms.GetUniqueItem("signedConsentPdfArchiveKey", require: false);
                    }

                    createRequest.DirectDebitDetails = new NewCreditRequest.DirectDebitDetailsModel
                    {
                        IsActive = isActive,
                        IsExternalStatusActive = isActive,
                        AccountNr = accountNr,
                        AccountOwner = accountOwner,
                        DirectDebitConsentFileArchiveKey = consentArchiveKey
                    };
                }

                KycQuestionCopyTask kycQuestionCopyTask;

                context.BeginTransaction();
                try
                {
                    var h = context.CreditApplicationHeadersQueryable.Single(x => x.ApplicationNr == request.ApplicationNr);
                    h.IsActive = false;
                    h.IsFinalDecisionMade = true;
                    context.CreateAndAddComment($"Loan {creditNr} created", "LoanCreated", creditApplicationHeader: h);

                    workflowService.ChangeStepStatusComposable(context, UnsecuredLoanStandardWorkflowService.PaymentStep.Name, workflowService.AcceptedStatusName, application: h);

                    context.SaveChanges();

                    creditClient.CreateCredits(new NewCreditRequest[] { createRequest }, null);

                    kycQuestionCopyTask = new KycQuestionCopyTask
                    {
                        ApplicationDate = h.ApplicationDate.DateTime,
                        ApplicationNr = h.ApplicationNr,
                        CreditNr = creditNr,
                        CustomerIds = createRequest.Applicants.Select(x => x.CustomerId).ToHashSetShared()
                    };

                    context.CommitTransaction();
                }
                catch
                {
                    context.RollbackTransaction();
                    throw;
                }

                KycQuestionCopyService.CopyUnsecuredLoanKycQuestions(new List<KycQuestionCopyTask> { kycQuestionCopyTask }, customerClient);

                return new CreateLoanFromApplicationUlStandardResponse
                {
                    CreditNr = creditNr
                };
            }
        }

        public static (bool WasCreated, string CreditNr) EnsureCreditNr(ApplicationInfoModel ai, ICreditClient creditClient, ComplexApplicationList.Row applicationRow, IPreCreditContextExtended context)
        {
            var creditNr = applicationRow.GetUniqueItem("creditNr");
            var wasCreated = false;
            if (creditNr == null)
            {
                //This can exist since before for print the agreement for instance. If not, we create and pre-save one here
                //The reason to save it is that if nPreCredit and nCredit fail to synch properly we want the next try to use the same credit nr
                //so we cant create the same loan multiple times by accident.
                creditNr = creditClient.NewCreditNumber();
                ComplexApplicationListService.SetSingleUniqueItem(ai.ApplicationNr, "Application", "creditNr", 1, creditNr, context);
                wasCreated = true;
            }
            return (WasCreated: wasCreated, CreditNr: creditNr);
        }

        private bool IsRegularBankAccount(IBankAccountNumber account)
            => account.AccountType == BankAccountNumberTypeCode.BankAccountSe;

        private bool IsRegularIban(IBankAccountNumber account)
            => account.AccountType == BankAccountNumberTypeCode.IBANFi;
    }

    public class CreateLoanFromApplicationUlStandardRequest
    {
        [Required]
        public string ApplicationNr { get; set; }
    }

    public class CreateLoanFromApplicationUlStandardResponse
    {
        public string CreditNr { get; set; }
    }
}
