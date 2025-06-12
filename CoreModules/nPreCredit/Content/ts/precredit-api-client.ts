module NTechPreCreditApi {
    export abstract class BaseApiClient {
        constructor(protected onError: ((errorMessage: string) => void),
            private $http: ng.IHttpService,
            protected $q: ng.IQService) {
        }

        public rejectWithFullError: boolean = false;
        private activePostCount: number = 0;
        public loggingContext: string = null;

        public valueToPromise<T>(value: T): ng.IPromise<T> {
            let d: ng.IDeferred<T> = this.$q.defer()
            d.resolve(value)
            return d.promise
        }

        protected post<TRequest, TResult>(url: string, data: TRequest): ng.IPromise<TResult> {
            let startTimeMs = performance.now();
            this.activePostCount++;
            let d: ng.IDeferred<TResult> = this.$q.defer()
            this.$http.post(url, data).then((result: ng.IHttpResponse<TResult>) => {
                d.resolve(result.data)
            }, err => {
                if (this.onError) {
                    if (err && err.data) {
                        this.onError(err.data.errorMessage || err.data.errorCode || err.statusText)
                    } else if (err) {
                        this.onError(err.statusText)
                    } else {
                        this.onError('unknown error')
                    }
                }
                if (this.rejectWithFullError) {
                    d.reject(err)
                } else {
                    d.reject(err.statusText)
                }                
            }).finally(() => {
                this.activePostCount--;
                let totalTimeMs = performance.now() - startTimeMs;
                let c = this.loggingContext == null ? '' : (this.loggingContext + ': ')
            })
            return d.promise
        }

        public postUsingApiGateway<TRequest, TResult>(seviceName: string, serviceLocalUrl: string, data: TRequest): ng.IPromise<TResult> {
            return this.post<TRequest, TResult>(`/Api/Gateway/${seviceName}${serviceLocalUrl[0] === '/' ? '' : '/'}${serviceLocalUrl}`, data)
        }

        public getUserModuleUrl(moduleName: string, serviceLocalUrl: string, parameters?: IStringDictionary<string>): ng.IPromise<{ Url: string, UrlInternal: string, UrlExternal: string }> {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters })
        }

        public getArchiveDocumentUrl(archiveKey: string, opts?: ArchiveDocumentOptions): string {
            if (!archiveKey) {
                return null
            }

            let url = `/Api/ArchiveDocument/Download?archiveKey=${archiveKey}`
            if (opts) {
                if (opts.downloadFileName) {
                    url += '&downloadFileName=' + encodeURIComponent(opts.downloadFileName)
                } else if (opts.useOriginalFileName) {
                    url += '&useOriginalFileName=True'
                }
            }

            return url
        }

        isLoading() {
            return this.activePostCount > 0;
        }
    }

    export interface ArchiveDocumentOptions {
        useOriginalFileName?: boolean
        downloadFileName?: string
    }

    export class ApiClient extends BaseApiClient {
        constructor(onError: ((errorMessage: string) => void),
            $http: ng.IHttpService,
            $q: ng.IQService) {
            super(onError, $http, $q)
        }

        fetchDocumentCheckStatus(applicationNr: string): ng.IPromise<DocumentCheckStatusResult> {
            return this.post('/api/DocumentCheck/FetchStatus', { applicationNr: applicationNr });
        }

        approveApplication(url: string): ng.IPromise<IApproveApplicationResult> {
            return this.post(url, {});
        }

        rejectMortageLoanApplication(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/mortageloan/reject-application', { applicationNr, wasAutomated: false })
        }

        findHistoricalDecisions(historyFromDate: string, historyToDate: string): ng.IPromise<FindHistoricalDecisionResult> {
            return this.post('/CreditDecision/FindHistoricalDecisions', {
                fromDate: historyFromDate,
                toDate: historyToDate
            })
        }

        getBatchDetails(batchId: number): ng.IPromise<GetBatchDetailsResult> {
            return this.post('/CreditDecision/GetBatchDetails', {
                batchId: batchId
            })
        }

        checkIfOverHandlerLimit(applicationNr: string, newLoanAmount: number, isCompanyLoan?: boolean): ng.IPromise<CheckIfOverHandlerLimitResult> {
            return this.post('/api/CreditHandlerLimit/CheckIfOver', {
                applicationNr: applicationNr,
                newLoanAmount: newLoanAmount,
                isCompanyLoan: isCompanyLoan
            })
        }

        fetchCustomerComponentInitialData(applicationNr: string, applicantNr: number, backTarget: string): ng.IPromise<CustomerComponentInitialData> {
            return this.post('/api/CustomerInfoComponent/FetchInitial', {
                applicationNr: applicationNr,
                applicantNr: applicantNr,
                backTarget: backTarget
            })
        }

        fetchCustomerComponentInitialDataByItemCompoundName(applicationNr: string, customerIdApplicationItemCompoundName: string, customerBirthDateApplicationItemCompoundName?: string, backTarget?: string): ng.IPromise<CustomerComponentInitialData> {
            return this.post('/api/CustomerInfoComponent/FetchInitialByItemName', {
                applicationNr: applicationNr,
                customerIdApplicationItemCompoundName: customerIdApplicationItemCompoundName,
                customerBirthDateApplicationItemCompoundName: customerBirthDateApplicationItemCompoundName,
                backTarget: backTarget
            })
        }

        fetchCustomerComponentInitialDataByCustomerId(customerId: number, backTarget: string): ng.IPromise<CustomerComponentInitialData> {
            return this.post('/api/CustomerInfoComponent/FetchInitialByCustomerId', {
                customerId: customerId,
                backTarget: backTarget
            })
        }

        fetchCustomerItems(customerId: number, itemNames: string[]): ng.IPromise<CustomerItem[]> {
            return this.post('/api/CustomerInfo/FetchItems', {
                customerId: customerId,
                itemNames: itemNames
            })
        }

        fetchCustomerItemsDict(customerId: number, itemNames: string[]): ng.IPromise<IStringDictionary<string>> {
            let p = this.$q.defer<IStringDictionary<string>>()

            this.fetchCustomerItems(customerId, itemNames).then(x => {
                let d: IStringDictionary<string> = {}
                for (let i of x) {
                    d[i.name] = i.value
                }
                p.resolve(d)
            }, e => p.reject(e))

            return p.promise
        }

        fetchCustomerItemsBulk(customerIds: number[], itemNames: string[]): ng.IPromise<INumberDictionary<IStringDictionary<string>>> {
            return this.post('/api/CustomerInfo/FetchItemsBulk', {
                customerIds: customerIds,
                itemNames: itemNames
            })
        }

        fetchApplicationInfo(applicationNr: string): ng.IPromise<ApplicationInfoModel> {
            return this.post('/api/ApplicationInfo/Fetch', { applicationNr: applicationNr })
        }

        fetchApplicationInfoBulk(applicationNrs: string[]): ng.IPromise<NTechPreCreditApi.IStringDictionary<ApplicationInfoModel>> {
            return this.post('/api/ApplicationInfo/FetchBulk', { applicationNrs: applicationNrs })
        }

        fetchApplicationInfoWithApplicants(applicationNr: string): ng.IPromise<ApplicationInfoWithApplicantsModel> {
            return this.post('/api/ApplicationInfo/FetchWithApplicants', { applicationNr: applicationNr })
        }

        fetchCreditHistoryByCustomerId(customerIds: number[]): ng.IPromise<ApplicationInfoWithApplicantsModel> {
            return this.postUsingApiGateway('nCredit', '/api/CustomerCreditHistoryBatch', { customerIds: customerIds })
        }

        fetchApplicationInfoWithCustom(applicationNr: string, includeAppliants: boolean, includeWorkflowStepOrder: boolean): ng.IPromise<ApplicationInfoWithCustomModel> {
            return this.post('/api/ApplicationInfo/FetchWithCustom', { applicationNr, includeAppliants, includeWorkflowStepOrder })
        }

        fetchFraudControlModel(applicationNr: string): ng.IPromise<FraudControlModel> {
            return this.post('/api/FraudControl/FetchModel', { applicationNr: applicationNr });
        }

        addApplicationComment(applicationNr: string, commentText: string, opt: AddApplicationCommentsOptional): ng.IPromise<ApplicationComment> {
            return this.post('/api/ApplicationComments/Add', {
                applicationNr: applicationNr, commentText: commentText,
                attachedFileAsDataUrl: opt ? opt.attachedFileAsDataUrl : null,
                attachedFileName: opt ? opt.attachedFileName : null,
                eventType: opt ? opt.eventType : null
            })
        }

        fetchApplicationComments(applicationNr: string, opt: FetchApplicationCommentsOptional): ng.IPromise<ApplicationComment[]> {
            return this.post('/api/ApplicationComments/FetchForApplication', {
                applicationNr: applicationNr,
                hideTheseEventTypes: opt ? opt.hideTheseEventTypes : null,
                showOnlyTheseEventTypes: opt ? opt.showOnlyTheseEventTypes : null
            })
        }

        fetchApplicationComment(commentId: number): ng.IPromise<ApplicationComment> {
            return this.post('/api/ApplicationComments/FetchSingle', { commentId: commentId })
        }

        setApplicationWaitingForAdditionalInformation(applicationNr: string, isWaitingForAdditionalInformation: boolean): ng.IPromise<SetIsWaitingForAdditionalInformationResult> {
            return this.post('/api/ApplicationWaitingForAdditionalInformation/Set', { applicationNr: applicationNr, isWaitingForAdditionalInformation: isWaitingForAdditionalInformation });
        }

        fetchMortageLoanApplicationInitialCreditCheckStatus(applicationNr: string, backUrl: string): ng.IPromise<MortgageLoanApplicationInitialCreditCheckStatusModel> {
            return this.post('/api/MortgageLoan/CreditCheck/FetchInitialStatus', { applicationNr: applicationNr, backUrl: backUrl })
        }

        fetchMortageLoanApplicationFinalCreditCheckStatus(applicationNr: string, backUrl: string): ng.IPromise<MortgageLoanApplicationFinalCreditCheckStatusModel> {
            return this.post('/api/MortgageLoan/CreditCheck/FetchFinalStatus', { applicationNr: applicationNr, backUrl: backUrl })
        }

        fetchMortageLoanApplicationCustomerCheckStatus(applicationNr: string, urlToHereFromOtherModule: string, alsoUpdateStatus: boolean): ng.IPromise<MortgageLoanApplicationCustomerCheckStatusModel> {
            return this.post('/api/MortgageLoan/CustomerCheck/FetchStatus', { applicationNr: applicationNr, urlToHereFromOtherModule: urlToHereFromOtherModule, alsoUpdateStatus: alsoUpdateStatus })
        }

        doCustomerCheckKycScreen(applicationNr: string, applicantNrs: number[]): ng.IPromise<MortgageLoanApplicationCustomerCheckScreenResultItem[]> {
            return this.post('/api/MortgageLoan/CustomerCheck/DoKycScreen', { applicationNr: applicationNr, applicantNrs: applicantNrs })
        }

        approveMortageLoanApplicationCustomerCheck(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/CustomerCheck/Approve', { applicationNr: applicationNr })
        }

        fetchAllCheckpointsForApplication(applicationNr: string, applicationType: string): ng.IPromise<ApplicationCheckPointModel[]> {
            return this.post('/api/ApplicationCheckpoint/FetchAllForApplication', { applicationNr: applicationNr, applicationType: applicationType });
        }

        fetchCheckpointReasonText(checkpointId: number): ng.IPromise<string> {
            return this.post('/api/ApplicationCheckpoint/FetchReasonText', { checkpointId: checkpointId });
        }

        cancelApplication(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/ApplicationCancellation/Cancel', { applicationNr: applicationNr });
        }

        reactivateCancelledApplication(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/ApplicationCancellation/Reactivate', { applicationNr: applicationNr });
        }

        fetchProviderInfo(providerName: string): ng.IPromise<ProviderInfoModel> {
            return this.post('/api/ProviderInfo/FetchSingle', { providerName: providerName });
        }

        fetchApplicationDocuments(applicationNr: string, documentTypes: string[]): ng.IPromise<ApplicationDocument[]> {
            return this.post('/api/ApplicationDocuments/FetchForApplication', { applicationNr: applicationNr, documentTypes: documentTypes })
        }

        fetchFreeformApplicationDocuments(applicationNr: string): ng.IPromise<ApplicationDocument[]> {
            return this.post('/api/ApplicationDocuments/FetchFreeformForApplication', { applicationNr: applicationNr })
        }

        addAndRemoveApplicationDocument(applicationNr: string, documentType: string, applicantNr: number, dataUrl: string, filename: string, documentIdToRemove: number, customerId: number, documentSubType: string): ng.IPromise<ApplicationDocument> {
            return this.post('/api/ApplicationDocuments/AddAndRemove', { applicationNr: applicationNr, documentType: documentType, applicantNr: applicantNr, dataUrl: dataUrl, filename: filename, documentIdToRemove: documentIdToRemove, customerId: customerId, documentSubType: documentSubType })
        }

        addApplicationDocument(applicationNr: string, documentType: string, applicantNr: number, dataUrl: string, filename: string, customerId: number, documentSubType: string): ng.IPromise<ApplicationDocument> {
            return this.post('/api/ApplicationDocuments/Add', { applicationNr: applicationNr, documentType: documentType, applicantNr: applicantNr, dataUrl: dataUrl, filename: filename, customerId: customerId, documentSubType: documentSubType })
        }

        removeApplicationDocument(applicationNr: string, documentId: number): ng.IPromise<void> {
            return this.post('/api/ApplicationDocuments/Remove', { applicationNr: applicationNr, documentId: documentId })
        }

        updateMortgageLoanDocumentCheckStatus(applicationNr: string): ng.IPromise<ApplicationDocumentCheckStatusUpdateResult> {
            return this.post('/api/ApplicationDocuments/UpdateMortgageLoanDocumentCheckStatus', { applicationNr: applicationNr })
        }

        fetchMortageLoanAdditionalQuestionsStatus(applicationNr: string): ng.IPromise<MortgageLoanAdditionalQuestionsStatusModel> {
            return this.post('/api/mortageloan/fetch-additional-questions-status2', { applicationNr: applicationNr })
        }

        fetchMortgageLoanAdditionalQuestionsDocument(key: string): ng.IPromise<MortgageLoanAdditionalQuestionsDocument> {
            return this.post('/api/mortageloan/fetch-additional-questions-document', { key: key })
        }

        fetchMortgageLoanCurrentLoans(applicationNr: string): ng.IPromise<MortageLoanCurrentLoansModel> {
            return this.post('/api/mortageloan/fetch-current-loans', { applicationNr: applicationNr })
        }

        completeMortgageLoanFinalCreditCheck(applicationNr: string, acceptedFinalOffer: MortgageLoanAcceptedFinalOffer, scoringSessionKey: string, wasHandlerLimitOverridden: boolean): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/CreditCheck/CompleteFinal', { applicationNr: applicationNr, acceptedFinalOffer: acceptedFinalOffer, scoringSessionKey: scoringSessionKey, wasHandlerLimitOverridden: wasHandlerLimitOverridden });
        }

        rejectMortgageLoanFinalCreditCheck(applicationNr: string, rejectionReasons: string[], scoringSessionKey: string): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/CreditCheck/RejectFinal', { applicationNr: applicationNr, rejectionReasons: rejectionReasons, scoringSessionKey: scoringSessionKey });
        }

        searchForMortgageLoanApplicationByOmniValue(omniSearchValue: string): ng.IPromise<MortgageApplicationWorkListApplication[]> {
            let r: ng.IPromise<{ Applications: MortgageApplicationWorkListApplication[] }> = this.post('/api/MortgageLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue });
            return r.then(x => x.Applications)
        }

        searchForMortgageLoanApplicationOrLeadsByOmniValue(omniSearchValue: string): ng.IPromise<{ Applications: MortgageApplicationWorkListApplication[], Leads: MortgageApplicationWorkListApplication[] }> {
            return this.post('/api/MortgageLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue, includeLeads: true });
        }

        createMortgageLoan(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/mortageloan/create-loan', {
                applicationNr: applicationNr
            })
        }

        setMortgageApplicationWorkflowStatus(applicationNr: string, stepName: string, statusName: string, commentText?: string, eventCode?: string, companionOperation?: string): ng.IPromise<{ WasChanged: boolean, EventId?: number }> {
            return this.post('/api/MortgageLoan/Set-WorkflowStatus', { applicationNr, stepName, statusName, commentText, eventCode, companionOperation })
        }

        scheduleMortgageLoanOutgoingSettlementPayment(applicationNr: string, interestDifferenceAmount: number, actualLoanAmount: number): ng.IPromise<void> {
            return this.post('/api/mortageloan/schedule-outgoing-settlement-payment', {
                applicationNr: applicationNr,
                interestDifferenceAmount: interestDifferenceAmount,
                actualLoanAmount: actualLoanAmount
            })
        }

        cancelScheduledMortgageLoanOutgoingSettlementPayment(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/mortageloan/cancel-outgoing-settlement-payment', {
                applicationNr: applicationNr
            })
        }

        fetchMortgageApplicationValuationStatus(applicationNr: string, backUrl: string, autoAcceptSuggestion: boolean): ng.IPromise<MortgageLoanApplicationValuationStatusModel> {
            return this.post('/Api/MortgageLoan/Valuation/FetchStatus', { applicationNr: applicationNr, backUrl: backUrl, autoAcceptSuggestion: autoAcceptSuggestion })
        }

        ucbvSokAddress(adress: string, postnr: string, postort: string, kommun: string): ng.IPromise<UcbvSokAdressHit[]> {
            return this.post('/Api/MortgageLoan/Valuation/UcbvSokAddress', { adress: adress, postnr: postnr, postort: postort, kommun: kommun })
        }

        ucbvHamtaObjekt(id: string): ng.IPromise<UcbvObjectInfo> {
            return this.post('/Api/MortgageLoan/Valuation/UcbvHamtaObjekt', { id: id })
        }

        ucbvVarderaBostadsratt(request: IUcbvVarderaBostadsrattRequest): ng.IPromise<UcbvVarderaBostadsrattResponse> {
            return this.post('/Api/MortgageLoan/Valuation/UcbvVarderaBostadsratt', request)
        }

        automateMortgageApplicationValution(applicationNr: string): ng.IPromise<MortgageApplicationValutionResult> {
            return this.post('/Api/MortgageLoan/Valuation/AutomateValution', { applicationNr: applicationNr })
        }

        tryAutomateMortgageApplicationValution(applicationNr: string): ng.IPromise<MortgageApplicationTryValutionResult> {
            return this.post('/Api/MortgageLoan/Valuation/TryAutomateValution', { applicationNr: applicationNr })
        }

        acceptMortgageLoanUcbvValuation(applicationNr: string, valuationItems: MortgageApplicationValutionResult): ng.IPromise<void> {
            return this.post('/Api/MortgageLoan/Valuation/AcceptUcbvValuation', { applicationNr: applicationNr, valuationItems: valuationItems })
        }

        updateMortgageLoanDirectDebitCheckStatus(applicationNr: string, newStatus: string, bankAccountNr: string, bankAccountOwnerApplicantNr: number): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/DirectDebitCheck/UpdateStatus', { applicationNr: applicationNr, newStatus: newStatus, bankAccountNr: bankAccountNr, bankAccountOwnerApplicantNr: bankAccountOwnerApplicantNr })
        }

        fetchMortgageLoanDirectDebitCheckStatus(applicationNr: string): ng.IPromise<MortgageLoanApplicationDirectDebitStatusModel> {
            return this.post('/api/MortgageLoan/DirectDebitCheck/FetchStatus', { applicationNr: applicationNr })
        }

        validateBankAccountNr(bankAccountNr: string, bankAccountNrType?: string): ng.IPromise<ValidateBankAccountNrResult> {
            return this.post('/api/bankaccount/validate-nr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType })
        }

        acceptNewLoan(request: AcceptNewLoanRequest): ng.IPromise<IPartialAcceptNewLoanResponse> {
            return this.post('/CreditCheck/AcceptNewLoan', request)
        }

        rejectUnsecuredLoanApplication(request: RejectUnsecuredLoanApplicationRequest): ng.IPromise<{ userWarningMessage?: string }> {
            return this.post('/CreditCheck/Reject', request)
        }

        acceptUnsecuredLoanAdditionalLoanApplication(request: AcceptUnsecuredLoanAdditionalLoanApplicationRequest): ng.IPromise<AcceptUnsecuredLoanAdditionalLoanApplicationResponse> {
            return this.post('/CreditCheck/AcceptAdditionalLoan', request)
        }

        fetchOtherApplications(applicationNr: string, backUrl: string, includeApplicationObjects: boolean = false): ng.IPromise<OtherApplicationsResponseModel> {
            return this.post('/api/OtherApplications/Fetch', { applicationNr: applicationNr, backUrl: backUrl, includeApplicationObjects: includeApplicationObjects })
        }

        fetchotherApplicationsByCustomerId(customerIds: number[], applicationNr: string, includeApplicationObjects: boolean = false): ng.IPromise<OtherApplicationsResponseModel> {
            return this.post('/api/OtherApplications/FetchByCustomerIds', { customerIds: customerIds, applicationNr: applicationNr, includeApplicationObjects: includeApplicationObjects })
        }

        fetchExternalApplicationRequestJson(applicationNr: string): ng.IPromise<FetchExternalApplicationRequestJsonResponse> {
            return this.post('/api/ApplicationInfo/FetchExternalRequestJson', { applicationNr: applicationNr })
        }

        fetchMortgageLoanObjectInfo(applicationNr: string): ng.IPromise<MortageLoanObjectModel> {
            return this.post('/api/MortgageLoan/Object/FetchInfo', { applicationNr: applicationNr })
        }

        fetchMortgageLoanSettlementData(applicationInfo: ApplicationInfoModel): ng.IPromise<MortgageLoanSettlementDataModel> {
            return this.post('/api/mortageloan/fetch-settlement-data', { applicationNr: applicationInfo.ApplicationNr })
        }

        sendMortgageLoanProviderCallback(applicationNr: string, eventName: string): ng.IPromise<MortageLoanProviderCallbackResultModel> {
            return this.post('/api/mortageloan/send-providercallback', { applicationNr: applicationNr, eventName: eventName })
        }

        fetchLeftToLiveOnRequiredItemNames(): ng.IPromise<FetchLeftToLiveOnRequiredItemNamesResult> {
            return this.post('/api/MortgageLoan/CreditCheck/FetchLeftToLiveOnRequiredItemNames', {})
        }

        computeLeftToLiveOn(scoringDataModel: ScoringDataModel, interestRatePercent: number): ng.IPromise<ComputeLeftToLiveOnResult> {
            return this.post('/api/MortgageLoan/CreditCheck/ComputeLeftToLiveOn', { jsonData: JSON.stringify({ scoringDataModel, interestRatePercent }) })
        }

        fetchMortgageLoanAmortizationBasis(applicationNr: string): ng.IPromise<MortgageLoanAmortizationBasisModel> {
            return this.post('/api/MortgageLoan/Amortization/FetchBasis', { applicationNr: applicationNr })
        }

        setMortgageLoanAmortizationBasis(applicationNr: string, basis: MortgageLoanAmortizationBasisModel): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/Amortization/SetBasis', { applicationNr: applicationNr, basis: basis })
        }

        calculateMortgageLoanAmortizationSuggestionBasedOnStandardBankForm(applicationNr: string, bankForm: MortgageLoanBankFormModel): ng.IPromise<MortgageLoanAmortizationBasisModel> {
            return this.post('/api/MortgageLoan/Amortization/CalculateSuggestionBasedOnStandardBankForm', { applicationNr: applicationNr, bankForm: bankForm })
        }

        fetchMortgageLoanApplicationBasisCurrentValues(applicationNr: string): ng.IPromise<MortgageLoanApplicationBasisCurrentValuesModel> {
            return this.post('/api/MortgageLoan/ApplicationBasis/FetchCurrentValues', { applicationNr: applicationNr })
        }

        fetchHouseholdIncomeModel(applicationNr: string, includeUsernames: boolean): ng.IPromise<FetchHouseholdIncomeModelResult> {
            return this.post('/api/MortgageLoan/ApplicationBasis/FetchHouseholdIncomeModel', { applicationNr: applicationNr, includeUsernames: includeUsernames })
        }

        setHouseholdIncomeModel(applicationNr: string, householdIncomeModel: HouseholdIncomeModel): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/ApplicationBasis/SetHouseholdIncomeModel', { applicationNr: applicationNr, householdIncomeModel: householdIncomeModel })
        }

        fetchMortgageApplicationWorkListPage(currentBlockCode: string, pageNr?: number, pageSize?: number, 
            includeCurrentBlockCodeCounts?: boolean, separatedWorkList?: string, 
            handlerFilter?: { onlyUnassigned?: boolean, assignedToHandlerUserId?: number }): ng.IPromise<MortgageApplicationWorkListPageResult> {

            return this.post('/api/MortgageLoan/WorkList/FetchPage', { 
                currentBlockCode: currentBlockCode, 
                pageNr: pageNr, 
                pageSize: pageSize, 
                includeCurrentBlockCodeCounts: includeCurrentBlockCodeCounts, 
                separatedWorkList: separatedWorkList,
                onlyNoHandlerAssignedApplications: handlerFilter ? handlerFilter.onlyUnassigned : null,
                assignedToHandlerUserId: handlerFilter ? handlerFilter.assignedToHandlerUserId : null
            })
        }

        updateMortgageLoanAdditionalQuestionsStatus(applicationNr: string): ng.IPromise<MortgageLoanAdditionalQuestionsStatusUpdateResult> {
            return this.post('/api/mortgageloan/update-additionalquestions-status/', {
                applicationNr: applicationNr
            })
        }

        calculateLeftToLiveOn(algorithmName: string, scoringData: ScoringDataModelFlat): ng.IPromise<CalculateLeftToLiveOnResult> {
            return this.post('/api/Scoring/LeftToLiveOn/Calculate', {
                AlgorithmName: algorithmName,
                ScoringData: scoringData
            })
        }

        fetchCustomerKycScreenStatus(customerId: number): ng.IPromise<FetchCustomerKycScreenStatus> {
            return this.post('/api/Kyc/FetchCustomerScreeningStatus', {
                CustomerId: customerId
            })
        }

        kycScreenCustomer(customerId: number, force: boolean): ng.IPromise<KycScreenCustomerResult> {
            return this.post('/api/Kyc/ScreenCustomer', {
                CustomerId: customerId,
                Force: force
            })
        }

        fetchUnsecuredLoanAdditionalQuestionsStatus(applicationNr: string): ng.IPromise<UnsecuredLoanAdditionalQuestionsStatusResult> {
            return this.post('/api/AdditionalQuestions/FetchApplicationStatus', { applicationNr: applicationNr })
        }

        fetchUnsecuredLoanCreditCheckStatus(applicationNr: string, urlToHere: string, backUrl: string, includePauseItems: boolean, includeRejectionReasonDisplayNames: boolean): ng.IPromise<UnsecuredLoanCreditCheckStatusResult> {
            return this.post('/api/UnsecuredApplication/FetchCreditCheckStatus', {
                ApplicationNr: applicationNr, BackUrl: backUrl,
                UrlToHere: urlToHere, IncludePauseItems: includePauseItems,
                IncludeRejectionReasonDisplayNames: includeRejectionReasonDisplayNames
            })
        }

        fetchAllAffiliateReportingEventsForApplication(applicationNr: string, includeAffiliateMetadata: boolean): ng.IPromise<AffiliateReportingEventsResponse> {
            return this.post('/api/AffiliateReporting/Events/FetchAllForApplication', {
                ApplicationNr: applicationNr,
                IncludeAffiliateMetadata: includeAffiliateMetadata
            })
        }

        resendAffiliateReportingEvent(eventId: number): ng.IPromise<void> {
            return this.post('/api/AffiliateReporting/Events/Resend', {
                Id: eventId
            })
        }

        fetchAllAffiliates(): ng.IPromise<FetchAffiliatesModel> {
            return this.post('/api/Affiliates/FetchAll', {})
        }

        fetchCustomerIdByCivicRegNr(civicRegNr: string): ng.IPromise<number> {
            return this.post('/api/CustomerInfo/FetchCustomerIdByCivicRegNr', { civicRegNr })
        }

        fetchCustomerIdByOrgnr(orgnr: string): ng.IPromise<number> {
            return this.post('/api/CustomerInfo/FetchCustomerIdByOrgnr', { orgnr })
        }

        addCustomerToApplicationList(applicationNr: string, listName: string, customerId: number, civicRegNr: string, firstName: string, lastName: string, email: string, phone: string, addressStreet: string, addressZipcode: string, addressCity: string, addressCountry: string): ng.IPromise<AddCustomerToApplicationListResponse> {
            return this.post('/api/ApplicationCustomerList/Add-Customer', {
                ApplicationNr: applicationNr,
                ListName: listName,
                CustomerId: customerId,
                CreateOrUpdateData: {
                    CivicRegNr: civicRegNr,
                    FirstName: firstName,
                    LastName: lastName,
                    Email: email,
                    Phone: phone,
                    AddressStreet: addressStreet,
                    AddressZipcode: addressZipcode,
                    AddressCity: addressCity,
                    AddressCountry: addressCountry
                }
            })
        }

        removeCustomerFromApplicationList(applicationNr: string, listName: string, customerId: number): ng.IPromise<RemoveCustomerFromApplicationListResponse> {
            return this.post('/api/ApplicationCustomerList/Remove-Customer', {
                ApplicationNr: applicationNr,
                ListName: listName,
                CustomerId: customerId
            })
        }

        fetchCustomerApplicationListMembers(applicationNr: string, listName: string): ng.IPromise<FetchCustomerApplicationListMembersResponse> {
            return this.post('/api/ApplicationCustomerList/Fetch-Members', {
                ApplicationNr: applicationNr,
                ListName: listName
            })
        }

        switchApplicationListStatus(applicationNr: string, listPrefixName: string, statusName: string, commentText?: string, eventCode?: string): ng.IPromise<void> {
            return this.post('/api/Application/Switch-ListStatus', { applicationNr, listPrefixName, statusName, commentText, eventCode })
        }

        kycScreenBatchByApplicationNr(applicationNr: string, screenDate: Date): ng.IPromise<{ Success: boolean }> {
            return this.post('/api/CompanyLoan/KycScreenByApplicationNr', { ApplicationNr: applicationNr, ScreenDate: screenDate })
        }

        fetchListCustomersWithKycStatusMethod(applicationNr: string, listNames: string[]): ng.IPromise<{
            Customers: ListCustomersWithKycStatusModel[]
        }> {
            return this.post('/api/CompanyLoan/FetchListCustomersWithKycStatusMethod', { applicationNr, listNames })
        }

        fetchApplicationDataSourceItems(applicationNr: string, requests: FetchApplicationDataSourceRequestItem[]): ng.IPromise<FetchApplicationDataSourceItemsResponse> {
            return this.post('/api/Application/FetchDataSourceItems', { applicationNr, requests })
        }

        createManualSignatureDocuments(dataUrl: string, fileName: string, civicRegNr: string, commentText: string): ng.IPromise<DocumentsResponse> {
            return this.post('/api/ManualSignatures/CreateDocuments', { dataUrl, fileName, civicRegNr, commentText })
        }

        deleteManualSignatureDocuments(sessionId: string) {
            this.post('/api/ManualSignatures/DeleteDocuments', { sessionId })
        }

        getManualSignatureDocuments(signedDocuments: boolean): ng.IPromise<ManualSignatureResponse[]> {
            return this.post('/api/ManualSignatures/GetDocuments', { signedDocuments })
        }

        handleManualSignatureDocuments(sessionId: string) {
            this.post('/api/ManualSignatures/HandleDocuments', { sessionId })
        }

        /**
         * @param applicationNr
         * @param groupedNames like application.amount or applicant2.signedAgreementKey
         * @param missingReplacementValue something like 'missing' which will be the result for items that have no value
         */
        fetchCreditApplicationItemSimple(applicationNr: string, groupedNames: string[], missingReplacementValue: string): ng.IPromise<IStringDictionary<string>> {
            let d = this.$q.defer<IStringDictionary<string>>()
            let request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSource(groupedNames, false, true, missingReplacementValue)
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(x => {
                let dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items)
                d.resolve(dict)
            }, d.reject)

            return d.promise
        }

        fetchComplexApplicationListItemSimple(applicationNr: string, groupedNames: string[], missingReplacementValue: string): ng.IPromise<IStringDictionary<string>> {
            let d = this.$q.defer<IStringDictionary<string>>()
            let request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSourceComplex(groupedNames, false, true, missingReplacementValue)
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(x => {
                let dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items)
                d.resolve(dict)
            }, d.reject)

            return d.promise
        }

        fetchCreditApplicationItemComplex(applicationNr: string, groupedNames: string[], missingReplacementValue: string): ng.IPromise<IStringDictionary<string>> {
            let d = this.$q.defer<IStringDictionary<string>>()
            let request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSourceComplex(groupedNames, false, true, missingReplacementValue)
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(x => {
                let dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items)
                d.resolve(dict)
            }, d.reject)

            return d.promise
        }

        fetchApplicationEditItemData(applicationNr: string, dataSourceName: string, itemName: string, defaultValueIfMissing: string, includeEdits: boolean): ng.IPromise<FetchApplicationEditItemDataResponse> {
            return this.post('/api/Application/Edit/FetchItemData', { applicationNr, dataSourceName, itemName, defaultValueIfMissing, includeEdits })
        }

        setApplicationEditItemData(applicationNr: string, dataSourceName: string, itemName: string, newValue: string, isDelete: boolean): ng.IPromise<SetApplicationEditItemDataResponse> {
            return this.post('/api/Application/Edit/SetItemData', {
                applicationNr, dataSourceName, itemName, newValue, isDelete
            })
        }

        fetchConsentAnswers(applicationNr: string): ng.IPromise<FetchConsentAnswersResponse> {
            return this.post('/api/Application/FetchConsentAnswers', {
                     ApplicationNr: applicationNr
            })
        }

        setApplicationEditItemDataBatched(applicationNr: string, edits: { dataSourceName: string, itemName: string, newValue: string, isDelete: boolean }[]): ng.IPromise<SetApplicationEditItemDataResponse> {
            return this.post('/api/Application/Edit/SetItemDataBatched', {
                applicationNr, edits
            })
        }

        fetchCurrentReferenceInterestRate(): ng.IPromise<number> {
            return this.postUsingApiGateway<{}, { referenceInterestRatePercent: number }>('nCredit', '/Api/ReferenceInterest/GetCurrent', {}).then(x => {
                return x.referenceInterestRatePercent
            })
        }

        getLockedAgreement(applicationNr: string): ng.IPromise<GetLockedAgreementResponse> {
            return this.post('/api/Agreement/Get-Locked', {
                ApplicationNr: applicationNr
            })
        }

        removeLockedAgreement(applicationNr: string): ng.IPromise<{ WasRemoved: boolean }> {
            return this.post('/api/Agreement/Remove-Locked', {
                ApplicationNr: applicationNr
            })
        }

        approveLockedAgreement(applicationNr: string, requestOverrideDuality?: boolean): ng.IPromise<ApproveLockedAgreementModel> {
            return this.post('/api/Agreement/Approve-Locked', {
                ApplicationNr: applicationNr,
                RequestOverrideDuality: requestOverrideDuality
            })
        }

        isValidAccountNr(bankAccountNr: string, bankAccountNrType: string): ng.IPromise<IsValidAccountNrResult> {
            return this.postUsingApiGateway('nCredit', 'Api/UnplacedPayment/IsValidAccountNr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType })
        }

        createItemBasedCreditDecision(request: {
            ApplicationNr: string,
            IsAccepted: boolean,
            SetAsCurrent: boolean,
            DecisionType?: string,
            WasAutomated?: boolean,
            ChangeCreditCheckStatusTo?: string,
            UniqueItems: IStringDictionary<string>,
            RepeatingItems: IStringDictionary<string[]>,
            RejectionReasonsItemName?: string
        }): ng.IPromise<{ Id: number }> {
            return this.post('/api/CreditDecision/Create-ItemBased', request)
        }

        fetchItemBasedCreditDecision(request: {
            ApplicationNr: string,
            MustBeCurrent?: boolean,
            MustBeAccepted?: boolean,
            MustBeRejected?: boolean,
            OnlyDecisionType?: string,
            MaxCount?: number,
            IncludeRejectionReasonToDisplayNameMapping?: boolean
        }): ng.IPromise<FetchItemBasedCreditDecisionModel> {
            return this.post('/api/CreditDecision/Fetch-ItemBased', request)
        }

        createOrUpdatePersonCustomer(request: {
            CivicRegNr: string
            BirthDate?: Date
            AdditionalSensitiveProperties?: string[]
            ExpectedCustomerId?: number
            EventType?: string
            EventSourceId?: string
            Properties: { Name: string, Value: string, ForceUpdate: boolean }[]
        }): ng.IPromise<{ CustomerId: number }> {
            return this.postUsingApiGateway('nCustomer', 'api/PersonCustomer/CreateOrUpdate', request)
        }

        createOrUpdatePersonCustomerSimple(
            civicRegNr: string,
            properties: IStringDictionary<string>,
            expectedCustomerId?: number,
            birthDate?: Date
        ): ng.IPromise<{ CustomerId: number }> {
            let r = {
                CivicRegNr: civicRegNr,
                BirthDate: birthDate,
                ExpectedCustomerId: expectedCustomerId,
                Properties: []
            }
            for (let name of Object.keys(properties)) {
                r.Properties.push({ Name: name, Value: properties[name], ForceUpdate: true })
            }
            return this.createOrUpdatePersonCustomer(r)
        }

        fetchCustomerOnboardingStatuses(customerIds: number[]): ng.IPromise<INumberDictionary<KycCustomerOnboardingStatusModel>> {
            return this.postUsingApiGateway('nCustomer', 'Api/KycManagement/FetchCustomerOnboardingStatuses', { customerIds: customerIds })
        }

        kycScreenBatch(customerIds: number[], screenDate: Date): ng.IPromise<{ Success: boolean }> {
            return this.postUsingApiGateway('nCustomer', 'Api/KycScreening/ListScreenBatch', { customerIds: customerIds, screenDate: screenDate })
        }

        auditAndCreateMortgageLoanLockedAgreement(applicationNr: string): ng.IPromise<{ LockedAgreement: LockedAgreementModel }> {
            return this.post('/api/MortgageLoan/Audit-And-Create-Locked-Agreement', { applicationNr: applicationNr })
        }

        fetchDualAgreementSignatureStatus(applicationNr: string): ng.IPromise<{
            IsSignatureStepAccepted: boolean,
            IsPendingSignatures: boolean,
            SignatureTokenByCustomerId: INumberDictionary<string>
        }> {
            return this.post('/api/MortgageLoan/Fetch-Dual-Agreement-SignatureStatus', { applicationNr: applicationNr })
        }

        fetchDualApplicationSignatureStatus(applicationNr: string): ng.IPromise<{ IsPendingSignatures: boolean, BankNamesByApplicantNr: INumberDictionary<string[]> }> {
            return this.post('/api/MortgageLoan/Fetch-Dual-Application-SignatureStatus', { applicationNr: applicationNr })
        }

        calculatePaymentPlan(request: {
            LoanAmount: number
            TotalInterestRatePercent: number
            RepaymentTimeInMonths?: number
            AnnuityOrFlatAmortizationAmount?: number
            IsFlatAmortization: boolean
            MonthlyFeeAmount?: number
            CapitalizedInitialFeeAmount?: number
            DrawnFromInitialPaymentInitialFeeAmount?: number
            PaidOnFirstNotificationInitialFeeAmount?: number
            MonthCountCapEvenIfNotFullyPaid?: number
            IncludePayments?: boolean
        }): ng.IPromise<CalculatedPaymentPlan> {
            return this.post('/api/PaymentPlan/Calculate', request)
        }

        initializeDualMortgageLoanSettlementPayments(applicationNr: string): ng.IPromise<{ WasInitialized: boolean }> {
            return this.post('/api/MortgageLoan/Initialize-Dual-SettlementPayments', { applicationNr: applicationNr })
        }

        createDualMortgageLoanSettlementPaymentsFile(applicationNr: string): ng.IPromise<{ PaymentFileArchiveKey: string }> {
            return this.post('/api/MortgageLoan/Create-DualSettlementPaymentsFile', { applicationNr: applicationNr })
        }

        createDualMortgageLoan(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/Create-Dual-Loan', { applicationNr: applicationNr })
        }

        submitAdditionalQuestions(applicationNr: string, document: MortgageLoanAdditionalQuestionsDocument, consumerBankAccountNr: string): ng.IPromise<void> {
            return this.post('/api/MortgageLoan/Submit-AdditionalQuestions', { ApplicationNr: applicationNr, QuestionsDocument: document, ConsumerBankAccountNr: consumerBankAccountNr })
        }

        createMortgageLoanLeadsWorkList(): ng.IPromise<{ WorkListId?: number, NoLeadsMatchFilter: boolean }> {
            return this.post('/api/MortgageLoan/Create-Leads-WorkList', {})
        }

        buyCreditReportForCustomerId(customerId: number, creditReportProviderName: string): ng.IPromise<void> {
            let returningItemNames = ["addressCountry", "addressStreet", "addressZipcode", "addressCity"] 
            return this.post('/api/BuyCreditReportForCustomer', { providerName: creditReportProviderName, customerId: customerId, returningItemNames: returningItemNames })
        }

        fetchMortgageLoanLeadsWorkListStatuses(overrideForUserId?: number): ng.IPromise<{ WorkLists: FetchMortgageLoanLeadsWorkListStatusesResultItem[]}> {
            return this.post('/api/MortgageLoan/Fetch-Leads-WorkList-Statuses', { UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId : !overrideForUserId })
        }

        tryCloseMortgageLoanWorkList(workListId: number): ng.IPromise<{ WasClosed: boolean }> {
            return this.post('/api/WorkLists/TryCloseWorkList', { WorkListId: workListId, UseCurrentUserId: true })
        }

        tryTakeMortgageLoanWorkListItem(workListId: number, overrideForUserId?: number): ng.IPromise<{ WasItemTaken: boolean, TakenItemId: string }> {
            return this.post('/api/WorkLists/TryTakeWorkListItem', { WorkListId: workListId, UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId : !overrideForUserId })
        }

        fetchMortgageLoanWorkListItemStatus(workListId: number, itemId: string, overrideForUserId?: number): ng.IPromise<MortgageLoanLeadsWorkListItemStatus> {
            return this.post('/api/MortgageLoan/Fetch-Leads-WorkList-Item-Status', { WorkListId: workListId, ItemId: itemId, UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId : !overrideForUserId })
        }

        tryCompleteOrReplaceMortgageLoanWorkListItem(workListId: number, itemId: string, isReplace: boolean): ng.IPromise<{ WasReplaced: boolean, WasCompleted: boolean }> {
            return this.post('/api/WorkLists/TryCompleteOrReplaceWorkListItem', { WorkListId: workListId, ItemId: itemId, IsReplace: isReplace })
        }

        tryComplateMortgageLoanLead(applicationNr: string, completionCode: string, rejectionReasons: string[], rejectionReasonOtherText: string, tryLaterDays: number): ng.IPromise<{ WasChangedToQualifiedLead: boolean, WasCancelled: boolean, WasRejected: boolean, WasTryLaterScheduled: boolean }> {
            return this.post('/api/MortgageLoan/Complete-Lead', { 
                ApplicationNr: applicationNr, 
                CompletionCode: completionCode,
                RejectionReasons: rejectionReasons,
                RejectionReasonOtherText: rejectionReasonOtherText,
                TryLaterDays: tryLaterDays
            })
        }

        fetchApplicationAssignedHandlers(opts: { applicationNr?: string, returnAssignedHandlers: boolean, returnPossibleHandlers: boolean })
            : ng.IPromise<{ AssignedHandlers: AssignedHandlerModel[], PossibleHandlers: AssignedHandlerModel[] }> {

            return this.post('/api/ApplicationAssignedHandlers/Fetch', { 
                ApplicationNr: opts ? opts.applicationNr : null,  
                ReturnAssignedHandlers : opts ? opts.returnAssignedHandlers : null,
                ReturnPossibleHandlers : opts ? opts.returnPossibleHandlers : null
            })
        }        

        setApplicationAssignedHandlers(applicationNr: string, assignHandlerUserIds: number[], unAssignHandlerUserIds : number[]): ng.IPromise<{ AllAssignedHandlers : AssignedHandlerModel[] }> {
            return this.post('/api/ApplicationAssignedHandlers/Set', {
                ApplicationNr: applicationNr,
                AssignHandlerUserIds: assignHandlerUserIds,
                UnAssignHandlerUserIds: unAssignHandlerUserIds
            })
        }

        createCampaignReturningId(name: string, id?: string) : ng.IPromise<string> {
            return this.post('/api/Campaigns/Create', { name, id })
        }

        fetchCampaigns(options: { pageSize?: number, zeroBasedPageNr?: number, includeInactive?: boolean, includeDeleted?: boolean, singleCampaignId?: string, includeCodes?: boolean }): ng.IPromise<{
            Campaigns: CampaignModel[]
            CurrentPageNr: number
            TotalPageCount: number
        }> {
            return this.post('/api/Campaigns/Fetch', options || {})
        }

        fetchCampaign(campaignId: string): ng.IPromise<CampaignModel> {
            return this.fetchCampaigns({ singleCampaignId: campaignId, includeDeleted: true, includeInactive: true, includeCodes: true }).then(x => {
                return x.Campaigns && x.Campaigns.length > 0 ? x.Campaigns[0] : null
            })
        }

        deleteOrInactivateCampaign(campaignId: string, isDelete: boolean) : ng.IPromise<void> {
            return this.post('/api/Campaigns/DeleteOrInactivate', { campaignId, IsDelete: !!isDelete, IsInactivate: !isDelete })
        }

        deleteCampaignCode(campaignCodeId: number) : ng.IPromise<void> {
            return this.post('/api/Campaigns/DeleteCampaignCode', { campaignCodeId })
        }

        createCampaignCode(campaignId: string,
            code: string,
            startDate?: string,
            endDate?: string,
            commentText?: string,
            isGoogleCampaign?: boolean): ng.IPromise<{ Id: string }> {

            return this.post('/api/Campaigns/CreateCampaignCode', { campaignId, code, startDate, endDate, commentText, isGoogleCampaign })
        }

        archiveSingleApplication(applicationNr: string) {
            return this.post('/api/Application/ArchiveSingle', { applicationNr })
        }

        cancelUnsecuredLoanApplicationSignatureSession(applicationNr: string) {
            return this.post('/api/UnsecuredLoanApplication/Cancel-Signature-Session', { applicationNr })
        }
    }
    
    export interface CampaignModel {
        Id: string
        Name: string
        CreatedDate: Date
        CreatedByUserId: number
        CreatedByUserDisplayName: string
        IsActive: boolean
        IsDeleted: boolean
        AppliedToApplicationCount: boolean
        AreCodesIncluded: boolean
        Codes: CampaignCodeModel[]
    }

    export interface CampaignCodeModel {
        Id: number
        Code: string
        IsGoogleCampaign: boolean
        StartDate?: Date
        EndDate?: Date
        CreatedDate: Date
        CreatedByUserId: number
        CreatedByUserDisplayName: string
        CommentText: string
    }

    export interface AssignedHandlerModel {
        UserId: number
        UserDisplayName: string
    }

    export interface MortgageLoanLeadsWorkListItemStatus {
        WorkListHeaderId: number
        ItemId: string
        CompletedCount: number
        TakenCount: number
        CurrentUserId: number
        IsTakenByCurrentUser: boolean
        TakeOrCompletedByCurrentUserCount: number
        IsTakePossible: boolean
    }

    export interface FetchMortgageLoanLeadsWorkListStatusesResultItem {
        WorkListHeaderId: number
        ClosedDate?: Date
        ClosedByUserId?: number
        ClosedByUserDisplayName?: string
        CreationDate: Date
        CreatedByUserId: number
        CreatedByUserDisplayName: string
        TotalCount: number
        CompletedCount: number        
        TakenCount: number
        CurrentUserActiveItemId: string
        TakeOrCompletedByCurrentUserCount: number
        IsTakePossible: boolean
        IsUnderConstruction: boolean
    }

    export interface CalculatedPaymentPlan {
        TotalPaidAmount: number
        EffectiveInterestRatePercent?: number
        AnnuityAmount?: number
        FlatAmortizationAmount?: number
        InitialCapitalDebtAmount: number
        Payments?: {
            CapitalAmount: number
            InterestAmount: number
            MonthlyFeeAmount: number
            InitialFeeAmount: number
            TotalAmount: number
        }[]
    }

    export interface KycCustomerOnboardingStatusModel {
        CustomerId: number
        IsPep?: boolean
        IsSanction?: boolean
        LatestScreeningDate?: Date
    }

    export interface ItemBasedDecisionModel {
        Id: number
        IsAccepted: boolean
        DecisionDate: Date
        DecisionType: string
        WasAutomated: boolean
        IsCurrent: boolean
        UniqueItems: IStringDictionary<string>,
        RepeatingItems: IStringDictionary<string[]>
        ExistsLaterDecisionOfDifferentType: boolean
        ExistsEarlierDecisionOfDifferentType: boolean
    }

    export interface FetchItemBasedCreditDecisionModel {
        Decisions: ItemBasedDecisionModel[]
        RejectionReasonToDisplayNameMapping: IStringDictionary<string>
        ExistsLaterDecisionOfDifferentType?: boolean
        ExistsEarlierDecisionOfDifferentType?: boolean
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

    export interface ApproveLockedAgreementModel {
        WasApproved: boolean
        LockedAgreement: LockedAgreementModel
    }

    export interface GetLockedAgreementResponse {
        LockedAgreement: LockedAgreementModel
    }

    export interface LockedAgreementModel {
        UnsignedAgreementArchiveKey: string,
        LoanAmount: number,
        CreditDecisionId: number,
        LockedByUserId: number
        LockedDate: Date,
        ApprovedByUserId?: number
        ApprovedDate?: Date
        IsMultiAgreement: boolean
        UnsignedAgreementArchiveKeyByCustomerId: INumberDictionary<string>
    }

    export interface SetApplicationEditItemDataResponse {
        ChangeId: number
    }

    export interface FetchConsentAnswersResponse {
        ConsentItems: ConsentItems[]
    }

    export interface ConsentItems {
        ApplicationNr: string
        GroupName: string
        Item: string
    }

    export interface FetchApplicationEditItemDataResponse {
        ApplicationNr: string
        ItemValue: string
        EditModel: FetchApplicationEditItemDataResponseEditModel
        HistoricalChanges: FetchApplicationEditItemDataResponseHistoryItemModel[]
    }

    export interface FetchApplicationEditItemDataResponseEditModel {
        DataSourceName: string
        ItemName: string
        EditorType: string
        DataType: string
        DropdownRawOptions?: string[]
        DropdownRawDisplayTexts?: string[]
        IsRemovable?: boolean
        IsRequired?: boolean
        IsReadonly?: boolean
        LabelText: string
    }

    export enum ApplicationEditItemDataType {
        positiveInt = 'positiveInt',
        positiveDecimal = 'positiveDecimal',
        dropdownRaw = 'dropdownRaw ',
        url = 'url'
    }

    export enum ApplicationStatusItem {
        cancelled = 'Cancelled', 
        rejected = 'Rejected', 
        finalDecisionMade = 'Paid Out'
    }

    export interface FetchApplicationEditItemDataResponseHistoryItemModel {
        ChangeId: number
        FromValue: string
        ToValue: string
        Date: Date
        UserId: number
        TransactionType: string
    }

    export interface FetchApplicationDataSourceItemsResponse {
        Results: FetchApplicationDataSourceItemsResponseItem[]
    }

    export interface FetchApplicationDataSourceItemsResponseItem {
        DataSourceName: string
        MissingNames: string[]
        Items: { Name: string, Value: string, EditorModel: FetchApplicationEditItemDataResponseEditModel }[]
        ChangedNames: string[]
    }

    export class DocumentsResponse {
        ArchiveDocumentUrl: string
        SessionId: string
        SignatureUrl: string
        CreationDate: Date
    }

    export class ManualSignatureResponse {
        SignatureSessionId: string
        CreationDate: Date
        CommentText: string
        UnSignedDocumentArchiveUrl: string
        IsRemoved: boolean
        RemovedDate: Date
        IsHandled: boolean
        HandledDate: Date
        SignedDocumentArchiveUrl: string
        SignedDate: Date
        SignatureUrl: string
    }

    export class FetchApplicationDataSourceRequestItem {
        DataSourceName: string
        Names: string[]
        MissingItemReplacementValue: string
        ErrorIfMissing: boolean
        ReplaceIfMissing: boolean
        IncludeIsChanged: boolean
        IncludeEditorModel: boolean

        public static createCreditApplicationItemSource(names: string[], errorIfMissing: boolean, replaceIfMissing: boolean, missingReplacementValue: string, includeIsChanged?: boolean, includeEditorModels?: boolean): FetchApplicationDataSourceRequestItem {
            return {
                DataSourceName: 'CreditApplicationItem',
                Names: names,
                ErrorIfMissing: errorIfMissing,
                ReplaceIfMissing: replaceIfMissing,
                MissingItemReplacementValue: missingReplacementValue,
                IncludeIsChanged: includeIsChanged,
                IncludeEditorModel: includeEditorModels
            }
        }

        public static createCreditApplicationItemSourceComplex(names: string[], errorIfMissing: boolean, replaceIfMissing: boolean, missingReplacementValue: string, includeIsChanged?: boolean, includeEditorModels?: boolean): FetchApplicationDataSourceRequestItem {
            return {
                DataSourceName: 'ComplexApplicationList',
                Names: names,
                ErrorIfMissing: errorIfMissing,
                ReplaceIfMissing: replaceIfMissing,
                MissingItemReplacementValue: missingReplacementValue,
                IncludeIsChanged: includeIsChanged,
                IncludeEditorModel: includeEditorModels
            }
        }

        public static resultAsDictionary(items: { Name: string, Value: string, EditorModel: FetchApplicationEditItemDataResponseEditModel }[]): IStringDictionary<string> {
            let dict: IStringDictionary<string> = {}
            for (let i of items) {
                dict[i.Name] = i.Value
            }
            return dict
        }

        public static editorModelsAsDictionary(items: { Name: string, Value: string, EditorModel: FetchApplicationEditItemDataResponseEditModel }[]): IStringDictionary<FetchApplicationEditItemDataResponseEditModel> {
            let dict: IStringDictionary<FetchApplicationEditItemDataResponseEditModel> = {}
            for (let i of items) {
                dict[i.Name] = i.EditorModel
            }
            return dict
        }
    }

    export interface FetchCustomerApplicationListMembersResponse {
        CustomerIds: number[]
    }

    export interface RemoveCustomerFromApplicationListResponse {
        CustomerId: number
        WasRemoved: boolean
    }

    export interface AddCustomerToApplicationListResponse {
        CustomerId: number
        WasAdded: boolean
    }

    export interface FetchAffiliatesModel {
        Affiliates: AffiliateModel[]
    }

    export interface AffiliateModel {
        ProviderName: string
        DisplayToEnduserName: string
        StreetAddress: string
        EnduserContactPhone: string
        EnduserContactEmail: string
        WebsiteAddress: string
        IsSelf: boolean
        IsSendingRejectionEmails: boolean
        IsUsingDirectLinkFlow: boolean
        HasBrandedAdditionalQuestions: boolean
        BrandingTag: string
        IsSendingAdditionalQuestionsEmail: boolean
        IsMortgageLoanProvider: boolean
        MortgageLoanProviderIntegrationName: string
        FallbackCampaignCode: string
    }

    export interface AcceptUnsecuredLoanAdditionalLoanApplicationRequest {
        applicationNr: string
        additionalLoanOffer: AcceptUnsecuredLoanAdditionalLoanApplicationRequestOffer
        recommendationKey: string
    }

    export interface AcceptUnsecuredLoanAdditionalLoanApplicationRequestOffer {
        AdditionalLoanCreditNr: string
        AdditionalLoanAmount: number
        NewMarginInterestRatePercent: number
        NewAnnuityAmount: number
        NewNotificationFeeAmount: number
    }

    export interface ApplicationInfoWithApplicantsModel {
        Info: ApplicationInfoModel
      //  CustomerIdByApplicantNr: { [applicantNr: number]: number }
        CustomerIdByApplicantNr: number[]
        CustomerCreditsByCustomerId: { CreditNr: string, CapitalBalance: number }[]
    }

    export interface ApplicationInfoWithCustomModel extends ApplicationInfoWithApplicantsModel {
        WorkflowStepOrder: string[]
    }

    export interface AffiliateReportingEventsResponse {
        Events: AffiliateReportingEventModel[]
        AffiliateMetadata: AffiliateReportingMetadataModel
    }

    export interface AffiliateReportingMetadataModel {
        HasDispatcher: boolean
    }

    export interface AffiliateReportingEventModel {
        Id: number
        ApplicationNr: string
        CreationDate: Date
        EventData: string
        EventType: string
        ProcessedDate: Date
        ProcessedStatus: string
        Items: AffiliateReportingEventItemModel[]
    }

    export interface AffiliateReportingEventItemModel {
        LogDate: Date
        ExceptionText: string
        MessageText: string
        ProcessedStatus: string
        OutgoingRequestBody: string
        OutgoingResponseBody: string
    }

    export interface AcceptUnsecuredLoanAdditionalLoanApplicationResponse {
        isAborted: boolean
        failedMessage: string
    }

    export interface UnsecuredLoanCreditCheckStatusResult {
        ApplicationNr: string
        NewCreditCheckUrl: string
        ViewCreditDecisionUrl: string
        CurrentCreditDecision: UnsecuredLoanCreditDecisionModel
        RejectionReasonDisplayNames: NameValuePair<string>[]
    }

    export interface UnsecuredLoanCreditDecisionModel {
        Id: number
        ApplicationNr: string
        DecisionDate: Date
        WasAutomated: boolean
        DecisionById: number
        RejectedDecisionModel: string
        AcceptedDecisionModel: string
        PauseItems: CreditDecisionPauseItemModel[]
    }

    export interface CreditDecisionPauseItemModel {
        Id: number
        ApplicationNr: string
        RejectionReasonName: string
        CustomerId: number
        PausedUntilDate: Date
        CreditDecisionId: number
    }

    export class UnsecuredLoanAdditionalQuestionsStatusResult {
        ApplicationNr: string
        AgreementSigningStatus: UnsecuredLoanAgreementSigningStatusModel
        AdditionalQuestionsStatus: UnsecuredLoanAdditionalQuestionsStatusModel
    }

    export class UnsecuredLoanAgreementSigningStatusModel {
        applicant1: UnsecuredLoanAgreementSigningStatusModelApplicant
        applicant2: UnsecuredLoanAgreementSigningStatusModelApplicant
        isSendAllowed: boolean
    }

    export class UnsecuredLoanAgreementSigningStatusModelApplicant {
        status: string
        signedDocumentUrl: string
        signedDate: Date
        failureMessage: string
        sentDate: Date
    }

    export class UnsecuredLoanAdditionalQuestionsStatusModel {
        sentDate: Date
        hasAnswered: boolean
        latestAnswerDate: Date
        canSkipAdditionalQuestions: boolean
    }

    export class KycScreenCustomerResult {
        Success: boolean
        Skipped: boolean
        FailureCode: string
    }

    export class FetchCustomerKycScreenStatus {
        CustomerId: number
        LatestScreeningDate: NTechDates.DateOnly
    }

    export class RequiredFiledsLeftToLiveOnResult {
        AlgorithmName: string
        RequiredApplicationFields: string[]
        RequiredApplicantFields: string[]
    }

    export class CalculateLeftToLiveOnResult {
        AlgorithmName: string
        LtlValue: number
        LtlParts: NameValuePair<number>[]
    }

    export class MortgageLoanAdditionalQuestionsStatusUpdateResult {
        AdditionalQuestionsStatusAfter: string
        WasStatusChanged: boolean
    }

    export class MortageLoanCurrentLoansModel {
        RequestedAmortizationAmount?: number
        Loans: MortageLoanCurrentLoansLoanModel[]
    }

    export class MortageLoanCurrentLoansLoanModel {
        BankName: string
        MonthlyAmortizationAmount?: number
        CurrentBalance?: number
        LoanNr: string
    }

    export class MortgageApplicationWorkListPageResult {
        Filter: MortgageApplicationWorkListFilter
        Applications: MortgageApplicationWorkListApplication[]
        CurrentBlockCodeCounts?: MortgageApplicationWorkListCodeCount[]
        CurrentPageNr: number
        TotalNrOfPages: number
    }

    export class MortgageApplicationWorkListApplication {
        ApplicationNr: string
        ProviderName: string
        ApplicationDate: Date
        LatestSystemCommentText: string
        CurrentBlockCode: string
        IsActive: boolean
    }

    export class MortgageApplicationWorkListFilter {
        CurrentBlockCode: string
        PageNr: number
        PageSize: number
        IncludeCurrentBlockCodeCounts?: boolean
        SeparatedWorkListName?: string
    }

    export class MortgageApplicationWorkListCodeCount {
        Code: string
        Count: number
    }

    export class AddApplicationCommentsOptional {
        eventType?: string
        attachedFileAsDataUrl?: string
        attachedFileName?: string
    }

    export class FetchApplicationCommentsOptional {
        showOnlyTheseEventTypes?: string[]
        hideTheseEventTypes?: string[]
    }

    export class UserIdAndDisplayName {
        UserId: number
        DisplayName: string
    }

    export class FetchHouseholdIncomeModelResult {
        model: HouseholdIncomeModel
        usernames: UserIdAndDisplayName[]
    }

    export class HouseholdIncomeModel {
        ApplicantIncomes: HouseholdIncomeApplicantModel[]
    }

    export class HouseholdIncomeApplicantModel {
        ApplicantNr: number
        EmploymentGrossMonthlyIncome: number
        CapitalGrossMonthlyIncome: number
        ServiceGrossMonthlyIncome: number
    }

    export class MortgageLoanApplicationBasisCurrentValuesModel {
        CombinedGrossMonthlyIncome: number
    }

    export class MortgageLoanBankFormModel {
        AmortizationBasisDate: NTechDates.DateOnly
        AmortizationBasisObjectValue: number
        AmortizationBasisLoanAmount: number
        RuleNoneCurrentAmount: number
        RuleR201616CurrentAmount: number
        RuleR201723CurrentAmount: number
        RuleAlternateCurrentAmount: number
        AlternateRuleAmortizationAmount: number
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

    export class FetchLeftToLiveOnRequiredItemNamesResult {
        RequiredApplicantItems: string[]
        RequiredApplicationItems: string[]
    }

    export class ComputeLeftToLiveOnResult {
        LeftToLiveOnAmount: number
        LoanFraction: number
        DebtMultiplier: number
        LeftToLiveOnParts: NameValuePair<number>[]
    }

    export class NameValuePair<T> {
        Name: string
        Value: T
    }

    export class ScoringDataModel {
        ApplicationItems: { [key: string]: string }
        ApplicantItems: { [applicantNr: number]: { [key: string]: string } }

        public static toDataTable(s: ScoringDataModel) {
            if (!s) {
                return null
            }
            let result: string[][] = []
            for (let name of Object.keys(s.ApplicationItems)) {
                result.push([name, 'Application', s.ApplicationItems[name]])
            }
            for (var applicantNr in s.ApplicantItems) {
                for (let name of Object.keys(s.ApplicantItems[applicantNr])) {
                    result.push([name, 'Applicant ' + applicantNr, s.ApplicantItems[applicantNr][name]])
                }
            }
            return result
        }
    }

    export class ScoringDataModelFlatItem {
        Name: string
        Value: string
        ApplicantNr?: number
    }

    export class ScoringDataModelFlat {
        Items: ScoringDataModelFlatItem[]

        public static toDataTable(s: ScoringDataModelFlat) {
            if (!s) {
                return null
            }
            let result: string[][] = []
            for (let i of s.Items) {
                if (!i.ApplicantNr) {
                    result.push([i.Name, 'Application', i.Value])
                }
            }
            for (let i of s.Items) {
                if (i.ApplicantNr) {
                    result.push([i.Name, 'Applicant ' + i.ApplicantNr, i.Value])
                }
            }
            return result
        }
    }

    export class MortageLoanOffer {
        LoanAmount: number
        MonthlyAmortizationAmount: number
        NominalInterestRatePercent: number
        MonthlyFeeAmount: number
        InitialFeeAmount: number
        BindingUntilDate: string
    }
    export class MortageLoanScoringResult {
        AcceptedOffer: MortageLoanOffer
        IsAccepted: boolean
        RiskClass: string
        RejectionRuleNames: string[]
        ManualAttentionRules: string[]
        ScorePointsByRuleName: { [index: string]: number }
        ScoringBasis: ScoringDataModel
    }
    export class MortageLoanObjectModel {
        PropertyType: number
        PropertyEstimatedValue: number
        PropertyMunicipality: string
        CondominiumPropertyDetails: MortageLoanObjectCondominiumDetailsModel
    }

    export class MortageLoanObjectCondominiumDetailsModel {
        Address: string
        PostalCode: string
        City: string
        NumberOfRooms: number
        LivingArea: number
        Floor: number
        AssociationName: string
        AssociationNumber: string
        MonthlyCost: number
        ApartmentNumber: number
        Elevator: boolean
        PatioType: number
        NewConstruction: boolean
    }

    export class MortageLoanProviderCallbackResultModel {
        IsSuccess: boolean
        Message: string
    }

    export class MortgageLoanSettlementDataModel {
        LoanTypeCode: string
        FinalOffer: any
        AmortizationPlanDocument: any
        PendingSettlementPayment: MortgageLoansSettlementPendingModel
        AmortizationModel: MortgageLoanAmortizationBasisModel
        CurrentLoansModel: MortageLoanCurrentLoansModel
    }

    export class MortgageLoansSettlementPendingModel {
        InitiatedDate: Date
        SettlementDate?: Date
        LoanAmount: number
    }

    export class FetchExternalApplicationRequestJsonResponse {
        ExternalApplicationRequestJson: string
    }

    export class OtherApplicationsResponseModel {
        ApplicationNr: string
        Applicants: OtherApplicationsResponseApplicantModel[]
    }
    export class OtherApplicationsResponseApplicantModel {
        CustomerId: number
        ApplicantNr: number
        Applications: OtherApplicationsResponseApplicationModel[]
        Credits: OtherApplicationsResponseCreditModel[]
    }
    export class OtherApplicationsResponseApplicantsInfoModel {
        CustomerId: number
        ApplicationDate: string
        ApplicationNr: string
        IsActive: boolean
        Status: ApplicationStatusItem
    }
    export class OtherApplicationsResponseApplicationModel {
        ApplicationNr: string
        ApplicationDate: Date
        ApplicationUrl: string
        IsActive: boolean
    }
    export class OtherApplicationsResponseCreditModel {
        CreditNr: string
        CreditStatus: string
        ApplicationNr: string
        Balance: number
        CustomerIds: number[]
        CreditUrl: string
    }

    export class AcceptNewLoanRequest {
        applicationNr: string
        amount: any
        repaymentTimeInMonths: any
        marginInterestRatePercent: any
        initialFeeAmount: string
        notificationFeeAmount: any
        referenceInterestRatePercent: number
        recommendationKey: string
    }

    export class RejectUnsecuredLoanApplicationRequest {
        applicationNr: string
        rejectionReasons: string[]
        recommendationKey: string
    }

    export interface IPartialAcceptNewLoanResponse {
        isAborted: boolean
        kycScreenFailed: boolean
        failedMessage: string
    }

    export class ValidateBankAccountNrResult {
        rawNr: string
        isValid: boolean
        validAccount: ValidateBankAccountNrResultAccount
    }

    export class ValidateBankAccountNrResultAccount {
        type: string //bankaccount or iban
        clearingNr: string //only bankaccount
        accountNr: string //only bankaccount
        bic: string //only iban
        bankName: string
        normalizedNr: string
        displayNr: string
        bankAccountNrType: string
    }

    export class MortgageLoanApplicationDirectDebitStatusModel {
        IsEditAllowed: boolean
        AdditionalQuestionsBankAccountNr: string
        AdditionalQuestionsBankName: string
        AdditionalQuestionAccountOwnerApplicantNr: number
        Applicant1: MortgageLoanApplicationDirectDebitStatusModel_Applicant
        Applicant2: MortgageLoanApplicationDirectDebitStatusModel_Applicant
        SignedDirectDebitConsentDocumentDownloadUrl: string
        DirectDebitCheckStatus: string
        DirectDebitCheckStatusDate: Date
        DirectDebitCheckBankAccountNr: string
        DirectDebitCheckBankName: string
        DirectDebitCheckAccountOwnerApplicantNr: number
    }

    export class MortgageLoanApplicationDirectDebitStatusModel_Applicant {
        FirstName: string
        BirthDate: Date
        StandardPaymentNumber: string
    }

    export class MortgageApplicationValutionResult {
        ucbvObjektId: string
        brfLghSkvLghNr: string
        brfLghYta: string
        brfNamn: string
        brfLghVaning: string
        brfLghAntalRum: string
        brfLghVarde: string
        brfSignalAr: string
        brfSignalBelaning: string
        brfSignalLikviditet: string
        brfSignalSjalvforsorjningsgrad: string
        brfSignalRantekanslighet: string
    }

    export class MortgageApplicationTryValutionResult {
        IsSuccess: boolean
        SuccessData: MortgageApplicationValutionResult
        FailedMessage: string
    }

    export interface IUcbvVarderaBostadsrattRequest {
        objektID: string
        yta?: string
        skvlghnr?: string
        avgift?: string
        vaning?: string
        rum?: string
        kontraktsdatum?: string
        kopesumma?: string
        brflghnr?: string
    }

    export class UcbvVarderaBostadsrattResponse {
        Varde: number
        RawJson: string
        Brfsignal: UcbvVarderaBostadsrattResponseBrfSignal
    }

    export class UcbvVarderaBostadsrattResponseBrfSignal {
        Ar: number
        Belaning: number
        Likviditet: number
        Sjalvforsorjningsgrad: number
        Rantekanslighet: number
    }

    export class UcbvObjectInfo {
        Kommun: string
        Adress: string[]
        Fastighet: string
        Forening: string
        Objekttyp: string
        Kommentar: string
        Lagenheter: UcbvObjectInfoLgh[]
    }
    export class UcbvObjectInfoLgh {
        Lghnr: string
        Boarea: number
        Vaning: number
        Rum: string
    }

    export class UcbvSokAdressHit {
        Id: string
        Name: string
    }

    export class MortgageLoanApplicationSearchHit {
        ApplicationNr: string
        ApplicationDate: Date
        IsActive: boolean
        IsPartiallyApproved: boolean
        IsFinalDecisionMade: boolean
        LatestSystemCommentText: string
        LatestSystemCommentDate: Date
        CurrentLoanAmount: number
        ProviderName: string
    }

    export class MortgageLoanApplicationCustomerCheckScreenResultItem {
        ApplicantNr: number
        IsSuccess: boolean
        FailureCode: string
    }

    export class MortgageLoanAcceptedFinalOffer {
        LoanAmount: number
        MonthlyAmortizationAmount: number
        NominalInterestRatePercent: number
        MonthlyFeeAmount: number
        InitialFeeAmount: number
        BindingUntilDate: string
    }

    export class MortgageLoanAdditionalQuestionsStatusModel {
        ApplicationNr: string
        AdditionalQuestionsStatus: string
        AdditionalQuestionsLink: string
        AdditionalQuestionsToken: string
        SentDate: Date
        AnsweredDate: Date
        AnswersDocumentKey: string
    }

    export class MortgageLoanAdditionalQuestionsDocument {
        Items: MortgageLoanAdditionalQuestionsDocumentItem[]
    }

    export class MortgageLoanAdditionalQuestionsDocumentItem {
        ApplicantNr: number
        CustomerId: number
        IsCustomerQuestion: boolean
        QuestionGroup: string
        QuestionCode: string
        AnswerCode: string
        QuestionText: string
        AnswerText: string
    }

    export class ApplicationDocumentCheckStatusUpdateResult {
        DocumentCheckStatusAfter: string
        WasStatusChanged: boolean
    }

    export class ApplicationDocument {
        DocumentId: number
        ApplicantNr: number
        CustomerId: number
        DocumentType: string
        DocumentSubType: string
        Filename: string
        DocumentArchiveKey: string
        DocumentDate: Date

        static GetDownloadUrlByKey(documentArchiveKey: string) {
            return '/CreditManagement/ArchiveDocument?key=' + documentArchiveKey
        }

        static GetDownloadUrl(d: ApplicationDocument) {
            if (!d) {
                return null
            } else {
                return ApplicationDocument.GetDownloadUrlByKey(d.DocumentArchiveKey)
            }
        }
    }

    export class ProviderInfoModel {
        ProviderName: string
        IsSelf: boolean
        IsSendingRejectionEmails: boolean
        IsUsingDirectLinkFlow: boolean
        IsSendingAdditionalQuestionsEmail: boolean
        IsMortgageLoanProvider: boolean
    }

    export class ApplicationCheckPointModel {
        applicantNr: number
        customerId: number
        checkpointId: number
        checkpointUrl: string
        isReasonTextLoaded: boolean
        isExpanded: boolean
        reasonText: string
    }

    export class MortgageLoanApplicationCustomerCheckStatusModel {
        Status: {
            IsApproveAllowed: boolean
            Issues: MortgageLoanApplicationCustomerCheckStatusModelIssue[]
        }
        CustomerLocalDecisionUrlPattern: string //{{customerId}} needs to be replaced to make it concrete
    }

    export class MortgageLoanApplicationCustomerCheckStatusModelIssue {
        Code: string
        ApplicantNr: number
        CustomerId: number
    }

    export class MortgageLoanApplicationInitialCreditCheckStatusModel {
        CreditCheckStatus: string;
        CustomerOfferStatus: string;
        IsViewDecisionPossible: boolean;
        ViewCreditDecisionUrl: string;
        RejectedDecision: MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel
        AcceptedDecision: MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel
    }

    export class MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel {
        ScoringPass: string
        RejectionReasons: MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel[]
    }

    export class MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel {
        ScoringPass: string
        Offer: MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel
    }

    export class MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel {
        LoanAmount: number
        MonthlyAmortizationAmount: number
        NominalInterestRatePercent: number
        InitialFeeAmount: number
        MonthlyFeeAmount: number
    }

    export class MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel {
        DisplayName: string
        Name: string
    }

    export class MortgageLoanApplicationFinalCreditCheckStatusModel {
        HasNonExpiredBindingOffer: boolean;
        CreditCheckStatus: string;
        NewCreditCheckUrl: string
        IsNewCreditCheckPossible: boolean
        IsViewDecisionPossible: boolean;
        ViewCreditDecisionUrl: string;
        UnsignedAgreementDocumentUrl: string
        RejectedDecision: MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel
        AcceptedDecision: MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel
    }

    export class MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel {
        ScoringPass: string
        RejectionReasons: MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel[]
    }

    export class MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel {
        DisplayName: string
        Name: string
    }

    export class MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel {
        ScoringPass: string
        Offer: MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel
    }

    export class MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel {
        LoanAmount: number
        MonthlyAmortizationAmount: number
        NominalInterestRatePercent: number
        InitialFeeAmount: number
        MonthlyFeeAmount: number
        BindingUntilDate: string
    }

    export class SetIsWaitingForAdditionalInformationResult {
        IsWaitingForAdditionalInformation: boolean
        AddedCommentId: number
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
        BankAccountRawJsonDataArchiveKey: string
        BankAccountPdfSummaryArchiveKey: string
    }

    export class FraudControlModel {
        fraudCheckStatus: string
        agreementStatus: string
        isApplicationWaitingForAdditionalInformation: boolean
        isApplicationPartiallyApproved: boolean
        isApplicationActive: boolean
        applicant1: FraudControlModelApplicant
        applicant2: FraudControlModelApplicant
    }

    export class FraudControlModelApplicant {
        status: string
        newUrl: string
        continueUrl: string
        viewUrl: string
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
        CustomerCheckStatus: string
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
        CompanyLoanAdditionalQuestionsStatus: string
        ListNames: string[]
        WorkflowVersion: string
        HasLockedAgreement: boolean
        IsLead: boolean
        CreditReportProviderName: string
        ListCreditReportProviders: string[]
    }

    export class CustomerComponentInitialData {
        public firstName: string
        public birthDate: string
        public customerId: number
        public isSanctionRejected: boolean
        public includeInFatcaExport: boolean
        public wasOnboardedExternally: boolean
        public customerCardUrl: string
        public legacyCustomerCardUrl: string
        public customerFatcaCrsUrl: string
        public isMissingAddress: boolean
        public isMissingEmail: boolean
        public isCompany: boolean
        public companyName: string
        public isArchived: boolean
        public pepKycCustomerUrl: string
        public localIsPep: boolean
        public localIsSanction: boolean
    }

    export class CustomerItem {
        public name: string
        public value: string
    }

    export class CheckIfOverHandlerLimitResult {
        isOverHandlerLimit: boolean
        isAllowedToOverrideHandlerLimit: boolean
    }
    export class GetBatchDetailsResult {
        batchItems: BatchDetail[]
    }

    export class BatchDetail {
        Id: number
    }

    export class FindHistoricalDecisionResult {
        batches: FindHistoricalDecisionBatchItem[]
    }

    export class FindHistoricalDecisionBatchItem {
        Id: number
        ApprovedDate: Date
        TotalCount: number
        TotalAmount: number
        OverridesCount: number
    }

    export interface IApproveApplicationResult {
        redirectToUrl: string,
        userWarningMessage: string,
        newComment: string
        reloadPage: boolean
    }

    export class DocumentCheckStatusResult {
        isApplicationActive: boolean;
        isApplicationPartiallyApproved: boolean;
        isAccepted: boolean;
        isRejected: boolean;
        allApplicantsHaveSignedAgreement: boolean;
        allApplicantsHaveAttachedDocuments: boolean;
        rejectionReasons: string[];
    }

    export class MortgageLoanApplicationValuationStatusModel {
        IsNewMortgageApplicationValuationAllowed: boolean
        NewMortgageApplicationValuationUrl: string
        ValuationItems: MortgageApplicationValutionResult
    }

    export interface IStringDictionary<T> {
        [key: string]: T
    }

    export interface INumberDictionary<T> {
        [key: number]: T
    }

    export function getNumberDictionarKeys<T>(i: INumberDictionary<T>): number[] {
        let r: number[] = []
        for (let k of Object.keys(i)) {
            r.push(parseInt(k))
        }
        return r
    }
    
    export class FindCreditReportsByCustomerId {
        public date: string
        public customerId: number
    }

    export interface ListCustomersWithKycStatusModel {
        FirstName: string
        BirthDate: string
        CustomerId: number
        MemberOfListNames: string[]
        IsPep: boolean
        IsSanction: boolean
        LatestScreeningDate: Date
    }
}