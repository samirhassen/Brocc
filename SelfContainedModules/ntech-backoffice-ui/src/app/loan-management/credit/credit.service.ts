import { Injectable } from '@angular/core';
import { CustomerInfoService } from 'src/app/common-components/customer-info/customer-info.service';
import { CrossModuleNavigationTarget } from 'src/app/common-services/backtarget-resolver.service';
import { ConfigService } from 'src/app/common-services/config.service';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { Dictionary, NumberDictionary, StringDictionary } from 'src/app/common.types';

@Injectable({
    providedIn: 'root',
})
export class CreditService {
    constructor(
        private apiService: NtechApiService,
        private customerService: CustomerInfoService,
        private config: ConfigService
    ) {}

    getActiveMenuItems(): MenuItemModel[] {
        let activeMenuItems: MenuItemModel[] = [];
        let add = (code: string, displayName: string, isEnabled: boolean) => {
            if (isEnabled) activeMenuItems.push({ code: code, displayName: displayName });
        };

        let isUnsecuredLoansEnabled = this.config.isFeatureEnabled('ntech.feature.unsecuredloans');
        let isDirectDebitPaymentsEnabled = this.config.isFeatureEnabled('ntech.feature.directdebitpaymentsenabled');
        let isMortgageLoansEnabled = this.config.isFeatureEnabled('ntech.feature.mortgageloans');
        let isStandardMortgageLoansEnabled = this.config.isFeatureEnabled('ntech.feature.mortgageloans.standard');
        let isSweden = this.config.baseCountry() === 'SE';

        add('details', 'Credit details', true);
        add('notifications', 'Notifications', true);
        add('changeTerms', 'Change terms', isUnsecuredLoansEnabled);
        add('mortgageLoanStandardChangeTerms', 'Change terms', isStandardMortgageLoansEnabled);
        add('mortgageloanStandardAmortizationSe', 'Amortization', isStandardMortgageLoansEnabled && isSweden);
        add('settlement', 'Settlement', true);
        add('directDebit', 'Direct debit', isDirectDebitPaymentsEnabled);
        add('customer', 'Customer', true);
        add('documents', 'Documents', true);
        add('security', 'Collateral', isMortgageLoansEnabled && !isStandardMortgageLoansEnabled);
        add('mortgageloanStandardCollateral', 'Collateral', isStandardMortgageLoansEnabled);

        return activeMenuItems;
    }

    getCreditDetails(creditNr: string): Promise<CreditDetailsResult> {
        return this.apiService.post('nCredit', 'Api/Credit/Details', { creditNr });
    }

    isMenuItemCodeActive(code: string): boolean {
        let items = this.getActiveMenuItems();
        return !!items.find((x) => x.code === code);
    }

    async fetchCreditAttentionStatus(creditNr: string) {
        return this.apiService.post<CreditAttentionStatus>('nCredit', '/Api/Credit/FetchAttentionStatus', {
            creditNr: creditNr,
        });
    }

    getCapitalDebtTransactionDetails(transactionId: number): Promise<{
        Details: AccountTransactionDetails;
    }> {
        return this.apiService.post<any>('nCredit', 'Api/AccountTransaction/CapitalDebtTransactionDetails', {
            transactionId,
        });
    }

    fetchAmortizationPlan(creditNr: string): Promise<IAmortizationPlan> {
        return this.apiService.post('NTechHost', '/Api/Credit/AmortizationPlan', { creditNr: creditNr });
    }

    addFuturePaymentFreeMonth(creditNr: string, item: IAmortizationPlanItem) {
        return this.apiService.post<IAmortizationPlan>('NTechHost', 'Api/Credit/AddFuturePaymentFreeMonth', {
            creditNr: creditNr,
            forMonth: item.eventTransactionDate,
            returningAmortizationPlan: true,
        });
    }

    cancelFuturePaymentFreeMonth(creditNr: string, item: IAmortizationPlanItem) {
        return this.apiService.post<IAmortizationPlan>('NTechHost', 'Api/Credit/CancelFuturePaymentFreeMonth', {
            creditNr: creditNr,
            forMonth: item.eventTransactionDate,
            returningAmortizationPlan: true,
        });
    }

