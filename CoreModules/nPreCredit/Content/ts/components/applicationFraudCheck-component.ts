namespace ApplicationFraudCheckComponentNs {

    export class ApplicationFraudCheckController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        m: NTechPreCreditApi.FraudControlModel;
        
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'applicationFraudCheck'
        }

        onChanges() {
            this.m = null;
            if (this.initialData) {
                this.apiClient.fetchFraudControlModel(this.initialData.applicationInfo.ApplicationNr).then(result => {
                    this.m = result;
                })
            }
        }
    }

    export class ApplicationFraudCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationFraudCheckController;
            this.templateUrl = 'application-fraud-check.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        backUrl: string
    }
}

angular.module('ntech.components').component('applicationFraudCheck', new ApplicationFraudCheckComponentNs.ApplicationFraudCheckComponent())