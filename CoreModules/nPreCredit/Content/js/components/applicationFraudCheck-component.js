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
var ApplicationFraudCheckComponentNs;
(function (ApplicationFraudCheckComponentNs) {
    var ApplicationFraudCheckController = /** @class */ (function (_super) {
        __extends(ApplicationFraudCheckController, _super);
        function ApplicationFraudCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        ApplicationFraudCheckController.prototype.componentName = function () {
            return 'applicationFraudCheck';
        };
        ApplicationFraudCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (this.initialData) {
                this.apiClient.fetchFraudControlModel(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                    _this.m = result;
                });
            }
        };
        ApplicationFraudCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationFraudCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationFraudCheckComponentNs.ApplicationFraudCheckController = ApplicationFraudCheckController;
    var ApplicationFraudCheckComponent = /** @class */ (function () {
        function ApplicationFraudCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationFraudCheckController;
            this.templateUrl = 'application-fraud-check.html';
        }
        return ApplicationFraudCheckComponent;
    }());
    ApplicationFraudCheckComponentNs.ApplicationFraudCheckComponent = ApplicationFraudCheckComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationFraudCheckComponentNs.InitialData = InitialData;
})(ApplicationFraudCheckComponentNs || (ApplicationFraudCheckComponentNs = {}));
angular.module('ntech.components').component('applicationFraudCheck', new ApplicationFraudCheckComponentNs.ApplicationFraudCheckComponent());
