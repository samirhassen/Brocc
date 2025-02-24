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
var CompanyLoanInitialCreditCheckViewComponentNs;
(function (CompanyLoanInitialCreditCheckViewComponentNs) {
    var CompanyLoanInitialCreditCheckViewController = /** @class */ (function (_super) {
        __extends(CompanyLoanInitialCreditCheckViewController, _super);
        function CompanyLoanInitialCreditCheckViewController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.onBack = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var target = _this.initialData.backTarget
                    ? NavigationTargetHelper.createCodeTarget(_this.initialData.backTarget)
                    : NavigationTargetHelper.createCrossModule('CompanyLoanApplication', { applicationNr: initialData.applicationNr });
                NavigationTargetHelper.handleBack(target, _this.apiClient, _this.$q, { applicationNr: initialData.applicationNr });
            };
            return _this;
        }
        CompanyLoanInitialCreditCheckViewController.prototype.componentName = function () {
            return 'companyLoanInitialCreditCheckView';
        };
        CompanyLoanInitialCreditCheckViewController.prototype.headerClassFromStatus = function (isAccepted) {
            return { 'text-success': isAccepted, 'text-danger': !isAccepted };
        };
        CompanyLoanInitialCreditCheckViewController.prototype.iconClassFromStatus = function (isAccepted) {
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': !isAccepted, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': !isAccepted };
        };
        CompanyLoanInitialCreditCheckViewController.prototype.getDisplayRejectionReason = function (reasonName) {
            var n = null;
            var mapping = this.m.recommendationInitialData.rejectionReasonToDisplayNameMapping;
            if (mapping) {
                n = mapping[reasonName];
            }
            return n ? n : reasonName;
        };
        CompanyLoanInitialCreditCheckViewController.prototype.init = function (applicationNr, result) {
            this.m = {
                applicationNr: applicationNr,
                recommendationInitialData: {
                    applicationNr: applicationNr,
                    recommendation: result.Decision.Recommendation,
                    apiClient: this.initialData.apiClient,
                    companyLoanApiClient: this.initialData.companyLoanApiClient,
                    creditUrlPattern: this.initialData.creditUrlPattern,
                    isTest: this.initialData.isTest,
                    rejectionReasonToDisplayNameMapping: this.initialData.rejectionReasonToDisplayNameMapping,
                    rejectionRuleToReasonNameMapping: this.initialData.rejectionRuleToReasonNameMapping,
                    isEditAllowed: false,
                    navigationTargetToHere: NTechNavigationTarget.createCrossModuleNavigationTargetCode("CompanyLoanViewCreditCheckDetails", { applicationNr: applicationNr })
                },
                d: result.Decision
            };
        };
        CompanyLoanInitialCreditCheckViewController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(this.initialData.applicationNr).then(function (x) {
                _this.init(_this.initialData.applicationNr, x);
            });
        };
        CompanyLoanInitialCreditCheckViewController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CompanyLoanInitialCreditCheckViewController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanInitialCreditCheckViewComponentNs.CompanyLoanInitialCreditCheckViewController = CompanyLoanInitialCreditCheckViewController;
    var CompanyLoanInitialCreditCheckViewComponent = /** @class */ (function () {
        function CompanyLoanInitialCreditCheckViewComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckViewController;
            this.templateUrl = 'company-loan-initial-credit-check-view.html';
        }
        return CompanyLoanInitialCreditCheckViewComponent;
    }());
    CompanyLoanInitialCreditCheckViewComponentNs.CompanyLoanInitialCreditCheckViewComponent = CompanyLoanInitialCreditCheckViewComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanInitialCreditCheckViewComponentNs.Model = Model;
})(CompanyLoanInitialCreditCheckViewComponentNs || (CompanyLoanInitialCreditCheckViewComponentNs = {}));
angular.module('ntech.components').component('companyLoanInitialCreditCheckView', new CompanyLoanInitialCreditCheckViewComponentNs.CompanyLoanInitialCreditCheckViewComponent());
