import * as moment from 'moment';
import { of } from 'rxjs';
import { NtechApiService } from 'src/app/common-services/ntech-api.service';
import { SizeAndTimeLimitedCache } from 'src/app/common-services/size-and-time-limited-cache';
import { Dictionary, NumberDictionary } from 'src/app/common.types';
import { StandardApplicationModelBase } from './standard-application-base';

export abstract class SharedApplicationApiService {
    constructor(protected apiService: NtechApiService, protected isForMortgageLoans: boolean) {}

    public shared() {
        return this.apiService.shared;
    }

    public api() {
        return this.apiService;
    }

    fetchApplicationComments(
        applicationNr: string,
        options?: FetchApplicationCommentsOptions
    ): Promise<ApplicationComment[]> {
        return this.apiService.post('nPreCredit', 'api/ApplicationComments/FetchForApplication', {
            applicationNr: applicationNr,
            hideTheseEventTypes: options?.hideTheseEventTypes,
            showOnlyTheseEventTypes: options?.showOnlyTheseEventTypes,
        });
    }

    addApplicationComment(
        applicationNr: string,
        commentText: string,
        options?: AddApplicationCommentOptions
    ): Promise<ApplicationComment> {
        return this.apiService.post('nPreCredit', '/api/ApplicationComments/Add', {
            applicationNr: applicationNr,
            commentText: commentText,
            attachedFileAsDataUrl: options?.attachedFileAsDataUrl,
            attachedFileName: options?.attachedFileName,
            eventType: options?.eventType,
        });
    }

    fetchApplicationAssignedHandlers(
        applicationNr: string,
        returnPossibleHandlers: boolean,
        returnAssignedHandlers: boolean
    ): Promise<{ AssignedHandlers: AssignedHandler[]; PossibleHandlers: AssignedHandler[] }> {
        return this.apiService.post('nPreCredit', '/api/ApplicationAssignedHandlers/Fetch', {
            applicationNr,
            returnPossibleHandlers,
            returnAssignedHandlers,
        });
    }

    setApplicationAssignedHandlers(
        applicationNr: string,
        assignHandlerUserIds: number[],
        unAssignHandlerUserIds: number[]
    ): Promise<{ AllAssignedHandlers: AssignedHandler[] }> {
        return this.apiService.post('nPreCredit', '/api/ApplicationAssignedHandlers/Set', {
            applicationNr,
            assignHandlerUserIds,
            unAssignHandlerUserIds,
        });
    }

    validateBankAccountNr(bankAccountNr: string, bankAccountNrType: string, skipLoadingIndicator: boolean) {
        return this.validateBankAccountNrsBatch(
            { '1': { bankAccountNr, bankAccountNrType } },
            skipLoadingIndicator
        ).then((x) => {
            return x.ValidatedAccountsByKey['1'];
        });
    }

    validateBankAccountNrsBatch(
        request: Dictionary<BankAccountNrValidationRequest>,
        skipLoadingIndicator: boolean
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let fromCacheResult: {
            ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
        } = { ValidatedAccountsByKey: {} };

        if (!request || Object.keys(request).length === 0) {
            return of({ ValidatedAccountsByKey: {} }).toPromise();
        }

        let anyNotInCache = false;

        for (let key of Object.keys(request)) {
            let account = request[key];
            let cacheHit = this.validateBankAccountNrsCache.get<BankAccountNrValidationResult>(
                this.getValidateBankAccountNrCacheKey(account)
            );
            if (!cacheHit) {
                anyNotInCache = true;
                break;
            }
            fromCacheResult.ValidatedAccountsByKey[key] = cacheHit;
        }

        if (anyNotInCache) {
            return this.validateBankAccountNrsBatchRaw(request, skipLoadingIndicator).then((x) => {
                for (let key of Object.keys(x.ValidatedAccountsByKey)) {
                    this.validateBankAccountNrsCache.set(
                        this.getValidateBankAccountNrCacheKey(request[key]),
                        x.ValidatedAccountsByKey[key]
                    );
                }
                return x;
            });
        } else {
            return of(fromCacheResult).toPromise();
        }
    }

