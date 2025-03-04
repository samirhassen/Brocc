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
var NotFoundComponentNs;
(function (NotFoundComponentNs) {
    var NotFoundController = /** @class */ (function (_super) {
        __extends(NotFoundController, _super);
        function NotFoundController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        NotFoundController.prototype.componentName = function () {
            return 'notFound';
        };
        NotFoundController.prototype.onChanges = function () {
        };
        NotFoundController.$inject = ['$http', '$q', 'ntechComponentService'];
        return NotFoundController;
    }(NTechComponents.NTechComponentControllerBase));
    NotFoundComponentNs.NotFoundController = NotFoundController;
    var NotFoundComponent = /** @class */ (function () {
        function NotFoundComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = NotFoundController;
            this.templateUrl = 'not-found.html';
        }
        return NotFoundComponent;
    }());
    NotFoundComponentNs.NotFoundComponent = NotFoundComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    NotFoundComponentNs.InitialData = InitialData;
})(NotFoundComponentNs || (NotFoundComponentNs = {}));
angular.module('ntech.components').component('notFound', new NotFoundComponentNs.NotFoundComponent());
