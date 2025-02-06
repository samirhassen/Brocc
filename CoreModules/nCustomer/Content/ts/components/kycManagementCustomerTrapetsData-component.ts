namespace KycManagementCustomerTrapetsDataComponentNs {

    export class KycManagementCustomerTrapetsDataController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);
        }

        initialData: InitialData
        m: Model

        componentName(): string {
            return 'kycManagementCustomerTrapetsData'
        }

        onChanges() {
            this.m = null
            if (this.initialData == null) {
                return
            }
            this.apiClient.fetchLatestTrapetsQueryResult(this.initialData.customerId).then(result => {
                this.m = {
                    latestTrapetsResult: result,
                    historySummary: null,
                    showHistoryDialogId: this.modalDialogService.generateDialogId()
                }
            })
        }

        showHistory(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.apiClient.fetchTrapetsQueryHistorySummary(this.initialData.customerId).then(result => {
                this.apiClient.fetchQueryResultHistoryDetails(this.initialData.customerId, 60).then(x => {
                    this.m.historySummary = result
                    this.m.historicalExternalIds = x.QueryDatesWithListHits;
                    this.modalDialogService.openDialog(this.m.showHistoryDialogId);
                });
            })
        }

        showLatestDetails(type: 'pep' | 'sanction', evt?: Event) {
            evt?.preventDefault();

            let handleEmpty = (arr: string[]) => !arr || arr.length === 0 ? ['-'] : arr;
            this.apiClient.kycManagementQueryDetails(this.m.latestTrapetsResult.Id).then(x => {
                if (type === 'sanction') {
                    this.m.latestSanctionExternalIds = handleEmpty(x.SactionExternalIds);
                } else if (type === 'pep') {
                    this.m.latestPepExternalIds = handleEmpty(x.PepExternalIds);
                }
            })
        }
    }

    export class KycManagementCustomerTrapetsDataComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementCustomerTrapetsDataController;
            this.templateUrl = 'kyc-management-customer-trapets-data.html';
        }
    }

    export class InitialData {
        customerId: number
    }

    export class Model {
        latestTrapetsResult: NTechCustomerApi.TrapetsQueryResultModel
        historySummary: NTechCustomerApi.TrapetsQueryHistorySummaryModel
        showHistoryDialogId: string
        latestSanctionExternalIds?: string[]
        latestPepExternalIds?: string[]
        historicalExternalIds?: {
            QueryDate: string
            PepExternalIds: string[]
            SanctionExternalIds: string[]
        }[]
    }
}

angular.module('ntech.components').component('kycManagementCustomerTrapetsData', new KycManagementCustomerTrapetsDataComponentNs.KycManagementCustomerTrapetsDataComponent())