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
var MortgageApplicationAdditionalQuestionsComponentNs;
(function (MortgageApplicationAdditionalQuestionsComponentNs) {
    var MortgageApplicationAdditionalQuestionsController = /** @class */ (function (_super) {
        __extends(MortgageApplicationAdditionalQuestionsController, _super);
        function MortgageApplicationAdditionalQuestionsController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            _this.onDocumentsAddedOrRemoved = function () {
                _this.apiClient.updateMortgageLoanAdditionalQuestionsStatus(_this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                    if (result.WasStatusChanged) {
                        _this.signalReloadRequired();
                    }
                });
            };
            _this.answersDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        MortgageApplicationAdditionalQuestionsController.prototype.applicationNr = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr;
            }
            else {
                return null;
            }
        };
        MortgageApplicationAdditionalQuestionsController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        MortgageApplicationAdditionalQuestionsController.prototype.nrOfApplicants = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.NrOfApplicants;
            }
            else {
                return null;
            }
        };
        MortgageApplicationAdditionalQuestionsController.prototype.showAnswers = function (key, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.a) {
                this.modalDialogService.openDialog(this.answersDialogId);
            }
            else {
                this.apiClient.fetchMortgageLoanAdditionalQuestionsDocument(key).then(function (result) {
                    _this.apiClient.fetchMortgageLoanCurrentLoans(_this.applicationNr()).then(function (loansResult) {
                        _this.a = {
                            answersDocument: result,
                            currentLoansModel: loansResult
                        };
                        _this.modalDialogService.openDialog(_this.answersDialogId);
                    });
                });
            }
        };
        MortgageApplicationAdditionalQuestionsController.prototype.componentName = function () {
            return 'mortgageApplicationAdditionalQuestions';
        };
        MortgageApplicationAdditionalQuestionsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            this.a = null;
            this.documentsInitialData = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchMortageLoanAdditionalQuestionsStatus(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                _this.m = result;
            });
            this.documentsInitialData = new ApplicationDocumentsComponentNs.InitialData(this.initialData.applicationInfo)
                .addSharedDocument('MortgageLoanCustomerAmortizationPlan', 'Amortization basis')
                .addSharedDocument('MortgageLoanLagenhetsutdrag', 'L\u00e4genhetsutdrag');
        };
        MortgageApplicationAdditionalQuestionsController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationAdditionalQuestionsController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationAdditionalQuestionsComponentNs.MortgageApplicationAdditionalQuestionsController = MortgageApplicationAdditionalQuestionsController;
    var MortgageApplicationAdditionalQuestionsComponent = /** @class */ (function () {
        function MortgageApplicationAdditionalQuestionsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationAdditionalQuestionsController;
            this.templateUrl = 'mortgage-application-additional-questions.html';
        }
        return MortgageApplicationAdditionalQuestionsComponent;
    }());
    MortgageApplicationAdditionalQuestionsComponentNs.MortgageApplicationAdditionalQuestionsComponent = MortgageApplicationAdditionalQuestionsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationAdditionalQuestionsComponentNs.InitialData = InitialData;
    var AnswersModel = /** @class */ (function () {
        function AnswersModel() {
        }
        return AnswersModel;
    }());
})(MortgageApplicationAdditionalQuestionsComponentNs || (MortgageApplicationAdditionalQuestionsComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationAdditionalQuestions', new MortgageApplicationAdditionalQuestionsComponentNs.MortgageApplicationAdditionalQuestionsComponent());
