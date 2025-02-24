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
var MortgageApplicationInitialCreditCheckComponentNs;
(function (MortgageApplicationInitialCreditCheckComponentNs) {
    var MortgageApplicationInitialCreditCheckController = /** @class */ (function (_super) {
        __extends(MortgageApplicationInitialCreditCheckController, _super);
        function MortgageApplicationInitialCreditCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationInitialCreditCheckController.prototype.applicationNr = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr;
            }
            else {
                return null;
            }
        };
        MortgageApplicationInitialCreditCheckController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        MortgageApplicationInitialCreditCheckController.prototype.nrOfApplicants = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.NrOfApplicants;
            }
            else {
                return null;
            }
        };
        MortgageApplicationInitialCreditCheckController.prototype.componentName = function () {
            return 'mortgageApplicationInitialCreditCheck';
        };
        MortgageApplicationInitialCreditCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchMortageLoanApplicationInitialCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl).then(function (result) {
                _this.m = result;
            });
        };
        MortgageApplicationInitialCreditCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationInitialCreditCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationInitialCreditCheckComponentNs.MortgageApplicationInitialCreditCheckController = MortgageApplicationInitialCreditCheckController;
    var MortgageApplicationInitialCreditCheckCheckComponent = /** @class */ (function () {
        function MortgageApplicationInitialCreditCheckCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationInitialCreditCheckController;
            this.templateUrl = 'mortgage-application-initial-credit-check.html';
        }
        return MortgageApplicationInitialCreditCheckCheckComponent;
    }());
    MortgageApplicationInitialCreditCheckComponentNs.MortgageApplicationInitialCreditCheckCheckComponent = MortgageApplicationInitialCreditCheckCheckComponent;
})(MortgageApplicationInitialCreditCheckComponentNs || (MortgageApplicationInitialCreditCheckComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationInitialCreditCheck', new MortgageApplicationInitialCreditCheckComponentNs.MortgageApplicationInitialCreditCheckCheckComponent());
