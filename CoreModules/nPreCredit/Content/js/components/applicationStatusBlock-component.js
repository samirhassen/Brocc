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
var ApplicationStatusBlockComponentNs;
(function (ApplicationStatusBlockComponentNs) {
    function getIconClass(isAccepted, isRejected) {
        var isOther = !isAccepted && !isRejected;
        return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected };
    }
    ApplicationStatusBlockComponentNs.getIconClass = getIconClass;
    function getHeaderClass(isAccepted, isRejected, isActive) {
        var isOther = !isAccepted && !isRejected;
        return { 'text-success': isAccepted, 'text-danger': isRejected, 'text-ntech-inactive': isOther && !isActive };
    }
    ApplicationStatusBlockComponentNs.getHeaderClass = getHeaderClass;
    var ApplicationStatusBlockController = /** @class */ (function (_super) {
        __extends(ApplicationStatusBlockController, _super);
        function ApplicationStatusBlockController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.isExpanded = false;
            return _this;
        }
        ApplicationStatusBlockController.prototype.componentName = function () {
            return 'applicationStatusBlock';
        };
        ApplicationStatusBlockController.prototype.onChanges = function () {
            this.setExpanded(this.initialData ? this.initialData.isInitiallyExpanded : false);
        };
        ApplicationStatusBlockController.prototype.toggleExpanded = function (evt) {
            this.setExpanded(!this.isExpanded);
        };
        ApplicationStatusBlockController.prototype.setExpanded = function (isExpanded) {
            this.isExpanded = isExpanded;
        };
        ApplicationStatusBlockController.prototype.headerClassFromStatus = function (status, isActive) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            return getHeaderClass(isAccepted, isRejected, isActive);
        };
        ApplicationStatusBlockController.prototype.iconClassFromStatus = function (status) {
            var isAccepted = status === 'Accepted';
            var isRejected = status === 'Rejected';
            return getIconClass(isAccepted, isRejected);
        };
        ApplicationStatusBlockController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationStatusBlockController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationStatusBlockComponentNs.ApplicationStatusBlockController = ApplicationStatusBlockController;
    var ApplicationStatusBlockComponent = /** @class */ (function () {
        function ApplicationStatusBlockComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationStatusBlockController;
            this.templateUrl = 'application-status-block.html';
        }
        return ApplicationStatusBlockComponent;
    }());
    ApplicationStatusBlockComponentNs.ApplicationStatusBlockComponent = ApplicationStatusBlockComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationStatusBlockComponentNs.InitialData = InitialData;
})(ApplicationStatusBlockComponentNs || (ApplicationStatusBlockComponentNs = {}));
angular.module('ntech.components').component('applicationStatusBlock', new ApplicationStatusBlockComponentNs.ApplicationStatusBlockComponent());