    loadCreditComments(
        creditNr: string,
        excludeTheseEventTypes?: string[],
        onlyTheseEventTypes?: string[]
    ): Promise<CreditCommentModel[]> {
        return this.apiService.post('nCredit', '/Api/CreditComment/LoadForCredit', {
            creditNr: creditNr,
            excludeTheseEventTypes: excludeTheseEventTypes,
            onlyTheseEventTypes: onlyTheseEventTypes,
        });
    }

    createCreditComment(
        creditNr: string,
        commentText: string,
        attachedFileAsDataUrl: string,
        attachedFileName: string
    ): Promise<{ comment: CreditCommentModel }> {
        return this.apiService.post('nCredit', '/Api/CreditComment/Create', {
            creditNr: creditNr,
            commentText: commentText,
            attachedFileAsDataUrl: attachedFileAsDataUrl,
            attachedFileName: attachedFileName,
        });
    }

    getSecureMessageTexts(messageIds: number[]): Promise<GetSecureMessageTextsResponse> {
        return this.apiService.post('nCustomer', 'api/CustomerMessage/GetMessageTexts', { messageIds });
    }

    getNotifications(creditNr: string): Promise<GetCreditNotificationsResult> {
        return this.apiService.post('nCredit', 'Api/Credit/Notifications', { creditNr });
    }

    addPromisedToPayDate(creditNr: string, promisedToPayDate: string) {
        return this.apiService.post('nCredit', '/Api/Credit/PromisedToPayDate/Add', {
            creditNr: creditNr,
            promisedToPayDate: promisedToPayDate,
            avoidReaddingSameValue: true,
        });
    }

    removedPromisedToPayDate(creditNr: string) {
        return this.apiService.post('nCredit', '/Api/Credit/PromisedToPayDate/Remove', { creditNr });
    }

    getNotificationDetails(notificationId: number): Promise<GetNotificationDetailsResult> {
        return this.apiService.post('nCredit', 'Api/Credit/NotificationDetails', { notificationId });
    }

    writeOffSingleNotification(notificationId: number, fullWriteOffAmountTypeUniqueIds: string[]) {
        return this.apiService.post('nCredit', 'Api/Credit/SingleNotificationWriteOff', {
            notificationId,
            fullWriteOffAmountTypeUniqueIds,
        });
    }

    getTermChangesInitialData(creditNr: string) {
        return this.apiService.post<GetCreditTermChangesResult>('nCredit', 'Api/Credit/ChangeTerms/FetchInitialData', {
            creditNr,
        });
    }

    computeNewCreditTermChanges(
        creditNr: string,
        newRepaymentTimeInMonths: number,
        newMarginInterestRatePercent: number
    ): Promise<{ newTerms: TermsChangeData }> {
        return this.apiService.post('nCredit', 'Api/Credit/ChangeTerms/ComputeNewTerms', {
            creditNr,
            newRepaymentTimeInMonths,
            newMarginInterestRatePercent,
        });
    }

    cancelPendingCreditTermChange(id: number) {
        return this.apiService.post('nCredit', 'Api/Credit/ChangeTerms/CancelPendingTermsChange', { id });
    }

    acceptPendingCreditTermChange(id: number) {
        return this.apiService.post('nCredit', 'Api/Credit/ChangeTerms/AcceptPendingTermsChange', { id });
    }

    sendNewCreditTermChange(
        creditNr: string,
        newRepaymentTimeInMonths: number,
        newMarginInterestRatePercent: number
    ): Promise<{
        pendingTerms: PendingTermsChangeData;
        userMessage: {
            link: string;
            text: string;
            title: string;
        };
        userWarningMessage?: string;
    }> {
        return this.apiService.post('nCredit', 'Api/Credit/ChangeTerms/SendNewTerms', {
            creditNr,
            newRepaymentTimeInMonths,
            newMarginInterestRatePercent,
        });
    }

    getCreditSettlementInitialData(
        creditNr: string,
        includeNotificationEmail: boolean
    ): Promise<GetCreditSettlementInitialDataResult> {
        return this.apiService.post('NTechHost', 'Api/Credit/SettlementSuggestion/FetchInitialData', {
            creditNr,
            includeNotificationEmail,
        });
    }

