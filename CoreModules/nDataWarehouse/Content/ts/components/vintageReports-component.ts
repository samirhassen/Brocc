namespace VintageReportsComponentNs {

    export class VintageReportsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'vintageReports'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            this.ntechComponentService.emitNTechEvent('changePageTitle', 'Vintage reports')
            this.apiClient.fetchAllProviders().then(allProviders => {
                this.apiClient.fetchAllRiskGroups().then(allRiskGroups => {
                    this.apiClient.fetchVintagePeriods(true).then(vintagePeriods => {
                        this.m = {
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
                        }
                    })
                })
            })
        }

        public getUrl(): string {
            if (!this.m) {
                return null
            }

            let request: NTechDwApi.FetchVintageReportDataRequest = {
                ShowPercent: 'false',
                IncludeDetails: 'false',
                AxisScaleX: 'Month',
                AxisScaleY: 'Month'
            }

            if (this.m.balanceOption === '30' || this.m.balanceOption === '30+') {
                request.OverdueMonthsFrom = '1'
                if (this.m.balanceOption === '30') {
                    request.OverdueMonthsTo = '1'
                }
            } else if (this.m.balanceOption === '60' || this.m.balanceOption === '60+') {
                request.OverdueMonthsFrom = '2'
                if (this.m.balanceOption === '60') {
                    request.OverdueMonthsTo = '2'
                }
            } else if (this.m.balanceOption === '90' || this.m.balanceOption === '90+') {
                request.OverdueMonthsFrom = '3'
                if (this.m.balanceOption === '90') {
                    request.OverdueMonthsTo = '3'
                }
            } else if (this.m.balanceOption === 'exclude') {
                request.ExcludeCapitalBalance = 'true'
            } //else 0+ always include

            if (this.m.debtCollectionOption == '0') {
                request.IncludeDebtCollectionBalance = 'true'
            } else if (this.m.debtCollectionOption == '0+') {
                request.AccumulateDebtCollectionBalance = 'true'
            } //else exclude

            request.CellValueIsCount = (this.m.valueType === 'count') ? 'true' : 'false'

            request.ShowPercent = (this.m.formatType === 'percent') ? 'true' : 'false'

            request.ProviderName = this.m.providerName ? this.m.providerName : null

            request.RiskGroup = this.m.riskGroup ? this.m.riskGroup : null

            request.AxisYFrom = this.m.yAxisFrom ? this.m.yAxisFrom : null
            request.AxisYTo = this.m.yAxisTo ? this.m.yAxisTo : null
            request.TreatNotificationsAsClosedMaxBalance = this.initialData.treatNotificationsAsClosedMaxBalance
                ? this.initialData.treatNotificationsAsClosedMaxBalance.toString()
                : ''

            return createVintageReportUrl(request, this.apiClient)
        }
    }

    export function createVintageReportUrl(request: NTechDwApi.FetchVintageReportDataRequest, apiClient: NTechDwApi.ApiClient): string {
        let url = '/ui/s/vintage-report'

        let qs = apiClient.toQueryString(request)

        if (qs) {
            url += `?${qs}`
        }

        return url
    }

    export class VintageReportsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = VintageReportsController;
            this.templateUrl = 'vintage-reports.html';
        }
    }

    export class InitialData {
        backofficeUrl: string
        treatNotificationsAsClosedMaxBalance: number
    }

    export class Model {
        balanceOption: string
        debtCollectionOption: string
        valueType: string
        formatType: string
        providerName: string
        riskGroup: string
        allProviders: NTechDwApi.ProviderModel[]
        allRiskGroups: NTechDwApi.RiskGroupModel[]
        allVintageMonths: Date[]
        yAxisFrom: string
        yAxisTo: string
    }
}

angular.module('ntech.components').component('vintageReports', new VintageReportsComponentNs.VintageReportsComponent())