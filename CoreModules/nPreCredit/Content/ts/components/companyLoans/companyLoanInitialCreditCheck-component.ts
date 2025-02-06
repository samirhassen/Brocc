namespace CompanyLoanInitialCreditCheckComponentNs {

    export class CompanyLoanInitialCreditCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData
        d: NTechCompanyLoanPreCreditApi.FetchCurrentCreditDecisionResult

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);

        }

        componentName(): string {
            return 'companyLoanInitialCreditCheck'
        }

        onChanges() {
            this.d = null
            if (!this.initialData || !this.initialData.applicationInfo) {
                return
            }

            if (!this.initialData.step.isStatusInitial(this.initialData.applicationInfo)) {
                this.initialData.companyLoanApiClient.fetchCurrentCreditDecision(this.initialData.applicationInfo.ApplicationNr).then(x => {
                    this.d = x
                })
            }
        }

        isNewCreditCheckPossibleButNeedsReactivate(): boolean {
            let a = this.initialData.applicationInfo
            return !a.IsActive && !a.IsFinalDecisionMade && (a.IsCancelled || a.IsRejected) && !a.HasLockedAgreement
        }

        isNewCreditCheckPossible(): boolean {
            let a = this.initialData.applicationInfo
            
            let isActiveAndOk = a.IsActive && !a.HasLockedAgreement
            return isActiveAndOk || this.isNewCreditCheckPossibleButNeedsReactivate()
        }

        isViewCreditCheckDetailsPossible(): boolean {
            if (!this.initialData) {
                return false
            }
            return !this.initialData.step.isStatusInitial(this.initialData.applicationInfo)            
        }

        newCreditCheck(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            if (!this.isNewCreditCheckPossible()) {
                return
            }

            let doCheck = () => {
                location.href = `/Ui/CompanyLoan/NewCreditCheck?applicationNr=${this.initialData.applicationInfo.ApplicationNr}&backTarget=${this.initialData.navigationTargetCodeToHere}`
            }

            if (this.isNewCreditCheckPossibleButNeedsReactivate()) {
                this.apiClient.reactivateCancelledApplication(this.initialData.applicationInfo.ApplicationNr).then(() => {
                    doCheck()
                })
            } else {
                doCheck()
            }
        }

        viewCreditCheckDetails(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }            

            if (!this.isViewCreditCheckDetailsPossible()) {
                return
            }

            location.href = `/Ui/CompanyLoan/ViewCreditCheckDetails?applicationNr=${this.initialData.applicationInfo.ApplicationNr}&backTarget=${this.initialData.navigationTargetCodeToHere}`          
        }

        getRejectionReasonDisplayName(reasonName: string) {
            let n = this.initialData && this.initialData.rejectionReasonToDisplayNameMapping ? this.initialData.rejectionReasonToDisplayNameMapping[reasonName] : null
            return n ? n : reasonName
        }
    }

    export class CompanyLoanInitialCreditCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanInitialCreditCheckController;
            this.templateUrl = 'company-loan-initial-credit-check.html';
        }
    }
}
angular.module('ntech.components').component('companyLoanInitialCreditCheck', new CompanyLoanInitialCreditCheckComponentNs.CompanyLoanInitialCreditCheckComponent())