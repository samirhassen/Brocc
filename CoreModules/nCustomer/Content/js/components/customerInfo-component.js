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
var CustomerInfoComponentNs;
(function (CustomerInfoComponentNs) {
    var ContactInfo = /** @class */ (function () {
        function ContactInfo() {
        }
        return ContactInfo;
    }());
    CustomerInfoComponentNs.ContactInfo = ContactInfo;
    var InitialCustomerData = /** @class */ (function () {
        function InitialCustomerData() {
        }
        return InitialCustomerData;
    }());
    CustomerInfoComponentNs.InitialCustomerData = InitialCustomerData;
    var CustomerInfoController = /** @class */ (function (_super) {
        __extends(CustomerInfoController, _super);
        function CustomerInfoController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        CustomerInfoController.prototype.componentName = function () {
            return 'customerInfo';
        };
        CustomerInfoController.prototype.onChanges = function () {
            var _this = this;
            this.contactInfo = null;
            this.civicRegNr = null;
            this.customer = null;
            if (this.initialData) {
                this.apiClient.fetchCustomerItems(this.initialData.customerId, ['firstName', 'birthDate']).then(function (items) {
                    _this.customer = {
                        firstName: _this.getArrayItemValue(items, 'firstName'),
                        birthDate: _this.getArrayItemValue(items, 'birthDate'),
                        customerId: _this.initialData.customerId,
                        customerCardUrl: '/Customer/CustomerCard?customerId=' + _this.initialData.customerId + (_this.initialData.backUrl ? ('&backUrl=' + encodeURIComponent(_this.initialData.backUrl)) : '')
                    };
                });
            }
        };
        CustomerInfoController.prototype.toggleContactInfo = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.contactInfo) {
                this.apiClient.fetchCustomerItems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email']).then(function (items) {
                    _this.contactInfo = {
                        isOpen: true,
                        firstName: _this.getArrayItemValue(items, 'firstName'),
                        lastName: _this.getArrayItemValue(items, 'lastName'),
                        addressStreet: _this.getArrayItemValue(items, 'addressStreet'),
                        addressCity: _this.getArrayItemValue(items, 'addressCity'),
                        addressZipcode: _this.getArrayItemValue(items, 'addressZipcode'),
                        addressCountry: _this.getArrayItemValue(items, 'addressCountry'),
                        phone: _this.getArrayItemValue(items, 'phone'),
                        email: _this.getArrayItemValue(items, 'email')
                    };
                });
            }
            else {
                this.contactInfo.isOpen = !this.contactInfo.isOpen;
            }
        };
        CustomerInfoController.prototype.formatPhoneNr = function (nr) {
            if (ntech && ntech.libphonenumber && ntechClientCountry) {
                if (!nr) {
                    return nr;
                }
                var p = ntech.libphonenumber.parsePhoneNr(nr, ntechClientCountry);
                if (p.isValid) {
                    return p.validNumber.standardDialingNumber;
                }
                else {
                    return nr;
                }
            }
            else {
                return nr;
            }
        };
        CustomerInfoController.prototype.unlockCivicRegNr = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerItems(this.customer.customerId, ['civicRegNr']).then(function (items) {
                _this.civicRegNr = _this.getArrayItemValue(items, 'civicRegNr');
            });
        };
        CustomerInfoController.prototype.formatmissing = function (i) {
            if (!i) {
                return '-';
            }
            else {
                return i;
            }
        };
        CustomerInfoController.prototype.getArrayItemValue = function (items, name) {
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var i = items_1[_i];
                if (i.name == name) {
                    return i.value;
                }
            }
            return null;
        };
        CustomerInfoController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CustomerInfoController;
    }(NTechComponents.NTechComponentControllerBase));
    CustomerInfoComponentNs.CustomerInfoController = CustomerInfoController;
    var ApplicationCustomerInfoComponent = /** @class */ (function () {
        function ApplicationCustomerInfoComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerInfoController;
            this.templateUrl = 'customer-info.html';
        }
        return ApplicationCustomerInfoComponent;
    }());
    CustomerInfoComponentNs.ApplicationCustomerInfoComponent = ApplicationCustomerInfoComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CustomerInfoComponentNs.InitialData = InitialData;
})(CustomerInfoComponentNs || (CustomerInfoComponentNs = {}));
angular.module('ntech.components').component('customerInfo', new CustomerInfoComponentNs.ApplicationCustomerInfoComponent());