    editApplicant(request: {
        applicationNr: string;
        applicantNr: number;
        isPartOfTheHousehold: string;
        employment: string;
        employer: string;
        employerPhone: string;
        employedSince: string;
        employedTo: string;
        claimsToBePep: string;
        marriage: string;
        incomePerMonthAmount: number;
        hasConsentedToShareBankAccountData: string;
        hasConsentedToCreditReport: string;
        claimsToHaveKfmDebt: string;
        hasLegalOrFinancialGuardian: boolean;
        claimsToBeGuarantor: boolean;
    }) {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/Edit-Applicant', request);
    }

    editHouseholdEconomy(request: {
        applicationNr: string;
        housing: string;
        housingCostPerMonthAmount: number;
        otherHouseholdFixedCostsAmount: number;
        otherHouseholdFinancialAssetsAmount: number;
        outgoingChildSupportAmount: number;
        incomingChildSupportAmount: number;
        childBenefitAmount: number;
        children: { ageInYears: number; sharedCustody: boolean }[];
        otherLoans: {
            loanType: string;
            currentDebtAmount: number;
            monthlyCostAmount: number;
            shouldBeSettled: boolean;
            bankAccountNrType: string;
            bankAccountNr: string;
            settlementPaymentReference: string;
        }[];
    }) {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/Edit-HouseholdEconomy', request);
    }

    private getValidateBankAccountNrCacheKey(request: BankAccountNrValidationRequest): string {
        return `${request.bankAccountNr}#${request.bankAccountNrType}`;
    }

    private validateBankAccountNrsCache: SizeAndTimeLimitedCache = new SizeAndTimeLimitedCache(200, 30);
    private validateBankAccountNrsBatchRaw(
        request: Dictionary<BankAccountNrValidationRequest>,
        skipLoadingIndicator: boolean
    ): Promise<{
        ValidatedAccountsByKey: Dictionary<BankAccountNrValidationResult>;
    }> {
        let accounts = Object.keys(request).map((x) => {
            return {
                requestKey: x,
                bankAccountNr: request[x].bankAccountNr,
                bankAccountNrType: request[x].bankAccountNrType,
            };
        });
        return this.apiService.post(
            'nPreCredit',
            'api/bankaccount/validate-nr-batch',
            { accounts },
            { skipLoadingIndicator: skipLoadingIndicator }
        );
    }

    kycScreenBatch(customerIds: number[], screenDate: moment.Moment) {
        return this.apiService.post('nCustomer', 'Api/KycScreening/ListScreenBatch', {
            customerIds,
            screenDate: screenDate.toISOString(),
        });
    }

    updateCustomerProperties(
        items: { customerId: number; name: string; group: string; value: string; isSensitive?: boolean }[],
        force: boolean
    ) {
        return this.apiService.post('nCustomer', 'Customer/UpdateCustomer', { items, force });
    }

    cancelApplication(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/ApplicationCancellation/Cancel', { applicationNr });
    }

    reactivateCancelledApplication(applicationNr: string) {
        return this.apiService.post('nPreCredit', 'api/ApplicationCancellation/Reactivate', { applicationNr });
    }

    getCreditReport(creditReportId: number, itemNames: string[]): Promise<GetCreditReportResult> {
        return this.apiService.post('nCreditReport', 'CreditReport/GetById', { creditReportId, itemNames });
    }

    fetchCreditReportTabledValues(creditReportId: number): Promise<{ title: string; value: string }[]> {
        return this.apiService.post('nCreditReport', 'CreditReport/FetchTabledValues', { creditReportId });
    }

    findCreditReportsForPrivatePersonCustomer(
        customerId: number,
        paging?: { batchSize: number; skipCount: number }
    ): Promise<{
        CreditReportsBatch: FindForCustomerCreditReportModel[];
        RemainingReportsCount: number;
    }> {
        return this.apiService.post('nCreditReport', 'CreditReport/FindForCustomer', {
            customerId,
            isCompany: false,
            batchSize: paging?.batchSize,
            skipCount: paging?.skipCount,
        });
    }

