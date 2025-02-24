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
var ApplicationEditorComponentNs;
(function (ApplicationEditorComponentNs) {
    var ApplicationEditorController = /** @class */ (function (_super) {
        __extends(ApplicationEditorController, _super);
        function ApplicationEditorController($http, $q, $scope, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.form = function () {
                if (!_this.$scope) {
                    return null;
                }
                var c = _this.$scope['formContainer'];
                if (!c) {
                    return null;
                }
                return c[_this.formName];
            };
            _this.formName = 'f' + NTechComponents.generateUniqueId(6);
            _this.$scope['formContainer'] = {};
            return _this;
        }
        ApplicationEditorController.prototype.componentName = function () {
            return 'applicationEditor';
        };
        ApplicationEditorController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(this.initialData.navigationOptionToHere, this.apiClient, this.$q, { applicationNr: this.initialData.applicationNr });
        };
        ApplicationEditorController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var d = this.initialData;
            d.loadItems().then(function (x) {
                var mode = d.isInPlaceEditAllowed ? Mode.DirectEdit : Mode.ViewOnly;
                var m = {
                    items: [],
                    models: {},
                    mode: mode,
                    isDirectEditOnlyMode: mode === Mode.DirectEdit,
                    isEditing: false,
                    directEditModels: null,
                    labelSize: _this.initialData.labelSize,
                    enableChangeTracking: _this.initialData.enableChangeTracking
                };
                for (var _i = 0, _a = x.Results; _i < _a.length; _i++) {
                    var ds = _a[_i];
                    m.models[ds.DataSourceName] = {};
                }
                var _loop_1 = function (i) {
                    if (i.dataSourceName) {
                        var dsResult = NTechLinq.first(x.Results, function (y) { return y.DataSourceName === i.dataSourceName; });
                        var dsItem = NTechLinq.first(dsResult.Items, function (x) { return x.Name === i.itemName; });
                        m.models[i.dataSourceName][i.itemName] = ApplicationItemEditorComponentNs.createDataModelUsingDataSourceResult(d.applicationNr, d.applicationType, (m.mode === Mode.DirectEdit) && !i.forceReadonly, d.navigationOptionToHere, dsResult);
                        m.items.push({ itemType: ItemType.DataSourceItem, dataSourceName: i.dataSourceName, itemName: i.itemName, editModel: dsItem.EditorModel, forceReadonly: i.forceReadonly }); //todo, buttons, static values and so on
                    }
                    else {
                        //TODO: Buttons, separators and such
                    }
                };
                for (var _b = 0, _c = d.getIncludedItems(); _b < _c.length; _b++) {
                    var i = _c[_b];
                    _loop_1(i);
                }
                _this.m = m;
            });
        };
        ApplicationEditorController.prototype.isFormInvalid = function () {
            return !this.form() || this.form().$invalid;
        };
        ApplicationEditorController.prototype.cancelDirectEdit = function () {
            if (!this.m) {
                return;
            }
            this.m.isEditing = false;
            this.m.directEditModels = null;
        };
        ApplicationEditorController.prototype.commitDirectEdit = function () {
            var _this = this;
            if (!this.m) {
                return;
            }
            var originalValues = this.createEditValues();
            var newValues = this.m.directEditModels;
            var edits = [];
            for (var _i = 0, _a = Object.keys(originalValues); _i < _a.length; _i++) {
                var dataSourceName = _a[_i];
                for (var _b = 0, _c = Object.keys(originalValues[dataSourceName]); _b < _c.length; _b++) {
                    var itemName = _c[_b];
                    var originalValue = originalValues[dataSourceName][itemName];
                    var newValue = newValues[dataSourceName][itemName];
                    if (originalValue !== newValue) {
                        edits.push({ dataSourceName: dataSourceName, itemName: itemName, fromValue: originalValue, toValue: newValue });
                    }
                }
            }
            if (edits.length == 0) {
                this.m.isEditing = false;
                this.m.directEditModels = null;
            }
            else {
                this.initialData.saveItems(edits).then(function (x) {
                    _this.m.isEditing = null;
                    _this.m.directEditModels = null;
                    for (var _i = 0, edits_1 = edits; _i < edits_1.length; _i++) {
                        var e = edits_1[_i];
                        var itemModel = _this.m.models[e.dataSourceName][e.itemName];
                        itemModel.isEditedByGroupedName[e.itemName] = true;
                        itemModel.valueByGroupedName[e.itemName] = e.toValue;
                    }
                });
            }
        };
        ApplicationEditorController.prototype.beginDirectEdit = function () {
            if (!this.m) {
                return;
            }
            this.m.directEditModels = this.createEditValues();
            this.m.isEditing = true;
        };
        ApplicationEditorController.prototype.createEditValues = function () {
            var ms = {};
            for (var _i = 0, _a = Object.keys(this.m.models); _i < _a.length; _i++) {
                var dataSourceName = _a[_i];
                ms[dataSourceName] = {};
                for (var _b = 0, _c = Object.keys(this.m.models[dataSourceName]); _b < _c.length; _b++) {
                    var itemName = _c[_b];
                    var item = this.m.models[dataSourceName][itemName];
                    var value = item.valueByGroupedName[itemName];
                    if (value === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        value = null;
                    }
                    ms[dataSourceName][itemName] = value;
                }
            }
            return ms;
        };
        ApplicationEditorController.$inject = ['$http', '$q', '$scope', 'ntechComponentService'];
        return ApplicationEditorController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationEditorComponentNs.ApplicationEditorController = ApplicationEditorController;
    var ApplicationEditorComponent = /** @class */ (function () {
        function ApplicationEditorComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationEditorController;
            this.template = "<div ng-if=\"$ctrl.m\" class=\"form-horizontal\">\n        <form novalidate name=\"{{$ctrl.formName}}\" ng-if=\"$ctrl.m.mode === 'NavigationEdit' || $ctrl.m.mode === 'ViewOnly'\">\n            <application-item-editor ng-repeat=\"i in $ctrl.m.items\" name=\"i.itemName\" data=\"$ctrl.m.models[i.dataSourceName][i.itemName]\" label-size=\"$ctrl.m.labelSize\" enable-change-tracking=\"$ctrl.m.enableChangeTracking\"></application-item-editor>\n        </form>\n\n        <form name=\"formContainer.{{$ctrl.formName}}\" ng-if=\"$ctrl.m.mode === 'DirectEdit'\">\n            <application-item-editor ng-repeat=\"i in $ctrl.m.items\" name=\"i.itemName\" direct-edit-form=\"$ctrl.form\" direct-edit=\"!i.forceReadonly && $ctrl.m.isEditing\" direct-edit-model=\"$ctrl.m.directEditModels[i.dataSourceName]\" data=\"$ctrl.m.models[i.dataSourceName][i.itemName]\" label-size=\"$ctrl.m.labelSize\" enable-change-tracking=\"$ctrl.m.enableChangeTracking\"></application-item-editor>\n\n            <div class=\"text-right pt-1\">\n                <button class=\"n-icon-btn n-white-btn\" style=\"margin-right:5px;\" ng-click=\"$ctrl.cancelDirectEdit()\" ng-if=\"$ctrl.m.isEditing\"><span class=\"glyphicon glyphicon-remove\"></span></button>\n                <button class=\"n-icon-btn n-green-btn\" ng-click=\"$ctrl.commitDirectEdit()\" ng-disabled=\"$ctrl.isFormInvalid()\" ng-if=\"$ctrl.m.isEditing\"><span class=\"glyphicon glyphicon-ok\"></span></button>\n                <button class=\"n-icon-btn n-blue-btn\" ng-click=\"$ctrl.beginDirectEdit()\" ng-if=\"!$ctrl.m.isEditing\"><span class=\"glyphicon glyphicon-pencil\"></span></button>\n            </div>\n        </form>\n</div>";
        }
        return ApplicationEditorComponent;
    }());
    ApplicationEditorComponentNs.ApplicationEditorComponent = ApplicationEditorComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ApplicationEditorComponentNs.Model = Model;
    var ItemType;
    (function (ItemType) {
        ItemType["DataSourceItem"] = "DataSourceItem ";
    })(ItemType = ApplicationEditorComponentNs.ItemType || (ApplicationEditorComponentNs.ItemType = {}));
    var Mode;
    (function (Mode) {
        Mode["ViewOnly"] = "ViewOnly";
        Mode["DirectEdit"] = "DirectEdit";
    })(Mode = ApplicationEditorComponentNs.Mode || (ApplicationEditorComponentNs.Mode = {}));
    function createInitialDataVirtual(dataSourceService, applicationNr, applicationType, navigationOptionToHere, opts) {
        var fields = new InitialData(dataSourceService, !!(opts && opts.isHorizontal), !!(opts && opts.isInPlaceEditAllowed), !!(opts && opts.labelIsTranslationKey), applicationNr, applicationType, navigationOptionToHere, opts ? opts.labelSize : null, opts ? opts.enableChangeTracking : null);
        return fields;
    }
    ApplicationEditorComponentNs.createInitialDataVirtual = createInitialDataVirtual;
    function createInitialData2(dataSourceService, applicationType, navigationOptionToHere, setupFields, opts) {
        var fields = new InitialData(dataSourceService, !!(opts && opts.isHorizontal), !!(opts && opts.isInPlaceEditAllowed), !!(opts && opts.labelIsTranslationKey), dataSourceService.applicationNr, applicationType, navigationOptionToHere, opts ? opts.labelSize : null, opts ? opts.enableChangeTracking : null);
        setupFields(dataSourceService);
        return fields;
    }
    ApplicationEditorComponentNs.createInitialData2 = createInitialData2;
    function createInitialData(applicationNr, applicationType, navigationOptionToHere, apiClient, $q, setupFields, opts) {
        var d = new ApplicationDataSourceHelper.ApplicationDataSourceService(applicationNr, apiClient, $q, opts ? opts.afterInPlaceEditsCommited : null, opts ? opts.afterDataLoaded : null);
        var fields = new InitialData(d, !!(opts && opts.isHorizontal), !!(opts && opts.isInPlaceEditAllowed), !!(opts && opts.labelIsTranslationKey), applicationNr, applicationType, navigationOptionToHere, opts ? opts.labelSize : null, opts ? opts.enableChangeTracking : null);
        setupFields(d);
        return fields;
    }
    ApplicationEditorComponentNs.createInitialData = createInitialData;
    var InitialData = /** @class */ (function () {
        function InitialData(dataSourceService, isHorizontal, isInPlaceEditAllowed, labelIsTranslationKey, applicationNr, applicationType, navigationOptionToHere, labelSize, enableChangeTracking) {
            this.dataSourceService = dataSourceService;
            this.isHorizontal = isHorizontal;
            this.isInPlaceEditAllowed = isInPlaceEditAllowed;
            this.labelIsTranslationKey = labelIsTranslationKey;
            this.applicationNr = applicationNr;
            this.applicationType = applicationType;
            this.navigationOptionToHere = navigationOptionToHere;
            this.labelSize = labelSize;
            this.enableChangeTracking = enableChangeTracking;
        }
        InitialData.prototype.getIncludedItems = function () {
            return this.dataSourceService.getIncludedItems();
        };
        InitialData.prototype.loadItems = function () {
            return this.dataSourceService.loadItems();
        };
        InitialData.prototype.saveItems = function (edits) {
            return this.dataSourceService.saveItems(edits);
        };
        return InitialData;
    }());
    ApplicationEditorComponentNs.InitialData = InitialData;
    var ItemDataType;
    (function (ItemDataType) {
        ItemDataType["Text"] = "Text";
        ItemDataType["Enum"] = "Enum";
        ItemDataType["Number"] = "Number";
        ItemDataType["Currency"] = "Currency";
        ItemDataType["Percent"] = "Percent";
        ItemDataType["Url"] = "Url";
    })(ItemDataType = ApplicationEditorComponentNs.ItemDataType || (ApplicationEditorComponentNs.ItemDataType = {}));
})(ApplicationEditorComponentNs || (ApplicationEditorComponentNs = {}));
angular.module('ntech.components').component('applicationEditor', new ApplicationEditorComponentNs.ApplicationEditorComponent());
