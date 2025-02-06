namespace MortgageApplicationFinalCreditCheckComponentNs {
    
    export class MortgageApplicationFinalCreditCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: NTechPreCreditApi.MortgageLoanApplicationFinalCreditCheckStatusModel;

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
            return 'mortgageApplicationFinalCreditCheck'
        }
        
        onChanges() {
            this.apiClient.fetchMortageLoanApplicationFinalCreditCheckStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl).then(result => {
                this.m = result;
            })
        }
    }

    export class MortgageApplicationFinalCreditCheckCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationFinalCreditCheckController;
            this.templateUrl = 'mortgage-application-final-credit-check.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        backUrl: string
    }
}

angular.module('ntech.components').component('mortgageApplicationFinalCreditCheck', new MortgageApplicationFinalCreditCheckComponentNs.MortgageApplicationFinalCreditCheckCheckComponent())