    computeSettlementSuggestion(
        creditNr: string,
        settlementDate: string,
        swedishRseInterestRatePercent: number
    ): Promise<{ suggestion: SettlementSuggestionData }> {
        return this.apiService.post('NTechHost', 'Api/Credit/SettlementSuggestion/ComputeSuggestion', {
            creditNr,
            settlementDate,
            swedishRseInterestRatePercent,
        });
    }

    createAndSendSettlementSuggestion(
        creditNr: string,
        settlementDate: string,
        email: string,
        swedishRseEstimatedAmount: number,
        swedishRseInterestRatePercent: number
    ): Promise<{
        pendingOffer: PendingSettlementSuggestionData;
        userWarningMessage?: string;
    }> {
        return this.apiService.post('NTechHost', 'Api/Credit/SettlementSuggestion/CreateAndSendSuggestion', {
            creditNr,
            settlementDate,
            email,
            swedishRseEstimatedAmount,
            swedishRseInterestRatePercent,
        });
    }

    cancelPendingSettlementSuggestion(
        id: number
    ): Promise<void> {
        return this.apiService.post('NTechHost', 'Api/Credit/SettlementSuggestion/CancelPendingSuggestion', {
            id
        });
    }

    getCreditCustomersSimple(creditNr: string): Promise<GetCreditCustomersSimpleResult> {
        return this.apiService.post('nCredit', 'Api/Credit/Customers-Simple', { creditNr });
    }

    createOrUpdatePersonCustomer(request: {
        CivicRegNr: string;
        ExpectedCustomerId?: number;
        Properties: { Name: string; Value: string; ForceUpdate: boolean }[];
    }): Promise<{ CustomerId: number }> {
        return this.apiService.post('nCustomer', 'api/PersonCustomer/CreateOrUpdate', request);
    }

    addCompanyConnections(customerId: number, creditNr: string, listNames: string[]): Promise<void> {
        return this.apiService.post('nCredit', 'Api/Credit/AddCompanyConnections', {
            customerId: customerId,
            creditNr: creditNr,
            listNames: listNames,
        });
    }

    removeCompanyConnection(customerId: number, creditNr: string, listName: string): Promise<void> {
        return this.apiService.post('nCredit', 'Api/Credit/RemoveCompanyConnection', {
            customerId: customerId,
            creditNr: creditNr,
            listName: listName,
        });
    }

    /*
    fetchMortgageLoanAmortizationBasis(creditNr: string): Promise<MortgageLoanAmortizationBasisModel> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/FetchAmortizationBasis', { creditNr: creditNr });
    }
    */

