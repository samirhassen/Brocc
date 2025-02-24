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
var UnsecuredApplicationFraudCheckComponentNs;
(function (UnsecuredApplicationFraudCheckComponentNs) {
    var UnsecuredApplicationFraudCheckController = /** @class */ (function (_super) {
        __extends(UnsecuredApplicationFraudCheckController, _super);
        function UnsecuredApplicationFraudCheckController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        UnsecuredApplicationFraudCheckController.prototype.componentName = function () {
            return 'unsecuredApplicationFraudCheck';
        };
        UnsecuredApplicationFraudCheckController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchFraudControlModel(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                _this.m = {
                    FraudControlModel: x
                };
            });
        };
        UnsecuredApplicationFraudCheckController.prototype.headerClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            return { 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationFraudCheckController.prototype.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            var isOther = !isAccepted && !isRejected;
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected };
        };
        UnsecuredApplicationFraudCheckController.$inject = ['$http', '$q', 'ntechComponentService'];
        return UnsecuredApplicationFraudCheckController;
    }(NTechComponents.NTechComponentControllerBase));
    UnsecuredApplicationFraudCheckComponentNs.UnsecuredApplicationFraudCheckController = UnsecuredApplicationFraudCheckController;
    var UnsecuredApplicationFraudCheckComponent = /** @class */ (function () {
        function UnsecuredApplicationFraudCheckComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationFraudCheckController;
            this.templateUrl = 'unsecured-application-fraud-check.html';
        }
        return UnsecuredApplicationFraudCheckComponent;
    }());
    UnsecuredApplicationFraudCheckComponentNs.UnsecuredApplicationFraudCheckComponent = UnsecuredApplicationFraudCheckComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    UnsecuredApplicationFraudCheckComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    UnsecuredApplicationFraudCheckComponentNs.Model = Model;
})(UnsecuredApplicationFraudCheckComponentNs || (UnsecuredApplicationFraudCheckComponentNs = {}));
angular.module('ntech.components').component('unsecuredApplicationFraudCheck', new UnsecuredApplicationFraudCheckComponentNs.UnsecuredApplicationFraudCheckComponent());
