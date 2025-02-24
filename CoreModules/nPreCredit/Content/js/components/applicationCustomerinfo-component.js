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
var ApplicationCustomerInfoComponentNs;
(function (ApplicationCustomerInfoComponentNs) {
    var ContactInfo = /** @class */ (function () {
        function ContactInfo() {
        }
        return ContactInfo;
    }());
    ApplicationCustomerInfoComponentNs.ContactInfo = ContactInfo;
    var PepKycInfo = /** @class */ (function () {
        function PepKycInfo() {
        }
        return PepKycInfo;
    }());
    var CustomerItem = /** @class */ (function () {
        function CustomerItem() {
        }
        return CustomerItem;
    }());
    var ApplicationCustomerInfoController = /** @class */ (function (_super) {
        __extends(ApplicationCustomerInfoController, _super);
        function ApplicationCustomerInfoController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        ApplicationCustomerInfoController.prototype.componentName = function () {
            return 'applicationCustomerInfo';
        };
        ApplicationCustomerInfoController.prototype.onChanges = function () {
            var _this = this;
            this.contactInfo = null;
            this.civicRegNr = null;
            this.customer = null;
            if (this.initialData == null) {
                return;
            }
            var c = 0;
            if (this.initialData.applicantNr)
                c += 1;
            if (this.initialData.customerIdCompoundItemName)
                c += 1;
            if (this.initialData.customerId)
                c += 1;
            if (c !== 1) {
                throw new Error('Exactly one of applicantNr, customerIdCompoundItemName and customerId must be set');
            }
            if (this.initialData.applicantNr) {
                this.apiClient.fetchCustomerComponentInitialData(this.initialData.applicationNr, this.initialData.applicantNr, this.initialData.backTarget).then(function (result) {
                    _this.customer = result;
                });
            }
            else if (this.initialData.customerIdCompoundItemName) {
                this.apiClient.fetchCustomerComponentInitialDataByItemCompoundName(this.initialData.applicationNr, this.initialData.customerIdCompoundItemName, this.initialData.birthDateCompoundItemName, this.initialData.backTarget).then(function (result) {
                    _this.customer = result;
                });
            }
            else {
                this.apiClient.fetchCustomerComponentInitialDataByCustomerId(this.initialData.customerId, this.initialData.backTarget).then(function (result) {
                    _this.customer = result;
                });
            }
        };
        ApplicationCustomerInfoController.prototype.isCompany = function () {
            if (!this.customer) {
                return null;
            }
            return this.customer.isCompany;
        };
        ApplicationCustomerInfoController.prototype.toggleContactInfo = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.contactInfo) {
                var itemNames = ['addressStreet', 'addressZipcode', 'addressCity', 'addressCountry', 'phone', 'email'];
                if (this.isCompany()) {
                    itemNames.push('companyName');
                }
                else {
                    itemNames.push('firstName');
                    itemNames.push('lastName');
                }
                this.apiClient.fetchCustomerItems(this.customer.customerId, itemNames).then(function (items) {
                    _this.contactInfo = {
                        isOpen: true,
                        firstName: _this.getArrayItemValue(items, 'firstName'),
                        lastName: _this.getArrayItemValue(items, 'lastName'),
                        addressStreet: _this.getArrayItemValue(items, 'addressStreet'),
                        addressCity: _this.getArrayItemValue(items, 'addressCity'),
                        addressZipcode: _this.getArrayItemValue(items, 'addressZipcode'),
                        addressCountry: _this.getArrayItemValue(items, 'addressCountry'),
                        phone: _this.getArrayItemValue(items, 'phone'),
                        email: _this.getArrayItemValue(items, 'email'),
                        companyName: _this.getArrayItemValue(items, 'companyName')
                    };
                });
            }
            else {
                this.contactInfo.isOpen = !this.contactInfo.isOpen;
            }
        };
        ApplicationCustomerInfoController.prototype.doKycScreen = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.pepKycInfo) {
                return;
            }
            this.apiClient.kycScreenCustomer(this.customer.customerId, false).then(function (x) {
                if (!x.Success) {
                    toastr.warning('Screening failed: ' + x.FailureCode);
                }
                else if (x.Skipped) {
                    toastr.info('Customer has already been screened');
                }
                else {
                    if (_this.initialData.onkycscreendone != null) {
                        if (!_this.initialData.onkycscreendone(_this.customer.customerId)) {
                            return;
                        }
                    }
                    _this.pepKycInfo = null;
                    _this.togglePepKycInfo(null);
                }
            });
        };
        ApplicationCustomerInfoController.prototype.togglePepKycInfo = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.pepKycInfo) {
                this.apiClient.fetchCustomerKycScreenStatus(this.customer.customerId).then(function (x) {
                    _this.pepKycInfo = {
                        latestScreeningDate: x.LatestScreeningDate,
                        isOpen: true
                    };
                });
            }
            else {
                this.pepKycInfo.isOpen = !this.pepKycInfo.isOpen;
            }
        };
        ApplicationCustomerInfoController.prototype.formatPhoneNr = function (nr) {
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
        ApplicationCustomerInfoController.prototype.unlockCivicRegNr = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var nrName = this.isCompany() ? 'orgnr' : 'civicRegNr';
            this.apiClient.fetchCustomerItems(this.customer.customerId, [nrName]).then(function (items) {
                _this.civicRegNr = _this.getArrayItemValue(items, nrName);
            });
        };
        ApplicationCustomerInfoController.prototype.formatmissing = function (i) {
            if (!i) {
                return '-';
            }
            else {
                return i;
            }
        };
        ApplicationCustomerInfoController.prototype.getArrayItemValue = function (items, name) {
            for (var _i = 0, items_1 = items; _i < items_1.length; _i++) {
                var i = items_1[_i];
                if (i.name == name) {
                    return i.value;
                }
            }
            return null;
        };
        ApplicationCustomerInfoController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationCustomerInfoController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationCustomerInfoComponentNs.ApplicationCustomerInfoController = ApplicationCustomerInfoController;
    var ApplicationCustomerInfoComponent = /** @class */ (function () {
        function ApplicationCustomerInfoComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCustomerInfoController;
            this.templateUrl = 'application-customerinfo.html';
        }
        return ApplicationCustomerInfoComponent;
    }());
    ApplicationCustomerInfoComponentNs.ApplicationCustomerInfoComponent = ApplicationCustomerInfoComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationCustomerInfoComponentNs.InitialData = InitialData;
})(ApplicationCustomerInfoComponentNs || (ApplicationCustomerInfoComponentNs = {}));
angular.module('ntech.components').component('applicationCustomerinfo', new ApplicationCustomerInfoComponentNs.ApplicationCustomerInfoComponent());
