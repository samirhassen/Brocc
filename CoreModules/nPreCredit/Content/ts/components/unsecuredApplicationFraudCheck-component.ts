namespace UnsecuredApplicationFraudCheckComponentNs {

    export class UnsecuredApplicationFraudCheckController extends NTechComponents.NTechComponentControllerBase {
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        initialData: InitialData

        m: Model        

        componentName(): string {
            return 'unsecuredApplicationFraudCheck'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }

            this.apiClient.fetchFraudControlModel(this.initialData.applicationInfo.ApplicationNr).then(x => {
                this.m = {
                    FraudControlModel: x
                }
            })
        }

        headerClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'

            return { 'text-success': isAccepted, 'text-danger': isRejected }
        }

        iconClassFromStatus(status: string) {
            var isAccepted = status === 'Accepted'
            var isRejected = status === 'Rejected'
            var isOther = !isAccepted && !isRejected
            return { 'glyphicon-ok': isAccepted, 'glyphicon-remove': isRejected, 'glyphicon-minus': isOther, 'glyphicon': true, 'text-success': isAccepted, 'text-danger': isRejected }
        }
    }

    export class UnsecuredApplicationFraudCheckComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = UnsecuredApplicationFraudCheckController;
            this.templateUrl = 'unsecured-application-fraud-check.html';
        }
    }

    export class InitialData {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
    }

    export class Model {
        FraudControlModel: NTechPreCreditApi.FraudControlModel
    }
}

angular.module('ntech.components').component('unsecuredApplicationFraudCheck', new UnsecuredApplicationFraudCheckComponentNs.UnsecuredApplicationFraudCheckComponent())