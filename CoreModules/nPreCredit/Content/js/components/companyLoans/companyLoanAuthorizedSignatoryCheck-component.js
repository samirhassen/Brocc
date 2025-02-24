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
var CompanyLoanAuthorizedSignatoryCheckComponentNs;
(function (CompanyLoanAuthorizedSignatoryCheckComponentNs) {
    var CompanyLoanAuthorizedSignatoryCheckController = /** @class */ (function (_super) {
        __extends(CompanyLoanAuthorizedSignatoryCheckController, _super);
        function CompanyLoanAuthorizedSignatoryCheckController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanAuthorizedSignatoryCheckController.prototype.componentName = function () {
            return 'companyLoanAuthorizedSignatoryCheck';
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.toggleCompanyLoanListBasedStatus = function () {
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
        CompanyLoanAuthorizedSignatoryCheckController.prototype.checkForRequiredCustomerProperties = function (requiredCustomerProperties, customerIds, messageIfNotOk, doOnOk) {
            var _this = this;
            this.apiClient.fetchCustomerItemsBulk(customerIds, requiredCustomerProperties).then(function (customersDataResult) {
                var areAllOk = true;
                for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                    var customerId = customerIds_1[_i];
                    for (var _a = 0, requiredCustomerProperties_1 = requiredCustomerProperties; _a < requiredCustomerProperties_1.length; _a++) {
                        var propertyName = requiredCustomerProperties_1[_a];
                        if (!_this.containsCustomerProperty(customersDataResult, customerId, propertyName)) {
                            areAllOk = false;
                        }
                    }
                }
                if (!areAllOk) {
                    toastr.warning(messageIfNotOk);
                    return;
                }
                doOnOk();
            });
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.isToggleCompanyLoanAuthorizedSignatoryCheckStatusAllowed = function () {
            if (!this.initialData) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && this.initialData.step.areAllStepBeforeThisAccepted(ai) && !ai.IsPartiallyApproved && !ai.HasLockedAgreement;
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.getCompanyLoanAuthorizedSignatoryCheckStatus = function () {
            if (!this.initialData) {
                return null;
            }
            return this.initialData.step.getStepStatus(this.initialData.applicationInfo);
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.toggleCompanyLoanAuthorizedSignatoryCheckStatus = function (evt) {
            var _this = this;
            if (evt) {
                evt.stopPropagation();
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerApplicationListMembers(this.initialData.applicationInfo.ApplicationNr, 'companyLoanAuthorizedSignatory').then(function (listMembersResult) {
                var customerIds = listMembersResult.CustomerIds;
                if (customerIds.length === 0) {
                    toastr.warning('At least one authorized signatory is required');
                    return;
                }
                _this.checkForRequiredCustomerProperties(['firstName', 'lastName', 'email', 'phone'], customerIds, 'Authorized signatories are missing name, email or phone', function () {
                    _this.toggleCompanyLoanListBasedStatus();
                });
            });
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.containsCustomerProperty = function (result, customerId, propertyName) {
            if (!result) {
                return false;
            }
            var d = result[customerId];
            if (!d) {
                return false;
            }
            return !!d[propertyName];
        };
        CompanyLoanAuthorizedSignatoryCheckController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            this.m = {
                authorizedSignatoryCheckInitialData: {
                    applicationInfo: ai,
                    header: "Authorized Signatories",
                    backUrl: this.initialData.urlToHereFromOtherModule,
                    backTarget: this.initialData.navigationTargetCodeToHere,
                    editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanAuthorizedSignatory', this.apiClient),
                    multipleEditorService: {
                        editorService: new ApplicationCustomerListComponentNs.CustomerApplicationListEditorService(ai.ApplicationNr, 'companyLoanBeneficialOwner', this.apiClient),
                        header: "Beneficial Owners",
                        includeCompanyRoles: true
                    },
                    isEditable: ai.IsActive && !ai.IsPartiallyApproved && this.initialData.step.isStatusInitial(ai)
                }
            };
        };
        CompanyLoanAuthorizedSignatoryCheckController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanAuthorizedSignatoryCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanAuthorizedSignatoryCheckComponentNs.CompanyLoanAuthorizedSignatoryCheckController = CompanyLoanAuthorizedSignatoryCheckController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanAuthorizedSignatoryCheckComponentNs.Model = Model;
    var CompanyLoanAuthorizedSignatoryCheckComponent = /** @class */ (function () {
        function CompanyLoanAuthorizedSignatoryCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanAuthorizedSignatoryCheckController;
            this.template = "<div> \n                    <application-customer-list initial-data=\"$ctrl.m.authorizedSignatoryCheckInitialData\"></application-customer-list>\n                    \n                    <div ng-show=\"$ctrl.isToggleCompanyLoanAuthorizedSignatoryCheckStatusAllowed()\">\n                        <label class=\"pr-2\">Company connection control {{$ctrl.getCompanyLoanAuthorizedSignatoryCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>\n\n                        <label class=\"n-toggle\">\n                            <input type=\"checkbox\" ng-checked=\"$ctrl.getCompanyLoanAuthorizedSignatoryCheckStatus() === 'Accepted'\" ng-click=\"$ctrl.toggleCompanyLoanAuthorizedSignatoryCheckStatus($event)\" />\n                            <span class=\"n-slider\"></span>\n                        </label>\n                    </div>\n\n                    </div>";
        }
        return CompanyLoanAuthorizedSignatoryCheckComponent;
    }());
    CompanyLoanAuthorizedSignatoryCheckComponentNs.CompanyLoanAuthorizedSignatoryCheckComponent = CompanyLoanAuthorizedSignatoryCheckComponent;
})(CompanyLoanAuthorizedSignatoryCheckComponentNs || (CompanyLoanAuthorizedSignatoryCheckComponentNs = {}));
angular.module('ntech.components').component('companyLoanAuthorizedSignatoryCheck', new CompanyLoanAuthorizedSignatoryCheckComponentNs.CompanyLoanAuthorizedSignatoryCheckComponent());
