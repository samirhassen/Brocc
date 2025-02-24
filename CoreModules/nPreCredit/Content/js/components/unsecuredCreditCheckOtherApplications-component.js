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
var UnsecuredCreditCheckOtherApplicationsComponentNs;
(function (UnsecuredCreditCheckOtherApplicationsComponentNs) {
    var UnsecuredCreditCheckOtherApplicationsController = /** @class */ (function (_super) {
        __extends(UnsecuredCreditCheckOtherApplicationsController, _super);
        function UnsecuredCreditCheckOtherApplicationsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        UnsecuredCreditCheckOtherApplicationsController.prototype.componentName = function () {
            return 'unsecuredCreditCheckOtherApplications';
        };
        UnsecuredCreditCheckOtherApplicationsController.prototype.onChanges = function () {
        };
        UnsecuredCreditCheckOtherApplicationsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return UnsecuredCreditCheckOtherApplicationsController;
    }(NTechComponents.NTechComponentControllerBase));
    UnsecuredCreditCheckOtherApplicationsComponentNs.UnsecuredCreditCheckOtherApplicationsController = UnsecuredCreditCheckOtherApplicationsController;
    var UnsecuredCreditCheckOtherApplicationsComponent = /** @class */ (function () {
        function UnsecuredCreditCheckOtherApplicationsComponent() {
            this.bindings = {
                otherApplications: '<'
            };
            this.controller = UnsecuredCreditCheckOtherApplicationsController;
            this.templateUrl = 'unsecured-credit-check-other-applications.html';
        }
        return UnsecuredCreditCheckOtherApplicationsComponent;
    }());
    UnsecuredCreditCheckOtherApplicationsComponentNs.UnsecuredCreditCheckOtherApplicationsComponent = UnsecuredCreditCheckOtherApplicationsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    UnsecuredCreditCheckOtherApplicationsComponentNs.InitialData = InitialData;
})(UnsecuredCreditCheckOtherApplicationsComponentNs || (UnsecuredCreditCheckOtherApplicationsComponentNs = {}));
angular.module('ntech.components').component('unsecuredCreditCheckOtherApplications', new UnsecuredCreditCheckOtherApplicationsComponentNs.UnsecuredCreditCheckOtherApplicationsComponent());
