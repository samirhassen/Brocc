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
var CustomerContactInfoComponentNs;
(function (CustomerContactInfoComponentNs) {
    var CustomerContactInfoController = /** @class */ (function (_super) {
        __extends(CustomerContactInfoController, _super);
        function CustomerContactInfoController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            return _this;
        }
        CustomerContactInfoController.prototype.componentName = function () {
            return 'customerContactInfo';
        };
        CustomerContactInfoController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q);
        };
        CustomerContactInfoController.prototype.formatDate = function (d) {
            if (d) {
                return moment(d).format('YYYY-MM-DD');
            }
            else {
                return null;
            }
        };
        CustomerContactInfoController.prototype.onChanges = function () {
            var _this = this;
            var includeSensitive = false;
            if (this.m && this.m.showSensitive) {
                includeSensitive = true;
            }
            this.m = null;
            this.apiClient.fetchCustomerContactInfo(this.initialData.customerId, includeSensitive, true).then(function (result) {
                var items = [];
                var includeSensitive = result.includeSensitive;
                var isSensitiveItem = function (name) { return result.sensitiveItems.indexOf(name) >= 0; };
                var addItem = function (name, displayLabelText, value, isEditable) {
                    var i = ItemViewModel.createEditableItemWithoutValue(name, displayLabelText);
                    if (includeSensitive || !isSensitiveItem(name)) {
                        i.value = value;
                        i.hasValue = true;
                    }
                    i.isEditable = isEditable;
                    items.push(i);
                };
                var isCompany = result.isCompany === 'true';
                var addSeparator = function () { return items.push(ItemViewModel.createSeparator()); };
                if (isCompany) {
                    addItem('companyName', 'Company name', result.companyName, true);
                    addItem('orgnr', 'Orgnr', result.orgnr, false);
                }
                else {
                    addItem('firstName', 'First name', result.firstName, true);
                    addItem('lastName', 'Last name', result.lastName, true);
                    addItem('birthDate', 'Birthdate', _this.formatDate(result.birthDate), true);
                    addItem('civicRegNr', 'Civic reg nr', result.civicRegNr, false);
                }
                addSeparator();
                addItem('addressStreet', 'Street', result.addressStreet, true);
                addItem('addressZipcode', 'Zipcode', result.addressZipcode, true);
                addItem('addressCity', 'City', result.addressCity, true);
                addItem('addressCountry', 'Country', result.addressCountry, true);
                addSeparator();
                addItem('email', 'Email', result.email, true);
                addItem('phone', 'Phone', result.phone, true);
                _this.m = {
                    items: items,
                    showSensitive: includeSensitive,
                    editCustomerContactInfoValueInitialData: null
                };
                if (initialData && initialData.testFunctions) {
                    var td = initialData.testFunctions;
                    td.addFunctionCall(td.generateUniqueScopeName(), 'Wipe name, address and contact info', function () {
                        _this.apiClient.wipeCustomerContactInfo([_this.initialData.customerId]).then(function (x) {
                            document.location.reload();
                        });
                    });
                }
            });
        };
        CustomerContactInfoController.prototype.showSensitive = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (this.m && !this.m.showSensitive) {
                this.m.showSensitive = true;
                this.onChanges();
            }
        };
        CustomerContactInfoController.prototype.editItem = function (i, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!(i && i.isEditable)) {
                return;
            }
            var closeEdit = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                _this.onChanges();
            };
            this.m.editCustomerContactInfoValueInitialData = {
                onClose: closeEdit,
                customerId: this.initialData.customerId,
                itemName: i.name
            };
        };
        CustomerContactInfoController.prototype.getDisplayLabelText = function (i) {
            //TODO: Get from translation and remove from view model
            return i.displayLabelText;
        };
        CustomerContactInfoController.$inject = ['$http', '$q', 'ntechComponentService'];
        return CustomerContactInfoController;
    }(NTechComponents.NTechComponentControllerBase));
    CustomerContactInfoComponentNs.CustomerContactInfoController = CustomerContactInfoController;
    var CustomerContactInfoComponent = /** @class */ (function () {
        function CustomerContactInfoComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerContactInfoController;
            this.templateUrl = 'customer-contact-info.html';
        }
        return CustomerContactInfoComponent;
    }());
    CustomerContactInfoComponentNs.CustomerContactInfoComponent = CustomerContactInfoComponent;
    var ItemViewModel = /** @class */ (function () {
        function ItemViewModel() {
        }
        ItemViewModel.createEditableItemWithoutValue = function (name, displayLabelText) {
            var i = {
                name: name,
                displayLabelText: displayLabelText,
                isEditable: true,
                isSeparator: false,
                hasValue: false,
                value: null
            };
            return i;
        };
        ItemViewModel.createSeparator = function () {
            var i = {
                isSeparator: true,
                name: null, displayLabelText: null, hasValue: null, isEditable: null, value: null
            };
            return i;
        };
        return ItemViewModel;
    }());
    CustomerContactInfoComponentNs.ItemViewModel = ItemViewModel;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CustomerContactInfoComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CustomerContactInfoComponentNs.Model = Model;
})(CustomerContactInfoComponentNs || (CustomerContactInfoComponentNs = {}));
angular.module('ntech.components').component('customerContactInfo', new CustomerContactInfoComponentNs.CustomerContactInfoComponent());
