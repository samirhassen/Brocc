var InitialCustomerInfo = /** @class */ (function () {
    function InitialCustomerInfo() {
    }
    return InitialCustomerInfo;
}());
var ContactInfo = /** @class */ (function () {
    function ContactInfo() {
    }
    return ContactInfo;
}());
var CustomerItem = /** @class */ (function () {
    function CustomerItem() {
    }
    return CustomerItem;
}());
var CustomerInfoController = /** @class */ (function () {
    function CustomerInfoController() {
    }
    CustomerInfoController.prototype.toggleContactInfo = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        if (!this.contactInfo) {
            this.fetchcustomeritems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email'], function (items) {
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
            }, function (msg) {
                toastr.warning(msg);
            });
        }
        else {
            this.contactInfo.isOpen = !this.contactInfo.isOpen;
        }
    };
    CustomerInfoController.prototype.unlockCivicRegNr = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.fetchcustomeritems(this.customer.customerId, ['civicRegNr'], function (items) {
            _this.civicRegNr = _this.getArrayItemValue(items, 'civicRegNr');
        }, function (msg) {
            toastr.warning(msg);
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
    return CustomerInfoController;
}());
var CustomerInfoComponent = /** @class */ (function () {
    function CustomerInfoComponent() {
        this.bindings = {
            customer: '<',
            fetchcustomeritems: '<'
        };
        this.controller = CustomerInfoController;
        this.templateUrl = 'customerinfo.html'; //In Shared/Component_CustomerInfo.cshtml
    }
    return CustomerInfoComponent;
}());
angular.module('ntech.components').component('customerinfo', new CustomerInfoComponent());
