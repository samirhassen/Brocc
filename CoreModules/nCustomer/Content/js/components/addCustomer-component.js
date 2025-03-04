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
var AddCustomerComponentNs;
(function (AddCustomerComponentNs) {
    var AddCustomerController = /** @class */ (function (_super) {
        __extends(AddCustomerController, _super);
        function AddCustomerController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            return _this;
        }
        AddCustomerController.prototype.componentName = function () {
            return 'addCustomer';
        };
        AddCustomerController.prototype.getArrayItemValue = function (items, name) {
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var i = items_1[_i];
                if (i.name == name) {
                    return i.value;
                }
            }
            return null;
        };
        AddCustomerController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q);
        };
        AddCustomerController.prototype.isValidCivicRegNr = function (value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            if (ntechClientCountry == 'SE') {
                return ntech.se.isValidCivicNr(value);
            }
            else if (ntechClientCountry == 'FI') {
                return ntech.fi.isValidCivicNr(value);
            }
            else {
                //So they can at least get the data in
                return true;
            }
        };
        AddCustomerController.prototype.saveNewCustomer = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m.FirstName)
                return;
            var proppertyitems = null;
            proppertyitems = [{
                    Value: this.m.FirstName, Name: "firstName", ForceUpdate: true
                }];
            proppertyitems.push({
                Value: this.m.City, Name: "addressCity", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.Country, Name: "addressCountry", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.Email, Name: "email", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.LastName, Name: "lastName", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.Phone, Name: "phone", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.Street, Name: "addressStreet", ForceUpdate: true
            });
            proppertyitems.push({
                Value: this.m.ZipCode, Name: "addressZipcode", ForceUpdate: true
            });
            this.apiClient.CreateOrUpdateCustomer(this.m.CivicNr, "CreateCustomerManually", proppertyitems).then(function (x) {
                _this.m = _this.createModel('view', function (y) {
                    y.CustomerInfoInitialData = {
                        backUrl: _this.initialData.urlToHere,
                        customerId: x.CustomerId
                    };
                });
            });
        };
        AddCustomerController.prototype.resetSearch = function () { this.m.SearchCivicNr = ""; };
        AddCustomerController.prototype.createModel = function (mode, modify) {
            var m = {
                Mode: mode,
                BackUrl: this.initialData ? this.initialData.backUrl : null
            };
            if (modify) {
                modify(m);
            }
            return m;
        };
        AddCustomerController.prototype.searchCivicNr = function (CivicNr) {
            var _this = this;
            this.apiClient.getCustomerIdsByCivicRegNrs(CivicNr).then(function (items) {
                var customerid = items.CustomerId;
                _this.apiClient.fetchCustomerItems(items.CustomerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email', 'civicRegNr']).then(function (items) {
                    if (_this.getArrayItemValue(items, 'civicRegNr') == null) //Case New Customer
                     {
                        _this.apiClient.parseCivicRegNr(CivicNr).then(function (birthDataItems) {
                            _this.m = _this.createModel('new', function (x) {
                                x.SearchCivicNr = CivicNr;
                                x.CivicNr = CivicNr;
                                x.BirthDate = moment(birthDataItems.BirthDate).format('YYYY-MM-DD');
                            });
                        });
                    }
                    else {
                        _this.m = _this.createModel('view', function (x) {
                            x.CustomerInfoInitialData = {
                                backUrl: _this.initialData.urlToHere,
                                customerId: customerid
                            }, x.SearchCivicNr = CivicNr, x.CivicNr = CivicNr;
                        });
                    }
                });
            });
        };
        AddCustomerController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.m = this.createModel('search');
        };
        AddCustomerController.$inject = ['$http', '$q', 'ntechComponentService'];
        return AddCustomerController;
    }(NTechComponents.NTechComponentControllerBase));
    AddCustomerComponentNs.AddCustomerController = AddCustomerController;
    var AddCustomerComponent = /** @class */ (function () {
        function AddCustomerComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = AddCustomerController;
            this.templateUrl = 'add-customer.html';
        }
        return AddCustomerComponent;
    }());
    AddCustomerComponentNs.AddCustomerComponent = AddCustomerComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    AddCustomerComponentNs.Model = Model;
})(AddCustomerComponentNs || (AddCustomerComponentNs = {}));
angular.module('ntech.components').component('addCustomer', new AddCustomerComponentNs.AddCustomerComponent());