    fetchMortgageLoanStandardCollaterals(creditNrs: string[]): Promise<{
        Collaterals: {
            CollateralId: number;
            CreditNrs: string[];
            CollateralItems: Dictionary<{
                Name: string;
                StringValue: string;
                DateValue: string;
                NumericValue: string;
            }>;
        }[];
    }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/Fetch-Collaterals', { creditNrs: creditNrs });
    }

    fetchCreditDocuments(
        creditNr: string,
        fetchFilenames: boolean,
        includeExtraDocuments: boolean
    ): Promise<CreditDocumentModel[]> {
        return this.apiService.post('nCredit', 'Api/Credit/Documents/Fetch', {
            creditNr: creditNr,
            fetchFilenames: fetchFilenames,
            includeExtraDocuments: includeExtraDocuments,
        });
    }

    searchCredit(searchString: string): Promise<{ hits: SearchCreditHit[] }> {
        let module = this.config.isFeatureEnabled('ntech.feature.creditsearchcore') ? 'NTechHost' : 'nCredit';
        return this.apiService.post(
            module,
            'Api/Credit/Search',
            { OmnisearchValue: searchString },
            { forceCamelCase: true }
        );
    }

    fetchCreditDirectEvents(creditNr: string): Promise<DirectDebitEventModel[]> {
        return this.apiService.post('nCredit', 'Api/Credit/DirectDebit/FetchEvents', { creditNr: creditNr });
    }

    validateBankAccountNr(bankAccountNr: string): Promise<ValidateBankAccountNrResult> {
        return this.apiService.post(
            'nCredit',
            'Api/BankAccount/ValidateNr',
            { bankAccountNr: bankAccountNr },
            { skipLoadingIndicator: true }
        );
    }

    scheduleDirectDebitActivation(
        isChangeActivated: boolean,
        creditNr: string,
        bankAccountNr: string,
        paymentNr: string,
        applicantNr: number,
        customerId: number
    ) {
        return this.apiService.post('nCredit', 'Api/Credit/DirectDebit/ScheduleActivation', {
            isChangeActivated: isChangeActivated,
            creditNr: creditNr,
            bankAccountNr: bankAccountNr,
            paymentNr: paymentNr,
            applicantNr: applicantNr,
            customerId: customerId,
        });
    }

    scheduleDirectDebitChange(
        currentStatus: string,
        isChangeActivated: boolean,
        creditNr: string,
        bankAccountNr: string,
        paymentNr: string,
        applicantNr: number,
        customerId: number
    ) {
        return this.apiService.post('nCredit', 'Api/Credit/DirectDebit/ScheduleChange', {
            currentStatus: currentStatus,
            isChangeActivated: isChangeActivated,
            creditNr: creditNr,
            bankAccountNr: bankAccountNr,
            paymentNr: paymentNr,
            applicantNr: applicantNr,
            customerId: customerId,
        });
    }

    scheduleDirectDebitCancellation(creditNr: string, isChangeActivated: boolean, paymentNr: string) {
        return this.apiService.post('nCredit', 'Api/Credit/DirectDebit/ScheduleCancellation', {
            creditNr: creditNr,
            isChangeActivated: isChangeActivated,
            paymentNr: paymentNr,
        });
    }

    updateDirectDebitCheckStatus(
        creditNr: string,
        newStatus: string,
        bankAccountNr: string,
        bankAccountOwnerApplicantNr: number
    ) {
        return this.apiService.post('nCredit', 'Api/Credit/DirectDebit/UpdateStatus', {
            creditNr: creditNr,
            newStatus: newStatus,
            bankAccountNr: bankAccountNr,
            bankAccountOwnerApplicantNr: bankAccountOwnerApplicantNr,
        });
    }

    removeDirectDebitSchedulation(creditNr: string, paymentNr: string) {
        return this.apiService.post('nCredit', '/Api/Credit/DirectDebit/RemoveSchedulation', {
            creditNr: creditNr,
            paymentNr: paymentNr,
        });
    }

    fetchCreditDirectDebitDetails(
        creditNr: string,
        backTarget: string,
        includeEvents: boolean
    ): Promise<FetchCreditDirectDebitDetailsResult> {
        return this.apiService.post('nCredit', '/Api/Credit/DirectDebit/FetchDetails', {
            creditNr: creditNr,
            backTarget: backTarget,
            includeEvents: includeEvents,
        });
    }

    setDatedCreditValue(
        creditNr: string,
        datedCreditValueCode: string,
        businessEventType: string,
        value: number
    ): Promise<{ NewValue: number; BusinessEventId: number }> {
        return this.apiService.post('nCredit', 'Api/DatedCreditValue/Set', {
            creditNr,
            datedCreditValueCode,
            businessEventType,
            value,
        });
    }

    inactivateTerminationLetters(creditNrs: string[]) {
        return this.apiService.post<{ inactivatedOnCreditNrs: string[] }>(
            'NTechHost',
            'Api/Credit/TerminationLetters/Inactivate-On-Credits',
            { creditNrs },
            { forceCamelCase: true }
        );
    }

    setupMortgageLoanStandardParties(
        creditCustomerData: GetCreditCustomersSimpleResult,
        customerData: NumberDictionary<StringDictionary>,
        targetToHere: CrossModuleNavigationTarget
    ): MortgageLoanStandardParty[] {
        let getCustomers = (listName: string) => {
            return creditCustomerData.ListCustomers.filter((x) => x.ListName == listName).map((x) => ({
                customerCardUrl: this.customerService.getCustomerCardUrl(x.CustomerId, targetToHere.getCode()),
                firstName: customerData[x.CustomerId] ? customerData[x.CustomerId]['firstName'] : '',
                birthDate: customerData[x.CustomerId] ? customerData[x.CustomerId]['birthDate'] : '',
            }));
        };

        return [
            {
                partyTypeDisplayName: 'Owner (Lagfarna ägare / besittningsrätt)',
                customers: getCustomers('mortgageLoanPropertyOwner'),
            },
            {
                partyTypeDisplayName: 'Consenting party',
                customers: getCustomers('mortgageLoanConsentingParty'),
            },
        ];
    }

    mlStartCreditTermsChange(
        creditNr: string,
        newFixedMonthsCount: number,
        newMarginInterestRatePercent: number,
        newInterestBoundFrom: Date,
        newReferenceInterestRatePercent: number
    ): Promise<{
        pendingTerms: MlPendingTermsChangeData;
        userMessage: {
            link: string;
            text: string;
            title: string;
        };
        userWarningMessage?: string;
    }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/StartCreditTermsChange', {
            creditNr,
            newTerms: {
                newFixedMonthsCount,
                newMarginInterestRatePercent,
                newInterestBoundFrom,
                newReferenceInterestRatePercent,
            },
        });
    }

    mlSendNewCreditTermChange(
        creditNr: string,
        newRepaymentTimeInMonths: number,
        newMarginInterestRatePercent: number
    ): Promise<{
        pendingTerms: MlPendingTermsChangeData;
        userMessage: {
            link: string;
            text: string;
            title: string;
        };
        userWarningMessage?: string;
    }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/SendNewTerms', {
            creditNr,
            newRepaymentTimeInMonths,
            newMarginInterestRatePercent,
        });
    }

    mlSchedulePendingCreditTermChange(id: number) {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/SchedulePendingTermsChange', { id });
    }

    mlCancelPendingCreditTermChange(id: number) {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/CancelPendingTermsChange', {
            id,
        });
    }

    getMlTermChangesInitialData(creditNr: string) {
        return this.apiService.post<MlGetCreditTermChangesResult>(
            'nCredit',
            'Api/MortgageLoans/ChangeTerms/FetchInitialData',
            {
                creditNr,
            }
        );
    }

    mlAttachChangeTermsAgreeement(id: number, dataUrl: string, fileName: string) {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/AttachAgreement', {
            id,
            dataUrl,
            fileName,
        });
    }

    mlRemoveAttachedChangeTermsAgreeement(id: number, archiveKey: string) {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/RemoveAgreement', {
            id,
            archiveKey,
        });
    }

    mlComputeNewCreditTermChanges(
        creditNr: string,
        newFixedMonthsCount: number,
        newMarginInterestRatePercent: number,
        newInterestBoundFrom: string,
        newReferenceInterestRatePercent: number
    ): Promise<{ newTerms: MlTermsChangeData }> {
        return this.apiService.post('nCredit', 'Api/MortgageLoans/ChangeTerms/ComputeNewTerms', {
            creditNr,
            newChangeTerms: {
                newFixedMonthsCount,
                newMarginInterestRatePercent,
                newInterestBoundFrom,
                newReferenceInterestRatePercent,
            },
        });
    }

    mlFetchLoanOwner(creditNr: string): Promise<MortgageLoanOwnerManagementModel> {
        return this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/LoanOwnerManagement/Fetch', {
            creditNr,
        });
    }

    mlEditLoanOwner(creditNr: string, loanOwnerName: string): Promise<MortgageLoanOwnerManagementModel> {
        return this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/LoanOwnerManagement/Edit', {
            creditNr,
            loanOwnerName,
        });
    }

    mlBulkEditLoanOwner(creditNrs: string[], loanOwnerName: string): Promise<MortgageLoanOwnerManagementModel> {
        return this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/LoanOwnerManagement/BulkEdit', {
            creditNrs,
            loanOwnerName,
        });
    }

    mlBulkEditLoanOwnerPreview(creditNrs: string[], loanOwnerName: string): Promise<BulkEditOwnerPreviewModel> {
        return this.apiService.post('NTechHost', 'Api/Credit/SeMortgageLoans/LoanOwnerManagement/BulkEditPreview', {
            creditNrs,
            loanOwnerName,
        });
    }
}

