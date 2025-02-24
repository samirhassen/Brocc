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
var ApplicationOtherApplicationsComponentNs;
(function (ApplicationOtherApplicationsComponentNs) {
    var ApplicationOtherApplicationsController = /** @class */ (function (_super) {
        __extends(ApplicationOtherApplicationsController, _super);
        function ApplicationOtherApplicationsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        ApplicationOtherApplicationsController.prototype.componentName = function () {
            return 'applicationOtherApplications';
        };
        ApplicationOtherApplicationsController.prototype.onChanges = function () {
            var _this = this;
            if (this.initialData) {
                this.apiClient.fetchOtherApplications(this.initialData.applicationNr, this.initialData.backUrl).then(function (result) {
                    _this.m = result;
                });
            }
        };
        ApplicationOtherApplicationsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationOtherApplicationsController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationOtherApplicationsComponentNs.ApplicationOtherApplicationsController = ApplicationOtherApplicationsController;
    var ApplicationOtherApplicationsComponent = /** @class */ (function () {
        function ApplicationOtherApplicationsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationOtherApplicationsController;
            this.templateUrl = 'application-other-applications.html';
        }
        return ApplicationOtherApplicationsComponent;
    }());
    ApplicationOtherApplicationsComponentNs.ApplicationOtherApplicationsComponent = ApplicationOtherApplicationsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationOtherApplicationsComponentNs.InitialData = InitialData;
})(ApplicationOtherApplicationsComponentNs || (ApplicationOtherApplicationsComponentNs = {}));
angular.module('ntech.components').component('applicationOtherApplications', new ApplicationOtherApplicationsComponentNs.ApplicationOtherApplicationsComponent());
