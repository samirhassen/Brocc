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
var MortgageApplicationDirectDebitCheckComponentNs;
(function (MortgageApplicationDirectDebitCheckComponentNs) {
    var MortgageApplicationDirectDebitCheckController = /** @class */ (function (_super) {
        __extends(MortgageApplicationDirectDebitCheckController, _super);
        function MortgageApplicationDirectDebitCheckController($http, $q, ntechComponentService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$timeout = $timeout;
            _this.getApplicant = function (result, applicantNr) {
                if (applicantNr == 1) {
                    return result.Applicant1;
                }
                else if (applicantNr == 2) {
                    return result.Applicant2;
                }
                else {
                    return null;
                }
            };
            return _this;
        }
        MortgageApplicationDirectDebitCheckController.prototype.componentName = function () {
            return 'mortgageApplicationDirectDebitCheck';
        };
        MortgageApplicationDirectDebitCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchMortgageLoanDirectDebitCheckStatus(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                _this.m = _this.createLocalData(result, _this.initialData.applicationInfo);
            });
        };
        MortgageApplicationDirectDebitCheckController.prototype.getPersonModel = function (result, applicantNr) {
            var a = this.getApplicant(result, applicantNr);
            return {
                ApplicantNr: applicantNr,
                BirthDate: a.BirthDate,
                FirstName: a.FirstName
            };
        };
        MortgageApplicationDirectDebitCheckController.prototype.createLocalData = function (result, applicationInfo) {
            var m = new Model();
            m.latestResult = result;
            if (!this.initialData.workflowModel.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                return m;
            }
            m.ro = new ReadonlyDataModel();
            if (result.AdditionalQuestionAccountOwnerApplicantNr) {
                var aqa = this.getApplicant(result, result.AdditionalQuestionAccountOwnerApplicantNr);
                m.ro.AdditionalQuestionAccountOwner = this.getPersonModel(result, result.AdditionalQuestionAccountOwnerApplicantNr);
                m.ro.AdditionalQuestionPaymentNumber = aqa.StandardPaymentNumber;
            }
            if (result.SignedDirectDebitConsentDocumentDownloadUrl) {
                m.ro.SignedDirectDebitConsentDocumentDownloadUrl = result.SignedDirectDebitConsentDocumentDownloadUrl;
            }
            if (result.AdditionalQuestionsBankAccountNr) {
                m.ro.AdditionalQuestionsBankAccountNr = result.AdditionalQuestionsBankAccountNr;
                m.ro.AdditionalQuestionsBankName = result.AdditionalQuestionsBankName;
            }
            m.dd = new DirectDebitSavedStateModel();
            if (result.DirectDebitCheckStatus) {
                m.dd.DirectDebitCheckStatus = result.DirectDebitCheckStatus;
                m.dd.DirectDebitCheckStatusDate = result.DirectDebitCheckStatusDate;
            }
            if (result.DirectDebitCheckAccountOwnerApplicantNr) {
                var dqa = this.getApplicant(result, result.DirectDebitCheckAccountOwnerApplicantNr);
                m.dd.AccountOwner = this.getPersonModel(result, result.DirectDebitCheckAccountOwnerApplicantNr);
                m.dd.PaymentNumber = dqa.StandardPaymentNumber;
            }
            if (result.DirectDebitCheckBankAccountNr) {
                m.dd.BankAccountNr = result.DirectDebitCheckBankAccountNr;
                m.dd.BankName = result.DirectDebitCheckBankName;
            }
            m.dd.IsEditAllowed = result.IsEditAllowed;
            m.allOwners = [];
            if (result.Applicant1) {
                m.allOwners.push(this.getPersonModel(result, 1));
            }
            if (result.Applicant2) {
                m.allOwners.push(this.getPersonModel(result, 2));
            }
            return m;
        };
        MortgageApplicationDirectDebitCheckController.prototype.beginEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.edit = {
                AccountOwnerApplicantNr: this.m.dd.AccountOwner ?
                    this.m.dd.AccountOwner.ApplicantNr.toString() :
                    (this.m.ro.AdditionalQuestionAccountOwner ? this.m.ro.AdditionalQuestionAccountOwner.ApplicantNr.toString() : null),
                Status: this.m.dd.DirectDebitCheckStatus ? this.m.dd.DirectDebitCheckStatus : 'Initial',
                BankAccountNr: this.m.dd.BankAccountNr ? this.m.dd.BankAccountNr : this.m.ro.AdditionalQuestionsBankAccountNr,
                BankAccountValidationResult: null,
                WasAccountOwnerApplicantNrRecentlyChanged: false
            };
        };
        MortgageApplicationDirectDebitCheckController.prototype.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.edit = null;
        };
        MortgageApplicationDirectDebitCheckController.prototype.commitEdit = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.updateMortgageLoanDirectDebitCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.m.edit.Status, this.m.edit.BankAccountNr, parseInt(this.m.edit.AccountOwnerApplicantNr)).then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageApplicationDirectDebitCheckController.prototype.onBankAccountEdited = function () {
            var _this = this;
            if (!this.m.edit) {
                return;
            }
            this.m.edit.BankAccountValidationResult = null;
            if (!this.m.edit.BankAccountNr) {
                return;
            }
            this.apiClient.validateBankAccountNr(this.m.edit.BankAccountNr).then(function (result) {
                _this.m.edit.BankAccountValidationResult = result;
            });
        };
        MortgageApplicationDirectDebitCheckController.prototype.onAccountOwnerApplicantNrEdited = function () {
            var _this = this;
            if (this.m.edit) {
                this.m.edit.WasAccountOwnerApplicantNrRecentlyChanged = true;
                this.$timeout(function () {
                    if (_this.m.edit) {
                        _this.m.edit.WasAccountOwnerApplicantNrRecentlyChanged = false;
                    }
                }, 400);
            }
        };
        MortgageApplicationDirectDebitCheckController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout'];
        return MortgageApplicationDirectDebitCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationDirectDebitCheckComponentNs.MortgageApplicationDirectDebitCheckController = MortgageApplicationDirectDebitCheckController;
    var MortgageApplicationDirectDebitCheckComponent = /** @class */ (function () {
        function MortgageApplicationDirectDebitCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationDirectDebitCheckController;
            this.templateUrl = 'mortgage-application-direct-debit-check.html';
        }
        return MortgageApplicationDirectDebitCheckComponent;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.MortgageApplicationDirectDebitCheckComponent = MortgageApplicationDirectDebitCheckComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.Model = Model;
    var ReadonlyDataModel = /** @class */ (function () {
        function ReadonlyDataModel() {
        }
        return ReadonlyDataModel;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.ReadonlyDataModel = ReadonlyDataModel;
    var DirectDebitSavedStateModel = /** @class */ (function () {
        function DirectDebitSavedStateModel() {
        }
        return DirectDebitSavedStateModel;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.DirectDebitSavedStateModel = DirectDebitSavedStateModel;
    var PersonNameAndDateModel = /** @class */ (function () {
        function PersonNameAndDateModel() {
        }
        return PersonNameAndDateModel;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.PersonNameAndDateModel = PersonNameAndDateModel;
    var EditModel = /** @class */ (function () {
        function EditModel() {
        }
        return EditModel;
    }());
    MortgageApplicationDirectDebitCheckComponentNs.EditModel = EditModel;
})(MortgageApplicationDirectDebitCheckComponentNs || (MortgageApplicationDirectDebitCheckComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationDirectDebitCheck', new MortgageApplicationDirectDebitCheckComponentNs.MortgageApplicationDirectDebitCheckComponent());
