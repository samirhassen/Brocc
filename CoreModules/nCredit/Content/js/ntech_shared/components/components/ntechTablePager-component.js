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
var NTechTablePagerComponentNs;
(function (NTechTablePagerComponentNs) {
    var NTechTablePagerController = /** @class */ (function (_super) {
        __extends(NTechTablePagerController, _super);
        function NTechTablePagerController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        NTechTablePagerController.prototype.componentName = function () {
            return 'ntechTablePager';
        };
        NTechTablePagerController.prototype.onChanges = function () {
        };
        NTechTablePagerController.prototype.onGotoPage = function (pageNr, event) {
            if (event) {
                event.preventDefault();
            }
            if (this.pagingObject.onGotoPage) {
                this.pagingObject.onGotoPage({ pageNr: pageNr, pagingObject: this.pagingObject, event: event });
            }
        };
        NTechTablePagerController.$inject = ['$http', '$q', 'ntechComponentService'];
        return NTechTablePagerController;
    }(NTechComponents.NTechComponentControllerBase));
    NTechTablePagerComponentNs.NTechTablePagerController = NTechTablePagerController;
    var NTechTablePagerComponent = /** @class */ (function () {
        function NTechTablePagerComponent() {
            this.transclude = true;
            this.bindings = {
                pagingObject: '<',
            };
            this.controller = NTechTablePagerController;
            this.template = "<div ng-if=\"$ctrl.pagingObject && $ctrl.pagingObject.pages && $ctrl.pagingObject.pages.length > 1\" class=\"dataTables_paginate paging_simple_numbers custom-pagination\">\n                <ul class=\"pagination\">\n                    <li class=\"paginate_button previous\" ng-show=\"$ctrl.pagingObject.isPreviousAllowed\"><a href=\"#\" ng-click=\"$ctrl.onGotoPage($ctrl.pagingObject.previousPageNr, $event)\">Previous</a></li>\n                    <li class=\"paginate_button previous disabled\" ng-hide=\"$ctrl.pagingObject.isPreviousAllowed\"><a href=\"#\" ng-click=\"$event.preventDefault()\">Previous</a></li>\n\n                    <li ng-repeat=\"p in $ctrl.pagingObject.pages\" class=\"paginate_button\" ng-class=\"{ 'active' : p.isCurrentPage, 'disabled' : p.isSeparator }\">\n                        <a href=\"#\" ng-click=\"$ctrl.onGotoPage(p.pageNr, $event)\" ng-hide=\"p.isSeparator\">{{p.pageNr+1}}</a>\n                        <a href=\"#\" ng-show=\"p.isSeparator\" ng-click=\"$event.preventDefault()\">...</a>\n                    </li>\n\n                    <li class=\"paginate_button next\" ng-show=\"$ctrl.pagingObject.isNextAllowed\"><a href=\"#\" ng-click=\"$ctrl.onGotoPage($ctrl.pagingObject.nextPageNr, $event)\">Next</a></li>\n                    <li class=\"paginate_button next disabled\" ng-hide=\"$ctrl.pagingObject.isNextAllowed\"><a href=\"#\" ng-click=\"$event.preventDefault()\">Next</a></li>\n                </ul>\n            </div>";
        }
        return NTechTablePagerComponent;
    }());
    NTechTablePagerComponentNs.NTechTablePagerComponent = NTechTablePagerComponent;
})(NTechTablePagerComponentNs || (NTechTablePagerComponentNs = {}));
angular.module('ntech.components').component('ntechTablePager', new NTechTablePagerComponentNs.NTechTablePagerComponent());
