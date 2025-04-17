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
var VintageReportsComponentNs;
(function (VintageReportsComponentNs) {
    var VintageReportsController = /** @class */ (function (_super) {
        __extends(VintageReportsController, _super);
        function VintageReportsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        VintageReportsController.prototype.componentName = function () {
            return 'vintageReports';
        };
        VintageReportsController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage reports');
            this.apiClient.fetchAllProviders().then(function (allProviders) {
                _this.apiClient.fetchAllRiskGroups().then(function (allRiskGroups) {
                    _this.apiClient.fetchVintagePeriods(true).then(function (vintagePeriods) {
                        _this.m = {
                            balanceOption: '30+',
                            debtCollectionOption: '0+',
                            valueType: 'balance',
                            formatType: 'value',
                            providerName: '',
                            allProviders: allProviders.Providers,
                            riskGroup: '',
                            allRiskGroups: allRiskGroups.Groups,
                            yAxisFrom: '',
                            yAxisTo: '',
                            allVintageMonths: vintagePeriods.VintageMonths
                        };
                    });
                });
            });
        };
        VintageReportsController.prototype.getUrl = function () {
            if (!this.m) {
                return null;
            }
            var request = {
                ShowPercent: 'false',
                IncludeDetails: 'false',
                AxisScaleX: 'Month',
                AxisScaleY: 'Month'
            };
            if (this.m.balanceOption === '30' || this.m.balanceOption === '30+') {
                request.OverdueMonthsFrom = '1';
                if (this.m.balanceOption === '30') {
                    request.OverdueMonthsTo = '1';
                }
            }
            else if (this.m.balanceOption === '60' || this.m.balanceOption === '60+') {
                request.OverdueMonthsFrom = '2';
                if (this.m.balanceOption === '60') {
                    request.OverdueMonthsTo = '2';
                }
            }
            else if (this.m.balanceOption === '90' || this.m.balanceOption === '90+') {
                request.OverdueMonthsFrom = '3';
                if (this.m.balanceOption === '90') {
                    request.OverdueMonthsTo = '3';
                }
            }
            else if (this.m.balanceOption === 'exclude') {
                request.ExcludeCapitalBalance = 'true';
            } //else 0+ always include
            if (this.m.debtCollectionOption == '0') {
                request.IncludeDebtCollectionBalance = 'true';
            }
            else if (this.m.debtCollectionOption == '0+') {
                request.AccumulateDebtCollectionBalance = 'true';
            } //else exclude
            request.CellValueIsCount = (this.m.valueType === 'count') ? 'true' : 'false';
            request.ShowPercent = (this.m.formatType === 'percent') ? 'true' : 'false';
            request.ProviderName = this.m.providerName ? this.m.providerName : null;
            request.RiskGroup = this.m.riskGroup ? this.m.riskGroup : null;
            request.AxisYFrom = this.m.yAxisFrom ? this.m.yAxisFrom : null;
            request.AxisYTo = this.m.yAxisTo ? this.m.yAxisTo : null;
            request.TreatNotificationsAsClosedMaxBalance = this.initialData.treatNotificationsAsClosedMaxBalance
                ? this.initialData.treatNotificationsAsClosedMaxBalance.toString()
                : '';
            return createVintageReportUrl(request, this.apiClient);
        };
        VintageReportsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return VintageReportsController;
    }(NTechComponents.NTechComponentControllerBase));
    VintageReportsComponentNs.VintageReportsController = VintageReportsController;
    function createVintageReportUrl(request, apiClient) {
        var url = '/ui/s/vintage-report';
        var qs = apiClient.toQueryString(request);
        if (qs) {
            url += "?".concat(qs);
        }
        return url;
    }
    VintageReportsComponentNs.createVintageReportUrl = createVintageReportUrl;
    var VintageReportsComponent = /** @class */ (function () {
        function VintageReportsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = VintageReportsController;
            this.templateUrl = 'vintage-reports.html';
        }
        return VintageReportsComponent;
    }());
    VintageReportsComponentNs.VintageReportsComponent = VintageReportsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    VintageReportsComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    VintageReportsComponentNs.Model = Model;
})(VintageReportsComponentNs || (VintageReportsComponentNs = {}));
angular.module('ntech.components').component('vintageReports', new VintageReportsComponentNs.VintageReportsComponent());
