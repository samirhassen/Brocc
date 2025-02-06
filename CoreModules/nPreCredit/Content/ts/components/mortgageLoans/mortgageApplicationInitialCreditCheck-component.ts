namespace MortgageApplicationInitialCreditCheckComponentNs {
    
    export class MortgageApplicationInitialCreditCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData;
        m: NTechPreCreditApi.MortgageLoanApplicationInitialCreditCheckStatusModel;

        applicationNr(): string {
            if (this.initialData) {
                return this.initialData.applicationInfo.ApplicationNr
            } else {
                return null;
            }
        }

        backUrl() {
            if (this.initialData) {
                return this.initialData.backUrl
            } else {
                return null;
            }
        }

        nrOfApplicants() {
            if (this.initialData) {
                return this.initialData.applicationInfo.NrOfApplicants
            } else {
                return null;
            } 
        }        

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }
        
        componentName(): string {
            return 'mortgageApplicationInitialCreditCheck'
        }
        
        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchMortageLoanApplicationInitialCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl).then(result => {
                this.m = result;
              })
        }
    }

    export class MortgageApplicationInitialCreditCheckCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationInitialCreditCheckController;
            this.templateUrl = 'mortgage-application-initial-credit-check.html';
        }
    }
}

angular.module('ntech.components').component('mortgageApplicationInitialCreditCheck', new MortgageApplicationInitialCreditCheckComponentNs.MortgageApplicationInitialCreditCheckCheckComponent())