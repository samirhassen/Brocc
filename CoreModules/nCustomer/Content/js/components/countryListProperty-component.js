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
var CountryListPropertyComponentNs;
(function (CountryListPropertyComponentNs) {
    var CountryListPropertyController = /** @class */ (function (_super) {
        __extends(CountryListPropertyController, _super);
        function CountryListPropertyController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        CountryListPropertyController.prototype.componentName = function () {
            return 'countryListProperty';
        };
        CountryListPropertyController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.m = {
                allCountryIsoCodes: this.getFilteredCodes(Object.keys(this.initialData.allCountryCodesAndNames), this.initialData.countryIsoCodes),
                countryIsoCodes: angular.copy(this.initialData.countryIsoCodes),
            };
        };
        CountryListPropertyController.prototype.getFilteredCodes = function (allCodes, exceptCodes) {
            var cs = angular.copy(allCodes);
            for (var _i = 0, exceptCodes_1 = exceptCodes; _i < exceptCodes_1.length; _i++) {
                var c = exceptCodes_1[_i];
                var i = cs.indexOf(c);
                if (i >= 0) {
                    cs.splice(i, 1);
                }
            }
            return cs;
        };
        CountryListPropertyController.prototype.getCountryName = function (countryIsoCode) {
            return this.initialData.allCountryCodesAndNames[countryIsoCode];
        };
        CountryListPropertyController.prototype.onCountryChosen = function () {
            if (!this.m || !this.m.editCountryCode || this.m.countryIsoCodes.indexOf(this.m.editCountryCode) >= 0) {
                return;
            }
            this.m.countryIsoCodes.push(this.m.editCountryCode);
            this.m.allCountryIsoCodes = this.getFilteredCodes(Object.keys(this.initialData.allCountryCodesAndNames), this.m.countryIsoCodes);
            this.m.editCountryCode = null;
        };
        CountryListPropertyController.prototype.removeCountry = function (countryIsoCode, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!countryIsoCode) {
                return;
            }
            var i = this.m.countryIsoCodes.indexOf(countryIsoCode);
            if (i < 0) {
                return;
            }
            this.m.countryIsoCodes.splice(i, 1);
        };
        CountryListPropertyController.prototype.saveEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.onSaveEdit(this.m.countryIsoCodes);
        };
        CountryListPropertyController.prototype.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.onCancelEdit();
        };
        CountryListPropertyController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CountryListPropertyController;
    }(NTechComponents.NTechComponentControllerBase));
    CountryListPropertyComponentNs.CountryListPropertyController = CountryListPropertyController;
    var CountryListPropertyComponent = /** @class */ (function () {
        function CountryListPropertyComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CountryListPropertyController;
            this.templateUrl = 'country-list-property.html';
        }
        return CountryListPropertyComponent;
    }());
    CountryListPropertyComponentNs.CountryListPropertyComponent = CountryListPropertyComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CountryListPropertyComponentNs.InitialData = InitialData;
    var HistoryItem = /** @class */ (function () {
        function HistoryItem() {
        }
        return HistoryItem;
    }());
    CountryListPropertyComponentNs.HistoryItem = HistoryItem;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CountryListPropertyComponentNs.Model = Model;
    function createEditHistoryItems(items, getValue) {
        var result = [];
        for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
            var i = items_1[_i];
            result.push({
                ByUserDisplayName: i.UserDisplayName,
                Date: i.EditDate,
                CountryIsoCodes: getValue(i)
            });
        }
        return result;
    }
    CountryListPropertyComponentNs.createEditHistoryItems = createEditHistoryItems;
})(CountryListPropertyComponentNs || (CountryListPropertyComponentNs = {}));
angular.module('ntech.components').component('countryListProperty', new CountryListPropertyComponentNs.CountryListPropertyComponent());
