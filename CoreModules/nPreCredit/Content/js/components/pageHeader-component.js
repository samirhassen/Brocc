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
var PageHeaderComponentNs;
(function (PageHeaderComponentNs) {
    var PageHeaderController = /** @class */ (function (_super) {
        __extends(PageHeaderController, _super);
        function PageHeaderController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        PageHeaderController.prototype.componentName = function () {
            return 'pageHeader';
        };
        PageHeaderController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.initialData.backTarget) {
                NavigationTargetHelper.handleBack(this.initialData.backTarget, this.apiClient, this.$q, this.initialData.backContext);
            }
            else {
                NavigationTargetHelper.handleBackWithInitialDataDefaults(this.initialData.host, this.apiClient, this.$q, this.initialData.backContext);
            }
        };
        PageHeaderController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData || !this.titleText) {
                return;
            }
            this.m = {
                titleText: this.titleText
            };
        };
        PageHeaderController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return PageHeaderController;
    }(NTechComponents.NTechComponentControllerBase));
    PageHeaderComponentNs.PageHeaderController = PageHeaderController;
    var PageHeaderComponent = /** @class */ (function () {
        function PageHeaderComponent() {
            this.bindings = {
                initialData: '<',
                titleText: '<'
            };
            this.controller = PageHeaderController;
            this.template = "<div class=\"pt-1 pb-2\" ng-if=\"$ctrl.m\">\n        <div class=\"pull-left\"><a class=\"n-back\" href=\"#\" ng-click=\"$ctrl.onBack($event)\"><span class=\"glyphicon glyphicon-arrow-left\"></span></a></div>\n        <h1 class=\"adjusted\">{{$ctrl.m.titleText}}</h1>\n    </div>";
        }
        return PageHeaderComponent;
    }());
    PageHeaderComponentNs.PageHeaderComponent = PageHeaderComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    PageHeaderComponentNs.Model = Model;
})(PageHeaderComponentNs || (PageHeaderComponentNs = {}));
angular.module('ntech.components').component('pageHeader', new PageHeaderComponentNs.PageHeaderComponent());
