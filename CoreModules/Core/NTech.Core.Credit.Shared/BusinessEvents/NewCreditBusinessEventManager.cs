using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DomainModel;
using NTech.Banking.BankAccounts;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Clients;
using NTech.Core.Module.Shared.Infrastructure;
using NTech.Core.Module.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using static nCredit.DbModel.BusinessEvents.NewCredit.NewCreditRequest;

namespace nCredit.DbModel.BusinessEvents.NewCredit
{
    public class NewCreditBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly LegalInterestCeilingService legalInterestCeilingService;
        private readonly BankAccountNumberParser bankAccountNumberParser;
        private readonly ICreditCustomerListServiceComposable creditCustomerListService;
        private readonly EncryptionService encryptionService;
        private readonly ICustomerClient customerClient;
        private readonly IOcrPaymentReferenceGenerator ocrPaymentReferenceGenerator;
        private readonly ICreditEnvSettings envSettings;
        private readonly PaymentAccountService paymentAccountService;
        private readonly CustomCostTypeService customCostTypeService;

        public NewCreditBusinessEventManager(INTechCurrentUserMetadata currentUser, LegalInterestCeilingService legalInterestCeilingService, ICreditCustomerListServiceComposable creditCustomerListService, EncryptionService encryptionService, ICoreClock coreClock, IClientConfigurationCore clientConfiguration,
            ICustomerClient customerClient, IOcrPaymentReferenceGenerator ocrPaymentReferenceGenerator, ICreditEnvSettings envSettings, PaymentAccountService paymentAccountService, CustomCostTypeService customCostTypeService) : base(currentUser, coreClock, clientConfiguration)
        {
            this.legalInterestCeilingService = legalInterestCeilingService;
            this.bankAccountNumberParser = new BankAccountNumberParser(clientConfiguration.Country.BaseCountry);
            this.creditCustomerListService = creditCustomerListService;
            this.encryptionService = encryptionService;
            this.customerClient = customerClient;
            this.ocrPaymentReferenceGenerator = ocrPaymentReferenceGenerator;
            this.envSettings = envSettings;
            this.paymentAccountService = paymentAccountService;
            this.customCostTypeService = customCostTypeService;
        }

