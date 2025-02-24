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
var CountryPickerComponentNs;
(function (CountryPickerComponentNs) {
    var CountryPickerController = /** @class */ (function (_super) {
        __extends(CountryPickerController, _super);
        function CountryPickerController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.selectedCountryCode = '';
            _this.selectedCountryCodes = {};
            return _this;
        }
        CountryPickerController.prototype.onCountrySelected = function () {
            var _this = this;
            if (this.selectedCountryCode) {
                var c = _.find(this.countries, function (x) { return x.code === _this.selectedCountryCode; });
                if (c) {
                    this.selectedCountries.push(c);
                    this.selectedCountryCode = '';
                }
            }
        };
        CountryPickerController.prototype.removeSelected = function (c, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var i = _.findIndex(this.selectedCountries, function (x) { return x.code === c.code; });
            if (i >= 0) {
                this.selectedCountries.splice(i, 1);
            }
        };
        CountryPickerController.prototype.isSelected = function (code) {
            return _.findIndex(this.selectedCountries, function (x) { return x.code === code; }) >= 0;
        };
        CountryPickerController.prototype.componentName = function () {
            return 'countryPicker';
        };
        CountryPickerController.prototype.onChanges = function () {
        };
        CountryPickerController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CountryPickerController;
    }(NTechComponents.NTechComponentControllerBase));
    CountryPickerComponentNs.CountryPickerController = CountryPickerController;
    var CountryPickerComponent = /** @class */ (function () {
        function CountryPickerComponent() {
            this.transclude = true;
            this.bindings = {
                countries: '<',
                label: '<',
                showRequiredMessage: '<',
                selectedCountries: '='
            };
            this.controller = CountryPickerController;
            this.templateUrl = 'country-picker.html';
        }
        return CountryPickerComponent;
    }());
    CountryPickerComponentNs.CountryPickerComponent = CountryPickerComponent;
    var CountryModel = /** @class */ (function () {
        function CountryModel() {
        }
        return CountryModel;
    }());
    CountryPickerComponentNs.CountryModel = CountryModel;
    var OnUpdateModel = /** @class */ (function () {
        function OnUpdateModel() {
        }
        return OnUpdateModel;
    }());
    CountryPickerComponentNs.OnUpdateModel = OnUpdateModel;
})(CountryPickerComponentNs || (CountryPickerComponentNs = {}));
angular.module('ntech.components').component('countryPicker', new CountryPickerComponentNs.CountryPickerComponent());
//# sourceMappingURL=countryPicker-component.js.map