    handlerLimitCheckAmount(
        loanAmount: number,
        skipLoadingIndicator: boolean
    ): Promise<{ isOverHandlerLimit: boolean; isAllowedToOverrideHandlerLimit: boolean }> {
        return this.apiService.post(
            'nPreCredit',
            'api/CreditHandlerLimit/CheckAmount',
            { amount: loanAmount },
            { skipLoadingIndicator: skipLoadingIndicator }
        );
    }

    generateDirectDebitPayerNumber(
        applicantNr: number,
        creditNr: string
    ): Promise<{
        ClientBankGiroNr: string;
        PayerNr: string;
    }> {
        return this.apiService.post('nCredit', 'api/DirectDebit/Generate-PayerNumber', { applicantNr, creditNr });
    }

    public createApplicationPageTitle(ai: ApplicationInfoModel): { title: string; browserTitle: string } {
        let title = `Application ${ai.ApplicationNr}`;
        let browserTitle = title;
        if (!ai.IsActive) {
            title += `<span> (inactive)</span>`;
        }
        title += `, <span class="page-title-smaller-text">${ai.ProviderDisplayName} ${moment(ai.ApplicationDate).format(
            'YYYY-MM-DD HH:mm'
        )}</span>`;

        return { title, browserTitle };
    }

    public abstract fetchApplicationInitialDataShared(
        applicationNr: string
    ): Promise<StandardApplicationModelBase | 'noSuchApplicationExists'>;

    public findRandomApplication(request: {
        isFinalDecisionMade?: boolean;
        isRejected?: boolean;
        isCancelled?: boolean;
        nrOfApplicants?: number;
        memberOfListName?: string;
    }): Promise<{ ApplicationNr: string }> {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/FindRandomApplication', request);
    }

    fetchStandardWorkListDataPage(
        providerName: string,
        listName: string,
        assignedHandler: ApplicationsListAssignedHandlerModel,
        pageSize: number,
        zeroBasedPageNr: number,
        options?: {
            forceShowUserHiddenItems?: boolean;
            includeListCounts?: boolean;
            includeProviders?: boolean;
            includeWorkflowModel?: boolean;
        }
    ): Promise<LoanStandardWorkListDataPageResult> {
        return this.apiService.post('nPreCredit', '/api/LoanStandard/Search/WorkListDataPage', {
            providerName,
            listName,
            assignedHandler,
            pageSize,
            zeroBasedPageNr,
            forceShowUserHiddenItems: !!options?.forceShowUserHiddenItems,
            includeListCounts: !!options?.includeListCounts,
            includeProviders: !!options?.includeProviders,
            includeWorkflowModel: !!options?.includeWorkflowModel,
        });
    }

    searchForLoanStandardApplicationByOmniValue(
        omniSearchValue: string,
        forceShowUserHiddenItems: boolean
    ): Promise<LoanStandardApplicationSearchHitResult> {
        return this.apiService.post('nPreCredit', '/api/LoanStandard/Search/ByOmniValue', {
            omniSearchValue: omniSearchValue,
            forceShowUserHiddenItems: forceShowUserHiddenItems,
        });
    }

    getSecureMessageTexts(messageIds: number[]): Promise<GetSecureMessageTextsResponse> {
        return this.apiService.post('nCustomer', 'api/CustomerMessage/GetMessageTexts', { messageIds });
    }

