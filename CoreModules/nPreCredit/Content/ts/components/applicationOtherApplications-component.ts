namespace ApplicationOtherApplicationsComponentNs {

    export class ApplicationOtherApplicationsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m : NTechPreCreditApi.OtherApplicationsResponseModel
                
        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'applicationOtherApplications'
        }

        onChanges() {
            if (this.initialData) {
                this.apiClient.fetchOtherApplications(this.initialData.applicationNr, this.initialData.backUrl).then(result => {
                    this.m = result
                })
            }
        }
    }

    export class ApplicationOtherApplicationsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationOtherApplicationsController;
            this.templateUrl = 'application-other-applications.html';
        }
    }

    export class InitialData {
        applicationNr: string
        backUrl: string
    }
}

angular.module('ntech.components').component('applicationOtherApplications', new ApplicationOtherApplicationsComponentNs.ApplicationOtherApplicationsComponent())