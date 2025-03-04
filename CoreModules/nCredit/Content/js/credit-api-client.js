var NTechCreditApi;
(function (NTechCreditApi) {
    var ApiClient = /** @class */ (function () {
        function ApiClient(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.activePostCount = 0;
            this.loggingContext = null;
        }
        ApiClient.prototype.post = function (url, data) {
            var _this = this;
            var startTimeMs = performance.now();
            this.activePostCount++;
            var d = this.$q.defer();
            this.$http.post(url, data).then(function (result) {
                d.resolve(result.data);
            }, function (err) {
                if (_this.onError) {
                    _this.onError(err.statusText);
                }
                d.reject(err.statusText);
            }).finally(function () {
                _this.activePostCount--;
                var totalTimeMs = performance.now() - startTimeMs;
                var c = _this.loggingContext == null ? '' : (_this.loggingContext + ': ');
                if (console) {
                    console.log("".concat(c, "post - ").concat(url, ": ").concat(totalTimeMs, "ms"));
                }
            });
            return d.promise;
        };
        ApiClient.prototype.postUsingApiGateway = function (seviceName, serviceLocalUrl, data) {
            return this.post("/Api/Gateway/".concat(seviceName).concat(serviceLocalUrl[0] === '/' ? '' : '/').concat(serviceLocalUrl), data);
        };
        ApiClient.prototype.getUserModuleUrl = function (moduleName, serviceLocalUrl, parameters) {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters });
        };
        ApiClient.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        ApiClient.prototype.fetchCreditDocuments = function (creditNr, fetchFilenames, includeExtraDocuments) {
            return this.post('/Api/Credit/Documents/Fetch', { creditNr: creditNr, fetchFilenames: fetchFilenames, includeExtraDocuments: includeExtraDocuments });
        };
        ApiClient.prototype.fetchSecurityItems = function (creditNr) {
            return this.post('/Api/Credit/Security/FetchItems', { creditNr: creditNr });
        };
        ApiClient.prototype.fetchCreditDirectDebitDetails = function (creditNr, backTarget, includeEvents) {
            return this.post('/Api/Credit/DirectDebit/FetchDetails', { creditNr: creditNr, backTarget: backTarget, includeEvents: includeEvents });
        };
        ApiClient.prototype.fetchCreditDirectEvents = function (creditNr) {
            return this.post('/Api/Credit/DirectDebit/FetchEvents', { creditNr: creditNr });
        };
        ApiClient.prototype.validateBankAccountNr = function (bankAccountNr) {
            return this.post('/Api/BankAccount/ValidateNr', { bankAccountNr: bankAccountNr });
        };
        ApiClient.prototype.updateDirectDebitCheckStatus = function (creditNr, newStatus, bankAccountNr, bankAccountOwnerApplicantNr) {
            return this.post('/Api/Credit/DirectDebit/UpdateStatus', { creditNr: creditNr, newStatus: newStatus, bankAccountNr: bankAccountNr, bankAccountOwnerApplicantNr: bankAccountOwnerApplicantNr });
        };
        ApiClient.prototype.scheduleDirectDebitActivation = function (isChangeActivated, creditNr, bankAccountNr, paymentNr, applicantNr, customerId) {
            return this.post('/Api/Credit/DirectDebit/ScheduleActivation', { isChangeActivated: isChangeActivated, creditNr: creditNr, bankAccountNr: bankAccountNr, paymentNr: paymentNr, applicantNr: applicantNr, customerId: customerId });
        };
        ApiClient.prototype.scheduleDirectDebitCancellation = function (creditNr, isChangeActivated, paymentNr) {
            return this.post('/Api/Credit/DirectDebit/ScheduleCancellation', { creditNr: creditNr, isChangeActivated: isChangeActivated, paymentNr: paymentNr });
        };
        ApiClient.prototype.scheduleDirectDebitChange = function (currentStatus, isChangeActivated, creditNr, bankAccountNr, paymentNr, applicantNr, customerId) {
            return this.post('/Api/Credit/DirectDebit/ScheduleChange', { currentStatus: currentStatus, isChangeActivated: isChangeActivated, creditNr: creditNr, bankAccountNr: bankAccountNr, paymentNr: paymentNr, applicantNr: applicantNr, customerId: customerId });
        };
        ApiClient.prototype.removeDirectDebitSchedulation = function (creditNr, paymentNr) {
            return this.post('/Api/Credit/DirectDebit/RemoveSchedulation', { creditNr: creditNr, paymentNr: paymentNr });
        };
        ApiClient.prototype.loadCreditComments = function (creditNr, excludeTheseEventTypes, onlyTheseEventTypes) {
            return this.post('/Api/CreditComment/LoadForCredit', { creditNr: creditNr, excludeTheseEventTypes: excludeTheseEventTypes, onlyTheseEventTypes: onlyTheseEventTypes });
        };
        ApiClient.prototype.createCreditComment = function (creditNr, commentText, attachedFileAsDataUrl, attachedFileName) {
            return this.post('/Api/CreditComment/Create', { creditNr: creditNr, commentText: commentText, attachedFileAsDataUrl: attachedFileAsDataUrl, attachedFileName: attachedFileName });
        };
        ApiClient.prototype.keyValueStoreGet = function (key, keySpace) {
            return this.post('/Api/KeyValueStore/Get', {
                "Key": key,
                "KeySpace": keySpace
            });
        };
        ApiClient.prototype.keyValueStoreRemove = function (key, keySpace) {
            return this.post('/Api/KeyValueStore/Remove', {
                "Key": key,
                "KeySpace": keySpace
            });
        };
        ApiClient.prototype.keyValueStoreSet = function (key, keySpace, value) {
            return this.post('/Api/KeyValueStore/Set', {
                "Key": key,
                "KeySpace": keySpace,
                "Value": value
            });
        };
        ApiClient.prototype.fetchUserNameByUserId = function (userId) {
            return this.post('/Api/UserName/ByUserId', { UserId: userId });
        };
        ApiClient.prototype.fetchPendingReferenceInterestChange = function () {
            return this.post('/Api/ReferenceInterestRate/FetchPendingChange', {});
        };
        ApiClient.prototype.beginReferenceInterestChange = function (newInterestRatePercent) {
            return this.post('/Api/ReferenceInterestRate/BeginChange', { NewInterestRatePercent: newInterestRatePercent });
        };
        ApiClient.prototype.cancelPendingReferenceInterestChange = function () {
            return this.post('/Api/ReferenceInterestRate/CancelPendingChange', {});
        };
        ApiClient.prototype.commitPendingReferenceInterestChange = function (expectedNewInterestRatePercent, requestOverrideDuality) {
            return this.post('/Api/ReferenceInterestRate/CommitPendingChange', { ExpectedNewInterestRatePercent: expectedNewInterestRatePercent, RequestOverrideDuality: requestOverrideDuality });
        };
        ApiClient.prototype.fetchReferenceInterestChangePage = function (pageSize, pageNr) {
            return this.post('/Api/Credit/GetReferenceInterestRateChangesPage', { pageSize: pageSize, pageNr: pageNr });
        };
        ApiClient.prototype.fetchCustomerCardItems = function (customerId, propertyNames) {
            var r = this.post('/Api/Credit/FetchCustomerItems', { customerId: customerId, propertyNames: propertyNames });
            return r.then(function (x) {
                var d = {};
                if (x && x.items) {
                    for (var _i = 0, _a = x.items; _i < _a.length; _i++) {
                        var i = _a[_i];
                        d[i.name] = i.value;
                    }
                }
                return d;
            });
        };
        ApiClient.prototype.fetchCustomerCardItemsBulk = function (customerIds, itemNames) {
            var r = this.post('/api/Customer/Bulk-Fetch-Properties', {
                customerIds: customerIds,
                propertyNames: itemNames
            });
            return r.then(function (x) { return x.Properties; });
        };
        ApiClient.prototype.repayUnplacedPayment = function (paymentId, customerName, repaymentAmount, leaveUnplacedAmount, bankAccountNrType, bankAccountNr) {
            return this.post('/Api/Credit/RepayPayment', {
                paymentId: paymentId,
                customerName: customerName,
                repaymentAmount: repaymentAmount,
                leaveUnplacedAmount: leaveUnplacedAmount,
                bankAccountNrType: bankAccountNrType,
                iban: bankAccountNr
            });
        };
        ApiClient.prototype.isValidAccountNr = function (bankAccountNr, bankAccountNrType) {
            return this.post('/Api/UnplacedPayment/IsValidAccountNr', { bankAccountNr: bankAccountNr, bankAccountNrType: bankAccountNrType });
        };
        ApiClient.prototype.createMortgageLoan = function (loan) {
            return this.post('/Api/MortgageLoans/Create', loan);
        };
        // Note, if the civicRegNr is not already a customer, it will increment a new customerId for the hash of that civicRegNr. 
        ApiClient.prototype.getPersonCustomerId = function (civicRegNr) {
            return this.postUsingApiGateway('nCustomer', '/api/CustomerIdByCivicRegNr', { CivicRegNr: civicRegNr });
        };
        ;
        ApiClient.prototype.createOrUpdatePersonCustomer = function (request) {
            return this.postUsingApiGateway('nCustomer', 'api/PersonCustomer/CreateOrUpdate', request);
        };
        ApiClient.prototype.generateNewCreditNumber = function () {
            return this.post('/Api/NewCreditNumber', {});
        };
        ApiClient.prototype.existCustomerByCustomerId = function (customerId) {
            return this.postUsingApiGateway('nCustomer', '/api/ExistCustomerByCustomerId', { CustomerId: customerId });
        };
        ;
        ApiClient.prototype.fetchDatedCreditValueItems = function (creditNr, name) {
            return this.post('/Api/Credit/FetchDatedCreditValueItems', { creditNr: creditNr, name: name });
        };
        ApiClient.prototype.getCustomerMessagesTexts = function (messageIds) {
            return this.postUsingApiGateway('nCustomer', 'api/CustomerMessage/GetMessageTexts', {
                MessageIds: messageIds
            });
        };
        ApiClient.prototype.importOrPreviewCompanCreditsFromFile = function (request) {
            return this.post('/api/CompanyCredit/ImportOrPreviewFile', request);
        };
        ApiClient.prototype.removeCompanyConnection = function (customerId, creditNr, listName) {
            return this.post('/Api/Credit/RemoveCompanyConnection', { customerId: customerId, creditNr: creditNr, listName: listName });
        };
        ApiClient.prototype.addCompanyConnections = function (customerId, creditNr, listNames) {
            return this.post('/Api/Credit/AddCompanyConnections', { customerId: customerId, creditNr: creditNr, listNames: listNames });
        };
        ApiClient.prototype.setDatedCreditValue = function (creditNr, datedCreditValueCode, businessEventType, value) {
            return this.post('/Api/DatedCreditValue/Set', { creditNr: creditNr, datedCreditValueCode: datedCreditValueCode, businessEventType: businessEventType, value: value });
        };
        ApiClient.prototype.fetchBookKeepingRules = function () {
            return this.post('/Api/Bookkeeping/RulesAsJson', {});
        };
        ApiClient.prototype.fetchCreditAttentionStatus = function (creditNr) {
            return this.post('/Api/Credit/FetchAttentionStatus', { creditNr: creditNr });
        };
        ApiClient.prototype.getCustomerDetails = function (creditNr, backTarget) {
            return this.post('/Api/Credit/Customers', { creditNr: creditNr, backTarget: backTarget });
        };
        ApiClient.prototype.fetchMortgageLoanStandardCollaterals = function (creditNrs) {
            return this.post('/Api/MortgageLoans/Fetch-Collaterals', { creditNrs: creditNrs });
        };
        return ApiClient;
    }());
    NTechCreditApi.ApiClient = ApiClient;
    var ImportOrPreviewCompanCreditsFromFileRequest = /** @class */ (function () {
        function ImportOrPreviewCompanCreditsFromFileRequest() {
        }
        return ImportOrPreviewCompanCreditsFromFileRequest;
    }());
    NTechCreditApi.ImportOrPreviewCompanCreditsFromFileRequest = ImportOrPreviewCompanCreditsFromFileRequest;
    var Loan = /** @class */ (function () {
        function Loan() {
        }
        return Loan;
    }());
    NTechCreditApi.Loan = Loan;
    var Applicants = /** @class */ (function () {
        function Applicants() {
        }
        return Applicants;
    }());
    NTechCreditApi.Applicants = Applicants;
    var MortgageLoanAmortizationBasisModel = /** @class */ (function () {
        function MortgageLoanAmortizationBasisModel() {
        }
        return MortgageLoanAmortizationBasisModel;
    }());
    NTechCreditApi.MortgageLoanAmortizationBasisModel = MortgageLoanAmortizationBasisModel;
    var CreateCreditCommentResponse = /** @class */ (function () {
        function CreateCreditCommentResponse() {
        }
        return CreateCreditCommentResponse;
    }());
    NTechCreditApi.CreateCreditCommentResponse = CreateCreditCommentResponse;
    var CreditCommentModel = /** @class */ (function () {
        function CreditCommentModel() {
        }
        return CreditCommentModel;
    }());
    NTechCreditApi.CreditCommentModel = CreditCommentModel;
    var FetchCreditDirectDebitDetailsResult = /** @class */ (function () {
        function FetchCreditDirectDebitDetailsResult() {
        }
        return FetchCreditDirectDebitDetailsResult;
    }());
    NTechCreditApi.FetchCreditDirectDebitDetailsResult = FetchCreditDirectDebitDetailsResult;
    var DirectDebitEventModel = /** @class */ (function () {
        function DirectDebitEventModel() {
        }
        return DirectDebitEventModel;
    }());
    NTechCreditApi.DirectDebitEventModel = DirectDebitEventModel;
    var CreditDirectDebitDetailsModel = /** @class */ (function () {
        function CreditDirectDebitDetailsModel() {
        }
        return CreditDirectDebitDetailsModel;
    }());
    NTechCreditApi.CreditDirectDebitDetailsModel = CreditDirectDebitDetailsModel;
    var SchedulationModel = /** @class */ (function () {
        function SchedulationModel() {
        }
        return SchedulationModel;
    }());
    NTechCreditApi.SchedulationModel = SchedulationModel;
    var SchedulationDetailsModel = /** @class */ (function () {
        function SchedulationDetailsModel() {
        }
        return SchedulationDetailsModel;
    }());
    NTechCreditApi.SchedulationDetailsModel = SchedulationDetailsModel;
    var CreditDirectDebitDetailsApplicantModel = /** @class */ (function () {
        function CreditDirectDebitDetailsApplicantModel() {
        }
        return CreditDirectDebitDetailsApplicantModel;
    }());
    NTechCreditApi.CreditDirectDebitDetailsApplicantModel = CreditDirectDebitDetailsApplicantModel;
    var CreditDocumentModel = /** @class */ (function () {
        function CreditDocumentModel() {
        }
        return CreditDocumentModel;
    }());
    NTechCreditApi.CreditDocumentModel = CreditDocumentModel;
    var CreditSecurityItemModel = /** @class */ (function () {
        function CreditSecurityItemModel() {
        }
        return CreditSecurityItemModel;
    }());
    NTechCreditApi.CreditSecurityItemModel = CreditSecurityItemModel;
    var DatedCreditValueModel = /** @class */ (function () {
        function DatedCreditValueModel() {
        }
        return DatedCreditValueModel;
    }());
    NTechCreditApi.DatedCreditValueModel = DatedCreditValueModel;
    var ValidateBankAccountNrResult = /** @class */ (function () {
        function ValidateBankAccountNrResult() {
        }
        return ValidateBankAccountNrResult;
    }());
    NTechCreditApi.ValidateBankAccountNrResult = ValidateBankAccountNrResult;
    var ValidateBankAccountNrResultAccount = /** @class */ (function () {
        function ValidateBankAccountNrResultAccount() {
        }
        return ValidateBankAccountNrResultAccount;
    }());
    NTechCreditApi.ValidateBankAccountNrResultAccount = ValidateBankAccountNrResultAccount;
    var BookKeepingRuleDescriptionTableRow = /** @class */ (function () {
        function BookKeepingRuleDescriptionTableRow() {
        }
        return BookKeepingRuleDescriptionTableRow;
    }());
    NTechCreditApi.BookKeepingRuleDescriptionTableRow = BookKeepingRuleDescriptionTableRow;
    var DirectDebitConsentFile = /** @class */ (function () {
        function DirectDebitConsentFile() {
        }
        DirectDebitConsentFile.GetDownloadUrlByKey = function (documentArchiveKey) {
            return '/Api/Credit/DirectDebit/DownloadConsentFile?key=' + documentArchiveKey;
        };
        DirectDebitConsentFile.GetDownloadUrl = function (d) {
            if (!d) {
                return null;
            }
            else {
                return DirectDebitConsentFile.GetDownloadUrlByKey(d.DocumentArchiveKey);
            }
        };
        return DirectDebitConsentFile;
    }());
    NTechCreditApi.DirectDebitConsentFile = DirectDebitConsentFile;
})(NTechCreditApi || (NTechCreditApi = {}));
