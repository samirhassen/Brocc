namespace CompanyLoanApplicationSearchComponentNs {
    const PageSize = 15
    const FilterLocalStorageName = 'filter'

    export class CompanyLoanApplicationSearchController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        pagingHelper: NTechTables.PagingHelper
        localStorage: NTechComponents.NTechLocalStorageContainer

        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout', 'ntechLocalStorageService', '$translate']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService,
            private ntechLocalStorageService: NTechComponents.NTechLocalStorageService,
            private $translate: any
        ) {
            super(ntechComponentService, $http, $q);

            this.pagingHelper = new NTechTables.PagingHelper($q, $http)
        }

        componentName(): string {
            return 'companyLoanApplicationSearch'
        }

        onChanges() {
            this.localStorage = this.ntechLocalStorageService.getUserContainer(
                'CompanyLoanApplicationSearch',
                this.initialData.currentUserId,
                '20191023.1') //Changes version if the format of local storage data is ever changed to prevent old data from being used

            this.m = null
            
            if (!this.initialData) {
                return
            }

            this.initialData.companyLoanApiClient.fetchApplicationWorkflowStepNames(true).then(x => {
                let s = this.localStorage.get<ICachedSearchFilterModel>(FilterLocalStorageName)

                this.m = {
                    searchResult: null,
                    listName: s ? s.listName : null,
                    omniSearchValue: null,
                    providerName: s ? s.providerName : null,
                    selectedSearchHit: null,
                    listCountsByName: null,
                    providers: x.Affiliates,
                    steps: []
                }

                for (let stepName of x.StepNames) {
                    this.m.steps.push({
                        initialListName: `${stepName}_Initial`,
                        stepName: stepName
                    })
                }
                
                this.search(s ? s.pageNr : 0, null)
            })
        }

        showOptions(application: NTechCompanyLoanPreCreditApi.CompanyLoanApplicationSearchHit, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.m.selectedSearchHit = {
                isDialogOpen: true,
                application: application
            }
        }

        getApplicationUrl(application: NTechCompanyLoanPreCreditApi.CompanyLoanApplicationSearchHit) {
            if (!application) {
                return null
            }            
            
            return `/Ui/CompanyLoan/Application?applicationNr=${encodeURIComponent(application.ApplicationNr)}&backTarget=${NavigationTargetHelper.createCrossModule('CompanyLoanSearch', {}).targetCode}`
        }

        reset(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.m.omniSearchValue = null
            this.search(0, null)
        }

        search(pageNr: number, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.m.selectedSearchHit = null;

            let searchFilter : ICachedSearchFilterModel = this.m.omniSearchValue
                ? null
                : { listName: this.m.listName, providerName: this.m.providerName, pageNr: pageNr }            

            if (this.m.omniSearchValue) {
                this.initialData.companyLoanApiClient.searchForCompanyLoanApplicationByOmniValue(this.m.omniSearchValue, true).then(result => {
                    this.cacheSearchFilter(searchFilter)
                    if (result && result.Applications && result.Applications.length === 1) {
                        this.m.searchResult = null
                        this.initialData.setIsLoading(true)
                        this.$timeout(() => {
                            document.location.href = this.getApplicationUrl(result.Applications[0])
                        })
                    } else {
                        this.m.searchResult = {
                            items: result.Applications,
                            paging: null
                        };
                    }
                })
            } else {
                this.initialData.companyLoanApiClient.fetchCompanyLoanWorkListDataPage(this.m.providerName, this.m.listName, this.initialData.isTest, true, PageSize, pageNr).then(x => {
                    this.cacheSearchFilter(searchFilter)
                    this.m.searchResult = {
                        items: x.PageApplications,
                        paging: this.pagingHelper.createPagingObjectFromPageResult(x)
                    }
                    this.m.listCountsByName = x.ListCountsByName                    
                })
            }
        }

        cacheSearchFilter(f: ICachedSearchFilterModel) {
            let copiedF: ICachedSearchFilterModel = { ...f }
            copiedF.pageNr = 0 //Caching the page can lead to really wierd result as you can end up on page two of a single page resultset where the paging buttons are hidden so you cant see why
            this.localStorage.set(FilterLocalStorageName, copiedF, 5)
        }

        getListCountLabel(listName: string): number {
            if (!this.m || !this.m.listCountsByName) {
                return null
            }
            let n = this.m.listCountsByName[listName]
            return n ? n : 0
        }

        getProviderDisplayName(providerName: string): string {
            if (!this.m || !this.m.providers) {
                return providerName
            }
            for (let p of this.m.providers) {
                if (p.ProviderName == providerName) {
                    return p.DisplayToEnduserName
                }
            }
            return providerName
        }

        getDisplayListName(stepName: string) {
            if (this.initialData) {
                let m = this.initialData.workflowModel
                let steps = m.Steps.filter(x => x.Name == stepName)
                if (steps.length === 1) {
                    return 'Pending ' + steps[0].DisplayName
                }
            }

            return 'Pending ' + stepName
        }

        isSpecialSearchMode() {
            return this.m && this.m.omniSearchValue
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m) {
                return
            }
            this.cacheSearchFilter(null) //We only want to remember the current page when going to an application and back not when leaving to the menu and coming back
            this.initialData.setIsLoading(true)
            this.$timeout(() => {
                
                NavigationTargetHelper.handleBackWithInitialDataDefaults(this.initialData, this.apiClient, this.$q, {})
            })
        }
    }

    export class CompanyLoanApplicationSearchComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApplicationSearchController;
            this.templateUrl = 'company-loan-application-search.html';
        }
    }

    export interface ICachedSearchFilterModel {
        providerName: string
        listName: string
        pageNr: number
    }

    export class Model {
        selectedSearchHit: SelectedSearchHitModel
        searchResult: SearchResultModel
        omniSearchValue: string
        providerName: string
        listName: string
        listCountsByName: NTechPreCreditApi.IStringDictionary<number>
        providers: NTechPreCreditApi.AffiliateModel[]        
        steps: { initialListName: string, stepName: string }[]
    }

    export class SelectedSearchHitModel {
        isDialogOpen: boolean
        application: NTechCompanyLoanPreCreditApi.CompanyLoanApplicationSearchHit
    }

    export class SearchResultModel {
        items: NTechCompanyLoanPreCreditApi.CompanyLoanApplicationSearchHit[]
        paging: NTechTables.PagingObject
    }

    export interface InitialData extends ComponentHostNs.ComponentHostInitialData {
        workflowModel: WorkflowHelper.WorkflowServerModel
    }
}

angular.module('ntech.components').component('companyLoanApplicationSearch', new CompanyLoanApplicationSearchComponentNs.CompanyLoanApplicationSearchComponent())