export class FetchCreditDirectDebitDetailsResult {
    Details: CreditDirectDebitDetailsModel;
    Events: DirectDebitEventModel[];
}

export class CreditDirectDebitDetailsModel {
    CreditNr: string;
    Applicants: CreditDirectDebitDetailsApplicantModel[];
    IsActive: boolean;
    SchedulationChangesModel: SchedulationModel;
    CurrentIsActiveStateDate: Date;
    AccountOwnerApplicantNr: number;

    BankAccount: ValidateBankAccountNrResult;
}
export class SchedulationModel {
    PendingSchedulationDetails: SchedulationDetailsModel;
    SchedulationDetails: SchedulationDetailsModel;
}

export class SchedulationDetailsModel {
    SchedulationOperation: string;
    AccountOwnerApplicantNr: number;
    BankAccount: ValidateBankAccountNrResult;
    PaymentNr: string;
}

export class CreditDirectDebitDetailsApplicantModel {
    ApplicantNr: number;
    CustomerId: number;
    FirstName: string;
    BirthDate: Date;
    StandardPaymentNr: string;
}

export class ValidateBankAccountNrResult {
    RawNr: string;
    IsValid: boolean;
    ValidAccount: ValidateBankAccountNrResultAccount;
}

export class ValidateBankAccountNrResultAccount {
    Type: string; //bankaccount or iban
    ClearingNr: string; //only bankaccount
    AccountNr: string; //only bankaccount
    Bic: string; //only iban
    BankName: string;
    NormalizedNr: string;
}

