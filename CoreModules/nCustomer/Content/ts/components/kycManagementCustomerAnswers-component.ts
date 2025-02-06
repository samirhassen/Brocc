namespace KycManagementCustomerAnswersComponentNs {

    export class KycManagementCustomerAnswersController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        initialData: InitialData
        m: Model

        componentName(): string {
            return 'kycManagementCustomerAnswers'
        }

        onChanges() {
            this.m = null
            if (this.initialData == null) {
                return
            }
            this.apiClient.kycManagementFetchLatestCustomerQuestionsSet(this.initialData.customerId).then(questionSet => {
                this.m = {
                    q: questionSet
                }
            })
        }
    }

    export class KycManagementCustomerAnswersComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementCustomerAnswersController;
            this.templateUrl = 'kyc-management-customer-answers.html';
        }
    }

    export class InitialData {
        customerId: number
    }

    export class Model {
        q: NTechCustomerApi.CustomerQuestionsSet
    }
}

angular.module('ntech.components').component('kycManagementCustomerAnswers', new KycManagementCustomerAnswersComponentNs.KycManagementCustomerAnswersComponent())