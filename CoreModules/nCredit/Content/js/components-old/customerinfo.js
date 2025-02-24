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
    CustomerInfoController.prototype.$onChanges = function (changesObj) {
        var _this = this;
        if (this.isReady) {
            return;
        }
        if (!this.initialData && this.customer) {
            //Old invocation style
            this.isReady = true;
            return;
        }
        else if (!this.initialData) {
            return;
        }
        //New invocation style
        var i = this.initialData;
        i.apiClient.fetchCustomerCardItems(i.customerId, ['isCompany', 'firstName', 'birthDate', 'companyName',]).then(function (x) {
            _this.fetchcustomeritems = function (cid, names, onSuccess) {
                i.apiClient.fetchCustomerCardItems(cid, names).then(function (y) { return onSuccess(y); });
            };
            _this.customer = {
                firstName: x['firstName'],
                birthDate: x['birthDate'],
                companyName: x['companyName'],
                isCompany: x['isCompany'],
                customerCardUrl: null,
                customerFatcaCrsUrl: null,
                customerId: i.customerId,
                pepKycCustomerUrl: null
            };
            _this.isReady = true;
        });
    };
    CustomerInfoController.prototype.isCompany = function () {
        return this.customer.isCompany === 'true';
    };
    CustomerInfoController.prototype.toggleContactInfo = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        if (!this.contactInfo) {
            this.fetchcustomeritems(this.customer.customerId, ['firstName', 'lastName', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email', 'companyName'], function (items) {
                _this.contactInfo = {
                    isOpen: true,
                    firstName: _this.isCompany() ? null : _this.getArrayItemValue(items, 'firstName'),
                    lastName: _this.isCompany() ? null : _this.getArrayItemValue(items, 'lastName'),
                    companyName: _this.isCompany() ? _this.getArrayItemValue(items, 'companyName') : null,
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
    CustomerInfoController.prototype.unlockCivicRegNrOrOrgnr = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var idName = this.isCompany() ? 'orgnr' : 'civicRegNr';
        this.fetchcustomeritems(this.customer.customerId, [idName], function (items) {
            _this.civicRegNrOrOrgnr = _this.getArrayItemValue(items, idName);
        }, function (msg) {
            toastr.warning(msg);
        });
    };
    CustomerInfoController.prototype.unlockAmlRiskClass = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        var propertyName = "amlRiskClass";
        this.fetchcustomeritems(this.customer.customerId, [propertyName], function (items) {
            var riskClass = _this.getArrayItemValue(items, propertyName);
            if (riskClass) {
                _this.amlRiskClass = riskClass;
            }
            else {
                _this.amlRiskClass = "-";
            }
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
        return items[name];
    };
    return CustomerInfoController;
}());
var CustomerInfoComponent = /** @class */ (function () {
    function CustomerInfoComponent() {
        this.bindings = {
            customer: '<',
            fetchcustomeritems: '<',
            initialData: '<',
        };
        this.controller = CustomerInfoController;
        this.templateUrl = 'customerinfo.html'; //In Shared/Component_CustomerInfo.cshtml
        this.transclude = true;
    }
    return CustomerInfoComponent;
}());
angular.module('ntech.components').component('customerinfo', new CustomerInfoComponent());
var CustomerInfoComponentNs;
(function (CustomerInfoComponentNs) {
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CustomerInfoComponentNs.InitialData = InitialData;
})(CustomerInfoComponentNs || (CustomerInfoComponentNs = {}));
