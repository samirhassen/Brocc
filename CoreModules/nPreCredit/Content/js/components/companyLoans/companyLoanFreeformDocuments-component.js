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
var CompanyLoanFreeformDocumentsComponentNs;
(function (CompanyLoanFreeformDocumentsComponentNs) {
    var CompanyLoanFreeformDocumentsController = /** @class */ (function (_super) {
        __extends(CompanyLoanFreeformDocumentsController, _super);
        function CompanyLoanFreeformDocumentsController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        CompanyLoanFreeformDocumentsController.prototype.componentName = function () {
            return 'companyLoanFreeformDocuments';
        };
        CompanyLoanFreeformDocumentsController.prototype.onChanges = function () {
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            this.m = {
                documentsInitialData: {
                    applicationInfo: ai,
                    isReadOnly: this.initialData.step.isStatusAccepted(ai)
                }
            };
        };
        CompanyLoanFreeformDocumentsController.prototype.getCompanyLoanDocumentCheckStatus = function () {
            if (!this.initialData) {
                return null;
            }
            return this.initialData.step.getStepStatus(this.initialData.applicationInfo);
        };
        CompanyLoanFreeformDocumentsController.prototype.isToggleCompanyLoanDocumentCheckStatusAllowed = function () {
            if (!this.initialData) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
                && this.initialData.step.areAllStepBeforeThisAccepted(ai);
        };
        CompanyLoanFreeformDocumentsController.prototype.toggleCompanyLoanDocumentCheckStatus = function () {
            this.toggleCompanyLoanListBasedStatus();
        };
        CompanyLoanFreeformDocumentsController.prototype.toggleCompanyLoanListBasedStatus = function () {
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
        CompanyLoanFreeformDocumentsController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return CompanyLoanFreeformDocumentsController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanFreeformDocumentsComponentNs.CompanyLoanFreeformDocumentsController = CompanyLoanFreeformDocumentsController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanFreeformDocumentsComponentNs.Model = Model;
    var CompanyLoanFreeformDocumentsComponent = /** @class */ (function () {
        function CompanyLoanFreeformDocumentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanFreeformDocumentsController;
            this.template = "<div ng-if=\"$ctrl.m\">\n                    <application-freeform-documents initial-data=\"$ctrl.m.documentsInitialData\">\n\n                    </application-freeform-documents>\n                    <div class=\"pt-3\" ng-show=\"$ctrl.isToggleCompanyLoanDocumentCheckStatusAllowed()\">\n                        <label class=\"pr-2\">Document control {{$ctrl.getCompanyLoanDocumentCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>\n                        <label class=\"n-toggle\">\n                            <input type=\"checkbox\" ng-checked=\"$ctrl.getCompanyLoanDocumentCheckStatus() === 'Accepted'\" ng-click=\"$ctrl.toggleCompanyLoanDocumentCheckStatus()\" />\n                            <span class=\"n-slider\"></span>\n                        </label>\n                    </div></div>";
        }
        return CompanyLoanFreeformDocumentsComponent;
    }());
    CompanyLoanFreeformDocumentsComponentNs.CompanyLoanFreeformDocumentsComponent = CompanyLoanFreeformDocumentsComponent;
})(CompanyLoanFreeformDocumentsComponentNs || (CompanyLoanFreeformDocumentsComponentNs = {}));
angular.module('ntech.components').component('companyLoanFreeformDocuments', new CompanyLoanFreeformDocumentsComponentNs.CompanyLoanFreeformDocumentsComponent());
