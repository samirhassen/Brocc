namespace UnsecuredCreditCheckOtherApplicationsComponentNs {

    export class UnsecuredCreditCheckOtherApplicationsController extends NTechComponents.NTechComponentControllerBase {
        otherApplications: any //TODO: Type

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'unsecuredCreditCheckOtherApplications'
        }

        onChanges() {

        }
    }

    export class UnsecuredCreditCheckOtherApplicationsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                otherApplications: '<'
            };
            this.controller = UnsecuredCreditCheckOtherApplicationsController;
            this.templateUrl = 'unsecured-credit-check-other-applications.html';
        }
    }

    export class InitialData {

    }
}

angular.module('ntech.components').component('unsecuredCreditCheckOtherApplications', new UnsecuredCreditCheckOtherApplicationsComponentNs.UnsecuredCreditCheckOtherApplicationsComponent())