using nCredit.Code;
using nCredit.Code.Services;
using nCredit.DbModel.DomainModel;
using nCredit.DomainModel;
using Newtonsoft.Json;
using NTech.Banking.LoanModel;
using NTech.Core;
using NTech.Core.Credit.Shared.Database;
using NTech.Core.Credit.Shared.Services;
using NTech.Core.Module.Shared.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace nCredit.DbModel.BusinessEvents
{
    public class NewMortgageLoanBusinessEventManager : BusinessEventManagerOrServiceBase
    {
        private readonly ICreditCustomerListServiceComposable creditCustomerListService;
        private readonly OcrPaymentReferenceGenerator ocrPaymentReferenceGenerator;
        private readonly ICreditEnvSettings creditEnvSettings;
        private readonly INotificationProcessSettingsFactory processSettingsFactory;
        private readonly CustomCostTypeService customCostTypeService;

        public NewMortgageLoanBusinessEventManager(INTechCurrentUserMetadata currentUser, ICreditCustomerListServiceComposable creditCustomerListService, OcrPaymentReferenceGenerator ocrPaymentReferenceGenerator,
            ICoreClock clock, IClientConfigurationCore clientConfiguration, ICreditEnvSettings creditEnvSettings, INotificationProcessSettingsFactory processSettingsFactory,
            CustomCostTypeService customCostTypeService) : base(currentUser, clock, clientConfiguration)
        {
            this.creditCustomerListService = creditCustomerListService;
            this.ocrPaymentReferenceGenerator = ocrPaymentReferenceGenerator;
            this.creditEnvSettings = creditEnvSettings;
            this.processSettingsFactory = processSettingsFactory;
            this.customCostTypeService = customCostTypeService;
        }

        public CreditHeader CreateNewMortgageLoan(ICreditContextExtended context, MortgageLoanRequest request)
        {
            context.EnsureCurrentTransaction();
            var model = new SharedDatedValueDomainModel(context);
            return CreateNewMortgageLoan(context, request, new Lazy<decimal>(() => model.GetReferenceInterestRatePercent(Clock.Today)));
        }

        public CreditHeader CreateNewMortgageLoan(ICreditContextExtended context,
            MortgageLoanRequest request,
            Lazy<decimal> currentReferenceInterestRate,
            BusinessEvent existingEvent = null,
            CollateralHeader existingCollateral = null)
        {
            context.EnsureCurrentTransaction();

            BusinessEvent newCreditEvent;
            DateTimeOffset startDate;
            if (existingEvent == null)
            {
                if (request.HistoricalStartDate.HasValue)
                {
                    if (request.HistoricalStartDate.Value.Date > context.CoreClock.Today.Date)
                        throw new NTechCoreWebserviceException("Historical transaction date cannot be a future date") { IsUserFacing = true, ErrorHttpStatusCode = 400 };
                    var d = request.HistoricalStartDate.Value;
                    startDate = new DateTimeOffset(d.Year, d.Month, d.Day, 12, 0, 0, Now.Offset); //Put it midday to avoid timezone issues changing the date;
                }
                else
                    startDate = Now;
                 
                newCreditEvent = new BusinessEvent
                {
                    EventDate = Now,
                    EventType = BusinessEventType.NewMortgageLoan.ToString(),
                    BookKeepingDate = startDate.Date,
                    TransactionDate = request.HistoricalStartDate?.Date ?? Now.ToLocalTime().Date,
                };
                FillInInfrastructureFields(newCreditEvent);
                context.AddBusinessEvent(newCreditEvent);
            }
            else
            {
                if (request.HistoricalStartDate.HasValue)
                    throw new NTechCoreWebserviceException("Cannot combine HistoricalStartDate and existingEvent");
                startDate = Now;
                newCreditEvent = existingEvent;
            }

            return CreateNewMortgageLoanInternal(context, request  , currentReferenceInterestRate, newCreditEvent, existingCollateral, startDate);
        }

        private CreditHeader CreateNewMortgageLoanInternal(ICreditContextExtended context,
            MortgageLoanRequest request,
            Lazy<decimal> currentReferenceInterestRate,
            BusinessEvent newCreditEvent,
            CollateralHeader existingCollateral,
            DateTimeOffset startDate)
        {
            context.EnsureCurrentTransaction();

            var additionalCommentTexts = new List<string>();            

            if (existingCollateral != null && request.CollateralId.HasValue)
            {
                throw new NTechCoreWebserviceException("Cannot combine existingCollateral and CollateralId");
            }

            var credit = new CreditHeader
            {
                CreditNr = request.CreditNr,
                CreditType = CreditType.MortgageLoan.ToString(),
                StartDate = startDate,
                NrOfApplicants = request.NrOfApplicants,
                ProviderName = request.ProviderName,
                CreatedByEvent = newCreditEvent,
                CollateralHeaderId = request.CollateralId,
                Collateral = existingCollateral
            };
            FillInInfrastructureFields(credit);
            context.AddCreditHeader(credit);

            var historicalStartTransactionDate = new GuardedHistoricalTransactionDate(context, request.HistoricalStartDate, Clock, credit);
            if (historicalStartTransactionDate.HistoricalTransactionDate.HasValue)
            {
                newCreditEvent.TransactionDate = historicalStartTransactionDate.HistoricalTransactionDate.Value;
            }

            SetStatus(credit, CreditStatus.Normal, newCreditEvent, context);

            AddDatedCreditValue(DatedCreditValueCode.NotificationFee.ToString(), request.MonthlyFeeAmount, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);

            AddDatedCreditDate(DatedCreditDateCode.MortgageLoanInitialSettlementDate, request.SettlementDate, newCreditEvent, context, credit: credit);

            if (request.EndDate.HasValue)
            {
                additionalCommentTexts.Add($"With end date {request.EndDate.Value.ToString("yyyy-MM-dd")}.");
                AddDatedCreditDate(DatedCreditDateCode.MortgageLoanEndDate, request.EndDate.Value, newCreditEvent, context, credit: credit);
            }

            if (!string.IsNullOrWhiteSpace(request.MortgageLoanAgreementNr))
                AddDatedCreditString(DatedCreditStringCode.MortgageLoanAgreementNr.ToString(), request.MortgageLoanAgreementNr, credit, newCreditEvent, context);

            HandleInitialInterestRate(request, currentReferenceInterestRate, context, newCreditEvent, credit, additionalCommentTexts, historicalStartTransactionDate);

            HandleAmortization(request, context, newCreditEvent, credit, historicalStartTransactionDate);

            var commentArchiveKeys = new List<string>();
            foreach (var a in request.Applicants.OrderBy(x => x.ApplicantNr).ToList())
            {
                if (a.OwnershipPercent.HasValue)
                    AddDatedCreditCustomerValue(DatedCreditCustomerValueCode.OwnerShipPercent.ToString(), a.OwnershipPercent.Value, credit, newCreditEvent, context, a.CustomerId, historicalTransactionDate: historicalStartTransactionDate);

                context.AddCreditCustomer(FillInInfrastructureFields(new CreditCustomer
                {
                    ApplicantNr = a.ApplicantNr,
                    CustomerId = a.CustomerId,
                    Credit = credit,
                }));
                if (a.AgreementPdfArchiveKey != null)
                {
                    AddDatedCreditString(DatedCreditStringCode.SignedInitialAgreementArchiveKey.ToString() + a.ApplicantNr, a.AgreementPdfArchiveKey, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                    commentArchiveKeys.Add(a.AgreementPdfArchiveKey);
                }
            }

            Action<string, List<int>> addCustomers = (listName, customerIds) =>
            {
                if (customerIds == null)
                    return;
                foreach (var customerId in customerIds)
                    creditCustomerListService.SetMemberStatusComposable(context, listName, true, customerId, credit: credit, evt: newCreditEvent);
            };

            addCustomers("mortgageLoanApplicant", request.Applicants.Select(x => x.CustomerId).ToList());
            addCustomers("mortgageLoanConsentingParty", request.ConsentingPartyCustomerIds);
            addCustomers("mortgageLoanPropertyOwner", request.PropertyOwnerCustomerIds);

            AddDatedCreditString(DatedCreditStringCode.OcrPaymentReference.ToString(), ocrPaymentReferenceGenerator.GenerateNew().NormalForm, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);

            if (!string.IsNullOrWhiteSpace(request.SharedOcrPaymentReference))
            {
                AddDatedCreditString(DatedCreditStringCode.SharedOcrPaymentReference.ToString(), request.SharedOcrPaymentReference, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                additionalCommentTexts.Add($"Shared payment ocr: {request.SharedOcrPaymentReference}.");
            }

            if (!string.IsNullOrWhiteSpace(request.ProviderApplicationId))
            {
                AddDatedCreditString(DatedCreditStringCode.ProviderApplicationId.ToString(), request.ProviderApplicationId, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
            }
            if (!string.IsNullOrWhiteSpace(request.ApplicationNr))
            {
                AddDatedCreditString(DatedCreditStringCode.ApplicationNr.ToString(), request.ApplicationNr, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
            }

            if (!string.IsNullOrWhiteSpace(request.KycQuestionsJsonDocumentArchiveKey))
            {
                AddDatedCreditString(DatedCreditStringCode.KycQuestionsJsonDocumentArchiveKey.ToString(), request.KycQuestionsJsonDocumentArchiveKey, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
            }

            if (request.LoanAmount.HasValue == (request.LoanAmountParts != null && request.LoanAmountParts.Count > 0))
            {
                throw new NTechCoreWebserviceException("Exactly one of LoanAmount and LoanAmountParts must be specified");
            }

            List<MortgageLoanRequest.AmountModel> loanAmountParts = null;
            if (request.LoanAmount.HasValue)
            {
                loanAmountParts = new List<MortgageLoanRequest.AmountModel> { new MortgageLoanRequest.AmountModel { Amount = request.LoanAmount.Value, SubAccountCode = null } };
            }
            else
                loanAmountParts = request.LoanAmountParts;

            var loanAmount = 0m;
            foreach (var p in loanAmountParts)
            {
                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.NotNotifiedCapital,
                    p.Amount,
                    newCreditEvent.BookKeepingDate,
                    newCreditEvent,
                    credit: credit,
                    businessEventRuleCode: "InitialLoan",
                    historicalTransactionDate: historicalStartTransactionDate,
                    subAccountCode: p.SubAccountCode));

                context.AddAccountTransactions(CreateTransaction(
                    TransactionAccountType.CapitalDebt,
                    p.Amount,
                    newCreditEvent.BookKeepingDate,
                    newCreditEvent,
                    credit: credit,
                    businessEventRuleCode: "InitialLoan",
                    historicalTransactionDate: historicalStartTransactionDate,
                    subAccountCode: p.SubAccountCode));
                loanAmount += p.Amount;
            }

            if (request.DrawnFromLoanAmountInitialFees != null && request.DrawnFromLoanAmountInitialFees.Count > 0)
            {
                var totalInitialFeesAmount = 0m;
                foreach (var fee in request.DrawnFromLoanAmountInitialFees)
                {
                    if (fee.Amount > 0m)
                    {
                        context.AddAccountTransactions(CreateTransaction(
                            TransactionAccountType.InitialFeeDrawnFromLoanAmount,
                            fee.Amount,
                            newCreditEvent.BookKeepingDate,
                            newCreditEvent,
                            credit: credit,
                            businessEventRuleCode: "initialFee",
                            subAccountCode: fee.SubAccountCode));
                        totalInitialFeesAmount += fee.Amount;
                    }
                }

                additionalCommentTexts.Add($"Initial fee of {totalInitialFeesAmount.ToString("f2")} withheld");
            }

            if (request.CapitalizedInitialFees != null && request.CapitalizedInitialFees.Count > 0)
            {
                var totalInitialFeesAmount = 0m;
                foreach (var fee in request.CapitalizedInitialFees)
                {
                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedCapital,
                        fee.Amount,
                        newCreditEvent.BookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        businessEventRuleCode: "initialFee",
                        subAccountCode: fee.SubAccountCode));

                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.CapitalDebt,
                        fee.Amount,
                        newCreditEvent.BookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        businessEventRuleCode: "initialFee",
                        subAccountCode: fee.SubAccountCode));

                    totalInitialFeesAmount += fee.Amount;

                    additionalCommentTexts.Add($"Initial fee of {totalInitialFeesAmount.ToString("f2")} capitalized");
                }
            }

            if (request.FirstNotificationCosts != null && request.FirstNotificationCosts.Count > 0)
            {
                var definedCosts = customCostTypeService.GetDefinedCodes();
                foreach (var firstNotificationCost in request.FirstNotificationCosts.Where(x => x.CostAmount > 0m))
                {
                    if (!definedCosts.Contains(firstNotificationCost.CostCode))
                        throw new NTechCoreWebserviceException("FirstNotificationCosts contains code that does not exist") { ErrorCode = "undefinedCustomCostCode", ErrorHttpStatusCode = 400, IsUserFacing = true };

                    context.AddAccountTransactions(CreateTransaction(
                        TransactionAccountType.NotNotifiedNotificationCost,
                        firstNotificationCost.CostAmount,
                        newCreditEvent.BookKeepingDate,
                        newCreditEvent,
                        credit: credit,
                        subAccountCode: firstNotificationCost.CostCode));
                }
            }

            if (request.Documents != null)
            {
                foreach (var d in request.Documents)
                {
                    AddCreditDocument(d.DocumentType, d.ApplicantNr, d.ArchiveKey, context, credit: credit);
                }
            }

            HandleObject(request, context, newCreditEvent, credit);

            if (request.ActiveDirectDebitAccount != null)
            {
                if (request.ActiveDirectDebitAccount.BankAccountNrOwnerApplicantNr < 1 || request.ActiveDirectDebitAccount.BankAccountNrOwnerApplicantNr > request.NrOfApplicants)
                    throw new Exception("Invalid direct debit account applicant nr");
                AddDatedCreditString(DatedCreditStringCode.IsDirectDebitActive.ToString(), "true", credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                AddDatedCreditString(DatedCreditStringCode.DirectDebitAccountOwnerApplicantNr.ToString(), request.ActiveDirectDebitAccount.BankAccountNrOwnerApplicantNr.ToString(), credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                if (!string.IsNullOrWhiteSpace(request.ActiveDirectDebitAccount.BankAccountNr))
                    AddDatedCreditString(DatedCreditStringCode.DirectDebitBankAccountNr.ToString(), request.ActiveDirectDebitAccount.BankAccountNr, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);

                additionalCommentTexts.Add($"Direct debit (autogiro) is active since {request.ActiveDirectDebitAccount.ActiveSinceDate.ToString("yyyy-MM-dd HH:mm")} for applicant {request.ActiveDirectDebitAccount.BankAccountNrOwnerApplicantNr} connected to account {request.ActiveDirectDebitAccount.BankAccountNr}.");
            }

            if (ClientCfg.Country.BaseCountry == "FI")
            {
                AddDatedCreditString(DatedCreditStringCode.IsForNonPropertyUse.ToString(), request.IsForNonPropertyUse ? "true" : "false", credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                if (request.IsForNonPropertyUse)
                    additionalCommentTexts.Add("Is for non property use.");
            }

            if (!string.IsNullOrWhiteSpace(request.MainCreditCreditNr))
            {
                AddDatedCreditString(DatedCreditStringCode.MainCreditCreditNr.ToString(), request.MainCreditCreditNr, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                additionalCommentTexts.Add($"Is a child loan of {request.MainCreditCreditNr}");
            }

            if (creditEnvSettings.HasPerLoanDueDay)
            {
                var d = request.NotificationDueDay ?? 28;
                if (d > 31 || d < 0)
                    d = 28;
                AddDatedCreditValue(DatedCreditValueCode.NotificationDueDay.ToString(), request.NotificationDueDay ?? 28, credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                if (string.IsNullOrWhiteSpace(request.MainCreditCreditNr))
                    additionalCommentTexts.Add($"Notification due day will be {d}");
            }

            if (historicalStartTransactionDate.HistoricalTransactionDate.HasValue && historicalStartTransactionDate.HistoricalTransactionDate.Value != Clock.Today)
            {
                additionalCommentTexts.Add($"Historical start date {historicalStartTransactionDate.HistoricalTransactionDate.Value.ToString("yyyy-MM-dd")} used.");
            }

            if (ClientCfg.Country.BaseCountry == "SE" && request.Applicants.All(x => x.OwnershipPercent.HasValue))
            {
                if (request.Applicants.Count == 1)
                {
                    additionalCommentTexts.Add($"Ownership distribution: 100%.");
                }
                else
                {
                    var ownerShipPercentText = new System.Text.StringBuilder();
                    var index = 0;
                    foreach (var a in request.Applicants.OrderBy(x => x.ApplicantNr).ToList())
                    {
                        ownerShipPercentText.Append(a.OwnershipPercent.Value.ToString("f2", CommentFormattingCulture) + "%");
                        if (index != request.Applicants.Count - 1)
                            ownerShipPercentText.Append(", ");
                        index++;
                    }
                    additionalCommentTexts.Add($"Ownership distribution: {ownerShipPercentText}.");
                }
            }

            if(!string.IsNullOrWhiteSpace(request.LoanOwnerName))
            {
                AddDatedCreditString(DatedCreditStringCode.LoanOwner.ToString(), request.LoanOwnerName, credit, newCreditEvent, context);
            }

            AddInitialRepaymentTime(credit, context);

            var comment = string.Join(" ", Enumerables
                .Singleton($"New mortgage loan created. Any payments associated with the initial balance of {loanAmount.ToString("f2", CommentFormattingCulture)} is assumed to have been already handled.")
                .Union(additionalCommentTexts));

            AddComment(
                comment,
                BusinessEventType.NewMortgageLoan,
                context,
                credit: credit,
                attachment: CreditCommentAttachmentModel.ArchiveKeysOnly(commentArchiveKeys));

            return credit;
        }

        private void AddInitialRepaymentTime(CreditHeader credit, ICreditContextExtended context)
        {
            string GetCode(DatedCreditStringCode code, bool isRequired) =>
                (isRequired ? credit.DatedCreditStrings.Single(x => x.Name == code.ToString()) : credit.DatedCreditStrings.SingleOrDefault(x => x.Name == code.ToString()))?.Value;
            decimal? GetValue(DatedCreditValueCode code, bool isRequired) =>
                (isRequired ? credit.DatedCreditValues.Single(x => x.Name == code.ToString()) : credit.DatedCreditValues.SingleOrDefault(x => x.Name == code.ToString()))?.Value;
            DateTime? GetDate(DatedCreditDateCode code, bool isRequired) =>
                (isRequired ? credit.DatedCreditDates.Single(x => x.Name == code.ToString()) : credit.DatedCreditDates.SingleOrDefault(x => x.Name == code.ToString()))?.Value;

            CreditAmortizationModel amortizationModel;
            if(GetCode(DatedCreditStringCode.AmortizationModel, true) == AmortizationModelCode.MonthlyFixedAmount.ToString())
            {
                amortizationModel = CreditAmortizationModel.CreateMonthlyFixedCapitalAmount(
                    GetValue(DatedCreditValueCode.MonthlyAmortizationAmount, true).Value,
                    null,
                    GetDate(DatedCreditDateCode.AmortizationExceptionUntilDate, false),
                    GetValue(DatedCreditValueCode.ExceptionAmortizationAmount, false));
            }
            else
            {
                amortizationModel = CreditAmortizationModel.CreateAnnuity(GetValue(DatedCreditValueCode.AnnuityAmount, true).Value, null);
            }

            var notificationDueDay = GetValue(DatedCreditValueCode.NotificationDueDay, false);

            var creditModel = new HistoricalCreditModel
            {
                AmortizationModel = amortizationModel,
                CreatedByEvent = new HistoricalCreditModel.ModelBusinessEvent
                {
                    EventType = credit.CreatedByEvent.EventType,
                    TransactionDate = credit.CreatedByEvent.TransactionDate
                },
                CreditNr = credit.CreditNr,
                CreditType = credit.CreditType,
                IsMortgageLoan = true,
                CurrentCapitalBalance = credit.Transactions.Where(x => x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Sum(x => x.Amount),
                EndDate = GetDate(DatedCreditDateCode.MortgageLoanEndDate, true).Value,
                InterestRebindMonthCount = GetValue(DatedCreditValueCode.MortgageLoanInterestRebindMonthCount, false),
                MarginInterestRatePercent = GetValue(DatedCreditValueCode.MarginInterestRate, true).Value,
                ReferenceInterestRatePercent = GetValue(DatedCreditValueCode.ReferenceInterestRate, true).Value,
                NextInterestRebindDate = GetDate(DatedCreditDateCode.MortgageLoanNextInterestRebindDate, false),
                NotificationDueDay = notificationDueDay.HasValue ? (int)notificationDueDay.Value : new int?(),
                NotificationFee = GetValue(DatedCreditValueCode.NotificationFee, false) ?? 0m,
                NrOfPaidNotifications = 0,
                PendingFuturePaymentFreeMonths = new List<HistoricalCreditModel.PendingFuturePaymentFreeMonthModel>(),
                ProcessSuspendingTerminationLetters = new List<HistoricalCreditModel.ProcessSuspendingTerminationLetter>(),
                Status = credit.Status,
                SinglePaymentLoanRepaymentDays = new int?(),
                Transactions = credit.Transactions.Where(x => x.AccountCode == TransactionAccountType.NotNotifiedCapital.ToString()).Select(x => new HistoricalCreditModel.ModelTransaction
                {
                    AccountCode = x.AccountCode,
                    Amount = x.Amount,
                    BusinessEvent = new HistoricalCreditModel.ModelBusinessEvent
                    {
                        EventType = x.BusinessEvent.EventType,
                        TransactionDate = x.BusinessEvent.TransactionDate
                    },
                    BusinessEventRoleCode = x.BusinessEventRoleCode,
                    CreditNotificationDueDate = null,
                    IsIncomingPayment = false,
                    IsWriteOff = false,
                    PaymentFreeMonthDueDate = null
                }).ToList()
            };

            var processSettings = processSettingsFactory.GetByCreditType(CreditType.MortgageLoan);

            if (!AmortizationPlan.TryGetAmortizationPlan(creditModel, processSettings, out var amortPlan, out var failedMessage, Clock, ClientCfg,
                CreditDomainModel.GetInterestDividerOverrideByCode(creditEnvSettings.ClientInterestModel)))
                throw new NTechCoreWebserviceException(failedMessage);

            AddDatedCreditValue(DatedCreditValueCode.InitialRepaymentTimeInMonths, amortPlan.NrOfRemainingPayments, credit.CreatedByEvent, context, credit: credit);
        }

        private void HandleObject(MortgageLoanRequest request, ICreditContextExtended context, BusinessEvent newCreditEvent, CreditHeader credit)
        {
            if (request.Collaterals != null)
            {
                KeyValueStoreService.SetValueComposable(context, credit.CreditNr,
                    KeyValueStoreKeySpaceCode.MortgageLoanCollateralsV1.ToString(),
                    JsonConvert.SerializeObject(request.Collaterals));
            }
        }        

        private void HandleAmortization(MortgageLoanRequest request, ICreditContextExtended context, BusinessEvent newCreditEvent, CreditHeader credit, GuardedHistoricalTransactionDate historicalStartTransactionDate)
        {
            if (request.AnnuityAmount.HasValue == request.ActualAmortizationAmount.HasValue)
                throw new Exception("Exactly one of AnnuityAmount and ActualAmortizationAmount must be set");

            if (request.ActualAmortizationAmount.HasValue)
            {
                AddDatedCreditString(DatedCreditStringCode.AmortizationModel.ToString(), AmortizationModelCode.MonthlyFixedAmount.ToString(), credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                AddDatedCreditValue(DatedCreditValueCode.MonthlyAmortizationAmount.ToString(), request.ActualAmortizationAmount.Value, credit, newCreditEvent, context);
            }
            else
            {
                AddDatedCreditString(DatedCreditStringCode.AmortizationModel.ToString(), AmortizationModelCode.MonthlyAnnuity.ToString(), credit, newCreditEvent, context, historicalTransactionDate: historicalStartTransactionDate);
                AddDatedCreditValue(DatedCreditValueCode.AnnuityAmount.ToString(), request.AnnuityAmount.Value, credit, newCreditEvent, context);
                if (request.AmortizationExceptionUntilDate.HasValue)
                    throw new Exception("AmortizationExceptionUntilDate cannot be used with annuities as amortization is fixed by the annuity");
            }

            if (request.AmortizationExceptionUntilDate.HasValue)
            {
                AddDatedCreditDate(DatedCreditDateCode.AmortizationExceptionUntilDate, request.AmortizationExceptionUntilDate.Value, newCreditEvent, context, credit: credit);
                AddDatedCreditValue(DatedCreditValueCode.ExceptionAmortizationAmount.ToString(), request.ExceptionAmortizationAmount.Value, credit, newCreditEvent, context);
                AddDatedCreditString(
                    DatedCreditStringCode.AmortizationExceptionReasons.ToString(),
                    JsonConvert.SerializeObject(request.AmortizationExceptionReasons ?? new List<string>()), credit, newCreditEvent, context);
            }
        }

        private void HandleInitialInterestRate(MortgageLoanRequest request, Lazy<decimal> currentReferenceInterestRate, ICreditContextExtended context, BusinessEvent newCreditEvent, CreditHeader credit, List<string> additionalCommentTexts, GuardedHistoricalTransactionDate historicalTransactionDate)
        {
            var referenceInterestRate = request.ReferenceInterestRate ?? currentReferenceInterestRate.Value;
            var nextInterestRebindDate = request.NextInterestRebindDate;
            var interestRebindMounthCount = request.InterestRebindMounthCount;
            if (!request.NextInterestRebindDate.HasValue && creditEnvSettings.MortgageLoanInterestBindingMonths.HasValue)
            {
                nextInterestRebindDate = Clock.Today.AddMonths(creditEnvSettings.MortgageLoanInterestBindingMonths.Value);
                interestRebindMounthCount = creditEnvSettings.MortgageLoanInterestBindingMonths.Value;
            }

            AddDatedCreditString(DatedCreditStringCode.NextInterestFromDate.ToString(), (historicalTransactionDate.HistoricalTransactionDate ?? newCreditEvent.TransactionDate).ToString("yyyy-MM-dd"), credit, newCreditEvent, context, historicalTransactionDate: historicalTransactionDate);


            var interestService = LegalInterestCeilingService.Create(null);
            var constrainedMarginInterestRate = interestService.GetConstrainedMarginInterestRate(referenceInterestRate, request.NominalInterestRatePercent);
            if (constrainedMarginInterestRate != request.NominalInterestRatePercent)
            {
                AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), constrainedMarginInterestRate, credit, newCreditEvent, context, historicalTransactionDate: historicalTransactionDate);
                AddDatedCreditValue(DatedCreditValueCode.RequestedMarginInterestRate.ToString(), request.NominalInterestRatePercent, credit, newCreditEvent, context, historicalTransactionDate: historicalTransactionDate);
                additionalCommentTexts.Add($", New Requested Margin Interest={(request.NominalInterestRatePercent / 100m).ToString("P", CommentFormattingCulture)}");
                additionalCommentTexts.Add($", New Margin Interest={(constrainedMarginInterestRate / 100m).ToString("P", CommentFormattingCulture)}");
            }
            else
            {
                AddDatedCreditValue(DatedCreditValueCode.MarginInterestRate.ToString(), request.NominalInterestRatePercent, credit, newCreditEvent, context, historicalTransactionDate: historicalTransactionDate);
            }
            AddDatedCreditValue(DatedCreditValueCode.ReferenceInterestRate.ToString(), referenceInterestRate, credit, newCreditEvent, context, historicalTransactionDate: historicalTransactionDate);

            var totalInterestRate = referenceInterestRate + request.NominalInterestRatePercent;
            additionalCommentTexts.Add($"Interest rate={(totalInterestRate / 100m).ToString("P")} ({request.NominalInterestRatePercent.ToString("f2")}+{referenceInterestRate.ToString("f2")}).");
            if (nextInterestRebindDate.HasValue)
            {
                if (!interestRebindMounthCount.HasValue)
                    throw new NTechCoreWebserviceException("NextInterestRebindDate requires InterestRebindMounthCount") { IsUserFacing = true, ErrorHttpStatusCode = 400 };

                AddDatedCreditDate(DatedCreditDateCode.MortgageLoanNextInterestRebindDate, nextInterestRebindDate.Value, newCreditEvent, context, credit: credit);
                AddDatedCreditValue(DatedCreditValueCode.MortgageLoanInterestRebindMonthCount.ToString(), interestRebindMounthCount.Value, credit, newCreditEvent, context);
                var lengthText = interestRebindMounthCount.Value % 12 == 0 ? $"{interestRebindMounthCount.Value / 12} years" : $"{interestRebindMounthCount.Value} months";
                additionalCommentTexts.Add($"Interest bound until date {nextInterestRebindDate.Value.ToString("yyyy-MM-dd")} ({lengthText}).");
            }
        }
    }
}