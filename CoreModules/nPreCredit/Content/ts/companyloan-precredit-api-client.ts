module NTechCompanyLoanPreCreditApi {
    export class ApiClient extends NTechPreCreditApi.BaseApiClient {
        constructor(onError: ((errorMessage: string) => void),
            $http: ng.IHttpService,
            $q: ng.IQService) {
            super(onError, $http, $q)
        }

        attachSignedAgreement(applicationNr: string, dataUrl: string, filename: string): ng.IPromise<{ WasAgreementAccepted: boolean}> {
            return this.post('/api/CompanyLoan/Attach-Signed-Agreement', { applicationNr, dataUrl, filename})
        }
        
        removeSignedAgreement(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/CompanyLoan/Remove-Signed-Agreement', { applicationNr })
        }

        searchForCompanyLoanApplicationByOmniValue(omniSearchValue: string, forceShowUserHiddenItems: boolean): ng.IPromise<CompanyLoanApplicationSearchHitResult> {
            return this.post('/api/CompanyLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue, forceShowUserHiddenItems: forceShowUserHiddenItems });
        }
        
        fetchCompanyLoanWorkListDataPage(providerName: string, listName: string, forceShowUserHiddenItems: boolean, includeListCounts: boolean, pageSize: number, zeroBasedPageNr: number): ng.IPromise<CompanyLoanWorkListDataPageResult> {
            return this.post('/api/CompanyLoan/Search/WorkListDataPage', {
                providerName,
                listName,
                forceShowUserHiddenItems,
                includeListCounts,
                pageSize,
                zeroBasedPageNr                
            })
        }

        initialCreditCheck(applicationNr: string, storeTempCopyOnServer?: boolean): ng.IPromise<InitialCreditCheckResponse> {
            return this.post('/api/CompanyLoan/Create-InitialScore', {
                applicationNr: applicationNr,
                storeTempCopyOnServer: storeTempCopyOnServer
            })
        }

        commitInitialCreditCheckDecisionAccept(applicationNr: string, scoreResultStorageKey: string,
            acceptedOffer?: CompanyLoanOfferModel) {
            return this.commitInitialCreditCheckDecisionInternal(applicationNr, scoreResultStorageKey, true, null, acceptedOffer)
        }

        commitInitialCreditCheckDecisionReject(applicationNr: string, scoreResultStorageKey: string,
            rejectionReasons?: string[]) {
            return this.commitInitialCreditCheckDecisionInternal(applicationNr, scoreResultStorageKey, false, rejectionReasons, null)
        }

        fetchCurrentCreditDecision(applicationNr: string): ng.IPromise<FetchCurrentCreditDecisionResult> {
            return this.post('/api/CompanyLoan/Fetch-Current-CreditDecision', { applicationNr: applicationNr })
        }

        private commitInitialCreditCheckDecisionInternal(
            applicationNr: string, scoreResultStorageKey: string,
            isAccepted: boolean,
            rejectionReasons: string[], acceptedOffer: CompanyLoanOfferModel): ng.IPromise<CommitInitialCreditCheckDecisionResult> {
            return this.post('/api/CompanyLoan/Commit-InitialScore-Decision', {
                ApplicationNr: applicationNr,
                ScoreResultStorageKey: scoreResultStorageKey,
                IsAccepted: isAccepted,
                RejectionReasons: rejectionReasons,
                WasAutomated: false,
                AcceptedOffer: acceptedOffer
            })
        }

        fetchApplicationsPendingFinalDecision(): ng.IPromise<FetchApplicationsPendingFinalDecisionResponse> {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Applications-Pending', {})
        }

        fetchFinalDecisionBatches(fromDate: string, toDate: string): ng.IPromise<FetchFinalDecisionBatchesResponse> {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Historical-Application-Batches', { fromDate, toDate })
        }

        fetchFinalDecisionBatchItems(batchId: number): ng.IPromise<FetchFinalDecsionBatchItemsResponse> {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Historical-Application-Batch-Items', { batchId })
        }


        createLoans(applicationNrs: string[]): ng.IPromise<CreateLoansResponse> {
            return this.post('/api/CompanyLoan/Create-Loans', { ApplicationNrs: applicationNrs })
        }

        fetchAdditionalQuestionsStatus(applicationNr: string): ng.IPromise<FetchCompanyLoanAdditionalQuestionsStatusResponse> {
            return this.post('/api/CompanyLoan/Fetch-AdditionalQuestions-Status', { applicationNr: applicationNr })
        }

        approveApplication(applicationNr: string): ng.IPromise<void> {
            return this.post('/api/CompanyLoan/Approve-Application', { applicationNr })
        }

        setApplicationWorkflowStatus(applicationNr: string, stepName: string, statusName: string, commentText?: string, eventCode?: string, companionOperation?: string): ng.IPromise<void> {
            return this.post('/api/CompanyLoan/Set-WorkflowStatus', { applicationNr, stepName, statusName, commentText, eventCode, companionOperation })
        }

        fetchApplicationWorkflowStepNames(includeAffiliates?: boolean): ng.IPromise<FetchApplicationWorkflowStepNamesResponse> {
            return this.post('/api/CompanyLoan/Fetch-WorkflowStepNames', { IncludeAffiliates: includeAffiliates  })
        }

        fetchAdditionalQuestionsAnswers(applicationNr: string): ng.IPromise<FetchAdditionalQuestionsAnswersResponse> {
            return this.post('/api/CompanyLoan/Fetch-AdditionalQuestions-Answers', { ApplicationNr: applicationNr })
        }

        getOrCreateAgreementSignatureSession(applicationNr: string, options?: GetOrCreateAgreementSignatureSessionOptions): ng.IPromise<GetOrCreateAgreementSignatureSessionResponse> {
            return this.post('/api/CompanyLoan/GetOrCreate-Agreement-Signature-Session', {
                ApplicationNr: applicationNr,
                RefreshSignatureSessionIfNeeded: options ? options.RefreshSignatureSessionIfNeeded : null,
                SupressSendingSignatureLinks: options ? options.SupressSendingSignatureLinks : null,
                ResendLinkOnExistingCustomerIds: options ? options.ResendLinkOnExistingCustomerIds : null
            })
        }

        cancelAgreementSignatureSession(applicationNr: string): ng.IPromise<{ WasCancelled: boolean }> {
            return this.post('/api/CompanyLoan/Cancel-Agreement-Signature-Session', { ApplicationNr: applicationNr })
        }

        createLockedAgreement(applicationNr: string): ng.IPromise<NTechPreCreditApi.LockedAgreementModel> {
            return this.post('/api/CompanyLoan/Create-Locked-Agreement', {
                ApplicationNr: applicationNr
            })
        }

        checkHandlerLimits(handlerUserId: number, loanAmount: number): ng.IPromise<{ Approved: boolean } > {
            return this.post('/api/CompanyLoan/CheckHandlerLimits', { HandlerUserId: handlerUserId, LoanAmount: loanAmount})
        }
    }



    export interface GetOrCreateAgreementSignatureSessionResponse {
        Session: AgreementSignatureSessionModel
    }

    export interface AgreementSignatureSessionModel {
        Static: AgreementSignatureSessionStaticModel
        State: AgreementSignatureSessionStateModel
    }

    export interface AgreementSignatureSessionStaticModel {
        Version: string
        SignatureProviderCode: string
        Signers: AgreementSignatureSessionSignerModel[]
        AlternateSignatureSessionId: string
    }

    export interface AgreementSignatureSessionSignerModel {
        CustomerId: number
        FirstName: string
        BirthDate: Date
        SignicatSessionApplicantNr: number
        ListMemberships: string[]
    }

    export interface AgreementSignatureSessionStateModel {
        SignatureSessionExpirationDateUtc: Date
        SignatureSessionId: string
        LatestSentDateByCustomerId: NTechPreCreditApi.INumberDictionary<Date>
        SignedDateByCustomerId: NTechPreCreditApi.INumberDictionary<Date>
    }

    export class GetOrCreateAgreementSignatureSessionOptions {
        RefreshSignatureSessionIfNeeded?: boolean
        SupressSendingSignatureLinks?: boolean
        ResendLinkOnExistingCustomerIds?: number[]
    }

    export interface FetchAdditionalQuestionsAnswersResponse {
        Document: AdditionalQuestionsAnswersModel
    }

    export interface AdditionalQuestionsAnswersModel {
        AnswerDate: Date | string;
        Items: AdditionalQuestionsAnswersItem[];
    }

    export interface AdditionalQuestionsAnswersItem {
        ApplicantNr: number | null;
        CustomerId: number | null;
        IsCustomerQuestion: boolean;
        QuestionGroup: string;
        QuestionCode: string;
        AnswerCode: string;
        QuestionText: string;
        AnswerText: string;
    }

    export interface FetchApplicationWorkflowStepNamesResponse {
        StepNames: string[]
        Affiliates: NTechPreCreditApi.AffiliateModel[]
    }

    export interface FetchCompanyLoanAdditionalQuestionsStatusResponse {
        ApplicationNr: string
        AnswerableSinceDate: Date
        AdditionalQuestionsLink: string
        AnsweredDate: Date
        AnswersDocumentKey: string
        IsPendingAnswers: boolean
        Offer: CompanyLoanOfferMinimalModel
    }

    export interface CreateLoansResponse {
        CreditNrs: string[]
    }

    export interface FetchApplicationsPendingFinalDecisionResponse {
        Applications: ApplicationPendingApproval[]
    }

    export interface ApplicationPendingApproval {
        ApplicationNr: string
        ApprovedByUserId: number
        OfferedAmount: number
    }

    export interface CompanyLoanWorkListDataPageResult extends NTechTables.IPagingResult {
        PageApplications: CompanyLoanApplicationSearchHit[]
        ListCountsByName: NTechPreCreditApi.IStringDictionary<number>
    }

    export interface CompanyLoanApplicationSearchHit {
        ApplicationNr: string
        ApplicationDate: Date
        IsActive: boolean
        IsPartiallyApproved: boolean
        IsFinalDecisionMade: boolean
        LatestSystemCommentText: string
        LatestSystemCommentDate: Date
        Amount: number
        ProviderName: string
    }


    export interface FetchCurrentCreditDecisionResult {
        DecisionId: number
        DecisionDate: Date
        DecisionByUserId: number
        Decision: FetchCurrentCreditDecisionResultDecision
    }

    export interface FetchCurrentCreditDecisionResultDecision {
        ScoringPass: string
        WasAccepted: boolean
        CompanyLoanOffer: CompanyLoanOfferModel
        RejectionReasons: string[]
        Recommendation: CompanyLoanScoringRecommendationModel
    }

    export interface CommitInitialCreditCheckDecisionResult {
        CreditDecisionId: number
    }

    export interface CompanyLoanScoringRecommendationModel {
        WasAccepted: boolean
        ScoringData: CompanyLoanScoringDataModel
        RejectionRuleNames: string[]
        ManualAttentionRuleNames: string[]
        ScorePointsByRuleName: NTechPreCreditApi.IStringDictionary<number>
        DebugDataByRuleNames: NTechPreCreditApi.IStringDictionary<string>
        RiskClass: string
        Offer: CompanyLoanOfferModel
    }

    export interface InitialCreditCheckResponse extends CompanyLoanScoringRecommendationModel {        
        TempCopyStorageKey: string
    }

    export interface CompanyLoanScoringDataModel {
        ApplicationItems: NTechPreCreditApi.IStringDictionary<string>
    }

    export interface CompanyLoanOfferMinimalModel {
        LoanAmount: number
        AnnuityAmount: number
        NominalInterestRatePercent: number
        MonthlyFeeAmount: number
        InitialFeeAmount: number
    }

    export interface CompanyLoanOfferModel extends CompanyLoanOfferMinimalModel {
        ReferenceInterestRatePercent: number
        RepaymentTimeInMonths: number
    }

    export interface CompanyLoanApplicationSearchHitResult {
        Applications: CompanyLoanApplicationSearchHit[]
    }

    export interface CompanyLoanApplicationSearchHit {
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

    export interface FetchFinalDecisionBatchesResponse {
        Batches: FetchFinalDecisionBatchesResponseItem[]
    }

    export interface FetchFinalDecisionBatchesResponseItem {
        Id: number
        ApprovedDate: Date
        TotalCount: number
        TotalAmount: number
    }

    export interface FetchFinalDecsionBatchItemsResponse {
        Items: FetchFinalDecsionBatchItemsResponseItem[]    
    }

    export interface FetchFinalDecsionBatchItemsResponseItem {
        Id: number
        HandlerUserId: number
        HandlerDisplayName: string
        Amount: number
        ApplicationNr: string
        CreditNr: string
        ApplicationUrl: string
        LoanUrl: string
        TypeName: string
    }
}