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
var LegacyCustomerCardComponentNs;
(function (LegacyCustomerCardComponentNs) {
    var LegacyCustomerCardController = /** @class */ (function (_super) {
        __extends(LegacyCustomerCardController, _super);
        function LegacyCustomerCardController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.apiClient = new NTechCustomerApi.ApiClient(function (msg) { return toastr.error(msg); }, $http, $q);
            return _this;
        }
        LegacyCustomerCardController.prototype.componentName = function () {
            return 'legacyCustomerCard';
        };
        LegacyCustomerCardController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q);
        };
        LegacyCustomerCardController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            this.prettyPrinter = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchLegacyCustomerCardUiData(this.initialData.customerId, this.initialData.backUrl).then(function (r) {
                _this.prettyPrinter = new CustomerCardPrettyPrinterNs.CustomerCardPrettyPrinter();
                _this.m = {
                    app: {
                        backUrl: _this.initialData.backUrl,
                        customerCard: {
                            customerId: _this.initialData.customerId,
                            items: r.customerCardItems
                        },
                        customerId: _this.initialData.customerId
                    },
                    editMode: false,
                    latestSavedData: null
                };
                _this.m.app.customerCard.items.forEach(function (item, index) {
                    item.FriendlyName = _this.prettyPrinter.getFriendlyName(item.Group, item.Name);
                    if (!item.Locked) {
                        item.FriendlyValue = _this.prettyPrinter.getFriendlyValue(item.Group, item.Name, item.Value);
                    }
                });
                _this.updateLatestSaved();
            });
        };
        LegacyCustomerCardController.prototype.itemByName = function (name) {
            if (!this.m) {
                return null;
            }
            for (var _i = 0, _a = this.m.app.customerCard.items; _i < _a.length; _i++) {
                var i = _a[_i];
                if (i.Name === name) {
                    return i;
                }
            }
            return null;
        };
        LegacyCustomerCardController.prototype.formatPhoneNr = function (nr) {
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
        };
        LegacyCustomerCardController.prototype.restoreLatestSaved = function () {
            this.m.app = JSON.parse(JSON.stringify(this.m.latestSavedData));
        };
        LegacyCustomerCardController.prototype.shouldBeSaved = function (item) {
            if (item.Locked || item.Group === 'civicRegNr' || item.Group === 'pep' || item.Group === 'taxResidency' || item.Group === 'amlCft') {
                return false;
            }
            if (item.Name == 'includeInFatcaExport' || item.Name == 'tin' || item.Name == 'taxcountries') {
                return false;
            }
            return true;
        };
        LegacyCustomerCardController.prototype.findSavedItem = function (name) {
            var foundItem = null;
            angular.forEach(this.m.latestSavedData.customerCard.items, function (newItem) {
                if (newItem.Name === name) {
                    foundItem = newItem;
                }
            });
            return foundItem;
        };
        LegacyCustomerCardController.prototype.updateLatestSaved = function () {
            this.m.latestSavedData = JSON.parse(JSON.stringify(this.m.app));
        };
        LegacyCustomerCardController.prototype.formatValue = function (item) {
            if (!item) {
                return item;
            }
            else if (item.Name === 'phone') {
                return this.formatPhoneNr(item.Value);
            }
            else {
                return item.Value;
            }
        };
        LegacyCustomerCardController.prototype.currentLanguage = function () {
            return "sv";
        };
        LegacyCustomerCardController.prototype.toggleEditMode = function () {
            if (this.m.editMode) {
                this.restoreLatestSaved();
            }
            this.m.editMode = !this.m.editMode;
        };
        LegacyCustomerCardController.prototype.save = function () {
            var _this = this;
            var itemsToSave = angular.copy(this.m.app.customerCard.items).filter(this.shouldBeSaved);
            var isInvalid = false;
            var invalidItems = "";
            angular.forEach(itemsToSave, function (newItem) {
                if (newItem.Locked === false) {
                    if (ntech.forms.isNullOrWhitespace(newItem.Value)) {
                        invalidItems = invalidItems + " " + newItem.Name;
                        isInvalid = true;
                    }
                    else if (newItem.UiType == "Date" && !_this.isValidDate(newItem.Value)) {
                        invalidItems = invalidItems + " " + newItem.Name;
                        isInvalid = true;
                    }
                    else if (newItem.UiType == "Email" && newItem.Value.indexOf('@') < 0) {
                        invalidItems = invalidItems + " " + newItem.Name;
                        isInvalid = true;
                    }
                    else if (newItem.UiType == "Boolean" && newItem.Value !== true && newItem.Value !== false && newItem.Value !== "true" && newItem.Value !== "false") {
                        invalidItems = invalidItems + " " + newItem.Name;
                        isInvalid = true;
                    }
                }
            });
            if (isInvalid) {
                toastr.warning("Cannot save because the following are invalid: " + invalidItems);
                return;
            }
            //Filter out items that have not changed
            itemsToSave = itemsToSave.filter(function (newItem) {
                var oldItem = _this.findSavedItem(newItem.Name);
                if (oldItem && angular.toJson(oldItem.Value) !== angular.toJson(newItem.Value)) {
                    return true;
                }
                return false;
            });
            if (itemsToSave.length == 0) {
                toastr.warning("Nothing has been changed");
                return;
            }
            for (var _i = 0, itemsToSave_1 = itemsToSave; _i < itemsToSave_1.length; _i++) {
                var i = itemsToSave_1[_i];
                i.CustomerId = this.m.app.customerId;
            }
            this.apiClient.updateCustomer(itemsToSave, true).then(function (result) {
                _this.onChanges();
            });
        };
        LegacyCustomerCardController.prototype.unlock = function (sensitiveItem, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.unlockSensitiveItemByName(sensitiveItem.CustomerId, sensitiveItem.Name).then(function (value) {
                sensitiveItem.Locked = false;
                sensitiveItem.Value = value;
            });
        };
        LegacyCustomerCardController.prototype.removeIsFlaggedForRemoval = function (item) {
            return !(item.isFlaggedForRemoval === true);
        };
        LegacyCustomerCardController.$inject = ['$http', '$q', 'ntechComponentService'];
        return LegacyCustomerCardController;
    }(NTechComponents.NTechComponentControllerBase));
    LegacyCustomerCardComponentNs.LegacyCustomerCardController = LegacyCustomerCardController;
    var LegacyCustomerCardComponent = /** @class */ (function () {
        function LegacyCustomerCardComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = LegacyCustomerCardController;
            this.templateUrl = 'legacy-customer-card.html';
        }
        return LegacyCustomerCardComponent;
    }());
    LegacyCustomerCardComponentNs.LegacyCustomerCardComponent = LegacyCustomerCardComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    LegacyCustomerCardComponentNs.InitialData = InitialData;
})(LegacyCustomerCardComponentNs || (LegacyCustomerCardComponentNs = {}));
angular.module('ntech.components').component('legacyCustomerCard', new LegacyCustomerCardComponentNs.LegacyCustomerCardComponent());
