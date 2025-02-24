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
var CompanyLoanInitialCreditCheckComponentNs;
(function (CompanyLoanInitialCreditCheckComponentNs) {
    var CompanyLoanInitialCreditCheckController = /** @class */ (function (_super) {
        __extends(CompanyLoanInitialCreditCheckController, _super);
        function CompanyLoanInitialCreditCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        CompanyLoanInitialCreditCheckController.prototype.componentName = function () {
            return 'companyLoanInitialCreditCheck';
        };
        CompanyLoanInitialCreditCheckController.prototype.onChanges = function () {
            var _this = this;
            this.d = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            if (!this.initialData.step.isStatusInitial(this.initialData.applicationInfo)) {
                this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                    _this.d = x;
                });
            }
        };
        CompanyLoanInitialCreditCheckController.prototype.isNewCreditCheckPossibleButNeedsReactivate = function () {
            var a = this.initialData.applicationInfo;
            return !a.IsActive && !a.IsFinalDecisionMade && (a.IsCancelled || a.IsRejected) && !a.HasLockedAgreement;
        };
        CompanyLoanInitialCreditCheckController.prototype.isNewCreditCheckPossible = function () {
            var a = this.initialData.applicationInfo;
            var isActiveAndOk = a.IsActive && !a.HasLockedAgreement;
            return isActiveAndOk || this.isNewCreditCheckPossibleButNeedsReactivate();
        };
        CompanyLoanInitialCreditCheckController.prototype.isViewCreditCheckDetailsPossible = function () {
            if (!this.initialData) {
                return false;
            }
            return !this.initialData.step.isStatusInitial(this.initialData.applicationInfo);
        };
        CompanyLoanInitialCreditCheckController.prototype.newCreditCheck = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isNewCreditCheckPossible()) {
                return;
            }
            var doCheck = function () {
                location.href = "/Ui/CompanyLoan/NewCreditCheck?applicationNr=".concat(_this.initialData.applicationInfo.ApplicationNr, "&backTarget=").concat(_this.initialData.navigationTargetCodeToHere);
            };
            if (this.isNewCreditCheckPossibleButNeedsReactivate()) {
                this.apiClient.reactivateCancelledApplication(this.initialData.applicationInfo.ApplicationNr).then(function () {
                    doCheck();
                });
            }
            else {
                doCheck();
            }
        };
        CompanyLoanInitialCreditCheckController.prototype.viewCreditCheckDetails = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.isViewCreditCheckDetailsPossible()) {
                return;
            }
            location.href = "/Ui/CompanyLoan/ViewCreditCheckDetails?applicationNr=".concat(this.initialData.applicationInfo.ApplicationNr, "&backTarget=").concat(this.initialData.navigationTargetCodeToHere);
        };
        CompanyLoanInitialCreditCheckController.prototype.getRejectionReasonDisplayName = function (reasonName) {
            var n = this.initialData && this.initialData.rejectionReasonToDisplayNameMapping ? this.initialData.rejectionReasonToDisplayNameMapping[reasonName] : null;
            return n ? n : reasonName;
        };
        CompanyLoanInitialCreditCheckController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanInitialCreditCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanInitialCreditCheckComponentNs.CompanyLoanInitialCreditCheckController = CompanyLoanInitialCreditCheckController;
    var CompanyLoanInitialCreditCheckComponent = /** @class */ (function () {
        function CompanyLoanInitialCreditCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckController;
            this.templateUrl = 'company-loan-initial-credit-check.html';
        }
        return CompanyLoanInitialCreditCheckComponent;
    }());
    CompanyLoanInitialCreditCheckComponentNs.CompanyLoanInitialCreditCheckComponent = CompanyLoanInitialCreditCheckComponent;
})(CompanyLoanInitialCreditCheckComponentNs || (CompanyLoanInitialCreditCheckComponentNs = {}));
angular.module('ntech.components').component('companyLoanInitialCreditCheck', new CompanyLoanInitialCreditCheckComponentNs.CompanyLoanInitialCreditCheckComponent());
