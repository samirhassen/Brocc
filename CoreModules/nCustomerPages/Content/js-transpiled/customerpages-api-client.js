var NTechCustomerPagesApi;
(function (NTechCustomerPagesApi) {
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
        ApiClient.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        ApiClient.prototype.createTestApplication = function (overrides) {
            return this.post('/api/mortgage-loan/create-test-application', { overrides: overrides });
        };
        ApiClient.prototype.acceptTestApplication = function (applicationNr) {
            return this.post('/api/mortgage-loan/accept-test-application', { applicationNr: applicationNr });
        };
        ApiClient.prototype.createTestPerson = function (isAccepted) {
            return this.post('/api/mortgageloan/generate-testperson', { isAccepted: isAccepted });
        };
        ApiClient.prototype.validateBankAccountNr = function (bankAccountNr) {
            return this.post('/api/v1/mortgage-loan/validate-bankaccount-nr', { bankAccountNr: bankAccountNr });
        };
        ApiClient.prototype.submitAdditionalQuestionsAnswers = function (request) {
            return this.post('/api/v1/mortgage-loan/answer-additional-question', request);
        };
        ApiClient.prototype.savingsStandardApplicationApply = function (application) {
            return this.post('/savings/standard-application-apply', { application: application });
        };
        ApiClient.prototype.createSecureMessage = function (request) {
            return this.post('/Api/CustomerMessage/CreateMessage', request);
        };
        ApiClient.prototype.getSecureMessages = function (request) {
            return this.post('/Api/CustomerMessage/GetMessages', request);
        };
        ApiClient.prototype.attachMessageDocument = function (request) {
            return this.post('/Api/CustomerMessage/AttachMessageDocument', request);
        };
        return ApiClient;
    }());
    NTechCustomerPagesApi.ApiClient = ApiClient;
    var MortageLoanObjectModel = /** @class */ (function () {
        function MortageLoanObjectModel() {
        }
        return MortageLoanObjectModel;
    }());
    NTechCustomerPagesApi.MortageLoanObjectModel = MortageLoanObjectModel;
    var MortageLoanObjectCondominiumDetailsModel = /** @class */ (function () {
        function MortageLoanObjectCondominiumDetailsModel() {
        }
        return MortageLoanObjectCondominiumDetailsModel;
    }());
    NTechCustomerPagesApi.MortageLoanObjectCondominiumDetailsModel = MortageLoanObjectCondominiumDetailsModel;
    var ValidateBankAccountResult = /** @class */ (function () {
        function ValidateBankAccountResult() {
        }
        return ValidateBankAccountResult;
    }());
    NTechCustomerPagesApi.ValidateBankAccountResult = ValidateBankAccountResult;
    var SubmitAdditionalQuestionsAnswersRequest = /** @class */ (function () {
        function SubmitAdditionalQuestionsAnswersRequest() {
        }
        return SubmitAdditionalQuestionsAnswersRequest;
    }());
    NTechCustomerPagesApi.SubmitAdditionalQuestionsAnswersRequest = SubmitAdditionalQuestionsAnswersRequest;
    var SubmitAdditionalQuestionsAnswersRequestItem = /** @class */ (function () {
        function SubmitAdditionalQuestionsAnswersRequestItem() {
        }
        return SubmitAdditionalQuestionsAnswersRequestItem;
    }());
    NTechCustomerPagesApi.SubmitAdditionalQuestionsAnswersRequestItem = SubmitAdditionalQuestionsAnswersRequestItem;
    var SubmitAdditionalQuestionsAnswersResult = /** @class */ (function () {
        function SubmitAdditionalQuestionsAnswersResult() {
        }
        return SubmitAdditionalQuestionsAnswersResult;
    }());
    NTechCustomerPagesApi.SubmitAdditionalQuestionsAnswersResult = SubmitAdditionalQuestionsAnswersResult;
    var AdditionalQuestionsStatusModel = /** @class */ (function () {
        function AdditionalQuestionsStatusModel() {
        }
        return AdditionalQuestionsStatusModel;
    }());
    NTechCustomerPagesApi.AdditionalQuestionsStatusModel = AdditionalQuestionsStatusModel;
    var AdditionalQuestionsApplicantStatusModel = /** @class */ (function () {
        function AdditionalQuestionsApplicantStatusModel() {
        }
        return AdditionalQuestionsApplicantStatusModel;
    }());
    NTechCustomerPagesApi.AdditionalQuestionsApplicantStatusModel = AdditionalQuestionsApplicantStatusModel;
})(NTechCustomerPagesApi || (NTechCustomerPagesApi = {}));
//# sourceMappingURL=customerpages-api-client.js.map