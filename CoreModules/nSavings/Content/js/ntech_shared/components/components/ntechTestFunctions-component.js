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
var NTechTestFunctionsComponentNs;
(function (NTechTestFunctionsComponentNs) {
    var NTechTestFunctionsController = /** @class */ (function (_super) {
        __extends(NTechTestFunctionsController, _super);
        function NTechTestFunctionsController($http, $q, $scope, $timeout, $filter, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$scope = $scope;
            _this.$timeout = $timeout;
            _this.$filter = $filter;
            return _this;
        }
        NTechTestFunctionsController.prototype.componentName = function () {
            return 'ntechTestFunctions';
        };
        NTechTestFunctionsController.prototype.onChanges = function () {
        };
        NTechTestFunctionsController.$inject = ['$http', '$q', '$scope', '$timeout', '$filter', 'ntechComponentService'];
        return NTechTestFunctionsController;
    }(NTechComponents.NTechComponentControllerBase));
    NTechTestFunctionsComponentNs.NTechTestFunctionsController = NTechTestFunctionsController;
    var NTechTestFunctionsComponent = /** @class */ (function () {
        function NTechTestFunctionsComponent() {
            this.transclude = true;
            this.bindings = {
                testFunctions: '<',
            };
            this.controller = NTechTestFunctionsController;
            this.template = "<div ng-show=\"$ctrl.isVisible\" class=\"frame popup-position\">           \n            <div class=\"pt-1\">\n                <button class=\"btn btn-primary\" ng-repeat=\"f in $ctrl.testFunctions\" ng-click=\"f.run()\">{{f.title}}</button>\n            </div>\n        </div>\n        <div style=\"position:fixed;bottom:20px;right:5%;\">\n            <button class=\"btn btn-default\" ng-class=\"{ 'toned-down' : $ctrl.isVisible }\" ng-click=\"$ctrl.isVisible=!$ctrl.isVisible\"><span class=\"glyphicon glyphicon-sort\"></span></button>\n        </div>";
        }
        return NTechTestFunctionsComponent;
    }());
    NTechTestFunctionsComponentNs.NTechTestFunctionsComponent = NTechTestFunctionsComponent;
    var TestFunction = /** @class */ (function () {
        function TestFunction() {
        }
        return TestFunction;
    }());
    NTechTestFunctionsComponentNs.TestFunction = TestFunction;
})(NTechTestFunctionsComponentNs || (NTechTestFunctionsComponentNs = {}));
angular.module('ntech.components').component('ntechTestFunctions', new NTechTestFunctionsComponentNs.NTechTestFunctionsComponent());
