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
var SimpleTableComponentNs;
(function (SimpleTableComponentNs) {
    var SimpleTableController = /** @class */ (function (_super) {
        __extends(SimpleTableController, _super);
        function SimpleTableController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        SimpleTableController.prototype.componentName = function () {
            return 'simpleTable';
        };
        SimpleTableController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var rows = [];
            for (var _i = 0, _a = this.initialData.tableRows; _i < _a.length; _i++) {
                var rowData = _a[_i];
                rows.push({ columnValues: rowData });
            }
            this.m = {
                headerCells: this.initialData.columns,
                rows: rows
            };
        };
        SimpleTableController.$inject = ['$http', '$q', 'ntechComponentService'];
        return SimpleTableController;
    }(NTechComponents.NTechComponentControllerBase));
    SimpleTableComponentNs.SimpleTableController = SimpleTableController;
    var SimpleTableComponent = /** @class */ (function () {
        function SimpleTableComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = SimpleTableController;
            this.templateUrl = 'simple-table.html';
        }
        return SimpleTableComponent;
    }());
    SimpleTableComponentNs.SimpleTableComponent = SimpleTableComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    SimpleTableComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    SimpleTableComponentNs.Model = Model;
    var HeaderCellModel = /** @class */ (function () {
        function HeaderCellModel() {
        }
        return HeaderCellModel;
    }());
    SimpleTableComponentNs.HeaderCellModel = HeaderCellModel;
    var RowModel = /** @class */ (function () {
        function RowModel() {
        }
        return RowModel;
    }());
    SimpleTableComponentNs.RowModel = RowModel;
})(SimpleTableComponentNs || (SimpleTableComponentNs = {}));
angular.module('ntech.components').component('simpleTable', new SimpleTableComponentNs.SimpleTableComponent());