export class DirectDebitEventModel {
    BusinessEventId: number;
    Date: Date;
    LongText: string;
    UserDisplayName: string;
}

export interface SearchCreditHit {
    creditNr: string;
    status: string;
    startDate: string;
    connectedCustomerIds: number[];
}

export class CreditDocumentModel {
    DocumentId: number;
    DocumentType: string;
    ApplicantNr?: number;
    CustomerId?: number;
    CreationDate: Date;
    DownloadUrl: string;
    FileName?: string;
    IsExtraDocument?: boolean;
    ExtraDocumentData?: string;
    ArchiveKey: string;
}

export interface MortgageLoanStandardParty {
    partyTypeDisplayName: string;
    customers: {
        customerCardUrl: string;
        firstName: string;
        birthDate: string;
    }[];
}

export interface GetCreditCustomersSimpleResult {
    CreditType: string;
    ListCustomers: { ListName: string; CustomerId: number }[];
    CreditCustomers: { ApplicantNr: number; CustomerId: number }[];
}

export interface PendingSettlementSuggestionData {
    id: number;
    creditNr: string;
    settlementAmount: number;
    settlementDate: string;
    autoExpireDate: string;
}

export interface SettlementSuggestionData {
    creditNr: string;
    ocrPaymentReference: string;
    settlementDate: string;
    notifiedCapitalBalance: number;
    notNotifiedCapitalBalance: number;
    totalCapitalBalance: number;
    notifiedInterestBalance: number;
    notNotifiedInterestBalance: number;
    partOfNotifiedInterestBalanceThatIsAfterSettlementDate: number;
    nrOfInterestDaysInNotNotifiedInterestBalance: number;
    totalInterestBalance: number;
    notifiedOtherBalance: number;
    totalOtherBalance: number;
    swedishRse?: {
        estimatedAmount: number;
        interestRatePercent: number;
    };
    totalSettlementBalance: number;
    willSendSuggestion: boolean;
}

export interface GetCreditSettlementInitialDataResult {
    creditNr: string;
    creditStatus: string;
    pendingOffer: PendingSettlementSuggestionData;
    notificationEmail: string;
    hasEmailProvider: boolean;
}

export interface MlGetCreditTermChangesResult {
    creditStatus: string;
    minAllowedMarginInterestRate: number;
    maxAllowedMarginInterestRate: number;
    currentTerms?: {
        interestRebindMonthCount: number;
        interestBoundUntil: Date;
        daysLeft: number;
        referenceInterest: number;
        marginInterest: number;
        customerTotalInterest: number;
    };
    pendingTerms?: MlPendingTermsChangeData;
}

export interface GetCreditTermChangesResult {
    creditStatus: string;
    isSingleRepaymentLoan: boolean;
    minAllowedMarginInterestRate: number;
    maxAllowedMarginInterestRate: number;
    currentTerms?: {
        annuityAmount: number;
        monthlyFixedCapitalAmount: number;
        nrOfRemainingPayments: number;
        amortizationPlanFailedMessage: string;
        marginInterestRatePercent: number;
    };
    pendingTerms?: PendingTermsChangeData;
}

