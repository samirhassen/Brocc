namespace MortgageLoanApplicationRawComponentNs {
    
    export class MortgageApplicationRawController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

        }
        
        componentName(): string {
            return 'mortgageLoanApplicationRaw'
        }
        
        onChanges() {
            if (!this.initialData) {
                return
            }
            let ai = this.initialData.applicationInfo
            this.apiClient.fetchCreditApplicationItemSimple(ai.ApplicationNr, ['*'], '').then(x => {
                this.m = {
                    application: x
                }
            })
        }
    }

    export class Model {
        application: NTechPreCreditApi.IStringDictionary<string>
    }
    
    export class MortgageLoanApplicationRawComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = `<div ng-if="$ctrl.m"><pre>{{$ctrl.m.application | json}}</pre></div>`
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationRaw', new MortgageLoanApplicationRawComponentNs.MortgageLoanApplicationRawComponent())