    buyCreditReportForStandardApplication(applicationNr: string, customerId: number) {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/CreditReport/BuyNewForApplication', {
            applicationNr,
            customerId,
        });
    }

    fetchApplicationUnreadCustomerMessagesCount(
        applicationNr: string,
        customerId: number,
        channelType: string
    ): Promise<ApplicationUnreadCustomerMessagesCountModel> {
        return this.apiService.post('nCustomer', 'api/CustomerMessage/GetCustomerMessagesByChannel', {
            channelType,
            customerId,
            channelId: applicationNr,
            isHandled: false,
            isFromCustomer: true,
        });
    }

    addCustomerToCustomerApplicationList(
        applicationNr: string,
        listName: string,
        customerId: number,
        createOrUpdateData?: {
            CivicRegNr: string;
            FirstName: string;
            LastName: string;
            Email: string;
            Phone: string;
            AddressStreet: string;
            AddressZipcode: string;
            AddressCity: string;
            AddressCountry: string;
        }
    ): Promise<{
        CustomerId: number;
        WasAdded: number;
    }> {
        return this.apiService.post('nPreCredit', 'api/ApplicationCustomerList/Add-Customer', {
            applicationNr,
            listName,
            customerId,
            createOrUpdateData,
        });
    }

    removeCustomerFromCustomerApplicationList(
        applicationNr: string,
        listName: string,
        customerId: number
    ): Promise<{
        CustomerId: number;
        WasRemoved: number;
    }> {
        return this.apiService.post('nPreCredit', 'api/ApplicationCustomerList/Remove-Customer', {
            applicationNr,
            listName,
            customerId,
        });
    }

    addKycCustomerQuestionsSets(
        customerQuestionsSets: CustomerKycQuestionsSet[],
        sourceType: string,
        sourceId: string
    ) {
        return this.apiService.post('nCustomer', 'Api/KycManagement/AddCustomerQuestionsSetBatch', {
            customerQuestionsSets: customerQuestionsSets,
            sourceType: sourceType,
            sourceId: sourceId,
        });
    }

    fetchKycCustomerOnboardingStatuses(
        customerIds: number[],
        sourceType: string,
        sourceId: string
    ): Promise<NumberDictionary<KycCustomerOnboardingStatusModel>> {
        return this.apiService.post('nCustomer', 'Api/KycManagement/FetchCustomerOnboardingStatuses', {
            customerIds,
            kycQuestionsSourceType: sourceType,
            kycQuestionsSourceId: sourceId,
        });
    }

    setIsApprovedKycStep(applicationNr: string, isApproved: boolean) {
        return this.apiService.post('nPreCredit', 'api/LoanStandard/Kyc/Set-Approved-Step', {
            applicationNr: applicationNr,
            isApproved: isApproved,
        });
    }
}

export interface KycCustomerOnboardingStatusModel {
    CustomerId: number;
    IsPep?: boolean;
    IsSanction?: boolean;
    LatestScreeningDate: string;
    LatestKycQuestionsAnswerDate: string;
    CustomerBirthDate: string;
    CustomerShortName: string;
    HasNameAndAddress: boolean;
}

export class CustomerKycQuestionsSet {
    constructor(public AnswerDate: string, public CustomerId: number) {
        this.Items = [];
    }

    addQuestion(questionCode: string, answerCode: string, questionText: string, answerText: string) {
        this.Items.push({
            QuestionCode: questionCode,
            AnswerCode: answerCode,
            QuestionText: questionText,
            AnswerText: answerText,
        });
    }

    public Items: {
        QuestionCode: string;
        AnswerCode: string;
        QuestionText: string;
        AnswerText: string;
    }[];
}

export class FetchApplicationCommentsOptions {
    showOnlyTheseEventTypes?: string[];
    hideTheseEventTypes?: string[];
}

export class ApplicationComment {
    Id: number;
    CommentDate: Date;
    CommentText: string;
    AttachmentFilename: string;
    AttachmentUrl: string;
    CommentByName: string;
    DirectUrlShortName: string;
    DirectUrl: string;
    RequestIpAddress: string;
    BankAccountRawJsonDataArchiveKey: string;
    BankAccountPdfSummaryArchiveKey: string;
    CustomerSecureMessageId: number;
}

export interface AddApplicationCommentOptions {
    eventType?: string;
    attachedFileAsDataUrl?: string;
    attachedFileName?: string;
}

export class AssignedHandler {
    UserId: number;
    UserDisplayName: string;
}

export interface BankAccountNrValidationRequest {
    bankAccountNr: string;
    bankAccountNrType: string;
}

