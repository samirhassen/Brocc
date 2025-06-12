namespace ApplicationCheckpointsComponentNs {

    export class ApplicationCheckpointsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData

        checkpoints: NTechPreCreditApi.ApplicationCheckPointModel[]        

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'applicationCheckpoints'
        }

        onChanges() {
            this.checkpoints = null;
            if (this.initialData) {
                this.apiClient.fetchAllCheckpointsForApplication(this.initialData.applicationNr, this.initialData.applicationType).then(result => {
                    for (let c of result) {
                        c.isExpanded = true;
                    }
                    this.checkpoints = result;
                })
            }
        }

        getRoleDisplayName(roleName: string) {
            if (roleName === 'List_companyLoanAuthorizedSignatory') {
                return 'Authorized signatory'
            } else if (roleName === 'List_companyLoanCollateral') {
                return 'Collateral'
            } else if (roleName === 'List_companyLoanBeneficialOwner') {
                return 'Beneficial owner'
            } else {
                return roleName
            }
        }

        unlockCheckpointReasonText(checkpoint: NTechPreCreditApi.ApplicationCheckPointModel, event: Event) {
            if (event) {
                event.preventDefault();
            }
            this.apiClient.fetchCheckpointReasonText(checkpoint.checkpointId).then(reasonText => {
                checkpoint.reasonText = reasonText;
                checkpoint.isReasonTextLoaded = true;
                checkpoint.isExpanded = true;
            })
        }
    }

    export class ApplicationCheckpointsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCheckpointsController;
            this.templateUrl = 'application-checkpoints.html';
        }
    }

    export class InitialData {
        applicationNr: string
        applicationType: string
    }
}

angular.module('ntech.components').component('applicationCheckpoints', new ApplicationCheckpointsComponentNs.ApplicationCheckpointsComponent())