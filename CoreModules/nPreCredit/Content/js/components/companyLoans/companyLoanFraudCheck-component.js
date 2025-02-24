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
var CompanyLoanFraudCheckComponentNs;
(function (CompanyLoanFraudCheckComponentNs) {
    var CompanyLoanFraudCheckController = /** @class */ (function (_super) {
        __extends(CompanyLoanFraudCheckController, _super);
        function CompanyLoanFraudCheckController($http, $q, ntechComponentService, modalDialogService, $translate) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            _this.$translate = $translate;
            return _this;
        }
        CompanyLoanFraudCheckController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        CompanyLoanFraudCheckController.prototype.componentName = function () {
            return 'companyLoanFraudCheck';
        };
        CompanyLoanFraudCheckController.prototype.isEditAllowed = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade && this.initialData.step.areAllStepBeforeThisAccepted(ai);
        };
        CompanyLoanFraudCheckController.prototype.isApproveAccountNrAllowed = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return this.m
                && !this.m.isMissingValidBankAccountNr
                && this.initialData.step.isStatusInitial(ai)
                && this.initialData.applicationInfo.ListNames.indexOf('CompanyLoanBankAccountNrCheck_Accepted') < 0;
        };
        CompanyLoanFraudCheckController.prototype.isAccountNrApproved = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.ListNames.indexOf('CompanyLoanBankAccountNrCheck_Accepted') >= 0;
        };
        CompanyLoanFraudCheckController.prototype.approveBankAccountNr = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var ai = this.initialData.applicationInfo;
            if (!ai) {
                return;
            }
            this.apiClient.switchApplicationListStatus(ai.ApplicationNr, 'CompanyLoanBankAccountNrCheck', 'Accepted', 'Company bank account verified').then(function () {
                _this.approveFraudCheckIfAllStepsAccepted();
            });
        };
        CompanyLoanFraudCheckController.prototype.approveFraudCheckIfAllStepsAccepted = function () {
            var _this = this;
            var applicationNr = this.initialData.applicationInfo.ApplicationNr;
            var requiredListNames = ['CompanyLoanBankAccountNrCheck_Accepted'];
            this.apiClient.fetchApplicationInfo(applicationNr).then(function (x) {
                var isOk = true;
                for (var _i = 0, requiredListNames_1 = requiredListNames; _i < requiredListNames_1.length; _i++) {
                    var n = requiredListNames_1[_i];
                    if (x.ListNames.indexOf(n) < 0) {
                        isOk = false;
                    }
                }
                if (isOk) {
                    _this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(applicationNr, _this.initialData.step.stepName, 'Accepted', 'Fraud check accepted', _this.initialData.step.stepName + 'Accepted', 'AcceptFraudCheck').then(function () {
                        _this.signalReloadRequired();
                    });
                }
                else {
                    _this.signalReloadRequired();
                }
            });
        };
        CompanyLoanFraudCheckController.prototype.getBankGiroUrl = function () {
            if (this.m && !this.m.isMissingValidBankAccountNr && this.m.bankAccountNrType === 'BankGiroSe') {
                return "https://www.bankgirot.se/sok-bankgironummer/?bgnr=".concat(this.m.bankAccountNrUsable, "&orgnr=").concat(this.m.companyOrgnr, "#bgsearchform");
            }
            else {
                return 'https://www.bankgirot.se/sok-bankgironummer/#bgsearchform';
            }
        };
        CompanyLoanFraudCheckController.prototype.getBankAccountEditUrl = function () {
            if (!this.initialData) {
                return null;
            }
            var ai = this.initialData.applicationInfo;
            var isEditAllowed = ai.IsActive && this.initialData.step.isStatusInitial(ai);
            return "/Ui/CompanyLoan/Application/EditItem?applicationNr=".concat(ai.ApplicationNr, "&dataSourceName=BankAccountTypeAndNr&itemName=BankAccountTypeAndNr&ro=").concat(isEditAllowed ? 'False' : 'True', "&backUrl=").concat(this.initialData.urlToHere);
        };
        CompanyLoanFraudCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            if (!this.initialData.step.areAllStepBeforeThisAccepted(this.initialData.applicationInfo)) {
                this.m = {
                    isWaitingForPreviousSteps: true,
                    bankAccountNrReadable: null,
                    bankAccountNrType: null,
                    bankAccountNrTypeReadable: null,
                    bankAccountNrUsable: null,
                    isMissingValidBankAccountNr: null,
                    companyOrgnr: null
                };
                return;
            }
            this.apiClient.fetchCreditApplicationItemSimple(this.initialData.applicationInfo.ApplicationNr, ['application.bankAccountNr', 'application.bankAccountNrType', 'application.companyOrgnr'], 'missing').then(function (x) {
                var bankAccountNr = x['application.bankAccountNr'];
                var bankAccountNrType = x['application.bankAccountNrType'];
                var companyOrgnr = x['application.companyOrgnr'];
                if (bankAccountNr === 'missing' || bankAccountNrType === 'missing') {
                    _this.m = {
                        bankAccountNrType: null,
                        bankAccountNrTypeReadable: null,
                        bankAccountNrReadable: null,
                        bankAccountNrUsable: null,
                        isMissingValidBankAccountNr: true,
                        companyOrgnr: null,
                        isWaitingForPreviousSteps: false
                    };
                }
                else {
                    _this.apiClient.validateBankAccountNr(bankAccountNr, bankAccountNrType).then(function (y) {
                        if (y.isValid) {
                            var readable = y.validAccount.displayNr;
                            if (y.validAccount.bankAccountNrType === 'BankAccountSe') {
                                readable = "Clearing: ".concat(y.validAccount.clearingNr, " Account: ").concat(y.validAccount.accountNr, " Bank: ").concat(y.validAccount.bankName);
                            }
                            else if (y.validAccount.bankAccountNrType === 'IBANFi') {
                                readable = "Nr: ".concat(y.validAccount.displayNr, " Bic: ").concat(y.validAccount.bic, " Bank: ").concat(y.validAccount.bankName);
                            }
                            _this.m = {
                                bankAccountNrType: y.validAccount.bankAccountNrType,
                                bankAccountNrTypeReadable: _this.$translate.instant('enum.bankAccountNumberTypeCode.' + y.validAccount.bankAccountNrType),
                                bankAccountNrReadable: '(' + readable + ')',
                                bankAccountNrUsable: y.validAccount.normalizedNr,
                                isMissingValidBankAccountNr: false,
                                companyOrgnr: companyOrgnr,
                                isWaitingForPreviousSteps: false
                            };
                        }
                        else {
                            _this.m = {
                                bankAccountNrType: null,
                                bankAccountNrTypeReadable: _this.$translate.instant('enum.bankAccountNumberTypeCode.' + bankAccountNrType),
                                bankAccountNrUsable: bankAccountNr,
                                bankAccountNrReadable: '(invalid!)',
                                isMissingValidBankAccountNr: true,
                                companyOrgnr: null,
                                isWaitingForPreviousSteps: false
                            };
                        }
                    });
                }
            });
        };
        CompanyLoanFraudCheckController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$translate'];
        return CompanyLoanFraudCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanFraudCheckComponentNs.CompanyLoanFraudCheckController = CompanyLoanFraudCheckController;
    var CompanyLoanFraudCheckComponent = /** @class */ (function () {
        function CompanyLoanFraudCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanFraudCheckController;
            this.templateUrl = 'company-loan-fraud-check.html';
        }
        return CompanyLoanFraudCheckComponent;
    }());
    CompanyLoanFraudCheckComponentNs.CompanyLoanFraudCheckComponent = CompanyLoanFraudCheckComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanFraudCheckComponentNs.Model = Model;
})(CompanyLoanFraudCheckComponentNs || (CompanyLoanFraudCheckComponentNs = {}));
angular.module('ntech.components').component('companyLoanFraudCheck', new CompanyLoanFraudCheckComponentNs.CompanyLoanFraudCheckComponent());