export interface BankAccountNrValidationResult {
    RawNr: string;
    IsValid: boolean;
    ValidAccount?: {
        BankName: string;
        ClearingNr: string;
        AccountNr: string;
        NormalizedNr: string;
        Bic: string;
        DisplayNr: string;
        BankAccountNrType: string;
    };
}

export interface GetCreditReportResult {
    CreditReportId: number;
    RequestDate: string;
    CustomerId: number;
    ProviderName: string;
    Items: { Name: string; Value: string }[];
    AgeInDays: number;
}

export interface FindForCustomerCreditReportModel {
    Id: number;
    RequestDate: string;
    CreditReportProviderName: string;
    CustomerId: number;
    HasReason: boolean;
    HasTableValuesPreview: boolean;
    HtmlPreviewArchiveKey: string;
    PdfPreviewArchiveKey: string;
    RawXmlArchiveKey: string;
}

export class ApplicationInfoModel {
    ApplicationNr: string;
    ApplicationType: string;
    CreditCheckStatus: string;
    FraudCheckStatus: string;
    AgreementStatus: string;
    MortgageLoanInitialCreditCheckStatus: string;
    MortgageLoanFinalCreditCheckStatus: string;
    MortgageLoanDocumentCheckStatus: string;
    MortgageLoanAdditionalQuestionsStatus: string;
    MortgageLoanValuationStatus: string;
    MortgageLoanDirectDebitCheckStatus: string;
    MortgageLoanAmortizationStatus: string;
    NrOfApplicants: number;
    CustomerCheckStatus: string;
    IsActive: boolean;
    IsWaitingForAdditionalInformation: boolean;
    IsFinalDecisionMade: boolean;
    IsCancelled: boolean;
    IsRejected: boolean;
    ProviderName: string;
    ProviderDisplayName: string;
    ApplicationDate: Date;
    IsPartiallyApproved: boolean;
    IsRejectAllowed: boolean;
    IsApproveAllowed: boolean;
    IsSettlementAllowed: boolean;
    CompanyLoanAdditionalQuestionsStatus: string;
    ListNames: string[];
    WorkflowVersion: string;
    HasLockedAgreement: boolean;
    IsLead: boolean;
    ListCreditReportProviders: string[];
}

export interface WorkflowModel {
    WorkflowVersion: number;
    Steps: WorkflowStepModel[];
}

export interface WorkflowStepModel {
    DisplayName: string;
    ComponentName: string;
    Name: string;
    CustomData: any;
}

export interface LoanStandardApplicationSearchHitResult {
    Applications: LoanStandardApplicationSearchHit[];
}

export interface LoanStandardApplicationSearchHit {
    ApplicationNr: string;
    ApplicationDate: Date;
    IsActive: boolean;
    IsPartiallyApproved: boolean;
    IsFinalDecisionMade: boolean;
    LatestSystemCommentText: string;
    LatestSystemCommentDate: Date;
    CurrentLoanAmount: number;
    RequestedAmount?: number;
    ProviderName: string;
}

export interface LoanStandardWorkListDataPageResult {
    CurrentPageNr: number;
    TotalNrOfPages: number;
    AssignableCount: number;
    PageApplications: LoanStandardApplicationSearchHit[];
    ListCountsByName: Dictionary<number>;
    ProviderDisplayNameByName: Dictionary<string>;
    AssignedHandlerDisplayNameByUserId: Dictionary<string>;
    CurrentWorkflowModel: WorkflowModel;
}

export class ApplicationsListAssignedHandlerModel {
    AssignedHandlerUserId: string;
    ExcludeAssignedApplications: boolean;
    ExcludeUnassignedApplications: boolean;
}

export interface GetSecureMessageTextsResponse {
    MessageTextByMessageId: NumberDictionary<string>;
    MessageTextFormat: NumberDictionary<string>;
    IsFromCustomerByMessageId: NumberDictionary<boolean>;
    AttachedDocumentsByMessageId: NumberDictionary<string>;
}

export class ApplicationUnreadCustomerMessagesCountModel {
    TotalMessageCount: number;
}
