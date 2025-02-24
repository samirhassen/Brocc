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
var AddRemoveListComponentNs;
(function (AddRemoveListComponentNs) {
    function createNewRow(applicationNr, listName, nr, apiClient) {
        var itemName = ComplexApplicationListHelper.getDataSourceItemName(listName, nr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No);
        return apiClient.setApplicationEditItemData(applicationNr, 'ComplexApplicationList', itemName, 'true', false).then(function (x) {
            return nr;
        });
    }
    AddRemoveListComponentNs.createNewRow = createNewRow;
    var AddRemoveListController = /** @class */ (function (_super) {
        __extends(AddRemoveListController, _super);
        function AddRemoveListController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        AddRemoveListController.prototype.componentName = function () {
            return 'addRemoveList';
        };
        AddRemoveListController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var ai = this.initialData.ai;
            ComplexApplicationListHelper.getNrs(ai.ApplicationNr, i.listName, this.apiClient).then(function (rowNrs) {
                _this.m = {
                    isEditAllowed: i.isEditAllowed,
                    headerText: i.headerText,
                    rows: NTechLinq.select(rowNrs, function (rowNr) {
                        return {
                            d: _this.createItemInitialData(rowNr),
                            nr: rowNr,
                            viewDetailsUrl: _this.initialData.getViewDetailsUrl ? _this.initialData.getViewDetailsUrl(rowNr) : null
                        };
                    })
                };
            });
        };
        AddRemoveListController.prototype.addRow = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var currentMax = 0;
            for (var _i = 0, _a = this.m.rows; _i < _a.length; _i++) {
                var c = _a[_i];
                currentMax = Math.max(c.nr, currentMax);
            }
            createNewRow(this.initialData.ai.ApplicationNr, this.initialData.listName, currentMax + 1, this.apiClient).then(function (newRowNr) {
                _this.m.rows.push({
                    d: _this.createItemInitialData(newRowNr),
                    nr: newRowNr,
                    viewDetailsUrl: _this.initialData.getViewDetailsUrl ? _this.initialData.getViewDetailsUrl(newRowNr) : null
                });
                _this.emitEvent(newRowNr, false, true, false);
            });
        };
        AddRemoveListController.prototype.deleteRow = function (nr, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            ComplexApplicationListHelper.deleteRow(this.initialData.ai.ApplicationNr, this.initialData.listName, nr, this.apiClient).then(function (x) {
                var i = NTechLinq.firstIndexOf(_this.m.rows, function (x) { return x.nr === nr; });
                if (i >= 0) {
                    _this.m.rows.splice(i, 1);
                    _this.emitEvent(nr, false, false, true);
                }
            });
        };
        AddRemoveListController.prototype.createItemInitialData = function (rowNr) {
            var _this = this;
            var i = this.initialData;
            var ai = i.ai;
            return ApplicationEditorComponentNs.createInitialData(ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createTargetFromComponentHostToHere(i.host), this.apiClient, this.$q, function (x) {
                for (var _i = 0, _a = i.itemNames; _i < _a.length; _i++) {
                    var itemName = _a[_i];
                    x.addComplexApplicationListItem("".concat(i.listName, "#").concat(rowNr, "#u#").concat(itemName));
                }
            }, {
                isInPlaceEditAllowed: true,
                afterInPlaceEditsCommited: function () { _this.emitEvent(rowNr, true, false, false); },
                labelSize: this.initialData.applicationEditorLabelSize,
                enableChangeTracking: this.initialData.applicationEditorEnableChangeTracking
            });
        };
        AddRemoveListController.prototype.emitEvent = function (nr, isEdit, isAdd, isRemove) {
            this.ntechComponentService.emitNTechCustomDataEvent(AddRemoveListComponentNs.ChangeEventName, {
                nr: nr,
                isEdit: isEdit,
                isAdd: isAdd,
                isRemove: isRemove,
                eventCorrelationId: this.initialData.eventCorrelationId
            });
        };
        AddRemoveListController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return AddRemoveListController;
    }(NTechComponents.NTechComponentControllerBase));
    AddRemoveListComponentNs.AddRemoveListController = AddRemoveListController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    AddRemoveListComponentNs.Model = Model;
    AddRemoveListComponentNs.ChangeEventName = 'addRemoveListChangeEvent';
    var AddRemoveListComponent = /** @class */ (function () {
        function AddRemoveListComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = AddRemoveListController;
            this.template = "<div>\n\n<div>\n    <h3>{{$ctrl.m.headerText}}</h3>\n    <hr class=\"hr-section\"/>\n    <button class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.addRow($event)\" ng-if=\"$ctrl.m.isEditAllowed\">Add</button>\n</div>\n\n<hr class=\"hr-section dotted\" />\n\n<div ng-repeat=\"c in $ctrl.m.rows\">\n    <div class=\"row\" >\n        <div ng-class=\"{ 'col-sm-8': c.viewDetailsUrl, 'col-sm-10': !c.viewDetailsUrl }\">\n            <application-editor initial-data=\"c.d\"></application-editor>\n        </div>\n        <div class=\"col-sm-2 text-right\">\n            <a ng-if=\"c.viewDetailsUrl\" class=\"n-anchor\" ng-href=\"{{c.viewDetailsUrl}}\">View details</a>\n        </div>\n        <div class=\"col-sm-2 text-right\">\n            <button ng-if=\"$ctrl.m.isEditAllowed\" class=\"n-icon-btn n-red-btn\" ng-click=\"$ctrl.deleteRow(c.nr, $event)\"><span class=\"glyphicon glyphicon-minus\"></span></button>\n        </div>\n    </div>\n    <hr ng-if=\"$ctrl.m.rows && $ctrl.m.rows.length > 0\" class=\"hr-section dotted\">\n</div>\n\n</div>";
        }
        return AddRemoveListComponent;
    }());
    AddRemoveListComponentNs.AddRemoveListComponent = AddRemoveListComponent;
})(AddRemoveListComponentNs || (AddRemoveListComponentNs = {}));
angular.module('ntech.components').component('addRemoveList', new AddRemoveListComponentNs.AddRemoveListComponent());