export interface MlPendingTermsChangeData extends MlTermsChangeData {
    Id: number;
    SentDate: string;
    ScheduledDate?: string;
    Signature: {
        UnsignedDocumentKey: string;
        SignedDocumentKey: string;
    };
    ActiveSignatureSessionKey?: string;
}

export interface PendingTermsChangeData extends TermsChangeData {
    Id: number;
    SentDate: string;
    Signatures: {
        ApplicantNr: number;
        UnsignedDocumentKey: string;
        SignedDocumentKey: string;
        SignatureDate: string;
        SignatureUrl: string;
    }[];
}

export interface TermsChangeData {
    AnnuityAmount: number;
    MonthlyAmount: number;
    NotificationFee: number;
    NrOfRemainingPayments: number;
    MarginInterestRatePercent: number;
    TotalInterestRatePercent: number;
    EffectiveInterestRatePercent: number;
    TotalPaidAmount: number;
    OriginalAnnuityAmount: number;
    OriginalMonthlyAmount: number;
    OriginalNrOfRemainingPayments: number;
    OriginalMarginInterestRatePercent: number;
    OriginalTotalInterestRatePercent: number;
}

export interface MlTermsChangeData {
    AmortizationAmount: number;
    NewInterestRebindMonthCount: number;
    CustomersNewTotalInterest: number;
    ReferenceInterest: number;
    MarginInterest: number;
    InterestBoundFrom: Date;
    InterestBoundTo: Date;
    CurrentCapitalBalance: number;
}

export interface MortgageLoanOwnerManagementModel {
    loanOwnerName: string;
    availableLoanOwnerOptions: string[];
}

export interface BulkEditOwnerPreviewModel {
    loanOwnerName: string;
    nrOfLoansEdit: number;
    isValid: boolean;
    validationErrorMessage?: string;
}

export interface GetNotificationDetailsResult {
    NotificationArchiveKey: string;
    CoNotificationCreditNrs: string[];
    OcrPaymentReference: string;
    SharedOcrPaymentReference: string;
    NotificationDate: string;
    DueDate: string;
    PaymentIBAN: string;
    PaymentBankGiro: string;
    Balance: Dictionary<number>;
    Payments: { PaymentId: number; TransactionDate: string; Amount: number }[];
    Reminders: { ReminderDate: string; ReminderNumber: number; ArchiveKey: string, CoReminderCreditNrs: string[] }[];
    CreditNr: string;
    PaymentOrderItems: PaymentOrderUiItem[];
}

export interface PaymentOrderUiItem {
    UniqueId: string
    Text: string
    OrderItem: {
        IsBuiltin: boolean
        Code: string
    }
}

export interface GetCreditNotificationsResult {
    today: string;
    ocrPaymentReference: string;
    creditNr: string;
    creditStatus: string;
    promisedToPayDate: string;
    notifications: CreditNotification[];
    totalUnpaidAmount: number;
    totalOverDueUnpaidAmount: number;
    hasTerminationLettersThatSuspendTheCreditProcess: boolean;
    latestActiveCreditProcessSuspendingTerminationLetterDuedate: string;
    latestActiveCreditProcessSuspendingTerminationLetters: {
        customerId: number,
        archiveKey: string,
        coTerminationCreditNrs: string[]
    }[]
}

export interface CreditNotification {
    Id: number;
    DueDate: string;
    CreditNr: string;
    InitialAmount: number;
    WrittenOffAmount: number;
    PaidAmount: number;
    LastPaidDate: string;
    IsPaid: boolean;
    IsOverDue: boolean;
    CurrentNrOfPassedDueDatesWithoutFullPayment: number;
    AtPaymentNrOfPassedDueDatesWithoutFullPayment: number;
}

export interface GetSecureMessageTextsResponse {
    MessageTextByMessageId: NumberDictionary<string>;
    MessageTextFormat: NumberDictionary<string>;
    IsFromCustomerByMessageId: NumberDictionary<boolean>;
    AttachedDocumentsByMessageId: NumberDictionary<string>;
}

