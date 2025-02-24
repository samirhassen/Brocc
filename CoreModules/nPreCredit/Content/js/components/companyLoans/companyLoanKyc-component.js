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
var CompanyLoanKycComponentNs;
(function (CompanyLoanKycComponentNs) {
    var nonWorkflowStepNames = [
        'CompanyLoanLexPressScreen',
        'CompanyLoanPepSanctionScreen',
    ];
    var CompanyLoanKycController = /** @class */ (function (_super) {
        __extends(CompanyLoanKycController, _super);
        function CompanyLoanKycController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanKycController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        CompanyLoanKycController.prototype.isEditAllowed = function () {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && !ai.IsPartiallyApproved && !ai.IsFinalDecisionMade
                && this.initialData.step.areAllStepBeforeThisAccepted(ai);
        };
        CompanyLoanKycController.prototype.isStepComplete = function (stepName) {
            if (!this.initialData || !this.initialData.applicationInfo) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return WorkflowHelper.isStepAccepted(stepName, ai);
        };
        CompanyLoanKycController.prototype.onStatusChanged = function (evt, forceDoneStepName) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var i = this.initialData;
            var ai = i.applicationInfo;
            var arePreviousStepsOk = i.step.areAllStepBeforeThisAccepted(ai);
            var areLocalStepsOk = WorkflowHelper.areAllStepsAccepted(nonWorkflowStepNames, ai, forceDoneStepName);
            var areAllPepSanctionDone = NTechLinq.all(this.m.Customers, function (x) { return x.IsPepSanctionDone; });
            if (arePreviousStepsOk && areLocalStepsOk && areAllPepSanctionDone) {
                i.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, i.step.stepName, 'Accepted', 'KYC approved', i.step.stepName + 'Accepted', 'AcceptCustomerCheck').then(function () {
                    _this.signalReloadRequired();
                });
            }
            else {
                this.signalReloadRequired();
            }
        };
        CompanyLoanKycController.prototype.approveLexPress = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.switchApplicationListStatus(this.initialData.applicationInfo.ApplicationNr, 'CompanyLoanLexPressScreen', 'Accepted', 'Lex Press screening done').then(function () {
                _this.onStatusChanged(null, 'CompanyLoanLexPressScreen');
            });
        };
        CompanyLoanKycController.prototype.approvePepSanctionScreen = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.switchApplicationListStatus(this.initialData.applicationInfo.ApplicationNr, 'CompanyLoanPepSanctionScreen', 'Accepted', 'Pep/Sanction screening done').then(function () {
                _this.onStatusChanged(null, 'CompanyLoanPepSanctionScreen');
            });
        };
        CompanyLoanKycController.prototype.screenNow = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.kycScreenBatchByApplicationNr(this.initialData.applicationInfo.ApplicationNr, moment(this.initialData.today).toDate()).then(function (success) {
                if (!success) {
                    toastr.error('Could not screen customers.');
                }
                else {
                    _this.apiClient.switchApplicationListStatus(_this.initialData.applicationInfo.ApplicationNr, 'CompanyLoanPepSanctionScreen', 'Accepted', 'Pep/Sanction screening done').then(function () {
                        _this.onStatusChanged(null, 'CompanyLoanPepSanctionScreen');
                    });
                }
            });
        };
        CompanyLoanKycController.prototype.isToggleCompanyLoanCollateralCheckStatusAllowed = function () {
            if (!this.initialData) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && this.m && this.m.IsCompanyCustomerOk;
        };
        CompanyLoanKycController.prototype.OverrideKYC = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var i = this.initialData;
            var ai = i.applicationInfo;
            var arePreviousStepsOk = i.step.areAllStepBeforeThisAccepted(ai);
            if (arePreviousStepsOk) {
                i.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, i.step.stepName, 'Accepted', 'KYC approved', i.step.stepName + 'Accepted', 'AcceptCustomerCheck').then(function () {
                    _this.signalReloadRequired();
                });
            }
        };
        CompanyLoanKycController.prototype.getGlyphIconClass = function (stepName) {
            if (this.isStepComplete(stepName)) {
                return 'glyphicon-ok';
            }
            else {
                return 'glyphicon-minus';
            }
        };
        CompanyLoanKycController.prototype.glyphIconClassFromBoolean = function (isAccepted) {
            return isAccepted ? 'glyphicon-ok' : 'glyphicon-minus';
        };
        CompanyLoanKycController.prototype.getCustomerKycManagementUrl = function (customerId) {
            return this.getUiGatewayUrl('nCustomer', 'Ui/KycManagement/Manage', [
                ['customerId', customerId.toString()],
                ['backTarget', this.initialData.navigationTargetCodeToHere]
            ]);
        };
        CompanyLoanKycController.prototype.componentName = function () {
            return 'companyLoanKyc';
        };
        CompanyLoanKycController.prototype.CheckScreenDisable = function () {
            if (this.m == null)
                return true;
            if (this.m.Customers == null)
                return true;
            if (this.m.Customers.length === 0)
                return true;
            return false;
        };
        CompanyLoanKycController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchListCustomersWithKycStatusMethod(this.initialData.applicationInfo.ApplicationNr, ['companyLoanBeneficialOwner', 'companyLoanAuthorizedSignatory']).then(function (x) {
                var m = {
                    Customers: [],
                    IsCompanyCustomerOk: false
                };
                for (var _i = 0, _a = x.Customers; _i < _a.length; _i++) {
                    var customerRaw = _a[_i];
                    var customer = customerRaw;
                    customer.IsPepSanctionDone = _this.isStepComplete('CompanyLoanPepSanctionScreen')
                        && NTechBooleans.isExactlyTrueOrFalse(customer.IsPep)
                        && NTechBooleans.isExactlyTrueOrFalse(customer.IsSanction);
                    m.Customers.push(customer);
                }
                if (_this.isStepComplete('CompanyLoanKycCheck')) {
                    m.IsCompanyCustomerOk = true;
                    _this.m = m;
                }
                else {
                    _this.apiClient.fetchCreditApplicationItemSimple(_this.initialData.applicationInfo.ApplicationNr, ['application.companyCustomerId'], '-').then(function (x) {
                        var companyCustomerId = parseInt(x['application.companyCustomerId']);
                        CompanyLoanCollateralCheckComponentNs.checkForRequiredCustomerProperties([companyCustomerId], true, _this.apiClient).then(function (x) {
                            m.IsCompanyCustomerOk = x.isOk;
                            _this.m = m;
                        });
                    });
                }
            });
        };
        CompanyLoanKycController.prototype.getListDisplayName = function (listName) {
            if (!listName) {
                return '';
            }
            return listName.replace('companyLoan', '');
        };
        CompanyLoanKycController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanKycController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanKycComponentNs.CompanyLoanKycController = CompanyLoanKycController;
    var CompanyLoanKycComponent = /** @class */ (function () {
        function CompanyLoanKycComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanKycController;
            this.templateUrl = 'company-loan-kyc.html';
        }
        return CompanyLoanKycComponent;
    }());
    CompanyLoanKycComponentNs.CompanyLoanKycComponent = CompanyLoanKycComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanKycComponentNs.Model = Model;
})(CompanyLoanKycComponentNs || (CompanyLoanKycComponentNs = {}));
angular.module('ntech.components').component('companyLoanKyc', new CompanyLoanKycComponentNs.CompanyLoanKycComponent());
