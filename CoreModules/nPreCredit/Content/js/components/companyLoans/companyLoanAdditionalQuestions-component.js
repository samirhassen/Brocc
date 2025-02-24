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
var CompanyLoanAdditionalQuestionsComponentNs;
(function (CompanyLoanAdditionalQuestionsComponentNs) {
    var CompanyLoanAdditionalQuestionsController = /** @class */ (function (_super) {
        __extends(CompanyLoanAdditionalQuestionsController, _super);
        function CompanyLoanAdditionalQuestionsController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            _this.answersDialogId = modalDialogService.generateDialogId();
            _this.linkDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        CompanyLoanAdditionalQuestionsController.prototype.applicationNr = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr;
            }
            else {
                return null;
            }
        };
        CompanyLoanAdditionalQuestionsController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        CompanyLoanAdditionalQuestionsController.prototype.showDirectLink = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.modalDialogService.openDialog(this.linkDialogId);
        };
        CompanyLoanAdditionalQuestionsController.prototype.getSwitchableAnswerCode = function (a) {
            if (!a) {
                return '';
            }
            if (a.QuestionCode === 'beneficialOwnerPercentCount' || a.QuestionCode === 'beneficialOwnerConnectionCount') {
                return a.AnswerCode === '0' ? 'false' : 'true';
            }
            return a.AnswerCode;
        };
        CompanyLoanAdditionalQuestionsController.prototype.showAnswers = function (key, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.a) {
                this.modalDialogService.openDialog(this.answersDialogId);
            }
            else {
                this.initialData.companyLoanApiClient.fetchAdditionalQuestionsAnswers(this.initialData.applicationInfo.ApplicationNr).then(function (r) {
                    var customerIds = NTechLinq.distinct(r.Document.Items.filter(function (x) { return !!x.CustomerId; }).map(function (x) { return x.CustomerId; }));
                    _this.apiClient.fetchCustomerItemsBulk(customerIds, ['firstName']).then(function (customerData) {
                        var names = {};
                        for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                            var customerId = customerIds_1[_i];
                            names[customerId] = customerData[customerId] ? customerData[customerId]['firstName'] : 'customer';
                        }
                        var answers = [];
                        for (var _a = 0, _b = r.Document.Items.filter(function (x) { return !x.CustomerId; }); _a < _b.length; _a++) {
                            var q = _b[_a];
                            answers.push({
                                Type: 'answer',
                                AnswerCode: q.AnswerCode,
                                AnswerText: q.AnswerText,
                                QuestionCode: q.QuestionCode,
                                QuestionText: q.QuestionText
                            });
                        }
                        answers.push({
                            Type: 'separator'
                        });
                        var _loop_1 = function (customerId) {
                            answers.push({
                                Type: 'customer',
                                CustomerId: customerId,
                                CustomerCardUrl: _this.initialData.customerCardUrlPattern.replace('[[[CUSTOMER_ID]]]', customerId.toString()).replace('[[[BACK_TARGET]]]', _this.initialData.navigationTargetCodeToHere)
                            });
                            for (var _d = 0, _e = r.Document.Items.filter(function (x) { return x.CustomerId === customerId; }); _d < _e.length; _d++) {
                                var q = _e[_d];
                                answers.push({
                                    Type: 'answer',
                                    CustomerId: q.CustomerId,
                                    AnswerCode: q.AnswerCode,
                                    AnswerText: q.AnswerText,
                                    QuestionCode: q.QuestionCode,
                                    QuestionText: q.QuestionText
                                });
                            }
                            answers.push({
                                Type: 'separator'
                            });
                        };
                        for (var _c = 0, customerIds_2 = customerIds; _c < customerIds_2.length; _c++) {
                            var customerId = customerIds_2[_c];
                            _loop_1(customerId);
                        }
                        _this.a = {
                            Answers: answers,
                            CustomerFirstNameByCustomerId: names
                        };
                        _this.modalDialogService.openDialog(_this.answersDialogId);
                    });
                });
            }
        };
        CompanyLoanAdditionalQuestionsController.prototype.componentName = function () {
            return 'companyLoanAdditionalQuestions';
        };
        CompanyLoanAdditionalQuestionsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            this.a = null;
            this.isWaitingForPreviousSteps = false;
            if (!this.initialData) {
                return;
            }
            if (!this.initialData.step.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                this.isWaitingForPreviousSteps = true;
                return;
            }
            this.initialData.companyLoanApiClient.fetchAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                _this.m = result;
            });
        };
        // To avoid onclick as inline-script due to CSP. 
        CompanyLoanAdditionalQuestionsController.prototype.focusAndSelect = function (evt) {
            evt.currentTarget.focus();
            evt.currentTarget.select();
        };
        CompanyLoanAdditionalQuestionsController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanAdditionalQuestionsController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanAdditionalQuestionsComponentNs.CompanyLoanAdditionalQuestionsController = CompanyLoanAdditionalQuestionsController;
    var CompanyLoanAdditionalQuestionsComponent = /** @class */ (function () {
        function CompanyLoanAdditionalQuestionsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAdditionalQuestionsController;
            this.templateUrl = 'company-loan-additional-questions.html';
        }
        return CompanyLoanAdditionalQuestionsComponent;
    }());
    CompanyLoanAdditionalQuestionsComponentNs.CompanyLoanAdditionalQuestionsComponent = CompanyLoanAdditionalQuestionsComponent;
    var AnswersModel = /** @class */ (function () {
        function AnswersModel() {
        }
        return AnswersModel;
    }());
    var AnswerTableItem = /** @class */ (function () {
        function AnswerTableItem() {
        }
        return AnswerTableItem;
    }());
})(CompanyLoanAdditionalQuestionsComponentNs || (CompanyLoanAdditionalQuestionsComponentNs = {}));
angular.module('ntech.components').component('companyLoanAdditionalQuestions', new CompanyLoanAdditionalQuestionsComponentNs.CompanyLoanAdditionalQuestionsComponent());
