namespace MortgageApplicationSearchComponentNs {
    export class MortgageApplicationSearchController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;

        searchResult: SearchResultModel
        omniSearchValue: string

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationSearch'
        }

        onChanges() {
            this.searchResult = null
            this.omniSearchValue = null
        }

        isGotoApplicationPossible(application: NTechPreCreditApi.MortgageApplicationWorkListApplication) {
            return application && this.initialData && this.initialData.onGotoApplication
        }

        gotoApplication(application: NTechPreCreditApi.MortgageApplicationWorkListApplication, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.$timeout(() => {
                if (this.initialData && this.initialData.onGotoApplication) {
                    this.initialData.onGotoApplication(application);
                }
            })
        }

        reset(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.onChanges()
        }

        search(evt: Event) {
            this.apiClient.searchForMortgageLoanApplicationOrLeadsByOmniValue(this.omniSearchValue).then(result => {
                let totalCount = (result.Applications ? result.Applications.length : 0) + (result.Leads ? result.Leads.length : 0)

                if (totalCount === 1) {
                    this.gotoApplication(result.Applications && result.Applications.length > 0 ? result.Applications[0] : result.Leads[0], null)
                } else {
                    this.searchResult = { totalCount: totalCount, applications: result.Applications, leads : result.Leads };
                }
            })
        }
    }

    export class MortgageApplicationSearchComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationSearchController;
            this.templateUrl = 'mortgage-application-search.html';
        }
    }

    export class InitialData {
        backUrl: string
        onGotoApplication: (searchHit: NTechPreCreditApi.MortgageApplicationWorkListApplication) => void
    }

    export class SearchResultModel {
        applications: NTechPreCreditApi.MortgageApplicationWorkListApplication[]
        leads: NTechPreCreditApi.MortgageApplicationWorkListApplication[]
        totalCount: number
    }
}

angular.module('ntech.components').component('mortgageApplicationSearch', new MortgageApplicationSearchComponentNs.MortgageApplicationSearchComponent())