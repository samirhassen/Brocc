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
var VintageReportComponentNs;
(function (VintageReportComponentNs) {
    var VintageReportController = /** @class */ (function (_super) {
        __extends(VintageReportController, _super);
        function VintageReportController($http, $q, ntechComponentService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            //red: #F8696B, yellow:  #FFEB84, green: #63BE7B. See https://github.com/gka/chroma.js
            _this.greenToYellowToRed = ['#63be7b', '#6ac07b', '#72c27c', '#79c37c', '#80c57d', '#88c77d', '#8fca7d', '#96cb7e', '#9dce7e', '#a3cf7f', '#a8d17f', '#afd37f', '#b4d580', '#bbd680', '#c2d980', '#c7da81', '#cedc81', '#d3de81', '#d9e082', '#dfe182', '#e5e383', '#eae583', '#f0e783', '#f7e884', '#fdea84', '#ffe884', '#ffe383', '#ffde82', '#ffda81', '#ffd580', '#ffd07f', '#ffcb7e', '#ffc67d', '#ffc07c', '#ffbc7b', '#ffb77a', '#ffb279', '#ffac78', '#ffa776', '#fea275', '#fe9c74', '#fd9774', '#fd9172', '#fc8d71', '#fc8670', '#fb816f', '#fa7b6e', '#fa756d', '#f96f6c', '#f8696b'];
            _this.redToYellowToGreen = angular.copy(_this.greenToYellowToRed).reverse();
            return _this;
        }
        VintageReportController.prototype.componentName = function () {
            return 'vintageReport';
        };
        VintageReportController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            this.destroyWeightedAverageGraph();
            if (!this.initialData) {
                return;
            }
            this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage report');
            var r = angular.copy(this.initialData.params);
            r.IncludeDetails = 'false';
            r.ShowPercent = 'false';
            this.apiClient.fetchVintageReportData(r).then(function (x) {
                var dataColumns = [];
                var weightedAverages = [];
                var columnSums = [];
                for (var i = 1; i <= x.ColumnCount; i++) {
                    dataColumns.push(i);
                    var sumAndWeightedAverage = _this.computeSumAndWeightedAverageForColumn(i - 1, x);
                    columnSums.push(sumAndWeightedAverage[0]);
                    weightedAverages.push(sumAndWeightedAverage[1]);
                }
                _this.m = {
                    showGraph: false,
                    initialValueFormat: _this.initialData.params.CellValueIsCount === 'true' ? 'count' : 'value',
                    cellFormat: _this.initialData.params.ShowPercent === 'true' ? 'percent' : (_this.initialData.params.CellValueIsCount ? 'count' : 'value'),
                    rows: x.DataRows,
                    detailRows: x.DetailRows,
                    dataColumns: dataColumns,
                    columnSums: columnSums,
                    weightedAverages: weightedAverages,
                    creationDate: x.CreationDate,
                    colorScheme: ''
                };
                _this.updateWeightedAverageGraph(dataColumns, weightedAverages);
            }).finally(function () {
                _this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage report');
            });
        };
        VintageReportController.prototype.getPercentCellValue = function (row, columnIndex) {
            if (!this.m) {
                return null;
            }
            var colValue = row.ColumnValues[columnIndex];
            if (colValue === null) {
                return null;
            }
            return row.InitialValue === 0 ? 0 : (100 * colValue / row.InitialValue);
        };
        VintageReportController.prototype.destroyWeightedAverageGraph = function () {
            if (this.weightedAverageChart) {
                this.weightedAverageChart.destroy();
                this.weightedAverageChart = null;
            }
        };
        VintageReportController.prototype.updateWeightedAverageGraph = function (columns, weightedAverages) {
            var dataLine = {
                labels: columns,
                datasets: [
                    {
                        backgroundColor: 'rgb(0,178,147)',
                        borderColor: 'rgb(0,178,147)',
                        type: 'line',
                        fill: false,
                        data: weightedAverages
                    }
                ]
            };
            this.destroyWeightedAverageGraph();
            var e = document.getElementById("weightedAverageChart");
            this.weightedAverageChart = new Chart(e.getContext('2d'), {
                type: 'line',
                data: dataLine,
                options: {
                    elements: { point: { radius: 3 } },
                    scales: {
                        xAxes: [{
                                display: true,
                                gridLines: {
                                    display: false
                                }
                            }],
                        yAxes: [{
                                display: true,
                                gridLines: {
                                    display: false
                                },
                                ticks: {
                                    suggestedMin: 0,
                                    suggestedMax: 100
                                }
                            }]
                    },
                    legend: {
                        display: false
                    },
                    tooltips: {}
                }
            });
        };
        VintageReportController.prototype.computeSumAndWeightedAverageForColumn = function (columnIndex, result) {
            var accInitialValue = 0;
            var accValue = 0;
            for (var rowIndex = 0; rowIndex < result.DataRows.length; rowIndex++) {
                var value = result.DataRows[rowIndex].ColumnValues[columnIndex];
                if (value !== null) {
                    accInitialValue += result.DataRows[rowIndex].InitialValue;
                    accValue += value;
                }
            }
            return [accValue, accInitialValue == 0 ? 0 : (100 * accValue / accInitialValue)];
        };
        VintageReportController.prototype.getDownloadDataUrl = function () {
            var r = angular.copy(this.initialData.params);
            r.IncludeDetails = 'true';
            var url = '/api/Reports/Vintage/Get';
            var qs = this.apiClient.toQueryString(r);
            if (qs) {
                url += "?".concat(qs);
            }
            return url;
        };
        VintageReportController.prototype.getCellColor = function (columnIndex, row) {
            if (!this.m) {
                return null;
            }
            var cs = [];
            if (this.m.colorScheme === 'greenYellowRed') {
                cs = this.greenToYellowToRed;
            }
            else if (this.m.colorScheme === 'redYellowGreen') {
                cs = this.redToYellowToGreen;
            }
            else {
                return null;
            }
            var cellValue = this.m.cellFormat === 'percent' ? this.getPercentCellValue(row, columnIndex) : row.ColumnValues[columnIndex];
            if (cellValue === null) {
                return null;
            }
            //This is wasteful ... cache if it becomes a problem
            var columnValues = [];
            for (var _i = 0, _a = this.m.rows; _i < _a.length; _i++) {
                var r = _a[_i];
                var v = this.m.cellFormat === 'percent' ? this.getPercentCellValue(r, columnIndex) : r.ColumnValues[columnIndex];
                if (v !== null) {
                    columnValues.push(v);
                }
            }
            var uColumnValues = _.uniq(columnValues);
            if (uColumnValues.length <= 1) {
                return null;
            }
            var sortedValues = uColumnValues.sort(function (x, y) { return x - y; });
            var i = sortedValues.lastIndexOf(cellValue);
            if (i === 0) {
                return cs[0];
            }
            else if (i === (sortedValues.length - 1)) {
                return cs[cs.length - 1];
            }
            else {
                return cs[Math.round((i / (sortedValues.length - 1)) * (cs.length - 1))];
            }
        };
        VintageReportController.prototype.getColorStyle = function (columnIndex, row) {
            var c = this.getCellColor(columnIndex, row);
            if (c == null) {
                return '';
            }
            else {
                return "color:black;background-color:".concat(c);
            }
        };
        VintageReportController.prototype.getHeaderDescription = function () {
            if (!this.initialData) {
                return null;
            }
            var p = this.initialData.params;
            var s = p.CellValueIsCount === 'true' ? 'count' : 'balance';
            if (p.OverdueMonthsFrom) {
                s += ' ' + (parseInt(p.OverdueMonthsFrom) * 30).toString();
                if (!p.OverdueMonthsTo) {
                    s += '+';
                }
            }
            else if (!(p.ExcludeCapitalBalance === 'true')) {
                s += ' 0+';
            }
            if (p.IncludeDebtCollectionBalance === 'true' || p.AccumulateDebtCollectionBalance === 'true') {
                s += ' debtcol';
                if (p.AccumulateDebtCollectionBalance === 'true') {
                    s += '+';
                }
            }
            return s;
        };
        VintageReportController.$inject = ['$http', '$q', 'ntechComponentService'];
        return VintageReportController;
    }(NTechComponents.NTechComponentControllerBase));
    VintageReportComponentNs.VintageReportController = VintageReportController;
    var VintageReportComponent = /** @class */ (function () {
        function VintageReportComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = VintageReportController;
            this.templateUrl = 'vintage-report.html';
        }
        return VintageReportComponent;
    }());
    VintageReportComponentNs.VintageReportComponent = VintageReportComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    VintageReportComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    VintageReportComponentNs.Model = Model;
})(VintageReportComponentNs || (VintageReportComponentNs = {}));
angular.module('ntech.components').component('vintageReport', new VintageReportComponentNs.VintageReportComponent());
