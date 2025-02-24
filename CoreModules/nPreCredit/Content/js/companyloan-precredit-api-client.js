var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var NTechCompanyLoanPreCreditApi;
(function (NTechCompanyLoanPreCreditApi) {
    var ApiClient = /** @class */ (function (_super) {
        __extends(ApiClient, _super);
        function ApiClient(onError, $http, $q) {
            return _super.call(this, onError, $http, $q) || this;
        }
        ApiClient.prototype.attachSignedAgreement = function (applicationNr, dataUrl, filename) {
            return this.post('/api/CompanyLoan/Attach-Signed-Agreement', { applicationNr: applicationNr, dataUrl: dataUrl, filename: filename });
        };
        ApiClient.prototype.removeSignedAgreement = function (applicationNr) {
            return this.post('/api/CompanyLoan/Remove-Signed-Agreement', { applicationNr: applicationNr });
        };
        ApiClient.prototype.searchForCompanyLoanApplicationByOmniValue = function (omniSearchValue, forceShowUserHiddenItems) {
            return this.post('/api/CompanyLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue, forceShowUserHiddenItems: forceShowUserHiddenItems });
        };
        ApiClient.prototype.fetchCompanyLoanWorkListDataPage = function (providerName, listName, forceShowUserHiddenItems, includeListCounts, pageSize, zeroBasedPageNr) {
            return this.post('/api/CompanyLoan/Search/WorkListDataPage', {
                providerName: providerName,
                listName: listName,
                forceShowUserHiddenItems: forceShowUserHiddenItems,
                includeListCounts: includeListCounts,
                pageSize: pageSize,
                zeroBasedPageNr: zeroBasedPageNr
            });
        };
        ApiClient.prototype.initialCreditCheck = function (applicationNr, storeTempCopyOnServer) {
            return this.post('/api/CompanyLoan/Create-InitialScore', {
                applicationNr: applicationNr,
                storeTempCopyOnServer: storeTempCopyOnServer
            });
        };
        ApiClient.prototype.commitInitialCreditCheckDecisionAccept = function (applicationNr, scoreResultStorageKey, acceptedOffer) {
            return this.commitInitialCreditCheckDecisionInternal(applicationNr, scoreResultStorageKey, true, null, acceptedOffer);
        };
        ApiClient.prototype.commitInitialCreditCheckDecisionReject = function (applicationNr, scoreResultStorageKey, rejectionReasons) {
            return this.commitInitialCreditCheckDecisionInternal(applicationNr, scoreResultStorageKey, false, rejectionReasons, null);
        };
        ApiClient.prototype.fetchCurrentCreditDecision = function (applicationNr) {
            return this.post('/api/CompanyLoan/Fetch-Current-CreditDecision', { applicationNr: applicationNr });
        };
        ApiClient.prototype.commitInitialCreditCheckDecisionInternal = function (applicationNr, scoreResultStorageKey, isAccepted, rejectionReasons, acceptedOffer) {
            return this.post('/api/CompanyLoan/Commit-InitialScore-Decision', {
                ApplicationNr: applicationNr,
                ScoreResultStorageKey: scoreResultStorageKey,
                IsAccepted: isAccepted,
                RejectionReasons: rejectionReasons,
                WasAutomated: false,
                AcceptedOffer: acceptedOffer
            });
        };
        ApiClient.prototype.fetchApplicationsPendingFinalDecision = function () {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Applications-Pending', {});
        };
        ApiClient.prototype.fetchFinalDecisionBatches = function (fromDate, toDate) {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Historical-Application-Batches', { fromDate: fromDate, toDate: toDate });
        };
        ApiClient.prototype.fetchFinalDecisionBatchItems = function (batchId) {
            return this.post('/api/CompanyLoan/FinalDecision/Fetch-Historical-Application-Batch-Items', { batchId: batchId });
        };
        ApiClient.prototype.createLoans = function (applicationNrs) {
            return this.post('/api/CompanyLoan/Create-Loans', { ApplicationNrs: applicationNrs });
        };
        ApiClient.prototype.fetchAdditionalQuestionsStatus = function (applicationNr) {
            return this.post('/api/CompanyLoan/Fetch-AdditionalQuestions-Status', { applicationNr: applicationNr });
        };
        ApiClient.prototype.approveApplication = function (applicationNr) {
            return this.post('/api/CompanyLoan/Approve-Application', { applicationNr: applicationNr });
        };
        ApiClient.prototype.setApplicationWorkflowStatus = function (applicationNr, stepName, statusName, commentText, eventCode, companionOperation) {
            return this.post('/api/CompanyLoan/Set-WorkflowStatus', { applicationNr: applicationNr, stepName: stepName, statusName: statusName, commentText: commentText, eventCode: eventCode, companionOperation: companionOperation });
        };
        ApiClient.prototype.fetchApplicationWorkflowStepNames = function (includeAffiliates) {
            return this.post('/api/CompanyLoan/Fetch-WorkflowStepNames', { IncludeAffiliates: includeAffiliates });
        };
        ApiClient.prototype.fetchAdditionalQuestionsAnswers = function (applicationNr) {
            return this.post('/api/CompanyLoan/Fetch-AdditionalQuestions-Answers', { ApplicationNr: applicationNr });
        };
        ApiClient.prototype.getOrCreateAgreementSignatureSession = function (applicationNr, options) {
            return this.post('/api/CompanyLoan/GetOrCreate-Agreement-Signature-Session', {
                ApplicationNr: applicationNr,
                RefreshSignatureSessionIfNeeded: options ? options.RefreshSignatureSessionIfNeeded : null,
                SupressSendingSignatureLinks: options ? options.SupressSendingSignatureLinks : null,
                ResendLinkOnExistingCustomerIds: options ? options.ResendLinkOnExistingCustomerIds : null
            });
        };
        ApiClient.prototype.cancelAgreementSignatureSession = function (applicationNr) {
            return this.post('/api/CompanyLoan/Cancel-Agreement-Signature-Session', { ApplicationNr: applicationNr });
        };
        ApiClient.prototype.createLockedAgreement = function (applicationNr) {
            return this.post('/api/CompanyLoan/Create-Locked-Agreement', {
                ApplicationNr: applicationNr
            });
        };
        ApiClient.prototype.checkHandlerLimits = function (handlerUserId, loanAmount) {
            return this.post('/api/CompanyLoan/CheckHandlerLimits', { HandlerUserId: handlerUserId, LoanAmount: loanAmount });
        };
        return ApiClient;
    }(NTechPreCreditApi.BaseApiClient));
    NTechCompanyLoanPreCreditApi.ApiClient = ApiClient;
    var GetOrCreateAgreementSignatureSessionOptions = /** @class */ (function () {
        function GetOrCreateAgreementSignatureSessionOptions() {
        }
        return GetOrCreateAgreementSignatureSessionOptions;
    }());
    NTechCompanyLoanPreCreditApi.GetOrCreateAgreementSignatureSessionOptions = GetOrCreateAgreementSignatureSessionOptions;
})(NTechCompanyLoanPreCreditApi || (NTechCompanyLoanPreCreditApi = {}));