export interface CreditCommentModel {
    CommentDate: Date;
    CommentText: string;
    ArchiveLinkKeys: string[];
    DisplayUserName: string;
    CustomerSecureMessageId?: number;
    RequestIpAddress?: string;
}

export interface IAmortizationPlan {
    annuity: number;
    monthlyFixedCapitalAmount: number;
    notificationFee: number;
    nrOfRemainingPayments: number;
    items: Array<IAmortizationPlanItem>;
    totalInterestAmount: number;
    totalCapitalAmount: number;
    singlePaymentLoanRepaymentDays ?: number;
    firstNotificationCostsAmount ?: number;
}

export interface IAmortizationPlanItem {
    isPaymentFreeMonthAllowed: boolean;
    capitalBefore: number;
    capitalTransaction: number;
    eventTransactionDate: Date;
    eventTypeCode: string;
    interestTransaction: number;
    isFutureItem: boolean;
    isWriteOff: boolean;
    notificationFeeTransaction: number;
    totalTransaction: number;
    businessEventRoleCode: string;
    futureItemDueDate?: Date;
    isTerminationLetterProcessReActivation: boolean;
    isTerminationLetterProcessSuspension: boolean;
}

export interface CreditAttentionStatus {
    isActive: boolean;
    isOverdue: boolean;
    text: string;
    code: string;
    statusDate: string;
}

export interface MenuItemModel {
    code: string;
    displayName: string;
}

export interface CreditDetails {
    applicationLink: string;
    applicationNr: string;
    campaignCode: string;
    capitalDebtAmount: number;
    childCreditCreditNr: string;
    coSignedAgreementLink: string;
    companyLoanRiskValues: {
        pd: number;
        lgd: number;
    };
    creditNr: string;
    currentNrOfOverdueDays: number;
    debtCollectionExportNrOfOverdueDays: number;
    currentStatusCode: string;
    isCompanyLoan: boolean;
    isForNonPropertyUse: boolean;
    isMortgageLoan: boolean;
    mainCreditCreditNr: string;
    mortgageLoanEndDate: string;
    mortgageLoanInterestRebindMonthCount: number;
    mortgageLoanNextInterestRebindDate: string;
    notNotifiedCapitalAmount: number;
    providerDisplayName: string;
    providerDisplayNameLong: string;
    repaymentTimeInMonths: number;
    repaymentTimeInMonthsFailedMessage: string;
    sentToDebtCollectionDate: string;
    signedAgreementLink1: string;
    signedAgreementLink2: string;
    startDate: string;
    totalInterestRatePercent: number;
    totalSentToDebtCollectionAmount: number;
    annuityAmount: number;
    currentFixedMonthlyCapitalPayment: number;
    marginInterestRatePercent: number;
    requestedMarginInterestRatePercent: number;
    referenceInterestRatePercent: number;
    mortgageLoanAgreementNr: string;
    singlePaymentLoanRepaymentDays: number;
}

export interface CreditCapitalTransaction {
    amount: number;
    businessEventRoleCode: string;
    eventDisplayName: string;
    id: number;
    isWriteOff: boolean;
    subAccountCode: string;
    totalAmountAfter: number;
    transactionDate: string;
}

export interface CreditDetailsResult {
    capitalTransactions: CreditCapitalTransaction[];
    details: CreditDetails;
}

export class AccountTransactionDetails {
    TransactionId: number;
    AccountCode: string;
    TransactionDate: Date;
    Amount: number;
    BusinessEventId: number;
    TransactionBusinessEventType: string;
    CreditNr: string;
    BookkeepingExportDate: Date;
    HasConnectedOutgoingPayment: boolean;
    OutgoingPaymentFileDate: Date;
    HasConnectedIncomingPayment: boolean;
    IncomingPaymentId: number;
    IncomingPaymentFileDate: Date;
    IncomingPaymentExternalId: string;
    IncomingPaymentOcrReference: string;
    IncomingPaymentClientAccountIban: string;
    IncomingPaymentCustomerName: string;
    IncomingPaymentAutogiroPayerNumber: string;
    BusinessEventRoleCode: string;
    SubAccountCode: string;
}