        public CreditHeader CreateNewCredit(ICreditContextExtended context, NewCreditRequest request, Lazy<decimal> currentReferenceInterestRate)
        {
            context.EnsureCurrentTransaction();

            ValidateCreditRequest(request);                

            var bookKeepingDate = Now.ToLocalTime().Date;
            var additionalCommentTexts = new List<string>();

            if (request.NrOfApplicants == 0)
            {
                request.NrOfApplicants = request.Applicants.Count;
            }

            if (request.CreditAmount > 0 && request.HasCreditAmountParts)
                throw new Exception("CreditAmount and CreditAmountParts cannot be combined");
            if (request.DrawnFromLoanAmountInitialFeeAmount.HasValue && request.HasCreditAmountParts)
                throw new Exception("DrawnFromLoanAmountInitialFeeAmount and CreditAmountParts cannot be combined. Use IsCoveringInitialFeeDrawnFromLoan on the part instead.");

            var newCreditEvent = AddBusinessEvent(BusinessEventType.NewCredit, context);

            var credit = context.FillInfrastructureFields(new CreditHeader
            {
                CreditNr = request.CreditNr,
                CreditType = request.IsCompanyCredit == true ? CreditType.CompanyLoan.ToString() : CreditType.UnsecuredLoan.ToString(),
                NrOfApplicants = request.NrOfApplicants,
                StartDate = Now,
                ProviderName = request.ProviderName,
                CreatedByEvent = newCreditEvent
            });
            context.AddCreditHeader(credit);

            SetStatus(credit, CreditStatus.Normal, newCreditEvent, context);
                        
            AddDatedCreditValue(DatedCreditValueCode.ReferenceInterestRate.ToString(), currentReferenceInterestRate.Value, credit, newCreditEvent, context);

            var interestChange = this.legalInterestCeilingService
                .HandleMarginInterestRateChange(currentReferenceInterestRate.Value, null, null, request.MarginInterestRatePercent);
            AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), interestChange.NewMarginInterestRate.Value, credit, newCreditEvent, context);
            if (interestChange.NewRequestedMarginInterestRate.HasValue)
            {
                additionalCommentTexts.Add($"Margin interest rate={(interestChange.NewMarginInterestRate.Value / 100m).ToString("P")}");
                additionalCommentTexts.Add($"Requested margin interest rate={(interestChange.NewRequestedMarginInterestRate.Value / 100m).ToString("P")}");
                AddDatedCreditValue(DatedCreditValueCode.RequestedMarginInterestRate.ToString(), interestChange.NewRequestedMarginInterestRate.Value, credit, newCreditEvent, context);
            }

            if (request.NotificationFee.HasValue && request.NotificationFee > 0m)
            {
                AddDatedCreditValue(DatedCreditValueCode.NotificationFee.ToString(), request.NotificationFee.Value, credit, newCreditEvent, context);
            }

            var outgoingPaymentService = new NewCreditOutgoingPaymentService(CurrentUser, bankAccountNumberParser, encryptionService, ClientCfg, Clock,
                envSettings, customerClient, paymentAccountService);
            IBankAccountNumber toBankAccountNr = outgoingPaymentService.HandleOutgoingPaymentBankAccount(request, context, newCreditEvent, credit);

            if (!string.IsNullOrWhiteSpace(request.BeforeImportCreditNr))
            {
                AddDatedCreditString(DatedCreditStringCode.BeforeImportCreditNr.ToString(), request.BeforeImportCreditNr, credit, newCreditEvent, context);
            }

            var commentArchiveKeys = new List<string>();
            foreach (var a in request.Applicants.OrderBy(x => x.ApplicantNr).ToList())
            {
                context.AddCreditCustomer(context.FillInfrastructureFields(new CreditCustomer
                {
                    ApplicantNr = a.ApplicantNr,
                    CustomerId = a.CustomerId,
                    Credit = credit
                }));
                if (a.AgreementPdfArchiveKey != null)
                {
                    AddDatedCreditString(DatedCreditStringCode.SignedInitialAgreementArchiveKey.ToString() + a.ApplicantNr, a.AgreementPdfArchiveKey, credit, newCreditEvent, context);
                    commentArchiveKeys.Add(a.AgreementPdfArchiveKey);
                    AddCreditDocument("InitialAgreement", a.ApplicantNr, a.AgreementPdfArchiveKey, context, credit: credit);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.SharedAgreementPdfArchiveKey))
            {
                AddCreditDocument("InitialAgreement", null, request.SharedAgreementPdfArchiveKey, context, credit: credit);
                AddDatedCreditString(DatedCreditStringCode.SignedInitialAgreementArchiveKey.ToString(), request.SharedAgreementPdfArchiveKey, credit, newCreditEvent, context);
            }

            if (request.ApplicationFreeformDocumentArchiveKeys != null)
            {
                foreach (var archiveKey in request.ApplicationFreeformDocumentArchiveKeys)
                    AddCreditDocument("ApplicationFreeform", null, archiveKey, context, credit: credit);
            }

            AddDatedCreditString(DatedCreditStringCode.OcrPaymentReference.ToString(), ocrPaymentReferenceGenerator.GenerateNew().NormalForm, credit, newCreditEvent, context);
            AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(), newCreditEvent.TransactionDate.ToString("yyyy-MM-dd"), credit, newCreditEvent, context);

            if (!string.IsNullOrWhiteSpace(request.ProviderApplicationId))
            {
                AddDatedCreditString(DatedCreditStringCode.ProviderApplicationId.ToString(), request.ProviderApplicationId, credit, newCreditEvent, context);
            }
            if (!string.IsNullOrWhiteSpace(request.ApplicationNr))
            {
                AddDatedCreditString(DatedCreditStringCode.ApplicationNr.ToString(), request.ApplicationNr, credit, newCreditEvent, context);
            }
            if (!string.IsNullOrWhiteSpace(request.CampaignCode))
            {
                AddDatedCreditString(DatedCreditStringCode.IntialLoanCampaignCode.ToString(), request.CampaignCode, credit, newCreditEvent, context);
                additionalCommentTexts.Add($"Campaign code={request.CampaignCode}");
            }
            if (!string.IsNullOrWhiteSpace(request.SourceChannel))
            {
                AddDatedCreditString(DatedCreditStringCode.InitialLoanSourceChannel.ToString(), request.SourceChannel, credit, newCreditEvent, context);
                additionalCommentTexts.Add($"Source channel={request.SourceChannel}");
            }
            if (!string.IsNullOrWhiteSpace(request.SniKodSe))
            {
                AddDatedCreditString(DatedCreditStringCode.CompanyLoanSniKodSe.ToString(), request.SniKodSe, credit, newCreditEvent, context);
            }
            if (request.ApplicationLossGivenDefault.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.ApplicationLossGivenDefault.ToString(), request.ApplicationLossGivenDefault.Value, credit, newCreditEvent, context);
            }
            if (request.ApplicationProbabilityOfDefault.HasValue)
            {
                AddDatedCreditValue(DatedCreditValueCode.ApplicationProbabilityOfDefault.ToString(), request.ApplicationProbabilityOfDefault.Value, credit, newCreditEvent, context);
            }

            if (request.DirectDebitDetails?.AccountNr != null)
            {
                SetCreditDirectDebitDetails(request, credit, context, newCreditEvent);
            }

            var drawnFromLoanAmountInitialFeeAmount = request.GetDrawnFromLoanAmountInitialFeeAmount();
            if (drawnFromLoanAmountInitialFeeAmount > 0m && drawnFromLoanAmountInitialFeeAmount <= request.GetComputedCreditAmount())
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.InitialFeeDrawnFromLoanAmount,
                    drawnFromLoanAmountInitialFeeAmount,
                    bookKeepingDate,
                    newCreditEvent,
                    credit: credit,
                    businessEventRuleCode: "initialFee"));

                additionalCommentTexts.Add($"Initial fee of {drawnFromLoanAmountInitialFeeAmount.ToString("f2")} withheld");
            }

            //Create a payment to the customer            
            var paidToCustomerAmount = 0m;
            Action saveEncryptedPaymentItems = null;
            if (!request.IsInitialPaymentAlreadyMade.GetValueOrDefault())
            {
                if (!request.HasCreditAmountParts)
                {
                    var outgoingPayment = outgoingPaymentService.CreateSingleOutgoingPayment(request, context, bookKeepingDate, newCreditEvent, credit, toBankAccountNr);

                    paidToCustomerAmount += outgoingPayment.PaidToCustomerAmount;
                    saveEncryptedPaymentItems = outgoingPayment.SaveEncryptedItems;
                }
                else
                {
                    var outgoingPayments = outgoingPaymentService.CreateSplitOutgoingPayment(request, context, bookKeepingDate, newCreditEvent, credit);

                    paidToCustomerAmount += outgoingPayments.PaidToCustomerAmount;
                    saveEncryptedPaymentItems = outgoingPayments.SaveEncryptedItems;
                }
            }

            //Capitalized initial fee event
            if (request.CapitalizedInitialFeeAmount.HasValue && request.CapitalizedInitialFeeAmount > 0m)
            {
                var feeEvent = AddBusinessEvent(BusinessEventType.CapitalizedInitialFee, context);

                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.NotNotifiedCapital,
                    request.CapitalizedInitialFeeAmount.Value,
                    bookKeepingDate,
                    feeEvent,
                    credit: credit,
                    businessEventRuleCode: "initialFee"));

                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.CapitalDebt,
                    request.CapitalizedInitialFeeAmount.Value,
                    bookKeepingDate,
                    feeEvent,
                    credit: credit,
                    businessEventRuleCode: "initialFee"));
            }

            /*
                We track this since paid to customer in nCredit means more like "paid to external party" so it includes payment settling other loans also.
                The model here is that the customer is really borrowing that money and using it to pay other loans, we are just helping transfer the money for free 
                instead of them doing it themselves so in the technical sense its all "paid to customer" but when talking to the customer it helps to know which is which.               
             */
            var settleOtherLoansAmount = 0m;

            if (request.CreditAmount > 0m)
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.NotNotifiedCapital,
                    request.CreditAmount,
                    bookKeepingDate,
                    newCreditEvent,
                    credit: credit));

                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.CapitalDebt,
                    request.CreditAmount,
                    bookKeepingDate,
                    newCreditEvent,
                    credit: credit));
            }
            else
            {
                foreach (var p in request.CreditAmountParts)
                {
                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedCapital,
                        p.Amount,
                        bookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        businessEventRuleCode: "InitialLoan",
                        subAccountCode: p.SubAccountCode));

                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.CapitalDebt,
                        p.Amount,
                        bookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        businessEventRuleCode: "InitialLoan",
                        subAccountCode: p.SubAccountCode));

                    if (p.IsSettlingOtherLoan.GetValueOrDefault())
                        settleOtherLoansAmount += p.Amount;
                }
            }

            Action<string, List<int>> addCustomers = (listName, customerIds) =>
            {
                if (customerIds == null)
                    return;
                foreach (var customerId in customerIds)
                    creditCustomerListService.SetMemberStatusComposable(context, listName, true, customerId, credit: credit, evt: newCreditEvent);
            };

            addCustomers("companyLoanApplicant", request.CompanyLoanApplicantCustomerIds);
            addCustomers("companyLoanBeneficialOwner", request.CompanyLoanBeneficialOwnerCustomerIds);
            addCustomers("companyLoanAuthorizedSignatory", request.CompanyLoanAuthorizedSignatoryCustomerIds);
            addCustomers("companyLoanCollateral", request.CompanyLoanCollateralCustomerIds);

            var interestRatePercent = interestChange.NewMarginInterestRate.Value + currentReferenceInterestRate.Value;
            HandleRepaymentModel(credit, newCreditEvent, context, request, interestRatePercent, bookKeepingDate);

            if (settleOtherLoansAmount > 0m)
            {
                additionalCommentTexts = Enumerables.Singleton($"{settleOtherLoansAmount.ToString("f2", CommentFormattingCulture)} will be used to settle other loans").Concat(additionalCommentTexts).ToList();
            }

            var additionalCommentText = additionalCommentTexts.Count == 0 ? "" : (" " + string.Join(", ", additionalCommentTexts));

            AddComment(
                $"New credit created: {(paidToCustomerAmount - settleOtherLoansAmount).ToString("f2", CommentFormattingCulture)} will be paid to the customer.{additionalCommentText}",
                BusinessEventType.NewCredit,
                context,
                credit: credit,
                attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(commentArchiveKeys));

            if (saveEncryptedPaymentItems != null)
                saveEncryptedPaymentItems();

            return credit;
        }

        private void HandleRepaymentModel(CreditHeader credit, BusinessEvent newCreditEvent, ICreditContextExtended context, NewCreditRequest request,
            decimal interestRatePercent, DateTime bookKeepingDate)
        {
            PaymentPlanCalculation.PaymentPlanCalculationBuilder paymentPlanCalcuation;
            var loanAmount = request.GetComputedCreditAmount();
            if (request.AnnuityAmount.HasValue)
            {
                paymentPlanCalcuation = PaymentPlanCalculation
                    .BeginCreateWithAnnuity(loanAmount, request.AnnuityAmount.Value, interestRatePercent, null, envSettings.CreditsUse360DayInterestYear);
            }
            else if (request.FixedMonthlyCapitalAmount.HasValue)
            {
                paymentPlanCalcuation = PaymentPlanCalculation
                    .BeginCreateWithFixedMonthlyCapitalAmount(loanAmount, request.FixedMonthlyCapitalAmount.Value, interestRatePercent, null, null, 
                    envSettings.CreditsUse360DayInterestYear);
            }
            else if (request.SinglePaymentLoanRepaymentTimeInDays.HasValue)
            {
                //TODO: This is incorrect ... needs to be day based
                paymentPlanCalcuation = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount, 1, interestRatePercent, false, null, 
                    envSettings.CreditsUse360DayInterestYear);

                var dueDate = Clock.Today.AddDays(request.SinglePaymentLoanRepaymentTimeInDays.Value);
                AddDatedCreditValue(DatedCreditValueCode.NotificationDueDay, dueDate.Day, newCreditEvent, context, credit: credit);
                AddDatedCreditValue(DatedCreditValueCode.SinglePaymentLoanRepaymentDays, request.SinglePaymentLoanRepaymentTimeInDays.Value,
                    newCreditEvent, context, credit: credit);
            }
            else if (request.RepaymentTimeInMonths.HasValue)
            {
                paymentPlanCalcuation = PaymentPlanCalculation.BeginCreateWithRepaymentTime(loanAmount, request.RepaymentTimeInMonths.Value, interestRatePercent, 
                    ClientCfg.IsFeatureEnabled("ntech.feature.useannuities"), null, envSettings.CreditsUse360DayInterestYear);
            }
            else
                throw new NotImplementedException();

            if (request.NotificationFee.HasValue && request.NotificationFee > 0m)
                paymentPlanCalcuation = paymentPlanCalcuation.WithMonthlyFee(request.NotificationFee.Value);

            if (request.CapitalizedInitialFeeAmount.HasValue)
                paymentPlanCalcuation = paymentPlanCalcuation.WithInitialFeeCapitalized(request.CapitalizedInitialFeeAmount.Value);

            if(request.FirstNotificationCosts != null && request.FirstNotificationCosts.Count > 0)
            {
                var costSum = request.FirstNotificationCosts.Sum(x => x.CostAmount);
                if(costSum > 0m)
                {
                    paymentPlanCalcuation = paymentPlanCalcuation.WithInitialFeePaidOnFirstNotification(costSum);
                }                
                var definedCosts = customCostTypeService.GetDefinedCodes();
                foreach(var firstNotificationCost in request.FirstNotificationCosts.Where(x => x.CostAmount > 0m))
                {
                    if (!definedCosts.Contains(firstNotificationCost.CostCode))
                        throw new NTechCoreWebserviceException("FirstNotificationCosts contains code that does not exist") { ErrorCode = "undefinedCustomCostCode", ErrorHttpStatusCode = 400, IsUserFacing = true };

                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedNotificationCost,
                        firstNotificationCost.CostAmount,
                        bookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        subAccountCode: firstNotificationCost.CostCode));
                }
            }

            if (request.GetDrawnFromLoanAmountInitialFeeAmount() > 0m)
                paymentPlanCalcuation = paymentPlanCalcuation.WithInitialFeeDrawnFromLoanAmount(request.GetDrawnFromLoanAmountInitialFeeAmount());

            var paymentPlan = paymentPlanCalcuation.EndCreate();

            if (paymentPlan.UsesAnnuities)
            {
                AddDatedCreditString(DatedCreditStringCode.AmortizationModel.ToString(), AmortizationModelCode.MonthlyAnnuity.ToString(), credit, newCreditEvent, context);
                AddDatedCreditValue(DatedCreditValueCode.AnnuityAmount.ToString(), paymentPlan.AnnuityAmount, credit, newCreditEvent, context);
            }
            else
            {
                AddDatedCreditString(DatedCreditStringCode.AmortizationModel.ToString(), AmortizationModelCode.MonthlyFixedAmount.ToString(), credit, newCreditEvent, context);
                AddDatedCreditValue(DatedCreditValueCode.MonthlyAmortizationAmount.ToString(), paymentPlan.FixedMonthlyCapitalAmount, credit, newCreditEvent, context);
            }

            try
            {
                AddDatedCreditValue(DatedCreditValueCode.InitialRepaymentTimeInMonths.ToString(), paymentPlan.Payments.Count, credit, newCreditEvent, context);
                AddDatedCreditValue(DatedCreditValueCode.InitialEffectiveInterestRatePercent.ToString(), paymentPlan.EffectiveInterestRatePercent.Value, credit, newCreditEvent, context);
            }
            catch (PaymentPlanCalculationException)
            {
                /*
                 * Temporary guard since ul legacy used to allow loan which are never repaid to be created. This feature will not be set on any client but
                 * will be kept as a backup for one release to ensure ul legacy doesnt rely on this in some way we did not anticipate.
                 */
                if (ClientCfg.IsFeatureEnabled("ntech.allowneverrepaid"))
                    return;
                throw;
            }
        }

        private void SetCreditDirectDebitDetails(NewCreditRequest request, CreditHeader credit, ICreditContextExtended context, BusinessEvent evt)
        {
            IBankAccountNumber directDebitBankAccountNr = null;
            if (!bankAccountNumberParser.TryParseFromStringWithDefaults(request.DirectDebitDetails.AccountNr, null, out directDebitBankAccountNr))
            {
                throw new Exception("Invalid direct debit bank account nr");
            }

            AddDatedCreditString(DatedCreditStringCode.DirectDebitBankAccountNr.ToString(), directDebitBankAccountNr.FormatFor(null), credit, evt, context);

            if (request.DirectDebitDetails.IsActive != null)
                AddDatedCreditString(DatedCreditStringCode.IsDirectDebitActive.ToString(), request.DirectDebitDetails.IsActive.ToString().ToLower(), credit, evt, context);

            if (request.DirectDebitDetails.AccountOwner != null)
                AddDatedCreditString(DatedCreditStringCode.DirectDebitAccountOwnerApplicantNr.ToString(), request.DirectDebitDetails.AccountOwner.ToString(), credit, evt, context);

            if (request.DirectDebitDetails.DirectDebitConsentFileArchiveKey != null)
                AddCreditDocument("DirectDebitConsent", null, request.DirectDebitDetails.DirectDebitConsentFileArchiveKey, context, credit: credit);

            if (request.DirectDebitDetails.IsExternalStatusActive != null)
            {
                AddDatedCreditString("DirectDebitIsExternalStatusActive", request.DirectDebitDetails.IsExternalStatusActive.ToString().ToLower(), credit, evt, context);

                if (request.DirectDebitDetails.IsExternalStatusActive == true)
                {
                    if (request.DirectDebitDetails.AccountOwner == null)
                    {
                        throw new Exception("DirectDebitExternalStatus can't be set to active without an account owner");
                    }

                    var mgr = new DirectDebitOnCreditCreationBusinessEventManager(CurrentUser, Clock, ClientCfg);
                    var g = new NTech.Banking.Autogiro.AutogiroPaymentNumberGenerator();
                    var accountOwnerApplicant = request.Applicants.Where(x => x.ApplicantNr == request.DirectDebitDetails.AccountOwner).FirstOrDefault();

                    var incomingPaymentAccount = paymentAccountService.GetIncomingPaymentBankAccountNr();
                    if (!mgr.TryScheduleDirectDebitActivation(context, request.CreditNr, (NTech.Banking.BankAccounts.Se.BankAccountNumberSe)directDebitBankAccountNr, g.GenerateNr(request.CreditNr, accountOwnerApplicant.ApplicantNr), accountOwnerApplicant.CustomerId, incomingPaymentAccount, out string failedMessage))
                    {
                        throw new Exception("Error when scheduling external direct debit status");
                    }
                }
            }
        }

        private void ValidateCreditRequest(NewCreditRequest request)
        {
            void ThrowUserFacingError(string message, string code = null) => 
                throw new NTechCoreWebserviceException(message) { IsUserFacing = true, ErrorHttpStatusCode = 400, ErrorCode = code };

            var isCompanyCredit = request.IsCompanyCredit.GetValueOrDefault();
            if (isCompanyCredit)
            {
                if (request.Applicants.Count != 1)
                    ThrowUserFacingError("Company credits can only have one applicant, the company");

                var customerId = request.Applicants.Single().CustomerId;

                var orgnr = customerClient.BulkFetchPropertiesByCustomerIdsD(new HashSet<int> { customerId }, "orgnr")?.OptVal(customerId)?.OptVal("orgnr");
                if (string.IsNullOrWhiteSpace(orgnr))
                    ThrowUserFacingError("Customer missing orgnr");
            }

            if(new[] { request.SinglePaymentLoanRepaymentTimeInDays.HasValue, request.RepaymentTimeInMonths.HasValue, request.AnnuityAmount.HasValue, request.FixedMonthlyCapitalAmount.HasValue }
                .Where(hasValue => hasValue == true)
                .Count() != 1)
            {
                ThrowUserFacingError("Exactly one of SinglePaymentLoanRepaymentTimeInDays, RepaymentTimeInMonths, AnnuityAmount or FixedMonthlyCapitalAmount must be used");
            }

            if(request.SinglePaymentLoanRepaymentTimeInDays.HasValue && request.SinglePaymentLoanRepaymentTimeInDays.Value < 10)
                ThrowUserFacingError("SinglePaymentLoanRepaymentTimeInDays must be >= 10");

            if(request.FirstNotificationCosts != null && request.FirstNotificationCosts.Count > 0)
            {
                var definedCodes = customCostTypeService.GetDefinedCodes();
                if(request.FirstNotificationCosts.Any(x => !definedCodes.Contains(x.CostCode)))
                {
                    throw new NTechCoreWebserviceException("FirstNotificationCosts contains undefined custom cost codes");
                }
            }
        }
    }
}