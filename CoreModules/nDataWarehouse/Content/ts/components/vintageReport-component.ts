namespace VintageReportComponentNs {

    export class VintageReportController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        weightedAverageChart: Chart

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

            //red: #F8696B, yellow:  #FFEB84, green: #63BE7B. See https://github.com/gka/chroma.js
            this.greenToYellowToRed = ['#63be7b', '#6ac07b', '#72c27c', '#79c37c', '#80c57d', '#88c77d', '#8fca7d', '#96cb7e', '#9dce7e', '#a3cf7f', '#a8d17f', '#afd37f', '#b4d580', '#bbd680', '#c2d980', '#c7da81', '#cedc81', '#d3de81', '#d9e082', '#dfe182', '#e5e383', '#eae583', '#f0e783', '#f7e884', '#fdea84', '#ffe884', '#ffe383', '#ffde82', '#ffda81', '#ffd580', '#ffd07f', '#ffcb7e', '#ffc67d', '#ffc07c', '#ffbc7b', '#ffb77a', '#ffb279', '#ffac78', '#ffa776', '#fea275', '#fe9c74', '#fd9774', '#fd9172', '#fc8d71', '#fc8670', '#fb816f', '#fa7b6e', '#fa756d', '#f96f6c', '#f8696b']
            this.redToYellowToGreen = angular.copy(this.greenToYellowToRed).reverse()
        }

        componentName(): string {
            return 'vintageReport'
        }

        onChanges() {
            this.m = null
            this.destroyWeightedAverageGraph()
            if (!this.initialData) {
                return
            }
            this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage report')

            let r = angular.copy(this.initialData.params)
            r.IncludeDetails = 'false'
            r.ShowPercent = 'false'
            this.apiClient.fetchVintageReportData(r).then(x => {
                let dataColumns: number[] = []
                let weightedAverages: number[] = []
                let columnSums: number[] = []
                for (let i = 1; i <= x.ColumnCount; i++) {
                    dataColumns.push(i)
                    let sumAndWeightedAverage = this.computeSumAndWeightedAverageForColumn(i - 1, x)
                    columnSums.push(sumAndWeightedAverage[0])
                    weightedAverages.push(sumAndWeightedAverage[1])
                }

                this.m = {
                    showGraph: false,
                    initialValueFormat: this.initialData.params.CellValueIsCount === 'true' ? 'count' : 'value',
                    cellFormat: this.initialData.params.ShowPercent === 'true' ? 'percent' : (this.initialData.params.CellValueIsCount ? 'count' : 'value'),
                    rows: x.DataRows,
                    detailRows: x.DetailRows,
                    dataColumns: dataColumns,
                    columnSums: columnSums,
                    weightedAverages: weightedAverages,
                    creationDate: x.CreationDate,
                    colorScheme: ''
                }

                this.updateWeightedAverageGraph(dataColumns, weightedAverages)
            }).finally(() => {
                this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage report')
            })
        }

        getPercentCellValue(row: NTechDwApi.VintageReportRow, columnIndex : number): number {
            if (!this.m) {
                return null
            }
            let colValue = row.ColumnValues[columnIndex]
            if (colValue === null) {
                return null
            }
            return row.InitialValue === 0 ? 0 : (100 * colValue / row.InitialValue)
        }

        private destroyWeightedAverageGraph() {
            if (this.weightedAverageChart) {
                this.weightedAverageChart.destroy()
                this.weightedAverageChart = null
            }
        }

        private updateWeightedAverageGraph(columns: number[], weightedAverages: number[]) {
            var dataLine = {
                labels: columns,
                datasets: [
                    {
                        backgroundColor: 'rgb(0,178,147)',
                        borderColor: 'rgb(0,178,147)',
                        type: 'line',
                        fill: false,
                        data: weightedAverages
                    }]
            }

            this.destroyWeightedAverageGraph()

            let e = document.getElementById("weightedAverageChart") as HTMLCanvasElement

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
                    tooltips: {

                    }
                }
            });
        }

        private computeSumAndWeightedAverageForColumn(columnIndex: number, result: NTechDwApi.FetchVintageReportDataResult): [number, number] {
            let accInitialValue = 0
            let accValue = 0
            for (var rowIndex = 0; rowIndex < result.DataRows.length; rowIndex++) {
                let value = result.DataRows[rowIndex].ColumnValues[columnIndex]
                if (value !== null) {
                    accInitialValue += result.DataRows[rowIndex].InitialValue
                    accValue += value
                }
            }
            return [accValue, accInitialValue == 0 ? 0 : (100 * accValue / accInitialValue)]
        }

        getDownloadDataUrl() {
            let r = angular.copy(this.initialData.params)
            r.IncludeDetails = 'true'

            let url = '/api/Reports/Vintage/Get'

            let qs = this.apiClient.toQueryString(r)

            if (qs) {
                url += `?${qs}`
            }

            return url
        }
        
        getCellColor(columnIndex: number, row: NTechDwApi.VintageReportRow) {
            if (!this.m) {
                return null
            }

            let cs: string[] = []

            if (this.m.colorScheme === 'greenYellowRed') {
                cs = this.greenToYellowToRed
            } else if (this.m.colorScheme === 'redYellowGreen') {
                cs = this.redToYellowToGreen
            } else {
                return null
            }

            let cellValue = this.m.cellFormat === 'percent' ? this.getPercentCellValue(row, columnIndex) : row.ColumnValues[columnIndex]

            if (cellValue === null) {
                return null
            }

            //This is wasteful ... cache if it becomes a problem
            let columnValues: number[] = []

            for (let r of this.m.rows) {
                let v = this.m.cellFormat === 'percent' ? this.getPercentCellValue(r, columnIndex) : r.ColumnValues[columnIndex]
                if (v !== null) {
                    columnValues.push(v)
                }
            }

            let uColumnValues = _.uniq(columnValues)
            if (uColumnValues.length <= 1) {
                return null
            }

            let sortedValues = uColumnValues.sort((x, y) => x - y)            
            let i = sortedValues.lastIndexOf(cellValue)
            if (i === 0) {
                return cs[0]
            } else if (i === (sortedValues.length - 1)) {
                return cs[cs.length-1]
            } else {                
                return cs[Math.round((i / (sortedValues.length - 1)) * (cs.length - 1))]
            }
        }

        getColorStyle(columnIndex: number, row: NTechDwApi.VintageReportRow) {
            var c = this.getCellColor(columnIndex, row)
            if (c == null) {
                return ''
            } else {
                return `color:black;background-color:${c}`
            }
        }

        getHeaderDescription() {
            if (!this.initialData) {
                return null
            }

            let p = this.initialData.params
            let s = p.CellValueIsCount === 'true' ? 'count' : 'balance'            
            if (p.OverdueMonthsFrom) {
                s += ' ' + (parseInt(p.OverdueMonthsFrom) * 30).toString()
                if (!p.OverdueMonthsTo) {
                    s += '+'
                }
            } else if (!(p.ExcludeCapitalBalance === 'true')) {
                s += ' 0+'
            }
            if (p.IncludeDebtCollectionBalance === 'true' || p.AccumulateDebtCollectionBalance === 'true') {
                s += ' debtcol'
                if (p.AccumulateDebtCollectionBalance === 'true') {
                    s += '+'
                }
            }
            return s
        }

        private greenToYellowToRed: string[]
        private redToYellowToGreen : string[]
    }

    export class VintageReportComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = VintageReportController;
            this.templateUrl = 'vintage-report.html';
        }
    }

    export class InitialData {
        params: { [index: string]: string }
    }

    export class Model {
        showGraph: boolean
        initialValueFormat: string
        cellFormat: string
        rows: NTechDwApi.VintageReportRow[]
        detailRows: NTechDwApi.VintateReportDetailsRow[]
        dataColumns: number[]
        weightedAverages: number[]
        columnSums: number[]
        creationDate: Date
        colorScheme: string
    }
}

angular.module('ntech.components').component('vintageReport', new VintageReportComponentNs.VintageReportComponent())