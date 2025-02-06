namespace MortgageLoanApplicationSearchWrapperComponentNs {
    export class MortgageLoanApplicationSearchWrapperController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: Model;

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$timeout']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
            this.ntechComponentService.subscribeToReloadRequired(() => {
                this.reload()
            })
        }

        componentName(): string {
            return 'mortgageLoanApplicationSearchWrapper'
        }

        onChanges() {
            this.reload()
        }

        reload() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.setMode(this.initialData.initialTabName || 'workList', null)
        }

        gotoApplication(applicationNr: string) {
            let tabName  = this.m.currentTabName
            this.m = null
            this.$timeout(() => {
                let url = this.initialData.applicationUrlPattern.replace('NNNNNN', applicationNr) + '&backTarget=' + this.navigationCodeFromTabName(tabName)
                location.href = url
            })
        }

        private navigationCodeFromTabName(tabName: string): NavigationTargetHelper.NavigationTargetCode {
            if (tabName === 'createWorkList') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList
            } else if (tabName === 'search') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch
            } else if (tabName === 'workList') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplications
            } else {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplications
            }
        }

        setMode(tabName: string, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (this.m && this.initialData.initialTabName !== tabName) {
                //When switching tab, update the url so these act like separate pages
                if (tabName === 'createWorkList' || tabName === 'search' || tabName === 'workList') {
                    NavigationTargetHelper.tryNavigateTo(this.navigationCodeFromTabName(tabName), null)
                    return
                }
            }

            this.m = null

            if (tabName == 'workList') {
                this.m = new Model(tabName, {
                    backUrl: this.initialData.urlToHere,
                    onGotoApplication: (application: NTechPreCreditApi.MortgageApplicationWorkListApplication) => {
                        this.apiClient.fetchApplicationInfo(application.ApplicationNr).then(x => {
                            this.gotoApplication(application.ApplicationNr)
                        })
                    },
                    currentUserId: this.initialData.currentUserId,
                    workflowModel: this.initialData.workflowModel,
                }, null, null)
            } else if (tabName == 'createWorkList') {
                this.m = new Model(tabName, null, null, {
                    hostData: this.initialData,
                    backUrl: this.initialData.urlToHere
                })
            } else if (tabName == 'search') {
                this.m = new Model(tabName, null, {
                    backUrl: this.initialData.urlToHere,
                    onGotoApplication: (searchHit: NTechPreCreditApi.MortgageApplicationWorkListApplication) => {
                        this.gotoApplication(searchHit.ApplicationNr)
                    }
                }, null)
            }
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBack(
                NavigationTargetHelper.create(this.initialData.backUrl, null, null),
                this.apiClient, this.$q, null)
        }
    }

    export class MortgageLoanApplicationSearchWrapperComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationSearchWrapperController;
            this.templateUrl = 'mortgage-application-search-wrapper.html';
        }
    }

    export interface InitialData extends ComponentHostNs.ComponentHostInitialData {
        workflowModel: WorkflowHelper.WorkflowServerModel
        applicationUrlPattern: string
        initialTabName: string
    }

    export class Model {
        constructor(public currentTabName: string,
            public workListInitialData: MortgageLoanApplicationWorkListComponentNs.InitialData,
            public searchInitialData: MortgageApplicationSearchComponentNs.InitialData,
            public createWorkListInitialData: MortgageLoanApplicationCreateWorkListComponentNs.InitialData) {
        }
    }
}

angular.module('ntech.components').component('mortgageApplicationSearchWrapper', new MortgageLoanApplicationSearchWrapperComponentNs.MortgageLoanApplicationSearchWrapperComponent())