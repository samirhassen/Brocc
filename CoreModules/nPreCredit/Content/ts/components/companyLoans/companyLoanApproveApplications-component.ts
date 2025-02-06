namespace CompanyLoanApproveApplicationsComponentNs {

    export class CompanyLoanApproveApplicationsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'companyLoanApproveApplications'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.initialData.companyLoanApiClient.fetchApplicationsPendingFinalDecision().then(x => {
                var apps: ApplicationModel[] = []
                for (let a of x.Applications) {

                    let url = `/Ui/CompanyLoan/Application?applicationNr=${a.ApplicationNr}`
                    if (this.initialData.backUrl) {
                        url += `&backUrl=${encodeURIComponent(this.initialData.backUrl)}` 
                    }

                    let un = this.initialData.userNameByUserId[a.ApprovedByUserId.toString()]

                    apps.push({
                        amount: a.OfferedAmount,
                        applicationNr: a.ApplicationNr,
                        isApproved: true,
                        applicationUrl: url,
                        handlerDisplayName: un ? un : 'User ' + a.ApprovedByUserId
                    })
                }

                let today = () => moment(this.initialData.today, 'YYYY-MM-DD', true)
                this.m = {
                    backUrl: this.initialData.backUrl,
                    applications: apps,
                    historyFromDate: today().subtract(30, 'days').format('YYYY-MM-DD'),
                    historyToDate: today().format('YYYY-MM-DD')
                }
            })
        }

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!this.m || !this.m.backUrl) {
                return
            }
            
            this.initialData.setIsLoading(true)
            this.$timeout(() => {
                document.location.href = this.m.backUrl
            })
        }

        newLoanCountToApprove() {
            if (!this.m) {
                return null
            }
            let count = 0
            for (let a of this.m.applications) {
                count += a.isApproved ? 1 : 0
            }
            return count
        }

        newLoanAmountToApprove() {
            if (!this.m) {
                return null
            }
            let sum = 0
            for (let a of this.m.applications) {
                sum += a.isApproved ? a.amount : 0
            }
            return sum
        }

        createCredits(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let applicationNrs: string[] = []
            let stillPendingApplications : ApplicationModel[] = []
            for (let a of this.m.applications) {
                if (a.isApproved) {
                    applicationNrs.push(a.applicationNr)
                } else {
                    stillPendingApplications.push(a)
                }
            }
            this.initialData.companyLoanApiClient.createLoans(applicationNrs).then(x => {
                this.m.applications = stillPendingApplications
                //TODO: Close/Reset the batch list
            })
        }

        filterHistory(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }            
            this.initialData.companyLoanApiClient.fetchFinalDecisionBatches(this.m.historyFromDate, this.m.historyToDate).then(x => {
                this.m.historyBatches = x.Batches
            })
        }

        loadBatchDetails(batch: BatchWithDetails, evt?: Event) {
            if (evt) {
                evt.preventDefault() 
            }
            this.initialData.companyLoanApiClient.fetchFinalDecisionBatchItems(batch.Id).then(x => {
                batch.batchDetails = x.Items
            })
        }
    }

    export class CompanyLoanApproveApplicationsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApproveApplicationsController;
            this.templateUrl = 'company-loan-approve-applications.html';
        }
    }

    export class Model {
        backUrl: string
        applications: ApplicationModel[]
        historyFromDate?: string
        historyToDate?: string
        historyBatches?: BatchWithDetails[]
    }

    export class ApplicationModel {
        applicationUrl: string
        applicationNr: string
        isApproved: boolean
        amount: number
        handlerDisplayName: string
    }

    export interface InitialData extends ComponentHostNs.ComponentHostInitialData {
        userNameByUserId : NTechPreCreditApi.IStringDictionary<string>
    }
    
    export interface BatchWithDetails extends NTechCompanyLoanPreCreditApi.FetchFinalDecisionBatchesResponseItem {
        batchDetails?: NTechCompanyLoanPreCreditApi.FetchFinalDecsionBatchItemsResponseItem[]
    }
    
}

angular.module('ntech.components').component('companyLoanApproveApplications', new CompanyLoanApproveApplicationsComponentNs.CompanyLoanApproveApplicationsComponent())