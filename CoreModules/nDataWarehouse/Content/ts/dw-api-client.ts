module NTechDwApi {
    export class ApiClient extends NTechComponents.NTechApiClientBase {
        constructor(onError: ((errorMessage: string) => void),
            $http: ng.IHttpService,
            $q: ng.IQService) {
            super(onError, $http, $q)
        }               

        public toQueryString(params: { [index: string]: string }): string {
            let s = ''
            for (let key of Object.keys(params)) {
                let value = params[key]
                if (value !== null && value !== '' && value !== undefined) {
                    if (s.length > 0) {
                        s += '&'
                    }
                    s += `${key}=${encodeURIComponent(params[key])}`
                }
            }
            return s
        }
        
        public fetchVintageReportData(request: FetchVintageReportDataRequest): ng.IPromise<FetchVintageReportDataResult> {
            return this.post('/api/Reports/Vintage/FetchData', request)
        }

        public fetchAllProviders(): ng.IPromise<ProvidersResult> {
            return this.post('/api/Providers/FetchAll', {})
        }

        public fetchAllRiskGroups(): ng.IPromise<RiskGroupsResult> {
            return this.post('/api/RiskGroups/FetchAll', {})
        }

        public fetchVintagePeriods(includeMonths?: boolean): ng.IPromise<FetchVintagePeriodsResult> {
            return this.post('/api/Reports/Vintage/FetchPeriods', { IncludeMonths: includeMonths })
        }
    }

    export interface FetchVintagePeriodsResult {
        VintageMonths: Date[]
    }

    export interface RiskGroupsResult {
        Groups: RiskGroupModel[] 
    }
    export interface RiskGroupModel {
        RiskGroup: string
    }

    export interface ProvidersResult {
        Providers: ProviderModel[]
    }
    export interface ProviderModel {
        ProviderName: string
    }

    export interface FetchVintageReportDataRequest {
        [index: string]: string 
        IncludeDebtCollectionBalance?: string
        AccumulateDebtCollectionBalance?: string
        OverdueDaysFrom?: string
        OverdueDaysTo?: string
        OverdueMonthsFrom?: string
        OverdueMonthsTo?: string
        ExcludeCapitalBalance?: string
        CellValueIsCount?: string
        ShowPercent?: string
        AxisScaleY?: string
        AxisYFrom?: string
        AxisYTo?: string
        AxisScaleX?: string
        ProviderName?: string
        RiskGroup?: string
        IncludeDetails?: string
        TreatNotificationsAsClosedMaxBalance?: string
    }

    export interface FetchVintageReportDataResult {
        ColumnCount: number
        DetailRows: VintateReportDetailsRow[]
        DataRows: VintageReportRow[]
        CreationDate: Date
    }

    export interface VintageReportRow {
        RowId: Date
        InitialValue: number
        ColumnValues: number[]
    }

    export interface VintateReportDetailsRow {
        RowId: Date
        InitialValue: number
        ColumnId: Date
        ItemId: string
        Value: number
    }
}