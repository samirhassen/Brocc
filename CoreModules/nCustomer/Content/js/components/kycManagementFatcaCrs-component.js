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
var KycManagementFatcaCrsComponentNs;
(function (KycManagementFatcaCrsComponentNs) {
    var KycManagementFatcaCrsController = /** @class */ (function (_super) {
        __extends(KycManagementFatcaCrsController, _super);
        function KycManagementFatcaCrsController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            return _this;
        }
        KycManagementFatcaCrsController.prototype.componentName = function () {
            return 'kycManagementFatcaCrs';
        };
        KycManagementFatcaCrsController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.refresh();
        };
        KycManagementFatcaCrsController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q);
        };
        KycManagementFatcaCrsController.prototype.refresh = function () {
            var _this = this;
            this.apiClient.fetchCustomerItemsDict(this.initialData.customerId, ['includeInFatcaExport', 'taxcountries', 'citizencountries']).then(function (c) {
                _this.apiClient.kycManagementFetchLatestCustomerQuestionsSet(_this.initialData.customerId).then(function (q) {
                    _this.m = {
                        includeInFatcaExport: _this.getValue(c, 'includeInFatcaExport', 'unknown'),
                        latestCustomerQuestionsSet: _this.filterQuestions(q),
                        customerInfoInitialData: {
                            customerId: _this.initialData.customerId,
                            backUrl: _this.initialData.backUrl
                        },
                        fatcaEditModel: null,
                        isTinUnlocked: false,
                        tin: null,
                        taxCountries: _this.parseCountriesFromString(_this.getValue(c, 'taxcountries', '[]'), true),
                        taxCountriesHistoryItems: null,
                        taxCountriesEdit: null,
                        citizenCountries: _this.parseCountriesFromString(_this.getValue(c, 'citizencountries', '[]'), false),
                        citizenCountriesHistoryItems: null,
                        citizenCountriesEdit: null,
                        customerRelations: null
                    };
                    _this.apiClient.fetchCustomerRelations(_this.initialData.customerId).then(function (relationResult) {
                        if (_this.m) {
                            _this.m.customerRelations = relationResult.CustomerRelations;
                        }
                    });
                });
            });
        };
        KycManagementFatcaCrsController.prototype.getRelationName = function (r) {
            if (!r) {
                return '';
            }
            var typeTag = r.RelationType;
            if (r.RelationType === 'Credit_UnsecuredLoan') {
                typeTag = 'Unsecured loan';
            }
            else if (r.RelationType === 'Credit_MortgageLoan') {
                typeTag = 'Mortgage loan';
            }
            else if (r.RelationType === 'SavingsAccount_StandardAccount') {
                typeTag = 'Savings account';
            }
            return "".concat(typeTag, " ").concat(r.RelationId);
        };
        KycManagementFatcaCrsController.prototype.parseCountriesFromString = function (c, isTaxCountries) {
            if (!c) {
                return [];
            }
            if (isTaxCountries) {
                var cs = JSON.parse(c);
                return _.pluck(cs, 'countryIsoCode');
            }
            else {
                return JSON.parse(c);
            }
        };
        KycManagementFatcaCrsController.prototype.loadTin = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['tin']).then(function (c) {
                _this.m.isTinUnlocked = true;
                _this.m.tin = _this.getValue(c.CurrentValues, 'tin', '');
            });
        };
        KycManagementFatcaCrsController.prototype.editFatca = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['tin', 'includeInFatcaExport']).then(function (c) {
                _this.m.fatcaEditModel = {
                    includeInFatcaExport: _this.getValue(c.CurrentValues, 'includeInFatcaExport', 'unknown'),
                    tin: _this.getValue(c.CurrentValues, 'tin', ''),
                    historyItems: c.HistoryItems
                };
            });
        };
        KycManagementFatcaCrsController.prototype.getFatcaDisplayValue = function (v) {
            if (v === 'true') {
                return 'Yes';
            }
            else if (v === 'false') {
                return 'No';
            }
            else {
                return '';
            }
        };
        KycManagementFatcaCrsController.prototype.cancelFatcaEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.fatcaEditModel = null;
        };
        KycManagementFatcaCrsController.prototype.saveFatcaEdit = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var items = null;
            if (this.m.fatcaEditModel.includeInFatcaExport === 'true' || this.m.fatcaEditModel.includeInFatcaExport === 'false') {
                items = [{ Name: 'includeInFatcaExport', Value: this.m.fatcaEditModel.includeInFatcaExport, Group: 'fatca', IsSensitive: false, CustomerId: this.initialData.customerId }];
                if (this.m.fatcaEditModel.includeInFatcaExport === 'true') {
                    items.push({ Name: 'tin', Value: this.m.fatcaEditModel.tin, Group: 'fatca', IsSensitive: true, CustomerId: this.initialData.customerId });
                }
            }
            if (items == null) {
                return;
            }
            this.apiClient.updateCustomer(items, true).then(function (x) {
                _this.refresh();
            });
        };
        KycManagementFatcaCrsController.prototype.getCountryName = function (countryIsoCode) {
            return this.initialData.allCountryCodesAndNames[countryIsoCode];
        };
        KycManagementFatcaCrsController.prototype.editTaxCountries = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['taxcountries']).then(function (x) {
                _this.m.taxCountriesHistoryItems = x.HistoryItems;
                _this.m.taxCountriesEdit = {
                    allCountryCodesAndNames: _this.initialData.allCountryCodesAndNames,
                    countryIsoCodes: _this.parseCountriesFromString(_this.getValue(x.CurrentValues, 'taxcountries', '[]'), true),
                    labelText: 'Tax recidency countries',
                    onSaveEdit: function (newTaxCountries) {
                        var tcs = [];
                        for (var _i = 0, newTaxCountries_1 = newTaxCountries; _i < newTaxCountries_1.length; _i++) {
                            var t = newTaxCountries_1[_i];
                            tcs.push({ countryIsoCode: t });
                        }
                        var customerProperties = [{
                                CustomerId: _this.initialData.customerId,
                                Group: 'taxResidency',
                                IsSensitive: false,
                                Name: 'taxcountries',
                                Value: JSON.stringify(tcs)
                            }];
                        _this.apiClient.updateCustomer(customerProperties, true).then(function (x) {
                            _this.m.taxCountriesEdit = null;
                            _this.m.taxCountriesHistoryItems = null;
                            _this.refresh();
                        });
                    },
                    onCancelEdit: function () {
                        _this.m.taxCountriesEdit = null;
                    },
                    historyItems: CountryListPropertyComponentNs.createEditHistoryItems(_this.m.taxCountriesHistoryItems, function (x) {
                        return _this.parseCountriesFromString(_this.getValue(x.Values, 'taxcountries', '[]'), true);
                    })
                };
            });
        };
        KycManagementFatcaCrsController.prototype.editCitizenCountries = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.fetchCustomerPropertiesWithGroupedEditHistory(this.initialData.customerId, ['citizencountries']).then(function (x) {
                _this.m.citizenCountriesHistoryItems = x.HistoryItems;
                _this.m.citizenCountriesEdit = {
                    allCountryCodesAndNames: _this.initialData.allCountryCodesAndNames,
                    countryIsoCodes: _this.parseCountriesFromString(_this.getValue(x.CurrentValues, 'citizencountries', '[]'), false),
                    labelText: 'Citizen countries',
                    onSaveEdit: function (newCitizenCountries) {
                        var customerProperties = [{
                                CustomerId: _this.initialData.customerId,
                                Group: 'taxResidency',
                                IsSensitive: false,
                                Name: 'citizencountries',
                                Value: JSON.stringify(newCitizenCountries)
                            }];
                        _this.apiClient.updateCustomer(customerProperties, true).then(function (x) {
                            _this.m.citizenCountriesEdit = null;
                            _this.m.citizenCountriesHistoryItems = null;
                            _this.refresh();
                        });
                    },
                    onCancelEdit: function () {
                        _this.m.citizenCountriesEdit = null;
                    },
                    historyItems: CountryListPropertyComponentNs.createEditHistoryItems(_this.m.citizenCountriesHistoryItems, function (x) {
                        return _this.parseCountriesFromString(_this.getValue(x.Values, 'citizencountries', '[]'), false);
                    })
                };
            });
        };
        KycManagementFatcaCrsController.prototype.getValue = function (items, n, defaultValue) {
            if (Object.keys(items).indexOf(n) < 0) {
                return defaultValue;
            }
            else {
                var v = items[n];
                if (v === null) {
                    return defaultValue;
                }
                else {
                    return v;
                }
            }
        };
        KycManagementFatcaCrsController.prototype.filterQuestions = function (q) {
            if (!q || !q.Items || q.Items.length == 0) {
                return null;
            }
            var items = _.filter(q.Items, function (x) { return x.QuestionCode !== 'ispep' && x.QuestionCode !== 'pep'; });
            if (items.length == 0) {
                return null;
            }
            return {
                AnswerDate: q.AnswerDate,
                CustomerId: q.CustomerId,
                Source: q.Source,
                Items: items
            };
        };
        KycManagementFatcaCrsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return KycManagementFatcaCrsController;
    }(NTechComponents.NTechComponentControllerBase));
    KycManagementFatcaCrsComponentNs.KycManagementFatcaCrsController = KycManagementFatcaCrsController;
    var TaxCountryModel = /** @class */ (function () {
        function TaxCountryModel() {
        }
        return TaxCountryModel;
    }());
    var KycManagementFatcaCrsComponent = /** @class */ (function () {
        function KycManagementFatcaCrsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementFatcaCrsController;
            this.templateUrl = 'kyc-management-fatca-crs.html';
        }
        return KycManagementFatcaCrsComponent;
    }());
    KycManagementFatcaCrsComponentNs.KycManagementFatcaCrsComponent = KycManagementFatcaCrsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    KycManagementFatcaCrsComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    KycManagementFatcaCrsComponentNs.Model = Model;
    var FatcaEditModel = /** @class */ (function () {
        function FatcaEditModel() {
        }
        return FatcaEditModel;
    }());
    KycManagementFatcaCrsComponentNs.FatcaEditModel = FatcaEditModel;
})(KycManagementFatcaCrsComponentNs || (KycManagementFatcaCrsComponentNs = {}));
angular.module('ntech.components').component('kycManagementFatcaCrs', new KycManagementFatcaCrsComponentNs.KycManagementFatcaCrsComponent());
