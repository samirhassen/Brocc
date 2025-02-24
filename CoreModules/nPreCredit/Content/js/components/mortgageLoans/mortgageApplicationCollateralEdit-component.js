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
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
var MortgageApplicationCollateralEditComponentNs;
(function (MortgageApplicationCollateralEditComponentNs) {
    MortgageApplicationCollateralEditComponentNs.ListName = 'ApplicationObject';
    MortgageApplicationCollateralEditComponentNs.FieldNames = ['propertyType', 'addressCity', 'addressStreet', 'addressZipCode',
        'use', 'plot', 'constructionYear', 'valuationAmount', 'valuationDate', 'valuationSource',
        'statValuationAmount', 'statValuationDate', 'priceAmount', 'priceAmountDate', 'securityElsewhereAmount', 'url', 'additionalInformation'];
    MortgageApplicationCollateralEditComponentNs.CompactFieldNames = ['propertyType', 'addressCity',
        'valuationAmount', 'statValuationAmount', 'priceAmount', 'securityElsewhereAmount', 'url'];
    MortgageApplicationCollateralEditComponentNs.ReloadCollateralEventName = 'MortgageApplicationCollateralEditComponentNs_ReloadCollateralEventName';
    function getDataSourceItemName(nr, itemName, repeatableCode) {
        return ComplexApplicationListHelper.getDataSourceItemName(MortgageApplicationCollateralEditComponentNs.ListName, nr, itemName, repeatableCode);
    }
    MortgageApplicationCollateralEditComponentNs.getDataSourceItemName = getDataSourceItemName;
    function reloadCollateralEstateData(applicationNr, apiClient, $q) {
        var d = new ApplicationDataSourceHelper.ApplicationDataSourceService(applicationNr, apiClient, $q, function (changes) { }, function (data) { });
        d.addComplexApplicationListItems([
            getDataSourceItemName(this.initialData.listNr, 'propertyType', ComplexApplicationListHelper.RepeatableCode.No),
            getDataSourceItemName(this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes)
        ]);
        return d.loadItems().then(function (x) {
            var currentPropertyType = null;
            var estateDeeds = [];
            for (var _i = 0, _a = x.Results; _i < _a.length; _i++) {
                var r = _a[_i];
                for (var _b = 0, _c = r.Items; _b < _c.length; _b++) {
                    var i = _c[_b];
                    var n = ComplexApplicationListHelper.parseCompoundItemName(i.Name);
                    if (n.itemName === 'propertyType' && i.Value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        currentPropertyType = i.Value;
                    }
                    else if (n.itemName === 'estateDeeds' && i.Value != ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        var models = JSON.parse(i.Value);
                        for (var _d = 0, models_1 = models; _d < models_1.length; _d++) {
                            var m = models_1[_d];
                            var model = JSON.parse(m);
                            estateDeeds.push(model);
                        }
                    }
                }
            }
            return {
                propertyType: currentPropertyType,
                estateDeeds: estateDeeds
            };
        });
    }
    MortgageApplicationCollateralEditComponentNs.reloadCollateralEstateData = reloadCollateralEstateData;
    var MortgageApplicationCollateralEditController = /** @class */ (function (_super) {
        __extends(MortgageApplicationCollateralEditController, _super);
        function MortgageApplicationCollateralEditController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            var reloadChildren = function () {
                var isReadonly = _this.getIsReadonly(_this.m.c.ai);
                _this.reloadCollateralChildren(_this.m.c.ai, isReadonly).then(function (d) {
                    _this.m.currentPropertyType = d.currentPropertyType;
                    _this.m.estateData = d.estateData;
                    _this.m.housingCompanyData = d.housingCompanyData;
                    _this.m.estateItems = d.estateItems;
                });
            };
            ntechComponentService.subscribeToNTechEvents(function (x) {
                if (x.eventName === ComplexListWithCustomerComponentNs.AfterEditEventName) {
                    var d = x.customData;
                    if (!d || !_this.m || d.correlationId !== _this.m.c.correlationId) {
                        return;
                    }
                    reloadChildren();
                }
                else if (x.eventName == MortgageApplicationCollateralEditComponentNs.ReloadCollateralEventName && _this.initialData && x.eventData == _this.initialData.applicationNr) {
                    reloadChildren();
                }
            });
            return _this;
        }
        MortgageApplicationCollateralEditController.prototype.componentName = function () {
            return 'mortgageApplicationCollateralEdit';
        };
        MortgageApplicationCollateralEditController.prototype.getIsReadonly = function (ai) {
            return !ai.IsActive || ai.IsFinalDecisionMade || ai.HasLockedAgreement;
        };
        MortgageApplicationCollateralEditController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var backContext = { listNr: i.listNr, applicationNr: i.applicationNr };
            this.apiClient.fetchApplicationInfo(i.applicationNr).then(function (x) {
                var isReadonly = _this.getIsReadonly(x);
                var ci = {
                    ai: x,
                    listName: MortgageApplicationCollateralEditComponentNs.ListName,
                    fieldNames: MortgageApplicationCollateralEditComponentNs.FieldNames,
                    listNr: parseInt(i.listNr),
                    correlationId: NTechComponents.generateUniqueId(6),
                    isReadonly: isReadonly
                };
                _this.reloadCollateralChildren(x, isReadonly).then(function (d) {
                    _this.m = {
                        c: __assign(__assign({}, i), ci),
                        currentPropertyType: d.currentPropertyType,
                        estateData: d.estateData,
                        housingCompanyData: d.housingCompanyData,
                        estateItems: d.estateItems,
                        isReadonly: isReadonly,
                        headerData: {
                            host: _this.initialData,
                            backTarget: NavigationTargetHelper.createCodeOrUrlFromInitialData(_this.initialData, backContext, NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication),
                            backContext: backContext
                        }
                    };
                    _this.m.c.backTarget = NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral, backContext).targetCode;
                });
            });
        };
        MortgageApplicationCollateralEditController.prototype.reloadCollateralChildren = function (ai, isReadonly) {
            var _this = this;
            var d = new ApplicationDataSourceHelper.ApplicationDataSourceService(this.initialData.applicationNr, this.apiClient, this.$q, function (changes) { }, function (data) { });
            d.addComplexApplicationListItems([
                getDataSourceItemName(this.initialData.listNr, 'propertyType', ComplexApplicationListHelper.RepeatableCode.No),
                getDataSourceItemName(this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes)
            ]);
            return reloadCollateralEstateData(this.initialData.applicationNr, this.apiClient, this.$q).then(function (x) {
                if (!x.estateDeeds) {
                    x.estateDeeds = [];
                }
                var estateItemsService = new EstateItemsService(ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement, _this.apiClient, _this.$q, ai.ApplicationNr, getDataSourceItemName(_this.initialData.listNr, 'estateDeeds', ComplexApplicationListHelper.RepeatableCode.Yes), _this.ntechComponentService);
                for (var _i = 0, _a = x.estateDeeds; _i < _a.length; _i++) {
                    var estateDeed = _a[_i];
                    var virtualDataSource = new VirtualEstateItemsDataSourceService(_this.$q, estateDeed, _this.formatNumberForEdit, _this.parseDecimalOrNull, estateItemsService);
                    var editorInitialData = ApplicationEditorComponentNs.createInitialDataVirtual(virtualDataSource, ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral), {
                        isInPlaceEditAllowed: !isReadonly,
                    });
                    estateItemsService.rows.push({ uid: estateDeed.uid, d: editorInitialData, viewDetailsUrl: null });
                }
                var create = function (a) {
                    return ApplicationEditorComponentNs.createInitialData(ai.ApplicationNr, ai.ApplicationType, NavigationTargetHelper.createCodeTarget(NavigationTargetHelper.NavigationTargetCode.MortgageLoanEditCollateral), _this.apiClient, _this.$q, a, {
                        isInPlaceEditAllowed: !isReadonly,
                    });
                };
                return {
                    housingCompanyData: create(function (x) {
                        x.addComplexApplicationListItems([getDataSourceItemName(_this.initialData.listNr, 'housingCompanyName', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(_this.initialData.listNr, 'housingApartmentNumber', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(_this.initialData.listNr, 'housingCompanyShareCount', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(_this.initialData.listNr, 'housingSuperCertDate', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(_this.initialData.listNr, 'housingCompanyLoans', ComplexApplicationListHelper.RepeatableCode.No)
                        ]);
                    }),
                    estateData: create(function (x) {
                        x.addComplexApplicationListItems([
                            getDataSourceItemName(_this.initialData.listNr, 'estatePropertyId', ComplexApplicationListHelper.RepeatableCode.No),
                            getDataSourceItemName(_this.initialData.listNr, 'estateRegisterUnit', ComplexApplicationListHelper.RepeatableCode.No)
                        ]);
                    }),
                    estateItems: estateItemsService,
                    currentPropertyType: x.propertyType
                };
            });
        };
        MortgageApplicationCollateralEditController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationCollateralEditController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationCollateralEditComponentNs.MortgageApplicationCollateralEditController = MortgageApplicationCollateralEditController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationCollateralEditComponentNs.Model = Model;
    var MortgageApplicationCollateralEditComponent = /** @class */ (function () {
        function MortgageApplicationCollateralEditComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationCollateralEditController;
            this.template = "<div ng-if=\"$ctrl.m\">\n    <page-header initial-data=\"$ctrl.m.headerData\" title-text=\"'Edit collateral'\"></page-header>\n\n    <complex-list-with-customer initial-data=\"$ctrl.m.c\" ></complex-list-with-customer>\n\n    <div class=\"row pt-2\" ng-if=\"$ctrl.m.currentPropertyType === 'estate'\">\n        <div class=\"col-sm-5\">\n            <div class=\"editblock\">\n                <application-editor initial-data=\"$ctrl.m.estateData\"></application-editor>\n                <div>\n                    <h3>Estate mortgage deeds</h3>\n                    <hr class=\"hr-section\" />\n                    <button class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.m.estateItems.addRow($event)\" ng-if=\"$ctrl.m.estateItems.isEditAllowed\">Add</button>\n                </div>\n                <hr class=\"hr-section dotted\" />\n\n                <div class=\"row\" ng-repeat=\"c in $ctrl.m.estateItems.rows\">\n                    <div class=\"col-sm-8\">\n                        <div>\n                            <application-editor initial-data=\"c.d\"></application-editor>\n                        </div>\n                    </div>\n                    <div class=\"col-sm-4 text-right\">\n                        <button ng-if=\"$ctrl.m.estateItems.isEditAllowed\" class=\"n-icon-btn n-red-btn\" ng-click=\"$ctrl.m.estateItems.deleteRow(c.uid, $event)\"><span class=\"glyphicon glyphicon-minus\"></span></button>\n                    </div>\n                    <div class=\"clearfix\"></div>\n                    <hr class=\"hr-section dotted\">\n                </div>\n            </div>\n        </div>\n    </div>\n\n    <div class=\"row pt-2\" ng-if=\"$ctrl.m.currentPropertyType === 'housingCompany'\">\n        <div class=\"col-sm-5\"><div class=\"editblock\"><application-editor initial-data=\"$ctrl.m.housingCompanyData\"></application-editor></div></div>\n    </div>\n</div>";
        }
        return MortgageApplicationCollateralEditComponent;
    }());
    MortgageApplicationCollateralEditComponentNs.MortgageApplicationCollateralEditComponent = MortgageApplicationCollateralEditComponent;
    var EstateItemsService = /** @class */ (function () {
        function EstateItemsService(isEditAllowed, apiClient, $q, applicationNr, estateDeedsDataSourceItemName, ntechComponentService) {
            this.isEditAllowed = isEditAllowed;
            this.apiClient = apiClient;
            this.$q = $q;
            this.applicationNr = applicationNr;
            this.estateDeedsDataSourceItemName = estateDeedsDataSourceItemName;
            this.ntechComponentService = ntechComponentService;
            this.rows = [];
        }
        EstateItemsService.prototype.addRow = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var newItem = {
                uid: NTechComponents.generateUniqueId(10),
                deedAmount: null,
                deedNr: null
            };
            this.changeValue(function (currentValues) {
                currentValues.push(newItem);
                return currentValues;
            });
        };
        EstateItemsService.prototype.deleteRow = function (uid, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.changeValue(function (currentValues) {
                var newValues = [];
                for (var _i = 0, currentValues_1 = currentValues; _i < currentValues_1.length; _i++) {
                    var v = currentValues_1[_i];
                    if (v.uid !== uid) {
                        newValues.push(v);
                    }
                }
                return newValues;
            });
        };
        EstateItemsService.prototype.changeValue = function (transform) {
            var _this = this;
            return reloadCollateralEstateData(this.applicationNr, this.apiClient, this.$q).then(function (x) {
                if (!x.estateDeeds) {
                    x.estateDeeds = [];
                }
                var newValues = transform(x.estateDeeds);
                var values = [];
                for (var _i = 0, newValues_1 = newValues; _i < newValues_1.length; _i++) {
                    var v = newValues_1[_i];
                    values.push(JSON.stringify(v));
                }
                var value = JSON.stringify(values);
                return _this.apiClient.setApplicationEditItemData(_this.applicationNr, 'ComplexApplicationList', _this.estateDeedsDataSourceItemName, value, false).then(function (x) {
                    _this.ntechComponentService.emitNTechEvent(MortgageApplicationCollateralEditComponentNs.ReloadCollateralEventName, _this.applicationNr);
                    return x;
                });
            });
        };
        return EstateItemsService;
    }());
    MortgageApplicationCollateralEditComponentNs.EstateItemsService = EstateItemsService;
    var VirtualEstateItemsDataSourceService = /** @class */ (function () {
        function VirtualEstateItemsDataSourceService($q, estateDeed, formatNrForEdit, parseNrForStorage, estateItemsService) {
            this.$q = $q;
            this.estateDeed = estateDeed;
            this.formatNrForEdit = formatNrForEdit;
            this.parseNrForStorage = parseNrForStorage;
            this.estateItemsService = estateItemsService;
        }
        VirtualEstateItemsDataSourceService.prototype.getIncludedItems = function () {
            return [{
                    dataSourceName: 'VirtualEstateItem',
                    itemName: 'deedAmount',
                    forceReadonly: false,
                    isNavigationEditOrViewPossible: false
                }, {
                    dataSourceName: 'VirtualEstateItem',
                    itemName: 'deedNr',
                    forceReadonly: false,
                    isNavigationEditOrViewPossible: false
                }];
        };
        VirtualEstateItemsDataSourceService.prototype.loadItems = function () {
            var r = {
                Results: [{
                        ChangedNames: [],
                        DataSourceName: 'VirtualEstateItem',
                        MissingNames: [],
                        Items: [
                            {
                                Name: 'deedAmount',
                                Value: this.formatNrForEdit(this.estateDeed.deedAmount),
                                EditorModel: {
                                    DataSourceName: 'VirtualEstateItem',
                                    DataType: 'positiveDecimal',
                                    EditorType: 'text',
                                    ItemName: 'deedAmount',
                                    LabelText: 'Deed amount',
                                    DropdownRawDisplayTexts: null,
                                    DropdownRawOptions: null,
                                    IsRemovable: false,
                                    IsRequired: true
                                }
                            },
                            {
                                Name: 'deedNr',
                                Value: this.estateDeed.deedNr,
                                EditorModel: {
                                    DataSourceName: 'VirtualEstateItem',
                                    DataType: 'string',
                                    EditorType: 'text',
                                    ItemName: 'deedNr',
                                    LabelText: 'Deed nr',
                                    DropdownRawDisplayTexts: null,
                                    DropdownRawOptions: null,
                                    IsRemovable: false,
                                    IsRequired: true
                                }
                            }
                        ]
                    }]
            };
            var p = this.$q.defer();
            p.resolve(r);
            return p.promise;
        };
        VirtualEstateItemsDataSourceService.prototype.saveItems = function (edits) {
            var _this = this;
            return this.estateItemsService.changeValue(function (currentValues) {
                var newValues = [];
                for (var _i = 0, currentValues_2 = currentValues; _i < currentValues_2.length; _i++) {
                    var v = currentValues_2[_i];
                    if (v.uid === _this.estateDeed.uid && edits) {
                        for (var _a = 0, edits_1 = edits; _a < edits_1.length; _a++) {
                            var e = edits_1[_a];
                            if (e.itemName === 'deedAmount') {
                                v.deedAmount = _this.parseNrForStorage(e.toValue);
                            }
                            else if (e.itemName === 'deedNr') {
                                v.deedNr = e.toValue;
                            }
                        }
                    }
                    newValues.push(v);
                }
                return newValues;
            }).then(function (x) { return [x]; });
        };
        return VirtualEstateItemsDataSourceService;
    }());
    MortgageApplicationCollateralEditComponentNs.VirtualEstateItemsDataSourceService = VirtualEstateItemsDataSourceService;
})(MortgageApplicationCollateralEditComponentNs || (MortgageApplicationCollateralEditComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationCollateralEdit', new MortgageApplicationCollateralEditComponentNs.MortgageApplicationCollateralEditComponent());
