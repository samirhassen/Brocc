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
var MortgageApplicationFinalCreditCheckComponentNs;
(function (MortgageApplicationFinalCreditCheckComponentNs) {
    var MortgageApplicationFinalCreditCheckController = /** @class */ (function (_super) {
        __extends(MortgageApplicationFinalCreditCheckController, _super);
        function MortgageApplicationFinalCreditCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationFinalCreditCheckController.prototype.applicationNr = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr;
            }
            else {
                return null;
            }
        };
        MortgageApplicationFinalCreditCheckController.prototype.backUrl = function () {
            if (this.initialData) {
                return this.initialData.backUrl;
            }
            else {
                return null;
            }
        };
        MortgageApplicationFinalCreditCheckController.prototype.nrOfApplicants = function () {
            if (this.initialData) {
                return this.initialData.applicationInfo.NrOfApplicants;
            }
            else {
                return null;
            }
        };
        MortgageApplicationFinalCreditCheckController.prototype.componentName = function () {
            return 'mortgageApplicationFinalCreditCheck';
        };
        MortgageApplicationFinalCreditCheckController.prototype.onChanges = function () {
            var _this = this;
            this.apiClient.fetchMortageLoanApplicationFinalCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl).then(function (result) {
                _this.m = result;
            });
        };
        MortgageApplicationFinalCreditCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationFinalCreditCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationFinalCreditCheckComponentNs.MortgageApplicationFinalCreditCheckController = MortgageApplicationFinalCreditCheckController;
    var MortgageApplicationFinalCreditCheckCheckComponent = /** @class */ (function () {
        function MortgageApplicationFinalCreditCheckCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationFinalCreditCheckController;
            this.templateUrl = 'mortgage-application-final-credit-check.html';
        }
        return MortgageApplicationFinalCreditCheckCheckComponent;
    }());
    MortgageApplicationFinalCreditCheckComponentNs.MortgageApplicationFinalCreditCheckCheckComponent = MortgageApplicationFinalCreditCheckCheckComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationFinalCreditCheckComponentNs.InitialData = InitialData;
})(MortgageApplicationFinalCreditCheckComponentNs || (MortgageApplicationFinalCreditCheckComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationFinalCreditCheck', new MortgageApplicationFinalCreditCheckComponentNs.MortgageApplicationFinalCreditCheckCheckComponent());
