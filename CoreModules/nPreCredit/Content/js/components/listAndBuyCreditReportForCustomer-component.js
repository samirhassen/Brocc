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
var ListAndBuyCreditReportsForCustomerComponentNs;
(function (ListAndBuyCreditReportsForCustomerComponentNs) {
    var ListAndBuyCreditReportsForCustomerController = /** @class */ (function (_super) {
        __extends(ListAndBuyCreditReportsForCustomerController, _super);
        function ListAndBuyCreditReportsForCustomerController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.creditReportDialogId = modalDialogService.generateDialogId();
            return _this;
        }
        ListAndBuyCreditReportsForCustomerController.prototype.componentName = function () {
            return 'listAndBuyCreditReportsForCustomer';
        };
        ListAndBuyCreditReportsForCustomerController.prototype.onChanges = function () {
            this.reload();
        };
        ListAndBuyCreditReportsForCustomerController.prototype.getCustomerId = function (inputCustomerId) {
            var customerIdDeferred = this.$q.defer();
            if (!inputCustomerId) {
                var amdl = this.initialData.applicationNrAndApplicantNr;
                this.apiClient.fetchCustomerComponentInitialData(amdl.applicationNr, amdl.applicantNr, null).then(function (result) {
                    customerIdDeferred.resolve(result.customerId);
                });
            }
            else {
                customerIdDeferred.resolve(this.initialData.customerId);
            }
            return customerIdDeferred.promise;
        };
        ListAndBuyCreditReportsForCustomerController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var customerIdPromise = this.getCustomerId(this.initialData.customerId);
            var providers = this.initialData.listProviders;
            customerIdPromise.then(function (customerId) {
                _this.apiClient.postUsingApiGateway("nCreditReport", "CreditReport/FindForProviders", { providers: providers, customerId: customerId }).then(function (result) {
                    _this.m = ({
                        applicantNr: _this.initialData.applicationNrAndApplicantNr.applicantNr,
                        customerId: customerId,
                        creditReports: result,
                        popupTabledValues: null
                    });
                });
            });
        };
        ListAndBuyCreditReportsForCustomerController.prototype.buyNewSatReport = function (customerId) {
            var _this = this;
            this.apiClient.buyCreditReportForCustomerId(customerId, this.initialData.creditReportProviderName).then(function () {
                _this.reload();
            });
        };
        ListAndBuyCreditReportsForCustomerController.prototype.showCreditReport = function (creditReportId) {
            var _this = this;
            this.modalDialogService.openDialog(this.creditReportDialogId, function () {
                _this.apiClient.postUsingApiGateway("nCreditReport", "CreditReport/FetchTabledValues", { creditReportId: creditReportId }).then(function (result) {
                    _this.m.popupTabledValues = result;
                });
            });
        };
        ListAndBuyCreditReportsForCustomerController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return ListAndBuyCreditReportsForCustomerController;
    }(NTechComponents.NTechComponentControllerBase));
    ListAndBuyCreditReportsForCustomerComponentNs.ListAndBuyCreditReportsForCustomerController = ListAndBuyCreditReportsForCustomerController;
    var CreditReport = /** @class */ (function () {
        function CreditReport() {
        }
        return CreditReport;
    }());
    var TabledValue = /** @class */ (function () {
        function TabledValue() {
        }
        return TabledValue;
    }());
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ListAndBuyCreditReportsForCustomerComponentNs.Model = Model;
    var ListAndBuyCreditReportsForCustomerComponent = /** @class */ (function () {
        function ListAndBuyCreditReportsForCustomerComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ListAndBuyCreditReportsForCustomerController;
            this.template = "\n<div>\n    <h3 class=\"text-center\">Applicant {{$ctrl.m.applicantNr}}</h3>\n    <div style=\"position: relative; height: 1em;\">\n        <div style=\"position: absolute; top: 0px; right: 0px; text-align: right;\">\n            <button class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.buyNewSatReport($ctrl.m.customerId)\">Buy new <span class=\"glyphicon glyphicon-shopping-cart\"></span></button>\n        </div>\n    </div>\n    <table class=\"table\">\n        <thead>\n            <tr>\n                <th>Date</th>\n                <th></th>\n            </tr>\n        </thead>\n        <tbody>\n            <tr ng-if=\"$ctrl.m.creditReports.length === 0\">\n                <td>-</td>\n                <td></td>\n            </tr>\n            <tr ng-repeat=\"cr in $ctrl.m.creditReports\">\n                <td>{{cr.RequestDate | date:'dd.MM.yyyy hh:mm'}}</td>\n                <td style=\"text-align: right;\">\n                    <button ng-disabled=\"!cr.CanFetchTabledValues\" ng-click=\"$ctrl.showCreditReport(cr.CreditReportId)\" class=\"n-direct-btn n-turquoise-btn\">\n                        <span ng-show=\"cr.CanFetchTabledValues\">Show <span class=\"glyphicon glyphicon-resize-full\"></span></span>\n                        <span ng-show=\"!cr.CanFetchTabledValues\">No preview</span>\n                    </button>\n                </td>\n            </tr>\n        </tbody>\n    </table>\n    <modal-dialog dialog-id=\"$ctrl.creditReportDialogId\" dialog-title=\"'Credit Report'\">\n    <div ng-if=\"$ctrl.m.popupTabledValues\">\n        <table class=\"table\">\n            <thead>\n                <tr>\n                    <th>Key</th>\n                    <th>Value</th>\n                </tr>\n            </thead>\n            <tbody>\n                <tr ng-repeat=\"row in $ctrl.m.popupTabledValues\">\n                    <td>{{row.title}}</td>\n                    <td>{{row.value}}</td>\n                </tr>\n            </tbody>\n        </table>\n    </div>\n    </modal-dialog>\n</div>";
        }
        return ListAndBuyCreditReportsForCustomerComponent;
    }());
    ListAndBuyCreditReportsForCustomerComponentNs.ListAndBuyCreditReportsForCustomerComponent = ListAndBuyCreditReportsForCustomerComponent;
})(ListAndBuyCreditReportsForCustomerComponentNs || (ListAndBuyCreditReportsForCustomerComponentNs = {}));
angular.module('ntech.components').component('listAndBuyCreditReportsForCustomer', new ListAndBuyCreditReportsForCustomerComponentNs.ListAndBuyCreditReportsForCustomerComponent());
