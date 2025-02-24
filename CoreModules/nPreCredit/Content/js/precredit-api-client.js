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
var NTechPreCreditApi;
(function (NTechPreCreditApi) {
    var BaseApiClient = /** @class */ (function () {
        function BaseApiClient(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.rejectWithFullError = false;
            this.activePostCount = 0;
            this.loggingContext = null;
        }
        BaseApiClient.prototype.valueToPromise = function (value) {
            var d = this.$q.defer();
            d.resolve(value);
            return d.promise;
        };
        BaseApiClient.prototype.post = function (url, data) {
            var _this = this;
            var startTimeMs = performance.now();
            this.activePostCount++;
            var d = this.$q.defer();
            this.$http.post(url, data).then(function (result) {
                d.resolve(result.data);
            }, function (err) {
                if (_this.onError) {
                    if (err && err.data) {
                        _this.onError(err.data.errorMessage || err.data.errorCode || err.statusText);
                    }
                    else if (err) {
                        _this.onError(err.statusText);
                    }
                    else {
                        _this.onError('unknown error');
                    }
                }
                if (_this.rejectWithFullError) {
                    d.reject(err);
                }
                else {
                    d.reject(err.statusText);
                }
            }).finally(function () {
                _this.activePostCount--;
                var totalTimeMs = performance.now() - startTimeMs;
                var c = _this.loggingContext == null ? '' : (_this.loggingContext + ': ');
            });
            return d.promise;
        };
        BaseApiClient.prototype.postUsingApiGateway = function (seviceName, serviceLocalUrl, data) {
            return this.post("/Api/Gateway/".concat(seviceName).concat(serviceLocalUrl[0] === '/' ? '' : '/').concat(serviceLocalUrl), data);
        };
        BaseApiClient.prototype.getUserModuleUrl = function (moduleName, serviceLocalUrl, parameters) {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters });
        };
        BaseApiClient.prototype.getArchiveDocumentUrl = function (archiveKey, opts) {
            if (!archiveKey) {
                return null;
            }
            var url = "/Api/ArchiveDocument/Download?archiveKey=".concat(archiveKey);
            if (opts) {
                if (opts.downloadFileName) {
                    url += '&downloadFileName=' + encodeURIComponent(opts.downloadFileName);
                }
                else if (opts.useOriginalFileName) {
                    url += '&useOriginalFileName=True';
                }
            }
            return url;
        };
        BaseApiClient.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        return BaseApiClient;
    }());
    NTechPreCreditApi.BaseApiClient = BaseApiClient;
    var ApiClient = /** @class */ (function (_super) {
        __extends(ApiClient, _super);
        function ApiClient(onError, $http, $q) {
            return _super.call(this, onError, $http, $q) || this;
        }
        ApiClient.prototype.fetchDocumentCheckStatus = function (applicationNr) {
            return this.post('/api/DocumentCheck/FetchStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.approveApplication = function (url) {
            return this.post(url, {});
        };
        ApiClient.prototype.rejectMortageLoanApplication = function (applicationNr) {
            return this.post('/api/mortageloan/reject-application', { applicationNr: applicationNr, wasAutomated: false });
        };
        ApiClient.prototype.findHistoricalDecisions = function (historyFromDate, historyToDate) {
            return this.post('/CreditDecision/FindHistoricalDecisions', {
                fromDate: historyFromDate,
                toDate: historyToDate
            });
        };
        ApiClient.prototype.getBatchDetails = function (batchId) {
            return this.post('/CreditDecision/GetBatchDetails', {
                batchId: batchId
            });
        };
        ApiClient.prototype.checkIfOverHandlerLimit = function (applicationNr, newLoanAmount, isCompanyLoan) {
            return this.post('/api/CreditHandlerLimit/CheckIfOver', {
                applicationNr: applicationNr,
                newLoanAmount: newLoanAmount,
                isCompanyLoan: isCompanyLoan
            });
        };
        ApiClient.prototype.fetchCustomerComponentInitialData = function (applicationNr, applicantNr, backTarget) {
            return this.post('/api/CustomerInfoComponent/FetchInitial', {
                applicationNr: applicationNr,
                applicantNr: applicantNr,
                backTarget: backTarget
            });
        };
        ApiClient.prototype.fetchCustomerComponentInitialDataByItemCompoundName = function (applicationNr, customerIdApplicationItemCompoundName, customerBirthDateApplicationItemCompoundName, backTarget) {
            return this.post('/api/CustomerInfoComponent/FetchInitialByItemName', {
                applicationNr: applicationNr,
                customerIdApplicationItemCompoundName: customerIdApplicationItemCompoundName,
                customerBirthDateApplicationItemCompoundName: customerBirthDateApplicationItemCompoundName,
                backTarget: backTarget
            });
        };
        ApiClient.prototype.fetchCustomerComponentInitialDataByCustomerId = function (customerId, backTarget) {
            return this.post('/api/CustomerInfoComponent/FetchInitialByCustomerId', {
                customerId: customerId,
                backTarget: backTarget
            });
        };
        ApiClient.prototype.fetchCustomerItems = function (customerId, itemNames) {
            return this.post('/api/CustomerInfo/FetchItems', {
                customerId: customerId,
                itemNames: itemNames
            });
        };
        ApiClient.prototype.fetchCustomerItemsDict = function (customerId, itemNames) {
            var p = this.$q.defer();
            this.fetchCustomerItems(customerId, itemNames).then(function (x) {
                var d = {};
                for (var _i = 0, x_1 = x; _i < x_1.length; _i++) {
                    var i = x_1[_i];
                    d[i.name] = i.value;
                }
                p.resolve(d);
            }, function (e) { return p.reject(e); });
            return p.promise;
        };
        ApiClient.prototype.fetchCustomerItemsBulk = function (customerIds, itemNames) {
            return this.post('/api/CustomerInfo/FetchItemsBulk', {
                customerIds: customerIds,
                itemNames: itemNames
            });
        };
        ApiClient.prototype.fetchApplicationInfo = function (applicationNr) {
            return this.post('/api/ApplicationInfo/Fetch', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchApplicationInfoBulk = function (applicationNrs) {
            return this.post('/api/ApplicationInfo/FetchBulk', { applicationNrs: applicationNrs });
        };
        ApiClient.prototype.fetchApplicationInfoWithApplicants = function (applicationNr) {
            return this.post('/api/ApplicationInfo/FetchWithApplicants', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchCreditHistoryByCustomerId = function (customerIds) {
            return this.postUsingApiGateway('nCredit', '/api/CustomerCreditHistoryBatch', { customerIds: customerIds });
        };
        ApiClient.prototype.fetchApplicationInfoWithCustom = function (applicationNr, includeAppliants, includeWorkflowStepOrder) {
            return this.post('/api/ApplicationInfo/FetchWithCustom', { applicationNr: applicationNr, includeAppliants: includeAppliants, includeWorkflowStepOrder: includeWorkflowStepOrder });
        };
        ApiClient.prototype.fetchFraudControlModel = function (applicationNr) {
            return this.post('/api/FraudControl/FetchModel', { applicationNr: applicationNr });
        };
        ApiClient.prototype.addApplicationComment = function (applicationNr, commentText, opt) {
            return this.post('/api/ApplicationComments/Add', {
                applicationNr: applicationNr, commentText: commentText,
                attachedFileAsDataUrl: opt ? opt.attachedFileAsDataUrl : null,
                attachedFileName: opt ? opt.attachedFileName : null,
                eventType: opt ? opt.eventType : null
            });
        };
        ApiClient.prototype.fetchApplicationComments = function (applicationNr, opt) {
            return this.post('/api/ApplicationComments/FetchForApplication', {
                applicationNr: applicationNr,
                hideTheseEventTypes: opt ? opt.hideTheseEventTypes : null,
                showOnlyTheseEventTypes: opt ? opt.showOnlyTheseEventTypes : null
            });
        };
        ApiClient.prototype.fetchApplicationComment = function (commentId) {
            return this.post('/api/ApplicationComments/FetchSingle', { commentId: commentId });
        };
        ApiClient.prototype.setApplicationWaitingForAdditionalInformation = function (applicationNr, isWaitingForAdditionalInformation) {
            return this.post('/api/ApplicationWaitingForAdditionalInformation/Set', { applicationNr: applicationNr, isWaitingForAdditionalInformation: isWaitingForAdditionalInformation });
        };
        ApiClient.prototype.fetchMortageLoanApplicationInitialCreditCheckStatus = function (applicationNr, backUrl) {
            return this.post('/api/MortgageLoan/CreditCheck/FetchInitialStatus', { applicationNr: applicationNr, backUrl: backUrl });
        };
        ApiClient.prototype.fetchMortageLoanApplicationFinalCreditCheckStatus = function (applicationNr, backUrl) {
            return this.post('/api/MortgageLoan/CreditCheck/FetchFinalStatus', { applicationNr: applicationNr, backUrl: backUrl });
        };
        ApiClient.prototype.fetchMortageLoanApplicationCustomerCheckStatus = function (applicationNr, urlToHereFromOtherModule, alsoUpdateStatus) {
            return this.post('/api/MortgageLoan/CustomerCheck/FetchStatus', { applicationNr: applicationNr, urlToHereFromOtherModule: urlToHereFromOtherModule, alsoUpdateStatus: alsoUpdateStatus });
        };
        ApiClient.prototype.doCustomerCheckKycScreen = function (applicationNr, applicantNrs) {
            return this.post('/api/MortgageLoan/CustomerCheck/DoKycScreen', { applicationNr: applicationNr, applicantNrs: applicantNrs });
        };
        ApiClient.prototype.approveMortageLoanApplicationCustomerCheck = function (applicationNr) {
            return this.post('/api/MortgageLoan/CustomerCheck/Approve', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchAllCheckpointsForApplication = function (applicationNr, applicationType) {
            return this.post('/api/ApplicationCheckpoint/FetchAllForApplication', { applicationNr: applicationNr, applicationType: applicationType });
        };
        ApiClient.prototype.fetchCheckpointReasonText = function (checkpointId) {
            return this.post('/api/ApplicationCheckpoint/FetchReasonText', { checkpointId: checkpointId });
        };
        ApiClient.prototype.cancelApplication = function (applicationNr) {
            return this.post('/api/ApplicationCancellation/Cancel', { applicationNr: applicationNr });
        };
        ApiClient.prototype.reactivateCancelledApplication = function (applicationNr) {
            return this.post('/api/ApplicationCancellation/Reactivate', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchProviderInfo = function (providerName) {
            return this.post('/api/ProviderInfo/FetchSingle', { providerName: providerName });
        };
        ApiClient.prototype.fetchApplicationDocuments = function (applicationNr, documentTypes) {
            return this.post('/api/ApplicationDocuments/FetchForApplication', { applicationNr: applicationNr, documentTypes: documentTypes });
        };
        ApiClient.prototype.fetchFreeformApplicationDocuments = function (applicationNr) {
            return this.post('/api/ApplicationDocuments/FetchFreeformForApplication', { applicationNr: applicationNr });
        };
        ApiClient.prototype.addAndRemoveApplicationDocument = function (applicationNr, documentType, applicantNr, dataUrl, filename, documentIdToRemove, customerId, documentSubType) {
            return this.post('/api/ApplicationDocuments/AddAndRemove', { applicationNr: applicationNr, documentType: documentType, applicantNr: applicantNr, dataUrl: dataUrl, filename: filename, documentIdToRemove: documentIdToRemove, customerId: customerId, documentSubType: documentSubType });
        };
        ApiClient.prototype.addApplicationDocument = function (applicationNr, documentType, applicantNr, dataUrl, filename, customerId, documentSubType) {
            return this.post('/api/ApplicationDocuments/Add', { applicationNr: applicationNr, documentType: documentType, applicantNr: applicantNr, dataUrl: dataUrl, filename: filename, customerId: customerId, documentSubType: documentSubType });
        };
        ApiClient.prototype.removeApplicationDocument = function (applicationNr, documentId) {
            return this.post('/api/ApplicationDocuments/Remove', { applicationNr: applicationNr, documentId: documentId });
        };
        ApiClient.prototype.updateMortgageLoanDocumentCheckStatus = function (applicationNr) {
            return this.post('/api/ApplicationDocuments/UpdateMortgageLoanDocumentCheckStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchMortageLoanAdditionalQuestionsStatus = function (applicationNr) {
            return this.post('/api/mortageloan/fetch-additional-questions-status2', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchMortgageLoanAdditionalQuestionsDocument = function (key) {
            return this.post('/api/mortageloan/fetch-additional-questions-document', { key: key });
        };
        ApiClient.prototype.fetchMortgageLoanCurrentLoans = function (applicationNr) {
            return this.post('/api/mortageloan/fetch-current-loans', { applicationNr: applicationNr });
        };
        ApiClient.prototype.completeMortgageLoanFinalCreditCheck = function (applicationNr, acceptedFinalOffer, scoringSessionKey, wasHandlerLimitOverridden) {
            return this.post('/api/MortgageLoan/CreditCheck/CompleteFinal', { applicationNr: applicationNr, acceptedFinalOffer: acceptedFinalOffer, scoringSessionKey: scoringSessionKey, wasHandlerLimitOverridden: wasHandlerLimitOverridden });
        };
        ApiClient.prototype.rejectMortgageLoanFinalCreditCheck = function (applicationNr, rejectionReasons, scoringSessionKey) {
            return this.post('/api/MortgageLoan/CreditCheck/RejectFinal', { applicationNr: applicationNr, rejectionReasons: rejectionReasons, scoringSessionKey: scoringSessionKey });
        };
        ApiClient.prototype.searchForMortgageLoanApplicationByOmniValue = function (omniSearchValue) {
            var r = this.post('/api/MortgageLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue });
            return r.then(function (x) { return x.Applications; });
        };
        ApiClient.prototype.searchForMortgageLoanApplicationOrLeadsByOmniValue = function (omniSearchValue) {
            return this.post('/api/MortgageLoan/Search/ByOmniValue', { omniSearchValue: omniSearchValue, includeLeads: true });
        };
        ApiClient.prototype.createMortgageLoan = function (applicationNr) {
            return this.post('/api/mortageloan/create-loan', {
                applicationNr: applicationNr
            });
        };
        ApiClient.prototype.setMortgageApplicationWorkflowStatus = function (applicationNr, stepName, statusName, commentText, eventCode, companionOperation) {
            return this.post('/api/MortgageLoan/Set-WorkflowStatus', { applicationNr: applicationNr, stepName: stepName, statusName: statusName, commentText: commentText, eventCode: eventCode, companionOperation: companionOperation });
        };
        ApiClient.prototype.scheduleMortgageLoanOutgoingSettlementPayment = function (applicationNr, interestDifferenceAmount, actualLoanAmount) {
            return this.post('/api/mortageloan/schedule-outgoing-settlement-payment', {
                applicationNr: applicationNr,
                interestDifferenceAmount: interestDifferenceAmount,
                actualLoanAmount: actualLoanAmount
            });
        };
        ApiClient.prototype.cancelScheduledMortgageLoanOutgoingSettlementPayment = function (applicationNr) {
            return this.post('/api/mortageloan/cancel-outgoing-settlement-payment', {
                applicationNr: applicationNr
            });
        };
        ApiClient.prototype.fetchMortgageApplicationValuationStatus = function (applicationNr, backUrl, autoAcceptSuggestion) {
            return this.post('/Api/MortgageLoan/Valuation/FetchStatus', { applicationNr: applicationNr, backUrl: backUrl, autoAcceptSuggestion: autoAcceptSuggestion });
        };
        ApiClient.prototype.ucbvSokAddress = function (adress, postnr, postort, kommun) {
            return this.post('/Api/MortgageLoan/Valuation/UcbvSokAddress', { adress: adress, postnr: postnr, postort: postort, kommun: kommun });
        };
        ApiClient.prototype.ucbvHamtaObjekt = function (id) {
            return this.post('/Api/MortgageLoan/Valuation/UcbvHamtaObjekt', { id: id });
        };
        ApiClient.prototype.ucbvVarderaBostadsratt = function (request) {
            return this.post('/Api/MortgageLoan/Valuation/UcbvVarderaBostadsratt', request);
        };
        ApiClient.prototype.automateMortgageApplicationValution = function (applicationNr) {
            return this.post('/Api/MortgageLoan/Valuation/AutomateValution', { applicationNr: applicationNr });
        };
        ApiClient.prototype.tryAutomateMortgageApplicationValution = function (applicationNr) {
            return this.post('/Api/MortgageLoan/Valuation/TryAutomateValution', { applicationNr: applicationNr });
        };
        ApiClient.prototype.acceptMortgageLoanUcbvValuation = function (applicationNr, valuationItems) {
            return this.post('/Api/MortgageLoan/Valuation/AcceptUcbvValuation', { applicationNr: applicationNr, valuationItems: valuationItems });
        };
        ApiClient.prototype.updateMortgageLoanDirectDebitCheckStatus = function (applicationNr, newStatus, bankAccountNr, bankAccountOwnerApplicantNr) {
            return this.post('/api/MortgageLoan/DirectDebitCheck/UpdateStatus', { applicationNr: applicationNr, newStatus: newStatus, bankAccountNr: bankAccountNr, bankAccountOwnerApplicantNr: bankAccountOwnerApplicantNr });
        };
        ApiClient.prototype.fetchMortgageLoanDirectDebitCheckStatus = function (applicationNr) {
            return this.post('/api/MortgageLoan/DirectDebitCheck/FetchStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.validateBankAccountNr = function (bankAccountNr, bankAccountNrType) {
            return this.post('/api/bankaccount/validate-nr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType });
        };
        ApiClient.prototype.acceptNewLoan = function (request) {
            return this.post('/CreditCheck/AcceptNewLoan', request);
        };
        ApiClient.prototype.rejectUnsecuredLoanApplication = function (request) {
            return this.post('/CreditCheck/Reject', request);
        };
        ApiClient.prototype.acceptUnsecuredLoanAdditionalLoanApplication = function (request) {
            return this.post('/CreditCheck/AcceptAdditionalLoan', request);
        };
        ApiClient.prototype.fetchOtherApplications = function (applicationNr, backUrl, includeApplicationObjects) {
            if (includeApplicationObjects === void 0) { includeApplicationObjects = false; }
            return this.post('/api/OtherApplications/Fetch', { applicationNr: applicationNr, backUrl: backUrl, includeApplicationObjects: includeApplicationObjects });
        };
        ApiClient.prototype.fetchotherApplicationsByCustomerId = function (customerIds, applicationNr, includeApplicationObjects) {
            if (includeApplicationObjects === void 0) { includeApplicationObjects = false; }
            return this.post('/api/OtherApplications/FetchByCustomerIds', { customerIds: customerIds, applicationNr: applicationNr, includeApplicationObjects: includeApplicationObjects });
        };
        ApiClient.prototype.fetchExternalApplicationRequestJson = function (applicationNr) {
            return this.post('/api/ApplicationInfo/FetchExternalRequestJson', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchMortgageLoanObjectInfo = function (applicationNr) {
            return this.post('/api/MortgageLoan/Object/FetchInfo', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchMortgageLoanSettlementData = function (applicationInfo) {
            return this.post('/api/mortageloan/fetch-settlement-data', { applicationNr: applicationInfo.ApplicationNr });
        };
        ApiClient.prototype.sendMortgageLoanProviderCallback = function (applicationNr, eventName) {
            return this.post('/api/mortageloan/send-providercallback', { applicationNr: applicationNr, eventName: eventName });
        };
        ApiClient.prototype.fetchLeftToLiveOnRequiredItemNames = function () {
            return this.post('/api/MortgageLoan/CreditCheck/FetchLeftToLiveOnRequiredItemNames', {});
        };
        ApiClient.prototype.computeLeftToLiveOn = function (scoringDataModel, interestRatePercent) {
            return this.post('/api/MortgageLoan/CreditCheck/ComputeLeftToLiveOn', { jsonData: JSON.stringify({ scoringDataModel: scoringDataModel, interestRatePercent: interestRatePercent }) });
        };
        ApiClient.prototype.fetchMortgageLoanAmortizationBasis = function (applicationNr) {
            return this.post('/api/MortgageLoan/Amortization/FetchBasis', { applicationNr: applicationNr });
        };
        ApiClient.prototype.setMortgageLoanAmortizationBasis = function (applicationNr, basis) {
            return this.post('/api/MortgageLoan/Amortization/SetBasis', { applicationNr: applicationNr, basis: basis });
        };
        ApiClient.prototype.calculateMortgageLoanAmortizationSuggestionBasedOnStandardBankForm = function (applicationNr, bankForm) {
            return this.post('/api/MortgageLoan/Amortization/CalculateSuggestionBasedOnStandardBankForm', { applicationNr: applicationNr, bankForm: bankForm });
        };
        ApiClient.prototype.fetchMortgageLoanApplicationBasisCurrentValues = function (applicationNr) {
            return this.post('/api/MortgageLoan/ApplicationBasis/FetchCurrentValues', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchHouseholdIncomeModel = function (applicationNr, includeUsernames) {
            return this.post('/api/MortgageLoan/ApplicationBasis/FetchHouseholdIncomeModel', { applicationNr: applicationNr, includeUsernames: includeUsernames });
        };
        ApiClient.prototype.setHouseholdIncomeModel = function (applicationNr, householdIncomeModel) {
            return this.post('/api/MortgageLoan/ApplicationBasis/SetHouseholdIncomeModel', { applicationNr: applicationNr, householdIncomeModel: householdIncomeModel });
        };
        ApiClient.prototype.fetchMortgageApplicationWorkListPage = function (currentBlockCode, pageNr, pageSize, includeCurrentBlockCodeCounts, separatedWorkList, handlerFilter) {
            return this.post('/api/MortgageLoan/WorkList/FetchPage', {
                currentBlockCode: currentBlockCode,
                pageNr: pageNr,
                pageSize: pageSize,
                includeCurrentBlockCodeCounts: includeCurrentBlockCodeCounts,
                separatedWorkList: separatedWorkList,
                onlyNoHandlerAssignedApplications: handlerFilter ? handlerFilter.onlyUnassigned : null,
                assignedToHandlerUserId: handlerFilter ? handlerFilter.assignedToHandlerUserId : null
            });
        };
        ApiClient.prototype.updateMortgageLoanAdditionalQuestionsStatus = function (applicationNr) {
            return this.post('/api/mortgageloan/update-additionalquestions-status/', {
                applicationNr: applicationNr
            });
        };
        ApiClient.prototype.calculateLeftToLiveOn = function (algorithmName, scoringData) {
            return this.post('/api/Scoring/LeftToLiveOn/Calculate', {
                AlgorithmName: algorithmName,
                ScoringData: scoringData
            });
        };
        ApiClient.prototype.fetchCustomerKycScreenStatus = function (customerId) {
            return this.post('/api/Kyc/FetchCustomerScreeningStatus', {
                CustomerId: customerId
            });
        };
        ApiClient.prototype.kycScreenCustomer = function (customerId, force) {
            return this.post('/api/Kyc/ScreenCustomer', {
                CustomerId: customerId,
                Force: force
            });
        };
        ApiClient.prototype.fetchUnsecuredLoanAdditionalQuestionsStatus = function (applicationNr) {
            return this.post('/api/AdditionalQuestions/FetchApplicationStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchUnsecuredLoanCreditCheckStatus = function (applicationNr, urlToHere, backUrl, includePauseItems, includeRejectionReasonDisplayNames) {
            return this.post('/api/UnsecuredApplication/FetchCreditCheckStatus', {
                ApplicationNr: applicationNr, BackUrl: backUrl,
                UrlToHere: urlToHere, IncludePauseItems: includePauseItems,
                IncludeRejectionReasonDisplayNames: includeRejectionReasonDisplayNames
            });
        };
        ApiClient.prototype.fetchAllAffiliateReportingEventsForApplication = function (applicationNr, includeAffiliateMetadata) {
            return this.post('/api/AffiliateReporting/Events/FetchAllForApplication', {
                ApplicationNr: applicationNr,
                IncludeAffiliateMetadata: includeAffiliateMetadata
            });
        };
        ApiClient.prototype.resendAffiliateReportingEvent = function (eventId) {
            return this.post('/api/AffiliateReporting/Events/Resend', {
                Id: eventId
            });
        };
        ApiClient.prototype.fetchAllAffiliates = function () {
            return this.post('/api/Affiliates/FetchAll', {});
        };
        ApiClient.prototype.fetchCustomerIdByCivicRegNr = function (civicRegNr) {
            return this.post('/api/CustomerInfo/FetchCustomerIdByCivicRegNr', { civicRegNr: civicRegNr });
        };
        ApiClient.prototype.fetchCustomerIdByOrgnr = function (orgnr) {
            return this.post('/api/CustomerInfo/FetchCustomerIdByOrgnr', { orgnr: orgnr });
        };
        ApiClient.prototype.addCustomerToApplicationList = function (applicationNr, listName, customerId, civicRegNr, firstName, lastName, email, phone, addressStreet, addressZipcode, addressCity, addressCountry) {
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
            });
        };
        ApiClient.prototype.removeCustomerFromApplicationList = function (applicationNr, listName, customerId) {
            return this.post('/api/ApplicationCustomerList/Remove-Customer', {
                ApplicationNr: applicationNr,
                ListName: listName,
                CustomerId: customerId
            });
        };
        ApiClient.prototype.fetchCustomerApplicationListMembers = function (applicationNr, listName) {
            return this.post('/api/ApplicationCustomerList/Fetch-Members', {
                ApplicationNr: applicationNr,
                ListName: listName
            });
        };
        ApiClient.prototype.switchApplicationListStatus = function (applicationNr, listPrefixName, statusName, commentText, eventCode) {
            return this.post('/api/Application/Switch-ListStatus', { applicationNr: applicationNr, listPrefixName: listPrefixName, statusName: statusName, commentText: commentText, eventCode: eventCode });
        };
        ApiClient.prototype.kycScreenBatchByApplicationNr = function (applicationNr, screenDate) {
            return this.post('/api/CompanyLoan/KycScreenByApplicationNr', { ApplicationNr: applicationNr, ScreenDate: screenDate });
        };
        ApiClient.prototype.fetchListCustomersWithKycStatusMethod = function (applicationNr, listNames) {
            return this.post('/api/CompanyLoan/FetchListCustomersWithKycStatusMethod', { applicationNr: applicationNr, listNames: listNames });
        };
        ApiClient.prototype.fetchApplicationDataSourceItems = function (applicationNr, requests) {
            return this.post('/api/Application/FetchDataSourceItems', { applicationNr: applicationNr, requests: requests });
        };
        ApiClient.prototype.createManualSignatureDocuments = function (dataUrl, fileName, civicRegNr, commentText) {
            return this.post('/api/ManualSignatures/CreateDocuments', { dataUrl: dataUrl, fileName: fileName, civicRegNr: civicRegNr, commentText: commentText });
        };
        ApiClient.prototype.deleteManualSignatureDocuments = function (sessionId) {
            this.post('/api/ManualSignatures/DeleteDocuments', { sessionId: sessionId });
        };
        ApiClient.prototype.getManualSignatureDocuments = function (signedDocuments) {
            return this.post('/api/ManualSignatures/GetDocuments', { signedDocuments: signedDocuments });
        };
        ApiClient.prototype.handleManualSignatureDocuments = function (sessionId) {
            this.post('/api/ManualSignatures/HandleDocuments', { sessionId: sessionId });
        };
        /**
         * @param applicationNr
         * @param groupedNames like application.amount or applicant2.signedAgreementKey
         * @param missingReplacementValue something like 'missing' which will be the result for items that have no value
         */
        ApiClient.prototype.fetchCreditApplicationItemSimple = function (applicationNr, groupedNames, missingReplacementValue) {
            var d = this.$q.defer();
            var request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSource(groupedNames, false, true, missingReplacementValue);
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(function (x) {
                var dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items);
                d.resolve(dict);
            }, d.reject);
            return d.promise;
        };
        ApiClient.prototype.fetchComplexApplicationListItemSimple = function (applicationNr, groupedNames, missingReplacementValue) {
            var d = this.$q.defer();
            var request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSourceComplex(groupedNames, false, true, missingReplacementValue);
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(function (x) {
                var dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items);
                d.resolve(dict);
            }, d.reject);
            return d.promise;
        };
        ApiClient.prototype.fetchCreditApplicationItemComplex = function (applicationNr, groupedNames, missingReplacementValue) {
            var d = this.$q.defer();
            var request = FetchApplicationDataSourceRequestItem.createCreditApplicationItemSourceComplex(groupedNames, false, true, missingReplacementValue);
            this.fetchApplicationDataSourceItems(applicationNr, [request]).then(function (x) {
                var dict = FetchApplicationDataSourceRequestItem.resultAsDictionary(x.Results[0].Items);
                d.resolve(dict);
            }, d.reject);
            return d.promise;
        };
        ApiClient.prototype.fetchApplicationEditItemData = function (applicationNr, dataSourceName, itemName, defaultValueIfMissing, includeEdits) {
            return this.post('/api/Application/Edit/FetchItemData', { applicationNr: applicationNr, dataSourceName: dataSourceName, itemName: itemName, defaultValueIfMissing: defaultValueIfMissing, includeEdits: includeEdits });
        };
        ApiClient.prototype.setApplicationEditItemData = function (applicationNr, dataSourceName, itemName, newValue, isDelete) {
            return this.post('/api/Application/Edit/SetItemData', {
                applicationNr: applicationNr,
                dataSourceName: dataSourceName,
                itemName: itemName,
                newValue: newValue,
                isDelete: isDelete
            });
        };
        ApiClient.prototype.fetchConsentAnswers = function (applicationNr) {
            return this.post('/api/Application/FetchConsentAnswers', {
                ApplicationNr: applicationNr
            });
        };
        ApiClient.prototype.setApplicationEditItemDataBatched = function (applicationNr, edits) {
            return this.post('/api/Application/Edit/SetItemDataBatched', {
                applicationNr: applicationNr,
                edits: edits
            });
        };
        ApiClient.prototype.fetchCurrentReferenceInterestRate = function () {
            return this.postUsingApiGateway('nCredit', '/Api/ReferenceInterest/GetCurrent', {}).then(function (x) {
                return x.referenceInterestRatePercent;
            });
        };
        ApiClient.prototype.getLockedAgreement = function (applicationNr) {
            return this.post('/api/Agreement/Get-Locked', {
                ApplicationNr: applicationNr
            });
        };
        ApiClient.prototype.removeLockedAgreement = function (applicationNr) {
            return this.post('/api/Agreement/Remove-Locked', {
                ApplicationNr: applicationNr
            });
        };
        ApiClient.prototype.approveLockedAgreement = function (applicationNr, requestOverrideDuality) {
            return this.post('/api/Agreement/Approve-Locked', {
                ApplicationNr: applicationNr,
                RequestOverrideDuality: requestOverrideDuality
            });
        };
        ApiClient.prototype.isValidAccountNr = function (bankAccountNr, bankAccountNrType) {
            return this.postUsingApiGateway('nCredit', 'Api/UnplacedPayment/IsValidAccountNr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType });
        };
        ApiClient.prototype.createItemBasedCreditDecision = function (request) {
            return this.post('/api/CreditDecision/Create-ItemBased', request);
        };
        ApiClient.prototype.fetchItemBasedCreditDecision = function (request) {
            return this.post('/api/CreditDecision/Fetch-ItemBased', request);
        };
        ApiClient.prototype.createOrUpdatePersonCustomer = function (request) {
            return this.postUsingApiGateway('nCustomer', 'api/PersonCustomer/CreateOrUpdate', request);
        };
        ApiClient.prototype.createOrUpdatePersonCustomerSimple = function (civicRegNr, properties, expectedCustomerId, birthDate) {
            var r = {
                CivicRegNr: civicRegNr,
                BirthDate: birthDate,
                ExpectedCustomerId: expectedCustomerId,
                Properties: []
            };
            for (var _i = 0, _a = Object.keys(properties); _i < _a.length; _i++) {
                var name_1 = _a[_i];
                r.Properties.push({ Name: name_1, Value: properties[name_1], ForceUpdate: true });
            }
            return this.createOrUpdatePersonCustomer(r);
        };
        ApiClient.prototype.fetchCustomerOnboardingStatuses = function (customerIds) {
            return this.postUsingApiGateway('nCustomer', 'Api/KycManagement/FetchCustomerOnboardingStatuses', { customerIds: customerIds });
        };
        ApiClient.prototype.kycScreenBatch = function (customerIds, screenDate) {
            return this.postUsingApiGateway('nCustomer', 'Api/KycScreening/ListScreenBatch', { customerIds: customerIds, screenDate: screenDate });
        };
        ApiClient.prototype.auditAndCreateMortgageLoanLockedAgreement = function (applicationNr) {
            return this.post('/api/MortgageLoan/Audit-And-Create-Locked-Agreement', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchDualAgreementSignatureStatus = function (applicationNr) {
            return this.post('/api/MortgageLoan/Fetch-Dual-Agreement-SignatureStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.fetchDualApplicationSignatureStatus = function (applicationNr) {
            return this.post('/api/MortgageLoan/Fetch-Dual-Application-SignatureStatus', { applicationNr: applicationNr });
        };
        ApiClient.prototype.calculatePaymentPlan = function (request) {
            return this.post('/api/PaymentPlan/Calculate', request);
        };
        ApiClient.prototype.initializeDualMortgageLoanSettlementPayments = function (applicationNr) {
            return this.post('/api/MortgageLoan/Initialize-Dual-SettlementPayments', { applicationNr: applicationNr });
        };
        ApiClient.prototype.createDualMortgageLoanSettlementPaymentsFile = function (applicationNr) {
            return this.post('/api/MortgageLoan/Create-DualSettlementPaymentsFile', { applicationNr: applicationNr });
        };
        ApiClient.prototype.createDualMortgageLoan = function (applicationNr) {
            return this.post('/api/MortgageLoan/Create-Dual-Loan', { applicationNr: applicationNr });
        };
        ApiClient.prototype.submitAdditionalQuestions = function (applicationNr, document, consumerBankAccountNr) {
            return this.post('/api/MortgageLoan/Submit-AdditionalQuestions', { ApplicationNr: applicationNr, QuestionsDocument: document, ConsumerBankAccountNr: consumerBankAccountNr });
        };
        ApiClient.prototype.createMortgageLoanLeadsWorkList = function () {
            return this.post('/api/MortgageLoan/Create-Leads-WorkList', {});
        };
        ApiClient.prototype.buyCreditReportForCustomerId = function (customerId, creditReportProviderName) {
            var returningItemNames = ["addressCountry", "addressStreet", "addressZipcode", "addressCity"];
            return this.post('/api/BuyCreditReportForCustomer', { providerName: creditReportProviderName, customerId: customerId, returningItemNames: returningItemNames });
        };
        ApiClient.prototype.fetchMortgageLoanLeadsWorkListStatuses = function (overrideForUserId) {
            return this.post('/api/MortgageLoan/Fetch-Leads-WorkList-Statuses', { UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId: !overrideForUserId });
        };
        ApiClient.prototype.tryCloseMortgageLoanWorkList = function (workListId) {
            return this.post('/api/WorkLists/TryCloseWorkList', { WorkListId: workListId, UseCurrentUserId: true });
        };
        ApiClient.prototype.tryTakeMortgageLoanWorkListItem = function (workListId, overrideForUserId) {
            return this.post('/api/WorkLists/TryTakeWorkListItem', { WorkListId: workListId, UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId: !overrideForUserId });
        };
        ApiClient.prototype.fetchMortgageLoanWorkListItemStatus = function (workListId, itemId, overrideForUserId) {
            return this.post('/api/MortgageLoan/Fetch-Leads-WorkList-Item-Status', { WorkListId: workListId, ItemId: itemId, UserId: overrideForUserId ? overrideForUserId : null, UseCurrentUserId: !overrideForUserId });
        };
        ApiClient.prototype.tryCompleteOrReplaceMortgageLoanWorkListItem = function (workListId, itemId, isReplace) {
            return this.post('/api/WorkLists/TryCompleteOrReplaceWorkListItem', { WorkListId: workListId, ItemId: itemId, IsReplace: isReplace });
        };
        ApiClient.prototype.tryComplateMortgageLoanLead = function (applicationNr, completionCode, rejectionReasons, rejectionReasonOtherText, tryLaterDays) {
            return this.post('/api/MortgageLoan/Complete-Lead', {
                ApplicationNr: applicationNr,
                CompletionCode: completionCode,
                RejectionReasons: rejectionReasons,
                RejectionReasonOtherText: rejectionReasonOtherText,
                TryLaterDays: tryLaterDays
            });
        };
        ApiClient.prototype.fetchApplicationAssignedHandlers = function (opts) {
            return this.post('/api/ApplicationAssignedHandlers/Fetch', {
                ApplicationNr: opts ? opts.applicationNr : null,
                ReturnAssignedHandlers: opts ? opts.returnAssignedHandlers : null,
                ReturnPossibleHandlers: opts ? opts.returnPossibleHandlers : null
            });
        };
        ApiClient.prototype.setApplicationAssignedHandlers = function (applicationNr, assignHandlerUserIds, unAssignHandlerUserIds) {
            return this.post('/api/ApplicationAssignedHandlers/Set', {
                ApplicationNr: applicationNr,
                AssignHandlerUserIds: assignHandlerUserIds,
                UnAssignHandlerUserIds: unAssignHandlerUserIds
            });
        };
        ApiClient.prototype.createCampaignReturningId = function (name, id) {
            return this.post('/api/Campaigns/Create', { name: name, id: id });
        };
        ApiClient.prototype.fetchCampaigns = function (options) {
            return this.post('/api/Campaigns/Fetch', options || {});
        };
        ApiClient.prototype.fetchCampaign = function (campaignId) {
            return this.fetchCampaigns({ singleCampaignId: campaignId, includeDeleted: true, includeInactive: true, includeCodes: true }).then(function (x) {
                return x.Campaigns && x.Campaigns.length > 0 ? x.Campaigns[0] : null;
            });
        };
        ApiClient.prototype.deleteOrInactivateCampaign = function (campaignId, isDelete) {
            return this.post('/api/Campaigns/DeleteOrInactivate', { campaignId: campaignId, IsDelete: !!isDelete, IsInactivate: !isDelete });
        };
        ApiClient.prototype.deleteCampaignCode = function (campaignCodeId) {
            return this.post('/api/Campaigns/DeleteCampaignCode', { campaignCodeId: campaignCodeId });
        };
        ApiClient.prototype.createCampaignCode = function (campaignId, code, startDate, endDate, commentText, isGoogleCampaign) {
            return this.post('/api/Campaigns/CreateCampaignCode', { campaignId: campaignId, code: code, startDate: startDate, endDate: endDate, commentText: commentText, isGoogleCampaign: isGoogleCampaign });
        };
        ApiClient.prototype.archiveSingleApplication = function (applicationNr) {
            return this.post('/api/Application/ArchiveSingle', { applicationNr: applicationNr });
        };
        ApiClient.prototype.cancelUnsecuredLoanApplicationSignatureSession = function (applicationNr) {
            return this.post('/api/UnsecuredLoanApplication/Cancel-Signature-Session', { applicationNr: applicationNr });
        };
        return ApiClient;
    }(BaseApiClient));
    NTechPreCreditApi.ApiClient = ApiClient;
    var ApplicationEditItemDataType;
    (function (ApplicationEditItemDataType) {
        ApplicationEditItemDataType["positiveInt"] = "positiveInt";
        ApplicationEditItemDataType["positiveDecimal"] = "positiveDecimal";
        ApplicationEditItemDataType["dropdownRaw"] = "dropdownRaw ";
        ApplicationEditItemDataType["url"] = "url";
    })(ApplicationEditItemDataType = NTechPreCreditApi.ApplicationEditItemDataType || (NTechPreCreditApi.ApplicationEditItemDataType = {}));
    var ApplicationStatusItem;
    (function (ApplicationStatusItem) {
        ApplicationStatusItem["cancelled"] = "Cancelled";
        ApplicationStatusItem["rejected"] = "Rejected";
        ApplicationStatusItem["finalDecisionMade"] = "Paid Out";
    })(ApplicationStatusItem = NTechPreCreditApi.ApplicationStatusItem || (NTechPreCreditApi.ApplicationStatusItem = {}));
    var DocumentsResponse = /** @class */ (function () {
        function DocumentsResponse() {
        }
        return DocumentsResponse;
    }());
    NTechPreCreditApi.DocumentsResponse = DocumentsResponse;
    var ManualSignatureResponse = /** @class */ (function () {
        function ManualSignatureResponse() {
        }
        return ManualSignatureResponse;
    }());
    NTechPreCreditApi.ManualSignatureResponse = ManualSignatureResponse;
    var FetchApplicationDataSourceRequestItem = /** @class */ (function () {
        function FetchApplicationDataSourceRequestItem() {
        }
        FetchApplicationDataSourceRequestItem.createCreditApplicationItemSource = function (names, errorIfMissing, replaceIfMissing, missingReplacementValue, includeIsChanged, includeEditorModels) {
            return {
                DataSourceName: 'CreditApplicationItem',
                Names: names,
                ErrorIfMissing: errorIfMissing,
                ReplaceIfMissing: replaceIfMissing,
                MissingItemReplacementValue: missingReplacementValue,
                IncludeIsChanged: includeIsChanged,
                IncludeEditorModel: includeEditorModels
            };
        };
        FetchApplicationDataSourceRequestItem.createCreditApplicationItemSourceComplex = function (names, errorIfMissing, replaceIfMissing, missingReplacementValue, includeIsChanged, includeEditorModels) {
            return {
                DataSourceName: 'ComplexApplicationList',
                Names: names,
                ErrorIfMissing: errorIfMissing,
                ReplaceIfMissing: replaceIfMissing,
                MissingItemReplacementValue: missingReplacementValue,
                IncludeIsChanged: includeIsChanged,
                IncludeEditorModel: includeEditorModels
            };
        };
        FetchApplicationDataSourceRequestItem.resultAsDictionary = function (items) {
            var dict = {};
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var i = items_1[_i];
                dict[i.Name] = i.Value;
            }
            return dict;
        };
        FetchApplicationDataSourceRequestItem.editorModelsAsDictionary = function (items) {
            var dict = {};
            for (var _i = 0, items_2 = items; _i < items_2.length; _i++) {
                var i = items_2[_i];
                dict[i.Name] = i.EditorModel;
            }
            return dict;
        };
        return FetchApplicationDataSourceRequestItem;
    }());
    NTechPreCreditApi.FetchApplicationDataSourceRequestItem = FetchApplicationDataSourceRequestItem;
    var UnsecuredLoanAdditionalQuestionsStatusResult = /** @class */ (function () {
        function UnsecuredLoanAdditionalQuestionsStatusResult() {
        }
        return UnsecuredLoanAdditionalQuestionsStatusResult;
    }());
    NTechPreCreditApi.UnsecuredLoanAdditionalQuestionsStatusResult = UnsecuredLoanAdditionalQuestionsStatusResult;
    var UnsecuredLoanAgreementSigningStatusModel = /** @class */ (function () {
        function UnsecuredLoanAgreementSigningStatusModel() {
        }
        return UnsecuredLoanAgreementSigningStatusModel;
    }());
    NTechPreCreditApi.UnsecuredLoanAgreementSigningStatusModel = UnsecuredLoanAgreementSigningStatusModel;
    var UnsecuredLoanAgreementSigningStatusModelApplicant = /** @class */ (function () {
        function UnsecuredLoanAgreementSigningStatusModelApplicant() {
        }
        return UnsecuredLoanAgreementSigningStatusModelApplicant;
    }());
    NTechPreCreditApi.UnsecuredLoanAgreementSigningStatusModelApplicant = UnsecuredLoanAgreementSigningStatusModelApplicant;
    var UnsecuredLoanAdditionalQuestionsStatusModel = /** @class */ (function () {
        function UnsecuredLoanAdditionalQuestionsStatusModel() {
        }
        return UnsecuredLoanAdditionalQuestionsStatusModel;
    }());
    NTechPreCreditApi.UnsecuredLoanAdditionalQuestionsStatusModel = UnsecuredLoanAdditionalQuestionsStatusModel;
    var KycScreenCustomerResult = /** @class */ (function () {
        function KycScreenCustomerResult() {
        }
        return KycScreenCustomerResult;
    }());
    NTechPreCreditApi.KycScreenCustomerResult = KycScreenCustomerResult;
    var FetchCustomerKycScreenStatus = /** @class */ (function () {
        function FetchCustomerKycScreenStatus() {
        }
        return FetchCustomerKycScreenStatus;
    }());
    NTechPreCreditApi.FetchCustomerKycScreenStatus = FetchCustomerKycScreenStatus;
    var RequiredFiledsLeftToLiveOnResult = /** @class */ (function () {
        function RequiredFiledsLeftToLiveOnResult() {
        }
        return RequiredFiledsLeftToLiveOnResult;
    }());
    NTechPreCreditApi.RequiredFiledsLeftToLiveOnResult = RequiredFiledsLeftToLiveOnResult;
    var CalculateLeftToLiveOnResult = /** @class */ (function () {
        function CalculateLeftToLiveOnResult() {
        }
        return CalculateLeftToLiveOnResult;
    }());
    NTechPreCreditApi.CalculateLeftToLiveOnResult = CalculateLeftToLiveOnResult;
    var MortgageLoanAdditionalQuestionsStatusUpdateResult = /** @class */ (function () {
        function MortgageLoanAdditionalQuestionsStatusUpdateResult() {
        }
        return MortgageLoanAdditionalQuestionsStatusUpdateResult;
    }());
    NTechPreCreditApi.MortgageLoanAdditionalQuestionsStatusUpdateResult = MortgageLoanAdditionalQuestionsStatusUpdateResult;
    var MortageLoanCurrentLoansModel = /** @class */ (function () {
        function MortageLoanCurrentLoansModel() {
        }
        return MortageLoanCurrentLoansModel;
    }());
    NTechPreCreditApi.MortageLoanCurrentLoansModel = MortageLoanCurrentLoansModel;
    var MortageLoanCurrentLoansLoanModel = /** @class */ (function () {
        function MortageLoanCurrentLoansLoanModel() {
        }
        return MortageLoanCurrentLoansLoanModel;
    }());
    NTechPreCreditApi.MortageLoanCurrentLoansLoanModel = MortageLoanCurrentLoansLoanModel;
    var MortgageApplicationWorkListPageResult = /** @class */ (function () {
        function MortgageApplicationWorkListPageResult() {
        }
        return MortgageApplicationWorkListPageResult;
    }());
    NTechPreCreditApi.MortgageApplicationWorkListPageResult = MortgageApplicationWorkListPageResult;
    var MortgageApplicationWorkListApplication = /** @class */ (function () {
        function MortgageApplicationWorkListApplication() {
        }
        return MortgageApplicationWorkListApplication;
    }());
    NTechPreCreditApi.MortgageApplicationWorkListApplication = MortgageApplicationWorkListApplication;
    var MortgageApplicationWorkListFilter = /** @class */ (function () {
        function MortgageApplicationWorkListFilter() {
        }
        return MortgageApplicationWorkListFilter;
    }());
    NTechPreCreditApi.MortgageApplicationWorkListFilter = MortgageApplicationWorkListFilter;
    var MortgageApplicationWorkListCodeCount = /** @class */ (function () {
        function MortgageApplicationWorkListCodeCount() {
        }
        return MortgageApplicationWorkListCodeCount;
    }());
    NTechPreCreditApi.MortgageApplicationWorkListCodeCount = MortgageApplicationWorkListCodeCount;
    var AddApplicationCommentsOptional = /** @class */ (function () {
        function AddApplicationCommentsOptional() {
        }
        return AddApplicationCommentsOptional;
    }());
    NTechPreCreditApi.AddApplicationCommentsOptional = AddApplicationCommentsOptional;
    var FetchApplicationCommentsOptional = /** @class */ (function () {
        function FetchApplicationCommentsOptional() {
        }
        return FetchApplicationCommentsOptional;
    }());
    NTechPreCreditApi.FetchApplicationCommentsOptional = FetchApplicationCommentsOptional;
    var UserIdAndDisplayName = /** @class */ (function () {
        function UserIdAndDisplayName() {
        }
        return UserIdAndDisplayName;
    }());
    NTechPreCreditApi.UserIdAndDisplayName = UserIdAndDisplayName;
    var FetchHouseholdIncomeModelResult = /** @class */ (function () {
        function FetchHouseholdIncomeModelResult() {
        }
        return FetchHouseholdIncomeModelResult;
    }());
    NTechPreCreditApi.FetchHouseholdIncomeModelResult = FetchHouseholdIncomeModelResult;
    var HouseholdIncomeModel = /** @class */ (function () {
        function HouseholdIncomeModel() {
        }
        return HouseholdIncomeModel;
    }());
    NTechPreCreditApi.HouseholdIncomeModel = HouseholdIncomeModel;
    var HouseholdIncomeApplicantModel = /** @class */ (function () {
        function HouseholdIncomeApplicantModel() {
        }
        return HouseholdIncomeApplicantModel;
    }());
    NTechPreCreditApi.HouseholdIncomeApplicantModel = HouseholdIncomeApplicantModel;
    var MortgageLoanApplicationBasisCurrentValuesModel = /** @class */ (function () {
        function MortgageLoanApplicationBasisCurrentValuesModel() {
        }
        return MortgageLoanApplicationBasisCurrentValuesModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationBasisCurrentValuesModel = MortgageLoanApplicationBasisCurrentValuesModel;
    var MortgageLoanBankFormModel = /** @class */ (function () {
        function MortgageLoanBankFormModel() {
        }
        return MortgageLoanBankFormModel;
    }());
    NTechPreCreditApi.MortgageLoanBankFormModel = MortgageLoanBankFormModel;
    var MortgageLoanAmortizationBasisModel = /** @class */ (function () {
        function MortgageLoanAmortizationBasisModel() {
        }
        return MortgageLoanAmortizationBasisModel;
    }());
    NTechPreCreditApi.MortgageLoanAmortizationBasisModel = MortgageLoanAmortizationBasisModel;
    var FetchLeftToLiveOnRequiredItemNamesResult = /** @class */ (function () {
        function FetchLeftToLiveOnRequiredItemNamesResult() {
        }
        return FetchLeftToLiveOnRequiredItemNamesResult;
    }());
    NTechPreCreditApi.FetchLeftToLiveOnRequiredItemNamesResult = FetchLeftToLiveOnRequiredItemNamesResult;
    var ComputeLeftToLiveOnResult = /** @class */ (function () {
        function ComputeLeftToLiveOnResult() {
        }
        return ComputeLeftToLiveOnResult;
    }());
    NTechPreCreditApi.ComputeLeftToLiveOnResult = ComputeLeftToLiveOnResult;
    var NameValuePair = /** @class */ (function () {
        function NameValuePair() {
        }
        return NameValuePair;
    }());
    NTechPreCreditApi.NameValuePair = NameValuePair;
    var ScoringDataModel = /** @class */ (function () {
        function ScoringDataModel() {
        }
        ScoringDataModel.toDataTable = function (s) {
            if (!s) {
                return null;
            }
            var result = [];
            for (var _i = 0, _a = Object.keys(s.ApplicationItems); _i < _a.length; _i++) {
                var name_2 = _a[_i];
                result.push([name_2, 'Application', s.ApplicationItems[name_2]]);
            }
            for (var applicantNr in s.ApplicantItems) {
                for (var _b = 0, _c = Object.keys(s.ApplicantItems[applicantNr]); _b < _c.length; _b++) {
                    var name_3 = _c[_b];
                    result.push([name_3, 'Applicant ' + applicantNr, s.ApplicantItems[applicantNr][name_3]]);
                }
            }
            return result;
        };
        return ScoringDataModel;
    }());
    NTechPreCreditApi.ScoringDataModel = ScoringDataModel;
    var ScoringDataModelFlatItem = /** @class */ (function () {
        function ScoringDataModelFlatItem() {
        }
        return ScoringDataModelFlatItem;
    }());
    NTechPreCreditApi.ScoringDataModelFlatItem = ScoringDataModelFlatItem;
    var ScoringDataModelFlat = /** @class */ (function () {
        function ScoringDataModelFlat() {
        }
        ScoringDataModelFlat.toDataTable = function (s) {
            if (!s) {
                return null;
            }
            var result = [];
            for (var _i = 0, _a = s.Items; _i < _a.length; _i++) {
                var i = _a[_i];
                if (!i.ApplicantNr) {
                    result.push([i.Name, 'Application', i.Value]);
                }
            }
            for (var _b = 0, _c = s.Items; _b < _c.length; _b++) {
                var i = _c[_b];
                if (i.ApplicantNr) {
                    result.push([i.Name, 'Applicant ' + i.ApplicantNr, i.Value]);
                }
            }
            return result;
        };
        return ScoringDataModelFlat;
    }());
    NTechPreCreditApi.ScoringDataModelFlat = ScoringDataModelFlat;
    var MortageLoanOffer = /** @class */ (function () {
        function MortageLoanOffer() {
        }
        return MortageLoanOffer;
    }());
    NTechPreCreditApi.MortageLoanOffer = MortageLoanOffer;
    var MortageLoanScoringResult = /** @class */ (function () {
        function MortageLoanScoringResult() {
        }
        return MortageLoanScoringResult;
    }());
    NTechPreCreditApi.MortageLoanScoringResult = MortageLoanScoringResult;
    var MortageLoanObjectModel = /** @class */ (function () {
        function MortageLoanObjectModel() {
        }
        return MortageLoanObjectModel;
    }());
    NTechPreCreditApi.MortageLoanObjectModel = MortageLoanObjectModel;
    var MortageLoanObjectCondominiumDetailsModel = /** @class */ (function () {
        function MortageLoanObjectCondominiumDetailsModel() {
        }
        return MortageLoanObjectCondominiumDetailsModel;
    }());
    NTechPreCreditApi.MortageLoanObjectCondominiumDetailsModel = MortageLoanObjectCondominiumDetailsModel;
    var MortageLoanProviderCallbackResultModel = /** @class */ (function () {
        function MortageLoanProviderCallbackResultModel() {
        }
        return MortageLoanProviderCallbackResultModel;
    }());
    NTechPreCreditApi.MortageLoanProviderCallbackResultModel = MortageLoanProviderCallbackResultModel;
    var MortgageLoanSettlementDataModel = /** @class */ (function () {
        function MortgageLoanSettlementDataModel() {
        }
        return MortgageLoanSettlementDataModel;
    }());
    NTechPreCreditApi.MortgageLoanSettlementDataModel = MortgageLoanSettlementDataModel;
    var MortgageLoansSettlementPendingModel = /** @class */ (function () {
        function MortgageLoansSettlementPendingModel() {
        }
        return MortgageLoansSettlementPendingModel;
    }());
    NTechPreCreditApi.MortgageLoansSettlementPendingModel = MortgageLoansSettlementPendingModel;
    var FetchExternalApplicationRequestJsonResponse = /** @class */ (function () {
        function FetchExternalApplicationRequestJsonResponse() {
        }
        return FetchExternalApplicationRequestJsonResponse;
    }());
    NTechPreCreditApi.FetchExternalApplicationRequestJsonResponse = FetchExternalApplicationRequestJsonResponse;
    var OtherApplicationsResponseModel = /** @class */ (function () {
        function OtherApplicationsResponseModel() {
        }
        return OtherApplicationsResponseModel;
    }());
    NTechPreCreditApi.OtherApplicationsResponseModel = OtherApplicationsResponseModel;
    var OtherApplicationsResponseApplicantModel = /** @class */ (function () {
        function OtherApplicationsResponseApplicantModel() {
        }
        return OtherApplicationsResponseApplicantModel;
    }());
    NTechPreCreditApi.OtherApplicationsResponseApplicantModel = OtherApplicationsResponseApplicantModel;
    var OtherApplicationsResponseApplicantsInfoModel = /** @class */ (function () {
        function OtherApplicationsResponseApplicantsInfoModel() {
        }
        return OtherApplicationsResponseApplicantsInfoModel;
    }());
    NTechPreCreditApi.OtherApplicationsResponseApplicantsInfoModel = OtherApplicationsResponseApplicantsInfoModel;
    var OtherApplicationsResponseApplicationModel = /** @class */ (function () {
        function OtherApplicationsResponseApplicationModel() {
        }
        return OtherApplicationsResponseApplicationModel;
    }());
    NTechPreCreditApi.OtherApplicationsResponseApplicationModel = OtherApplicationsResponseApplicationModel;
    var OtherApplicationsResponseCreditModel = /** @class */ (function () {
        function OtherApplicationsResponseCreditModel() {
        }
        return OtherApplicationsResponseCreditModel;
    }());
    NTechPreCreditApi.OtherApplicationsResponseCreditModel = OtherApplicationsResponseCreditModel;
    var AcceptNewLoanRequest = /** @class */ (function () {
        function AcceptNewLoanRequest() {
        }
        return AcceptNewLoanRequest;
    }());
    NTechPreCreditApi.AcceptNewLoanRequest = AcceptNewLoanRequest;
    var RejectUnsecuredLoanApplicationRequest = /** @class */ (function () {
        function RejectUnsecuredLoanApplicationRequest() {
        }
        return RejectUnsecuredLoanApplicationRequest;
    }());
    NTechPreCreditApi.RejectUnsecuredLoanApplicationRequest = RejectUnsecuredLoanApplicationRequest;
    var ValidateBankAccountNrResult = /** @class */ (function () {
        function ValidateBankAccountNrResult() {
        }
        return ValidateBankAccountNrResult;
    }());
    NTechPreCreditApi.ValidateBankAccountNrResult = ValidateBankAccountNrResult;
    var ValidateBankAccountNrResultAccount = /** @class */ (function () {
        function ValidateBankAccountNrResultAccount() {
        }
        return ValidateBankAccountNrResultAccount;
    }());
    NTechPreCreditApi.ValidateBankAccountNrResultAccount = ValidateBankAccountNrResultAccount;
    var MortgageLoanApplicationDirectDebitStatusModel = /** @class */ (function () {
        function MortgageLoanApplicationDirectDebitStatusModel() {
        }
        return MortgageLoanApplicationDirectDebitStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel = MortgageLoanApplicationDirectDebitStatusModel;
    var MortgageLoanApplicationDirectDebitStatusModel_Applicant = /** @class */ (function () {
        function MortgageLoanApplicationDirectDebitStatusModel_Applicant() {
        }
        return MortgageLoanApplicationDirectDebitStatusModel_Applicant;
    }());
    NTechPreCreditApi.MortgageLoanApplicationDirectDebitStatusModel_Applicant = MortgageLoanApplicationDirectDebitStatusModel_Applicant;
    var MortgageApplicationValutionResult = /** @class */ (function () {
        function MortgageApplicationValutionResult() {
        }
        return MortgageApplicationValutionResult;
    }());
    NTechPreCreditApi.MortgageApplicationValutionResult = MortgageApplicationValutionResult;
    var MortgageApplicationTryValutionResult = /** @class */ (function () {
        function MortgageApplicationTryValutionResult() {
        }
        return MortgageApplicationTryValutionResult;
    }());
    NTechPreCreditApi.MortgageApplicationTryValutionResult = MortgageApplicationTryValutionResult;
    var UcbvVarderaBostadsrattResponse = /** @class */ (function () {
        function UcbvVarderaBostadsrattResponse() {
        }
        return UcbvVarderaBostadsrattResponse;
    }());
    NTechPreCreditApi.UcbvVarderaBostadsrattResponse = UcbvVarderaBostadsrattResponse;
    var UcbvVarderaBostadsrattResponseBrfSignal = /** @class */ (function () {
        function UcbvVarderaBostadsrattResponseBrfSignal() {
        }
        return UcbvVarderaBostadsrattResponseBrfSignal;
    }());
    NTechPreCreditApi.UcbvVarderaBostadsrattResponseBrfSignal = UcbvVarderaBostadsrattResponseBrfSignal;
    var UcbvObjectInfo = /** @class */ (function () {
        function UcbvObjectInfo() {
        }
        return UcbvObjectInfo;
    }());
    NTechPreCreditApi.UcbvObjectInfo = UcbvObjectInfo;
    var UcbvObjectInfoLgh = /** @class */ (function () {
        function UcbvObjectInfoLgh() {
        }
        return UcbvObjectInfoLgh;
    }());
    NTechPreCreditApi.UcbvObjectInfoLgh = UcbvObjectInfoLgh;
    var UcbvSokAdressHit = /** @class */ (function () {
        function UcbvSokAdressHit() {
        }
        return UcbvSokAdressHit;
    }());
    NTechPreCreditApi.UcbvSokAdressHit = UcbvSokAdressHit;
    var MortgageLoanApplicationSearchHit = /** @class */ (function () {
        function MortgageLoanApplicationSearchHit() {
        }
        return MortgageLoanApplicationSearchHit;
    }());
    NTechPreCreditApi.MortgageLoanApplicationSearchHit = MortgageLoanApplicationSearchHit;
    var MortgageLoanApplicationCustomerCheckScreenResultItem = /** @class */ (function () {
        function MortgageLoanApplicationCustomerCheckScreenResultItem() {
        }
        return MortgageLoanApplicationCustomerCheckScreenResultItem;
    }());
    NTechPreCreditApi.MortgageLoanApplicationCustomerCheckScreenResultItem = MortgageLoanApplicationCustomerCheckScreenResultItem;
    var MortgageLoanAcceptedFinalOffer = /** @class */ (function () {
        function MortgageLoanAcceptedFinalOffer() {
        }
        return MortgageLoanAcceptedFinalOffer;
    }());
    NTechPreCreditApi.MortgageLoanAcceptedFinalOffer = MortgageLoanAcceptedFinalOffer;
    var MortgageLoanAdditionalQuestionsStatusModel = /** @class */ (function () {
        function MortgageLoanAdditionalQuestionsStatusModel() {
        }
        return MortgageLoanAdditionalQuestionsStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanAdditionalQuestionsStatusModel = MortgageLoanAdditionalQuestionsStatusModel;
    var MortgageLoanAdditionalQuestionsDocument = /** @class */ (function () {
        function MortgageLoanAdditionalQuestionsDocument() {
        }
        return MortgageLoanAdditionalQuestionsDocument;
    }());
    NTechPreCreditApi.MortgageLoanAdditionalQuestionsDocument = MortgageLoanAdditionalQuestionsDocument;
    var MortgageLoanAdditionalQuestionsDocumentItem = /** @class */ (function () {
        function MortgageLoanAdditionalQuestionsDocumentItem() {
        }
        return MortgageLoanAdditionalQuestionsDocumentItem;
    }());
    NTechPreCreditApi.MortgageLoanAdditionalQuestionsDocumentItem = MortgageLoanAdditionalQuestionsDocumentItem;
    var ApplicationDocumentCheckStatusUpdateResult = /** @class */ (function () {
        function ApplicationDocumentCheckStatusUpdateResult() {
        }
        return ApplicationDocumentCheckStatusUpdateResult;
    }());
    NTechPreCreditApi.ApplicationDocumentCheckStatusUpdateResult = ApplicationDocumentCheckStatusUpdateResult;
    var ApplicationDocument = /** @class */ (function () {
        function ApplicationDocument() {
        }
        ApplicationDocument.GetDownloadUrlByKey = function (documentArchiveKey) {
            return '/CreditManagement/ArchiveDocument?key=' + documentArchiveKey;
        };
        ApplicationDocument.GetDownloadUrl = function (d) {
            if (!d) {
                return null;
            }
            else {
                return ApplicationDocument.GetDownloadUrlByKey(d.DocumentArchiveKey);
            }
        };
        return ApplicationDocument;
    }());
    NTechPreCreditApi.ApplicationDocument = ApplicationDocument;
    var ProviderInfoModel = /** @class */ (function () {
        function ProviderInfoModel() {
        }
        return ProviderInfoModel;
    }());
    NTechPreCreditApi.ProviderInfoModel = ProviderInfoModel;
    var ApplicationCheckPointModel = /** @class */ (function () {
        function ApplicationCheckPointModel() {
        }
        return ApplicationCheckPointModel;
    }());
    NTechPreCreditApi.ApplicationCheckPointModel = ApplicationCheckPointModel;
    var MortgageLoanApplicationCustomerCheckStatusModel = /** @class */ (function () {
        function MortgageLoanApplicationCustomerCheckStatusModel() {
        }
        return MortgageLoanApplicationCustomerCheckStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModel = MortgageLoanApplicationCustomerCheckStatusModel;
    var MortgageLoanApplicationCustomerCheckStatusModelIssue = /** @class */ (function () {
        function MortgageLoanApplicationCustomerCheckStatusModelIssue() {
        }
        return MortgageLoanApplicationCustomerCheckStatusModelIssue;
    }());
    NTechPreCreditApi.MortgageLoanApplicationCustomerCheckStatusModelIssue = MortgageLoanApplicationCustomerCheckStatusModelIssue;
    var MortgageLoanApplicationInitialCreditCheckStatusModel = /** @class */ (function () {
        function MortgageLoanApplicationInitialCreditCheckStatusModel() {
        }
        return MortgageLoanApplicationInitialCreditCheckStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModel = MortgageLoanApplicationInitialCreditCheckStatusModel;
    var MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel = /** @class */ (function () {
        function MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel() {
        }
        return MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel = MortgageLoanApplicationInitialCreditCheckStatusModelRejectedDecisionModel;
    var MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel = /** @class */ (function () {
        function MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel() {
        }
        return MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel = MortgageLoanApplicationInitialCreditCheckStatusModelAcceptedDecisionModel;
    var MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel = /** @class */ (function () {
        function MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel() {
        }
        return MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel = MortgageLoanApplicationInitialCreditCheckStatusModelOfferModel;
    var MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel = /** @class */ (function () {
        function MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel() {
        }
        return MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel = MortgageLoanApplicationInitialCreditCheckStatusModelRejectionReasonModel;
    var MortgageLoanApplicationFinalCreditCheckStatusModel = /** @class */ (function () {
        function MortgageLoanApplicationFinalCreditCheckStatusModel() {
        }
        return MortgageLoanApplicationFinalCreditCheckStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModel = MortgageLoanApplicationFinalCreditCheckStatusModel;
    var MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel = /** @class */ (function () {
        function MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel() {
        }
        return MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel = MortgageLoanApplicationFinalCreditCheckStatusModelRejectedDecisionModel;
    var MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel = /** @class */ (function () {
        function MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel() {
        }
        return MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel = MortgageLoanApplicationFinalCreditCheckStatusModelRejectionReasonModel;
    var MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel = /** @class */ (function () {
        function MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel() {
        }
        return MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel = MortgageLoanApplicationFinalCreditCheckStatusModelAcceptedDecisionModel;
    var MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel = /** @class */ (function () {
        function MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel() {
        }
        return MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel = MortgageLoanApplicationFinalCreditCheckStatusModelOfferModel;
    var SetIsWaitingForAdditionalInformationResult = /** @class */ (function () {
        function SetIsWaitingForAdditionalInformationResult() {
        }
        return SetIsWaitingForAdditionalInformationResult;
    }());
    NTechPreCreditApi.SetIsWaitingForAdditionalInformationResult = SetIsWaitingForAdditionalInformationResult;
    var ApplicationComment = /** @class */ (function () {
        function ApplicationComment() {
        }
        return ApplicationComment;
    }());
    NTechPreCreditApi.ApplicationComment = ApplicationComment;
    var FraudControlModel = /** @class */ (function () {
        function FraudControlModel() {
        }
        return FraudControlModel;
    }());
    NTechPreCreditApi.FraudControlModel = FraudControlModel;
    var FraudControlModelApplicant = /** @class */ (function () {
        function FraudControlModelApplicant() {
        }
        return FraudControlModelApplicant;
    }());
    NTechPreCreditApi.FraudControlModelApplicant = FraudControlModelApplicant;
    var ApplicationInfoModel = /** @class */ (function () {
        function ApplicationInfoModel() {
        }
        return ApplicationInfoModel;
    }());
    NTechPreCreditApi.ApplicationInfoModel = ApplicationInfoModel;
    var CustomerComponentInitialData = /** @class */ (function () {
        function CustomerComponentInitialData() {
        }
        return CustomerComponentInitialData;
    }());
    NTechPreCreditApi.CustomerComponentInitialData = CustomerComponentInitialData;
    var CustomerItem = /** @class */ (function () {
        function CustomerItem() {
        }
        return CustomerItem;
    }());
    NTechPreCreditApi.CustomerItem = CustomerItem;
    var CheckIfOverHandlerLimitResult = /** @class */ (function () {
        function CheckIfOverHandlerLimitResult() {
        }
        return CheckIfOverHandlerLimitResult;
    }());
    NTechPreCreditApi.CheckIfOverHandlerLimitResult = CheckIfOverHandlerLimitResult;
    var GetBatchDetailsResult = /** @class */ (function () {
        function GetBatchDetailsResult() {
        }
        return GetBatchDetailsResult;
    }());
    NTechPreCreditApi.GetBatchDetailsResult = GetBatchDetailsResult;
    var BatchDetail = /** @class */ (function () {
        function BatchDetail() {
        }
        return BatchDetail;
    }());
    NTechPreCreditApi.BatchDetail = BatchDetail;
    var FindHistoricalDecisionResult = /** @class */ (function () {
        function FindHistoricalDecisionResult() {
        }
        return FindHistoricalDecisionResult;
    }());
    NTechPreCreditApi.FindHistoricalDecisionResult = FindHistoricalDecisionResult;
    var FindHistoricalDecisionBatchItem = /** @class */ (function () {
        function FindHistoricalDecisionBatchItem() {
        }
        return FindHistoricalDecisionBatchItem;
    }());
    NTechPreCreditApi.FindHistoricalDecisionBatchItem = FindHistoricalDecisionBatchItem;
    var DocumentCheckStatusResult = /** @class */ (function () {
        function DocumentCheckStatusResult() {
        }
        return DocumentCheckStatusResult;
    }());
    NTechPreCreditApi.DocumentCheckStatusResult = DocumentCheckStatusResult;
    var MortgageLoanApplicationValuationStatusModel = /** @class */ (function () {
        function MortgageLoanApplicationValuationStatusModel() {
        }
        return MortgageLoanApplicationValuationStatusModel;
    }());
    NTechPreCreditApi.MortgageLoanApplicationValuationStatusModel = MortgageLoanApplicationValuationStatusModel;
    function getNumberDictionarKeys(i) {
        var r = [];
        for (var _i = 0, _a = Object.keys(i); _i < _a.length; _i++) {
            var k = _a[_i];
            r.push(parseInt(k));
        }
        return r;
    }
    NTechPreCreditApi.getNumberDictionarKeys = getNumberDictionarKeys;
    var FindCreditReportsByCustomerId = /** @class */ (function () {
        function FindCreditReportsByCustomerId() {
        }
        return FindCreditReportsByCustomerId;
    }());
    NTechPreCreditApi.FindCreditReportsByCustomerId = FindCreditReportsByCustomerId;
})(NTechPreCreditApi || (NTechPreCreditApi = {}));
