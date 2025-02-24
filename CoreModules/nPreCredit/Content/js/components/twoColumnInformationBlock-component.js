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
var TwoColumnInformationBlockComponentNs;
(function (TwoColumnInformationBlockComponentNs) {
    var TwoColumnInformationBlockController = /** @class */ (function (_super) {
        __extends(TwoColumnInformationBlockController, _super);
        function TwoColumnInformationBlockController($http, $q, $filter, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$filter = $filter;
            return _this;
        }
        TwoColumnInformationBlockController.prototype.componentName = function () {
            return 'twoColumnInformationBlock';
        };
        TwoColumnInformationBlockController.prototype.onChanges = function () {
        };
        TwoColumnInformationBlockController.prototype.getItemValue = function (i) {
            if (i.filterName) {
                if (i.filterName === 'percent') {
                    var f = this.$filter('number');
                    return f(i.value, 2) + ' %';
                }
                else {
                    var f = this.$filter(i.filterName);
                    return f(i.value);
                }
            }
            else {
                return i.value;
            }
        };
        TwoColumnInformationBlockController.prototype.lblColCnt = function (item) {
            if (!this.initialData) {
                return 6;
            }
            var v = this.initialData.rightItems.length > 0 ? 6 : 3;
            return item && item.extraLabelColumnCount ? v + item.extraLabelColumnCount : v;
        };
        TwoColumnInformationBlockController.prototype.valColCnt = function (item) {
            if (!this.initialData) {
                return 6;
            }
            var v = this.initialData.rightItems.length > 0 ? 6 : 9;
            return item && item.extraLabelColumnCount ? v - item.extraLabelColumnCount : v;
        };
        TwoColumnInformationBlockController.$inject = ['$http', '$q', '$filter', 'ntechComponentService'];
        return TwoColumnInformationBlockController;
    }(NTechComponents.NTechComponentControllerBase));
    TwoColumnInformationBlockComponentNs.TwoColumnInformationBlockController = TwoColumnInformationBlockController;
    var TwoColumnInformationBlockComponent = /** @class */ (function () {
        function TwoColumnInformationBlockComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = TwoColumnInformationBlockController;
            this.template = "    <div>\n        <div class=\"form-horizontal\" ng-if=\"$ctrl.initialData.rightItems.length > 0\">\n            <div class=\"form-group col-xs-12 text-right\" ng-if=\"$ctrl.initialData.viewDetailsUrl\">\n                <a class=\"n-anchor\" ng-href=\"{{$ctrl.initialData.viewDetailsUrl}}\">View details</a>\n            </div>\n            <div class=\"col-xs-6\">\n                <div class=\"form-group\" ng-repeat=\"c in $ctrl.initialData.leftItems track by $index\">\n                    <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelText && !c.labelKey\">{{c.labelText}}</label>\n                    <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelKey\">{{c.labelKey | translate}}</label>\n                    <div class=\"col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable\">{{$ctrl.getItemValue(c)}}</div>\n                </div>\n            </div>\n            <div class=\"col-xs-6\">\n                <div class=\"form-group\" ng-repeat=\"c in $ctrl.initialData.rightItems track by $index\">\n                    <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelText && !c.labelKey\">{{c.labelText}}</label>\n                    <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelKey\">{{c.labelKey | translate}}</label>\n                    <div class=\"col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable\">{{$ctrl.getItemValue(c)}}</div>\n                </div>\n            </div>\n            <div class=\"clearfix\"></div>\n        </div>\n        <div class=\"form-horizontal\" ng-if=\"$ctrl.initialData.rightItems.length === 0\">\n            <div class=\"form-group col-xs-12 text-right\" ng-if=\"$ctrl.initialData.viewDetailsUrl\">\n                <a class=\"n-anchor\" ng-href=\"{{$ctrl.initialData.viewDetailsUrl}}\">View details</a>\n            </div>\n            <div class=\"form-group\" ng-repeat=\"c in $ctrl.initialData.leftItems track by $index\">\n                <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelText && !c.labelKey\">{{c.labelText}}</label>\n                <label class=\"col-xs-{{$ctrl.lblColCnt(c)}} control-label\" ng-if=\"c.labelKey\">{{c.labelKey | translate}}</label>\n                <div class=\"col-xs-{{$ctrl.valColCnt(c)}} form-control-static copyable\">{{$ctrl.getItemValue(c)}}</div>\n            </div>\n            <div class=\"clearfix\"></div>\n        </div>\n    </div>";
        }
        return TwoColumnInformationBlockComponent;
    }());
    TwoColumnInformationBlockComponentNs.TwoColumnInformationBlockComponent = TwoColumnInformationBlockComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
            var _this = this;
            this.isNullOrWhitespace = function (input) {
                if (typeof input === 'undefined' || input == null)
                    return true;
                if ($.type(input) === 'string') {
                    return $.trim(input).length < 1;
                }
                else {
                    return false;
                }
            };
            this.isValidDecimal = function (value) {
                if (_this.isNullOrWhitespace(value))
                    return true;
                var v = value.toString();
                return (/^([-]?[0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v);
            };
            this.parseDecimalOrNull = function (n) {
                if (_this.isNullOrWhitespace(n) || !_this.isValidDecimal(n)) {
                    return null;
                }
                if ($.type(n) === 'string') {
                    return parseFloat(n.replace(',', '.'));
                }
                else {
                    return parseFloat(n);
                }
            };
            this.leftItems = [];
            this.rightItems = [];
        }
        InitialData.prototype.item = function (isLeft, value, labelKey, labelText, filterName, extraLabelColumnCount) {
            var item = {
                labelText: labelText,
                labelKey: labelKey,
                value: value,
                filterName: filterName,
                extraLabelColumnCount: extraLabelColumnCount
            };
            if (isLeft) {
                this.leftItems.push(item);
            }
            else {
                this.rightItems.push(item);
            }
            return this;
        };
        InitialData.prototype.applicationItem = function (isLeft, value, editorModel, extraLabelColumnCount) {
            var actualValue = ApplicationItemEditorComponentNs.getItemDisplayValueShared(value, editorModel, this.parseDecimalOrNull);
            this.item(isLeft, actualValue, null, editorModel.LabelText || 'Unknown', null, extraLabelColumnCount);
        };
        return InitialData;
    }());
    TwoColumnInformationBlockComponentNs.InitialData = InitialData;
    var InitialDataFromObjectBuilder = /** @class */ (function () {
        function InitialDataFromObjectBuilder(valueObject, translationPrefix, leftItemNames, rightItemNames) {
            this.valueObject = valueObject;
            this.translationPrefix = translationPrefix;
            this.leftItemNames = leftItemNames;
            this.rightItemNames = rightItemNames;
            this.currencyItemNames = {};
            this.mappedValues = {};
        }
        InitialDataFromObjectBuilder.prototype.addCurrencyItems = function (itemNames) {
            for (var _i = 0, itemNames_1 = itemNames; _i < itemNames_1.length; _i++) {
                var i = itemNames_1[_i];
                this.currencyItemNames[i] = true;
            }
            return this;
        };
        InitialDataFromObjectBuilder.prototype.addMappedValue = function (itemName, map) {
            this.mappedValues[itemName] = map;
            return this;
        };
        InitialDataFromObjectBuilder.prototype.buildInitialData = function () {
            var _this = this;
            var d = new InitialData();
            var addItems = function (isLeft, itemNames) {
                for (var _i = 0, itemNames_2 = itemNames; _i < itemNames_2.length; _i++) {
                    var n = itemNames_2[_i];
                    var filterName = null;
                    if (_this.currencyItemNames[n]) {
                        filterName = 'currency';
                    }
                    var value = _this.valueObject[n];
                    if (_this.mappedValues[n]) {
                        value = _this.mappedValues[n](value);
                    }
                    d.item(isLeft, value, _this.translationPrefix ? _this.translationPrefix + n : null, _this.translationPrefix ? null : n, filterName);
                }
            };
            addItems(true, this.leftItemNames);
            addItems(false, this.rightItemNames);
            return d;
        };
        return InitialDataFromObjectBuilder;
    }());
    TwoColumnInformationBlockComponentNs.InitialDataFromObjectBuilder = InitialDataFromObjectBuilder;
    var DataItem = /** @class */ (function () {
        function DataItem() {
        }
        return DataItem;
    }());
    TwoColumnInformationBlockComponentNs.DataItem = DataItem;
})(TwoColumnInformationBlockComponentNs || (TwoColumnInformationBlockComponentNs = {}));
angular.module('ntech.components').component('twoColumnInformationBlock', new TwoColumnInformationBlockComponentNs.TwoColumnInformationBlockComponent());
