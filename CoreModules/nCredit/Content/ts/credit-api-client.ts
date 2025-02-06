module NTechCreditApi {
    export class ApiClient {
        constructor(private onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            private $q: ng.IQService) {
        }

        private activePostCount: number = 0;
        public loggingContext: string = null;

        private post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            let startTimeMs = performance.now();
            this.activePostCount++;
            let d: ng.IDeferred<TResult> = this.$q.defer()
            this.$http.post(url, data).then((result: ng.IHttpResponse<TResult>) => {
                d.resolve(result.data)
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText)
                }
                d.reject(err.statusText)
            }).finally(() => {
                this.activePostCount--;
                let totalTimeMs = performance.now() - startTimeMs;
                let c = this.loggingContext == null ? '' : (this.loggingContext + ': ')
                if (console) {
                    console.log(`${c}post - ${url}: ${totalTimeMs}ms`);
                }
            })
            return d.promise
        }

        public postUsingApiGateway<TRequest, TResult>(seviceName: string, serviceLocalUrl: string, data: TRequest): ng.IPromise<TResult> {
            return this.post<TRequest, TResult>(`/Api/Gateway/${seviceName}${serviceLocalUrl[0] === '/' ? '' : '/'}${serviceLocalUrl}`, data)
        }

        public getUserModuleUrl(moduleName: string, serviceLocalUrl: string, parameters?: IStringDictionary<string>): ng.IPromise<{ Url: string, UrlInternal: string, UrlExternal: string }> {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters })
        }

        isLoading() {
            return this.activePostCount > 0;
        }

        fetchCreditDocuments(creditNr: string, fetchFilenames: boolean, includeExtraDocuments: boolean): ng.IPromise<CreditDocumentModel[]> {
            return this.post('/Api/Credit/Documents/Fetch', { creditNr: creditNr, fetchFilenames: fetchFilenames, includeExtraDocuments: includeExtraDocuments });
        }

        fetchSecurityItems(creditNr: string): ng.IPromise<CreditSecurityItemModel[]> {
            return this.post('/Api/Credit/Security/FetchItems', { creditNr: creditNr })
        }

        fetchCreditDirectDebitDetails(creditNr: string, backTarget: string, includeEvents: boolean): ng.IPromise<FetchCreditDirectDebitDetailsResult> {
            return this.post('/Api/Credit/DirectDebit/FetchDetails', { creditNr: creditNr, backTarget: backTarget, includeEvents: includeEvents })
        }

        fetchCreditDirectEvents(creditNr: string): ng.IPromise<DirectDebitEventModel[]> {
            return this.post('/Api/Credit/DirectDebit/FetchEvents', { creditNr: creditNr })
        }

        validateBankAccountNr(bankAccountNr: string): ng.IPromise<ValidateBankAccountNrResult> {
            return this.post('/Api/BankAccount/ValidateNr', { bankAccountNr: bankAccountNr })
        }

        updateDirectDebitCheckStatus(creditNr: string, newStatus: string, bankAccountNr: string, bankAccountOwnerApplicantNr: number): ng.IPromise<void> {
            return this.post('/Api/Credit/DirectDebit/UpdateStatus', { creditNr: creditNr, newStatus: newStatus, bankAccountNr: bankAccountNr, bankAccountOwnerApplicantNr: bankAccountOwnerApplicantNr })
        }

        scheduleDirectDebitActivation(isChangeActivated: boolean, creditNr: string, bankAccountNr: string, paymentNr: string, applicantNr: number, customerId: number): ng.IPromise<void> {
            return this.post('/Api/Credit/DirectDebit/ScheduleActivation', { isChangeActivated: isChangeActivated, creditNr: creditNr, bankAccountNr: bankAccountNr, paymentNr: paymentNr, applicantNr: applicantNr, customerId: customerId })
        }

        scheduleDirectDebitCancellation(creditNr: string, isChangeActivated: boolean, paymentNr: string): ng.IPromise<void> {
            return this.post('/Api/Credit/DirectDebit/ScheduleCancellation', { creditNr: creditNr, isChangeActivated: isChangeActivated, paymentNr: paymentNr })
        }

        scheduleDirectDebitChange(currentStatus: string, isChangeActivated: boolean, creditNr: string, bankAccountNr: string, paymentNr: string, applicantNr: number, customerId: number): ng.IPromise<void> {
            return this.post('/Api/Credit/DirectDebit/ScheduleChange', { currentStatus: currentStatus, isChangeActivated: isChangeActivated, creditNr: creditNr, bankAccountNr: bankAccountNr, paymentNr: paymentNr, applicantNr: applicantNr, customerId: customerId })
        }
        
        removeDirectDebitSchedulation(creditNr: string, paymentNr: string): ng.IPromise<void> {
            return this.post('/Api/Credit/DirectDebit/RemoveSchedulation', { creditNr: creditNr, paymentNr: paymentNr })
        }
        
        loadCreditComments(creditNr: string, excludeTheseEventTypes?: string[], onlyTheseEventTypes?: string[]): ng.IPromise<CreditCommentModel[]> {
            return this.post('/Api/CreditComment/LoadForCredit', { creditNr: creditNr, excludeTheseEventTypes: excludeTheseEventTypes, onlyTheseEventTypes: onlyTheseEventTypes })
        }

        createCreditComment(creditNr: string, commentText: string, attachedFileAsDataUrl: string, attachedFileName: string): ng.IPromise<CreateCreditCommentResponse> {
            return this.post('/Api/CreditComment/Create', { creditNr: creditNr, commentText: commentText, attachedFileAsDataUrl: attachedFileAsDataUrl, attachedFileName: attachedFileName })
        }

        keyValueStoreGet(key: string, keySpace: string): ng.IPromise<KeyValueStoreGetResult> {
            return this.post('/Api/KeyValueStore/Get', {
                "Key": key,
                "KeySpace": keySpace
            });
        }

        keyValueStoreRemove(key: string, keySpace: string): ng.IPromise<void> {
            return this.post('/Api/KeyValueStore/Remove', {
                "Key": key,
                "KeySpace": keySpace
            });
        }

        keyValueStoreSet(key: string, keySpace: string, value: string): ng.IPromise<void> {
            return this.post('/Api/KeyValueStore/Set', {
                "Key": key,
                "KeySpace": keySpace,
                "Value": value
            });
        }

        fetchUserNameByUserId(userId: number): ng.IPromise<FetchUserNameByUserIdResult> {
            return this.post('/Api/UserName/ByUserId', { UserId: userId })
        }

        fetchPendingReferenceInterestChange(): ng.IPromise<PendingReferenceInterestChangeModel> {
            return this.post('/Api/ReferenceInterestRate/FetchPendingChange', {})
        }

        beginReferenceInterestChange(newInterestRatePercent: number): ng.IPromise<PendingReferenceInterestChangeModel> {
            return this.post('/Api/ReferenceInterestRate/BeginChange', { NewInterestRatePercent: newInterestRatePercent })
        }

        cancelPendingReferenceInterestChange(): ng.IPromise<void> {
            return this.post('/Api/ReferenceInterestRate/CancelPendingChange', {})
        }

        commitPendingReferenceInterestChange(expectedNewInterestRatePercent: number, requestOverrideDuality?: boolean): ng.IPromise<ReferenceInterestChangeResult> {
            return this.post('/Api/ReferenceInterestRate/CommitPendingChange', { ExpectedNewInterestRatePercent: expectedNewInterestRatePercent, RequestOverrideDuality: requestOverrideDuality })
        }

        fetchReferenceInterestChangePage(pageSize: number, pageNr: number): ng.IPromise<FetchReferenceInterestChangePageResult> {
            return this.post('/Api/Credit/GetReferenceInterestRateChangesPage', { pageSize: pageSize, pageNr: pageNr })
        }

        fetchCustomerCardItems(customerId: number, propertyNames: string[]): ng.IPromise<IStringStringDictionary> {
            let r: ng.IPromise<FetchCustomerCardItemsResult> = this.post('/Api/Credit/FetchCustomerItems', { customerId: customerId, propertyNames: propertyNames })

            return r.then(x => {
                let d: IStringStringDictionary = {}
                if (x && x.items) {
                    for (let i of x.items) {
                        d[i.name] = i.value
                    }
                }
                return d
            })
        }

        fetchCustomerCardItemsBulk(customerIds: number[], itemNames: string[]): ng.IPromise<INumberDictionary<IStringDictionary<string>>> {
            let r: ng.IPromise<any> = this.post('/api/Customer/Bulk-Fetch-Properties', {
                customerIds: customerIds,
                propertyNames: itemNames
            })

            return r.then(x => x.Properties)
        }

        repayUnplacedPayment(paymentId: number,
            customerName: string,
            repaymentAmount: number,
            leaveUnplacedAmount: number,
            bankAccountNrType: string,
            bankAccountNr: string): ng.IPromise<RepayUnplacedPaymentResult> {
            return this.post('/Api/Credit/RepayPayment', {
                paymentId: paymentId,
                customerName: customerName,
                repaymentAmount: repaymentAmount,
                leaveUnplacedAmount: leaveUnplacedAmount,
                bankAccountNrType: bankAccountNrType,
                iban: bankAccountNr
            })
        }

        isValidAccountNr(bankAccountNr: string, bankAccountNrType: string): ng.IPromise<IsValidAccountNrResult> {
            return this.post('/Api/UnplacedPayment/IsValidAccountNr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType })
        }

        createMortgageLoan(loan: Loan): ng.IPromise<any> {
            return this.post('/Api/MortgageLoans/Create', loan)
        }

        // Note, if the civicRegNr is not already a customer, it will increment a new customerId for the hash of that civicRegNr. 
        getPersonCustomerId(civicRegNr: string): ng.IPromise<CustomerIdResult> {
            return this.postUsingApiGateway('nCustomer', '/api/CustomerIdByCivicRegNr', { CivicRegNr: civicRegNr });
        };

        createOrUpdatePersonCustomer(request: {
            CivicRegNr: string
            ExpectedCustomerId?: number
            Properties: { Name: string, Value: string, ForceUpdate: boolean }[]
        }): ng.IPromise<{ CustomerId: number }> {
            return this.postUsingApiGateway('nCustomer', 'api/PersonCustomer/CreateOrUpdate', request)
        }

        generateNewCreditNumber(): ng.IPromise<GenerateNewCreditNumberResponseModel> {
            return this.post('/Api/NewCreditNumber', {})
        }

        existCustomerByCustomerId(customerId: number): ng.IPromise<ExistCustomerResult> {
            return this.postUsingApiGateway('nCustomer', '/api/ExistCustomerByCustomerId', { CustomerId: customerId });
        };

        fetchDatedCreditValueItems(creditNr: string, name: string): ng.IPromise<DatedCreditValueModel[]> {
            return this.post('/Api/Credit/FetchDatedCreditValueItems', { creditNr: creditNr, name: name })
        }

        getCustomerMessagesTexts(messageIds: number[]): ng.IPromise<{ MessageTextByMessageId: INumberDictionary<string>, MessageTextFormat: INumberDictionary<string>, IsFromCustomerByMessageId: INumberDictionary<boolean>, AttachedDocumentsByMessageId: INumberDictionary<string> }> {
            return this.postUsingApiGateway('nCustomer', 'api/CustomerMessage/GetMessageTexts', {
                MessageIds: messageIds
            })
        }

        importOrPreviewCompanCreditsFromFile(request: ImportOrPreviewCompanCreditsFromFileRequest): ng.IPromise<ImportOrPreviewCompanCreditsFromFileResponse> {
            return this.post('/api/CompanyCredit/ImportOrPreviewFile', request)
        }

        removeCompanyConnection(customerId: number, creditNr: string, listName: string): ng.IPromise<void> {
            return this.post('/Api/Credit/RemoveCompanyConnection', { customerId: customerId, creditNr: creditNr, listName: listName });
        }

        addCompanyConnections(customerId: number, creditNr: string, listNames: string[]): ng.IPromise<void> {
            return this.post('/Api/Credit/AddCompanyConnections', { customerId: customerId, creditNr: creditNr, listNames: listNames });
        }

        setDatedCreditValue(creditNr: string, datedCreditValueCode: string, businessEventType: string, value: number)
            : Promise<{ NewValue: number, BusinessEventId: number }> {
            return this.post('/Api/DatedCreditValue/Set', { creditNr, datedCreditValueCode, businessEventType, value } )
        }

        fetchBookKeepingRules(): ng.IPromise<{ ruleRows: BookKeepingRuleDescriptionTableRow[], allConnections: string[], 
            allAccountNames: string[], accountNrByAccountName: IStringStringDictionary }> {
            return this.post('/Api/Bookkeeping/RulesAsJson', {})
        }

        fetchCreditAttentionStatus(creditNr: string): ng.IPromise<any[]> {
            return this.post('/Api/Credit/FetchAttentionStatus', { creditNr: creditNr });
        }

        getCustomerDetails(creditNr: string, backTarget?: string): ng.IPromise<any> {
            return this.post('/Api/Credit/Customers', { creditNr: creditNr, backTarget: backTarget })
        }

        fetchMortgageLoanStandardCollaterals(creditNrs: string[]): ng.IPromise<{
            Collaterals: {
                CollateralId: number,
                CreditNrs: string[],
                CollateralItems: IStringDictionary<{
                    Name: string,
                    StringValue: string,
                    DateValue: string,
                    NumericValue: string
                }>
            }[]
        }> {
            return this.post('/Api/MortgageLoans/Fetch-Collaterals', { creditNrs: creditNrs })
        }
    }

    export class ImportOrPreviewCompanCreditsFromFileRequest {
        ExcelFileAsDataUrl: string
        FileName: string
        IsPreviewMode: boolean
        IsImportMode: boolean
        IncludeRaw: boolean
    }

    export interface ImportOrPreviewCompanCreditsFromFileResponse {
        Shared: {
            Errors: String[]
            Warnings: String[]
        }
        Preview: {
            Loans: any[]
            Persons: any[]
            Summaries: { Key: string, Value: string }[]
        }
        Import: {
            CreditNrs: { ImportedCreditNr: string, NewCreditNr: string }[]
        }
    }

    export interface DatedCreditValueModel {
        creditNr: string
        name: string
    }

    export interface GenerateNewCreditNumberResponseModel {
        nr: string
    }

    export interface CustomerIdResult {
        CustomerId: number
    }

    export interface ExistCustomerResult {
        Exist: boolean
    }

    export class Loan {
        CreditNr: string
        MonthlyFeeAmount: number
        NominalInterestRatePercent: number
        NrOfApplicants: string
        ProviderName: string
        ProviderApplicationId: string
        ApplicationNr: string
        SettlementDate: string
        HistoricalStartDate: string
        EndDate: string
        NextInterestRebindDate: string
        ReferenceInterestRate: number
        LoanAmount: number
        ActualAmortizationAmount: number
        ExceptionAmortizationAmount: number
        AmortizationExceptionReasons: string[]
        AmortizationRule: string
        AmortizationBasisLoanAmount: number
        AmortizationBasisObjectValue: number
        DebtIncomeRatioBasisAmount: number
        CurrentCombinedYearlyIncomeAmount: number
        RequiredAlternateAmortizationAmount: number
        KycQuestionsJsonDocumentArchiveKey: string
        Applicants: Applicants[]
        AmortizationExceptionUntilDate: string
        AmortizationBasisDate: string
        CurrentObjectValue: number
        Documents:
            {
                DocumentType: string,
                ApplicantNr: number,
                ArchiveKey: string
            }
        SecurityItems: NTechCreditApi.IStringDictionary<string>
        ActiveDirectDebitAccount: {
            BankAccountNrOwnerApplicantNr: number,
            BankAccountNr: string,
            ActiveSinceDate: Date
        }
        CurrentObjectValueDate: string
    }

    export class Applicants {
        ApplicantNr: number
        CustomerId: number
        AgreementPdfArchiveKey: string
        OwnerShipPercent: number
    }

    export interface IsValidAccountNrResult {
        isValid: boolean
        normalizedValue?: string
        displayValue?: string
        bankName?: string
        message?: string
        ibanFormatted?: {
            nr: string
            bankName: string
        }
        bankAccountNrType?: string
    }

    export interface RepayUnplacedPaymentResult {
    }

    export interface IStringStringDictionary {
        [key: string]: string
    }

    export interface IStringDictionary<T> {
        [key: string]: T
    }

    export interface INumberDictionary<T> {
        [key: number]: T
    }

    interface FetchCustomerCardItemsResult {
        customerId: number
        items: FetchCustomerCardItemsItem[]
    }

    interface FetchCustomerCardItemsItem {
        name: string
        value: string
    }

    export interface FetchReferenceInterestChangePageResult {
        CurrentPageNr: number;
        TotalNrOfPages: number;
        Page: FetchReferenceInterestChangePageModel[];
    }

    export interface FetchReferenceInterestChangePageModel {
        TransactionDate: Date;
        ApprovedDate: Date;
        UserId: number;
        UserDisplayName: string;
        ChangedToValue: number;
        ChangedCreditCount: number;
        InitiatedByUserId: number;
        InitiatedByDisplayName: string;
        InitiatedDate: Date;
    }

    export interface ReferenceInterestChangeResult {
        NrOfCreditsUpdated: number
    }

    export interface PendingReferenceInterestChangeModel {
        NewInterestRatePercent: number;
        InitiatedByUserId: number;
        InitiatedDate: Date;
        InitiatedByUserName: string;
    }

    export interface KeyValueStoreGetResult {
        Value: string
    }
    export interface FetchUserNameByUserIdResult {
        UserName: string
    }

    export class MortgageLoanAmortizationBasisModel {
        TransactionDate: NTechDates.DateOnly
        ActualAmortizationAmount: number
        RequiredAmortizationAmount: number
        ExceptionAmortizationAmount: number
        AmortizationExceptionUntilDate: NTechDates.DateOnly
        AmortizationExceptionReasons: string[]
        AmortizationFreeUntilDate: NTechDates.DateOnly
        AmortizationRule: string
        AmortizationBasisObjectValue: number
        AmortizationBasisDate: NTechDates.DateOnly
        AmortizationBasisLoanAmount: number
        CurrentCombinedYearlyIncomeAmount: number
        CurrentCombinedTotalLoanAmount: number
        CurrentLoanAmount: number
    }

    export interface IAmortizationPlan {
        Annuity: number
        NotificationFee: number
        NrOfRemainingPayments: number
        Items: Array<IAmortizationPlanItem>
        TotalInterestAmount: number
        TotalCapitalAmount: number
    }

    export interface IAmortizationPlanItem {
        IsPaymentFreeMonthAllowed: boolean
        CapitalBefore: number
        CapitalTransaction: number
        EventTransactionDate: Date
        EventTypeCode: string
        InterestTransaction: number
        IsFutureItem: boolean
        IsWriteOff: boolean
        NotificationFeeTransaction: number
        TotalTransaction: number
        BusinessEventRoleCode: string
        FutureItemDueDate?: Date
    }

    export class CreateCreditCommentResponse {
        comment: CreditCommentModel
    }

    export class CreditCommentModel {
        CommentDate: Date
        CommentText: string
        ArchiveLinks: string[]
        DisplayUserName: string
        CustomerSecureMessageId?: number
    }

    export class FetchCreditDirectDebitDetailsResult {
        Details: CreditDirectDebitDetailsModel
        Events: DirectDebitEventModel[]
    }

    export class DirectDebitEventModel {
        BusinessEventId: number
        Date: Date
        LongText: string
    }

    export class CreditDirectDebitDetailsModel {
        CreditNr: string
        Applicants: CreditDirectDebitDetailsApplicantModel[]
        IsActive: boolean
        SchedulationChangesModel: SchedulationModel
        CurrentIsActiveStateDate: Date
        AccountOwnerApplicantNr: number
        BankAccount: ValidateBankAccountNrResult
    }

    export class SchedulationModel {
        PendingSchedulationDetails: SchedulationDetailsModel
        SchedulationDetails: SchedulationDetailsModel
    }

    export class SchedulationDetailsModel {
        SchedulationOperation: string
        AccountOwnerApplicantNr: number
        BankAccount: ValidateBankAccountNrResult
        PaymentNr: string
    }

    export class CreditDirectDebitDetailsApplicantModel {
        ApplicantNr: number
        CustomerId: number
        FirstName: string
        BirthDate: Date
        StandardPaymentNr: string
    }

    export class CreditDocumentModel {
        DocumentId: number
        DocumentType: string
        ApplicantNr?: number
        CustomerId?: number
        CreationDate: Date
        DownloadUrl: string
        FileName?: string
        IsExtraDocument?: boolean
        ExtraDocumentData?: string
    }

    export class CreditSecurityItemModel {
        Id: number
        CreditNr: string
        TransactionDate: Date
        Name: string
        StringValue: string
        NumericValue: number
        DateValue: Date
    }

    export class DatedCreditValueModel {
        TransactionDate: string
        Value: number
    }

    export class ValidateBankAccountNrResult {
        RawNr: string
        IsValid: boolean
        ValidAccount: ValidateBankAccountNrResultAccount
    }

    export class ValidateBankAccountNrResultAccount {
        Type: string //bankaccount or iban
        ClearingNr: string //only bankaccount
        AccountNr: string //only bankaccount
        Bic: string //only iban
        BankName: string
        NormalizedNr: string
    }

    export class BookKeepingRuleDescriptionTableRow
    {
        EventName: string 
        LedgerAccountName: string
        Connections: string[]
        DebetAccountNr: string
        DebetAccountName: string
        CreditAccountNr: string
        CreditAccountName: string
        Filter: string
    }

    export class DirectDebitConsentFile {
        DocumentId: number
        Filename: string
        DocumentArchiveKey: string
        DocumentDate: Date

        static GetDownloadUrlByKey(documentArchiveKey: string) {
            return '/Api/Credit/DirectDebit/DownloadConsentFile?key=' + documentArchiveKey
        }

        static GetDownloadUrl(d: DirectDebitConsentFile) {
            if (!d) {
                return null
            } else {
                return DirectDebitConsentFile.GetDownloadUrlByKey(d.DocumentArchiveKey)
            }
        }
    }
}