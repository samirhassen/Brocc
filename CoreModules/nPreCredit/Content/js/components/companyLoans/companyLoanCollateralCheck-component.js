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
var CompanyLoanCollateralCheckComponentNs;
(function (CompanyLoanCollateralCheckComponentNs) {
    var CompanyLoanCollateralCheckController = /** @class */ (function (_super) {
        __extends(CompanyLoanCollateralCheckController, _super);
        function CompanyLoanCollateralCheckController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanCollateralCheckController.prototype.componentName = function () {
            return 'companyLoanCollateralCheck';
        };
        CompanyLoanCollateralCheckController.prototype.isToggleCompanyLoanCollateralCheckStatusAllowed = function () {
            if (!this.initialData) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && !ai.IsPartiallyApproved && !ai.HasLockedAgreement;
        };
        CompanyLoanCollateralCheckController.prototype.getCompanyLoanCollateralCheckStatus = function () {
            if (!this.initialData) {
                return null;
            }
            return this.initialData.step.getStepStatus(this.initialData.applicationInfo);
        };
        CompanyLoanCollateralCheckController.prototype.toggleCompanyLoanCollateralCheckStatus = function (evt) {
            var _this = this;
            if (evt) {
                evt.stopPropagation();
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerApplicationListMembers(this.initialData.applicationInfo.ApplicationNr, 'companyLoanCollateral').then(function (listMembersResult) {
                var customerIds = listMembersResult.CustomerIds;
                var onOk = function () {
                    _this.toggleCompanyLoanListBasedStatus();
                };
                if (customerIds.length === 0) {
                    onOk();
                }
                else {
                    checkForRequiredCustomerProperties(customerIds, false, _this.apiClient).then(function (x) {
                        if (x.isOk) {
                            onOk();
                        }
                        else {
                            toastr.warning('Collaterals are missing name, email, phone or adress');
                        }
                    });
                }
            });
        };
        CompanyLoanCollateralCheckController.prototype.toggleCompanyLoanListBasedStatus = function () {
            var _this = this;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var step = this.initialData.step;
            //TODO: This is not a great way of doing this
            this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, step.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(function () {
                _this.signalReloadRequired();
            });
        };
        CompanyLoanCollateralCheckController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            this.m = {
                collateralInitialData: {
                    applicationInfo: ai,
                    backUrl: this.initialData.urlToHereFromOtherModule,
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanCollateral', this.apiClient),
                    isEditable: ai.IsActive && !ai.IsPartiallyApproved && this.initialData.step.isStatusInitial(ai)
                }
            };
        };
        CompanyLoanCollateralCheckController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanCollateralCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanCollateralCheckComponentNs.CompanyLoanCollateralCheckController = CompanyLoanCollateralCheckController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanCollateralCheckComponentNs.Model = Model;
    function checkForRequiredCustomerProperties(customerIds, isCompany, apiClient) {
        var requiredCustomerProperties = ['email', 'addressZipcode'];
        if (isCompany) {
            requiredCustomerProperties.unshift('companyName');
        }
        else {
            requiredCustomerProperties.unshift('phone');
            requiredCustomerProperties.unshift('lastName');
            requiredCustomerProperties.unshift('firstName');
        }
        var containsCustomerProperty = function (result, customerId, propertyName) {
            if (!result) {
                return false;
            }
            var d = result[customerId];
            if (!d) {
                return false;
            }
            return !!d[propertyName];
        };
        return apiClient.fetchCustomerItemsBulk(customerIds, requiredCustomerProperties).then(function (customersDataResult) {
            var areAllOk = true;
            for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                var customerId = customerIds_1[_i];
                for (var _a = 0, requiredCustomerProperties_1 = requiredCustomerProperties; _a < requiredCustomerProperties_1.length; _a++) {
                    var propertyName = requiredCustomerProperties_1[_a];
                    if (!containsCustomerProperty(customersDataResult, customerId, propertyName)) {
                        areAllOk = false;
                    }
                }
            }
            return { isOk: areAllOk };
        });
    }
    CompanyLoanCollateralCheckComponentNs.checkForRequiredCustomerProperties = checkForRequiredCustomerProperties;
    var CompanyLoanCollateralCheckComponent = /** @class */ (function () {
        function CompanyLoanCollateralCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanCollateralCheckController;
            this.template = "<div>\n                    <application-customer-list initial-data=\"$ctrl.m.collateralInitialData\"></application-customer-list>\n\n                    <div ng-show=\"$ctrl.isToggleCompanyLoanCollateralCheckStatusAllowed()\">\n                        <label class=\"pr-2\">Collateral control {{$ctrl.getCompanyLoanCollateralCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>\n\n                        <label class=\"n-toggle\">\n                            <input type=\"checkbox\" ng-checked=\"$ctrl.getCompanyLoanCollateralCheckStatus() === 'Accepted'\" ng-click=\"$ctrl.toggleCompanyLoanCollateralCheckStatus($event)\" />\n                            <span class=\"n-slider\"></span>\n                        </label>\n                    </div>\n\n                    </div>";
        }
        return CompanyLoanCollateralCheckComponent;
    }());
    CompanyLoanCollateralCheckComponentNs.CompanyLoanCollateralCheckComponent = CompanyLoanCollateralCheckComponent;
})(CompanyLoanCollateralCheckComponentNs || (CompanyLoanCollateralCheckComponentNs = {}));
angular.module('ntech.components').component('companyLoanCollateralCheck', new CompanyLoanCollateralCheckComponentNs.CompanyLoanCollateralCheckComponent());
