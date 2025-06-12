namespace MortgageApplicationObjectValuationComponentNs {

    export class MortgageApplicationObjectValuationController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData;
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageApplicationObjectValuation'
        }

        onChanges() {
            this.m = null

            if (this.initialData == null) {
                return
            }

            this.apiClient.fetchMortgageApplicationValuationStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl, false).then(result => {
                let ai = this.initialData.applicationInfo
                this.m = {
                    valuation: result,
                    isNewMortgageApplicationValuationPossible: result.IsNewMortgageApplicationValuationAllowed && ai.IsActive,
                    isReadOnly: !ai.IsActive,
                    twoColumns: true,
                    stepStatus: this.initialData.workflowModel.getStepStatus(ai)
                }
            })
        }
    }

    export class MortgageApplicationObjectValuationComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationController;
            this.templateUrl = 'mortgage-application-object-valuation.html';
        }
    }

    export class Model {
        isNewMortgageApplicationValuationPossible: boolean
        valuation: NTechPreCreditApi.MortgageLoanApplicationValuationStatusModel
        isReadOnly: boolean
        twoColumns: boolean
        stepStatus: string
    }
}

angular.module('ntech.components').component('mortgageApplicationObjectValuation', new MortgageApplicationObjectValuationComponentNs.MortgageApplicationObjectValuationComponent())