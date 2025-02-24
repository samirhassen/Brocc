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
var ApplicationCustomerListComponentNs;
(function (ApplicationCustomerListComponentNs) {
    var ApplicationCustomerListController = /** @class */ (function (_super) {
        __extends(ApplicationCustomerListController, _super);
        function ApplicationCustomerListController($http, $q, ntechComponentService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$timeout = $timeout;
            return _this;
        }
        ApplicationCustomerListController.prototype.componentName = function () {
            return 'applicationCustomerList';
        };
        ApplicationCustomerListController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.initialData.editorService.fetchCustomerIds().then(function (customerIds) {
                _this.m = {
                    selectNr: {},
                    customers: [],
                    multipleEditorServiceCustomers: []
                };
                for (var _i = 0, customerIds_1 = customerIds; _i < customerIds_1.length; _i++) {
                    var customerId = customerIds_1[_i];
                    _this.addCustomerToModel(customerId);
                }
            }).then(function (x) {
                if (_this.initialData.multipleEditorService) {
                    _this.initialData.multipleEditorService.editorService.fetchCustomerIds().then(function (customerIds) {
                        for (var _i = 0, customerIds_2 = customerIds; _i < customerIds_2.length; _i++) {
                            var customerId = customerIds_2[_i];
                            _this.addCustomerToMultipleEditorModel(customerId);
                        }
                    });
                }
            });
        };
        ApplicationCustomerListController.prototype.selectNr = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var nr = this.m.selectNr.nr;
            this.apiClient.fetchCustomerIdByCivicRegNr(nr).then(function (customerId) {
                _this.apiClient.fetchCustomerItemsDict(customerId, ['firstName', 'lastName', 'email', 'phone', 'addressStreet', 'addressZipcode', 'addressCity', 'addressCountry']).then(function (customer) {
                    _this.m.selectNr = null;
                    _this.m.editData = {
                        customerId: customerId,
                        nr: nr,
                        firstName: customer['firstName'],
                        lastName: customer['lastName'],
                        email: customer['email'],
                        phone: customer['phone'],
                        addressStreet: customer['addressStreet'],
                        addressZipcode: customer['addressZipcode'],
                        addressCity: customer['addressCity'],
                        addressCountry: customer['addressCountry']
                    };
                });
            });
        };
        ApplicationCustomerListController.prototype.addCustomerToModel = function (customerId) {
            this.m.customers.push({
                componentData: {
                    customerId: customerId,
                    onkycscreendone: null,
                    customerIdCompoundItemName: null,
                    applicantNr: null,
                    applicationNr: this.initialData.applicationInfo.ApplicationNr,
                    backTarget: this.initialData.backTarget
                },
                isRemoved: false
            });
        };
        ApplicationCustomerListController.prototype.addCustomerToMultipleEditorModel = function (customerId) {
            this.m.multipleEditorServiceCustomers.push({
                componentData: {
                    customerId: customerId,
                    onkycscreendone: null,
                    customerIdCompoundItemName: null,
                    applicantNr: null,
                    applicationNr: this.initialData.applicationInfo.ApplicationNr,
                    backTarget: this.initialData.backTarget
                },
                isRemoved: false
            });
        };
        ApplicationCustomerListController.prototype.addCustomer = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.initialData.multipleEditorService && this.initialData.multipleEditorService.includeCompanyRoles)
                this.addCustomerForMultipleEditorService();
            else {
                var d_1 = this.m.editData;
                this.initialData.editorService.addCustomer(d_1.customerId, d_1.nr, d_1.firstName, d_1.lastName, d_1.email, d_1.phone, d_1.addressStreet, d_1.addressZipcode, d_1.addressCity, d_1.addressCountry).then(function (wasAdded) {
                    if (wasAdded) {
                        _this.addCustomerToModel(d_1.customerId);
                    }
                    _this.m.selectNr = {};
                    _this.m.editData = null;
                });
            }
        };
        ApplicationCustomerListController.prototype.addCustomerForMultipleEditorService = function () {
            var _this = this;
            var d = this.m.editData;
            if (!d.isBeneficialOwner && !d.isAuthorizedSignatory)
                toastr.error('Customer must be at least one of Beneficial Owner and Authorized Signatory.');
            if (d.isBeneficialOwner) {
                this.initialData.multipleEditorService.editorService.addCustomer(d.customerId, d.nr, d.firstName, d.lastName, d.email, d.phone, d.addressStreet, d.addressZipcode, d.addressCity, d.addressCountry).then(function (wasAdded) {
                    if (wasAdded) {
                        _this.addCustomerToMultipleEditorModel(d.customerId);
                    }
                    _this.m.selectNr = {};
                    _this.m.editData = null;
                });
            }
            if (d.isAuthorizedSignatory) {
                this.initialData.editorService.addCustomer(d.customerId, d.nr, d.firstName, d.lastName, d.email, d.phone, d.addressStreet, d.addressZipcode, d.addressCity, d.addressCountry).then(function (wasAdded) {
                    if (wasAdded) {
                        _this.addCustomerToModel(d.customerId);
                    }
                    _this.m.selectNr = {};
                    _this.m.editData = null;
                });
            }
        };
        ApplicationCustomerListController.prototype.removeCustomer = function (model, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.editorService.removeCustomer(model.componentData.customerId).then(function (wasRemoved) {
                if (wasRemoved) {
                    model.isRemoved = true;
                }
                else {
                    toastr.error('Customer could not be removed');
                }
            });
        };
        ApplicationCustomerListController.prototype.removeMultipleEditorServiceCustomer = function (model, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.initialData.multipleEditorService.editorService.removeCustomer(model.componentData.customerId).then(function (wasRemoved) {
                if (wasRemoved) {
                    model.isRemoved = true;
                }
                else {
                    toastr.error('Customer could not be removed');
                }
            });
        };
        ApplicationCustomerListController.prototype.isValidCivicRegNr = function (value) {
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
        ApplicationCustomerListController.prototype.isToggleCompanyLoanDocumentCheckStatusAllowed = function () {
            if (!this.initialData) {
                return false;
            }
            var ai = this.initialData.applicationInfo;
            return ai.IsActive;
        };
        ApplicationCustomerListController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout'];
        return ApplicationCustomerListController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationCustomerListComponentNs.ApplicationCustomerListController = ApplicationCustomerListController;
    var ApplicationCustomerListComponent = /** @class */ (function () {
        function ApplicationCustomerListComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCustomerListController;
            this.templateUrl = 'application-customer-list.html';
        }
        return ApplicationCustomerListComponent;
    }());
    ApplicationCustomerListComponentNs.ApplicationCustomerListComponent = ApplicationCustomerListComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ApplicationCustomerListComponentNs.Model = Model;
    var CustomerModel = /** @class */ (function () {
        function CustomerModel() {
        }
        return CustomerModel;
    }());
    ApplicationCustomerListComponentNs.CustomerModel = CustomerModel;
    var SelectNrModel = /** @class */ (function () {
        function SelectNrModel() {
        }
        return SelectNrModel;
    }());
    ApplicationCustomerListComponentNs.SelectNrModel = SelectNrModel;
    var EditDataModel = /** @class */ (function () {
        function EditDataModel() {
        }
        return EditDataModel;
    }());
    ApplicationCustomerListComponentNs.EditDataModel = EditDataModel;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationCustomerListComponentNs.InitialData = InitialData;
    var CustomerApplicationListEditorService = /** @class */ (function () {
        function CustomerApplicationListEditorService(applicationNr, listName, apiClient) {
            this.applicationNr = applicationNr;
            this.listName = listName;
            this.apiClient = apiClient;
        }
        CustomerApplicationListEditorService.prototype.removeCustomer = function (customerId) {
            return this.apiClient.removeCustomerFromApplicationList(this.applicationNr, this.listName, customerId).then(function (x) { return x.WasRemoved; });
        };
        CustomerApplicationListEditorService.prototype.addCustomer = function (customerId, civicRegNr, firstName, lastName, email, phone, addressStreet, addressZipcode, addressCity, addressCountry) {
            return this.apiClient.addCustomerToApplicationList(this.applicationNr, this.listName, customerId, civicRegNr, firstName, lastName, email, phone, addressStreet, addressZipcode, addressCity, addressCountry).then(function (x) { return x.WasAdded; });
        };
        CustomerApplicationListEditorService.prototype.fetchCustomerIds = function () {
            return this.apiClient.fetchCustomerApplicationListMembers(this.applicationNr, this.listName).then(function (x) { return x.CustomerIds; });
        };
        return CustomerApplicationListEditorService;
    }());
    ApplicationCustomerListComponentNs.CustomerApplicationListEditorService = CustomerApplicationListEditorService;
    var ComplexApplicationListEditorService = /** @class */ (function () {
        function ComplexApplicationListEditorService(applicationNr, listName, nr, apiClient) {
            this.applicationNr = applicationNr;
            this.apiClient = apiClient;
            this.dataSourceName = 'ComplexApplicationList';
            this.dataSourceItemName = "".concat(listName, "#").concat(nr, "#r#customerIds");
        }
        ComplexApplicationListEditorService.prototype.changeCustomerIds = function (newCustomerId, isRemove) {
            var _this = this;
            var wasInList = false;
            return this.fetchCustomerIds().then(function (currentCustomerIds) {
                var newCustomerIds = [];
                for (var _i = 0, currentCustomerIds_1 = currentCustomerIds; _i < currentCustomerIds_1.length; _i++) {
                    var c = currentCustomerIds_1[_i];
                    if (c === newCustomerId) {
                        wasInList = true;
                        if (!isRemove) {
                            newCustomerIds.push(c.toString());
                        }
                    }
                    else {
                        newCustomerIds.push(c.toString());
                    }
                }
                if (!isRemove && !wasInList) {
                    newCustomerIds.push(newCustomerId.toString());
                }
                var wasChanged = (isRemove && wasInList) || (!isRemove && !wasInList);
                if (wasChanged) {
                    return _this.apiClient.setApplicationEditItemData(_this.applicationNr, _this.dataSourceName, _this.dataSourceItemName, JSON.stringify(newCustomerIds), false).then(function (x) { return true; });
                }
                else {
                    return false;
                }
            });
        };
        ComplexApplicationListEditorService.prototype.removeCustomer = function (customerId) {
            return this.changeCustomerIds(customerId, true);
        };
        ComplexApplicationListEditorService.prototype.addCustomer = function (customerId, civicRegNr, firstName, lastName, email, phone) {
            var _this = this;
            var p = {};
            var add = function (n, v) {
                if (v) {
                    p[n] = v;
                }
            };
            add('firstName', firstName);
            add('lastName', lastName);
            add('email', email);
            add('phone', phone);
            return this.apiClient.createOrUpdatePersonCustomerSimple(civicRegNr, p, customerId).then(function (_) {
                return _this.changeCustomerIds(customerId, false);
            });
        };
        ComplexApplicationListEditorService.prototype.fetchCustomerIds = function () {
            var r = {
                DataSourceName: this.dataSourceName,
                ErrorIfMissing: false,
                IncludeEditorModel: false,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                Names: [this.dataSourceItemName],
                ReplaceIfMissing: true
            };
            return this.apiClient.fetchApplicationDataSourceItems(this.applicationNr, [r]).then(function (x) {
                var rv = [];
                var i = x.Results[0].Items;
                if (i.length > 0) {
                    var value = i[0].Value;
                    if (value === ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        return rv;
                    }
                    for (var _i = 0, _a = JSON.parse(i[0].Value); _i < _a.length; _i++) {
                        var v = _a[_i];
                        rv.push(parseInt(v));
                    }
                }
                return rv;
            });
        };
        return ComplexApplicationListEditorService;
    }());
    ApplicationCustomerListComponentNs.ComplexApplicationListEditorService = ComplexApplicationListEditorService;
})(ApplicationCustomerListComponentNs || (ApplicationCustomerListComponentNs = {}));
angular.module('ntech.components').component('applicationCustomerList', new ApplicationCustomerListComponentNs.ApplicationCustomerListComponent());
