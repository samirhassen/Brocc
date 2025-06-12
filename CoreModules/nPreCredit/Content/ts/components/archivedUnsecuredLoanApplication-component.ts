namespace ArchivedUnsecuredLoanApplicationComponentNs {
    export class ArchivedUnsecuredLoanApplicationController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'archivedUnsecuredLoanApplication'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchApplicationInfo(this.initialData.applicationNr).then(x => {
                let applicants: ApplicationCustomerInfoComponentNs.InitialData[] = []
                for (let applicantNr = 1; applicantNr <= x.NrOfApplicants; applicantNr++) {
                    applicants.push({ applicantNr: applicantNr, applicationNr: this.initialData.applicationNr, customerIdCompoundItemName: null, backTarget: this.initialData.backTarget, isArchived: true })
                }
                this.m = {
                    applicationInfo: x,
                    commentsInitialData: {
                        applicationInfo: x
                    },
                    applicants: applicants
                }
            })
        }

        onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, { applicationNr: initialData.ApplicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplications)
        }
    }

    export class ArchivedUnsecuredLoanApplicationComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ArchivedUnsecuredLoanApplicationController;
            this.templateUrl = 'archived-unsecured-loan-application.html';
        }
    }

    export class InitialData {
        applicationNr: string
        backTarget: string
        urlToHereFromOtherModule: string
    }

    export class Model {
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        commentsInitialData: ApplicationCommentsComponentNs.InitialData
        applicants: ApplicationCustomerInfoComponentNs.InitialData[]
    }
}

angular.module('ntech.components').component('archivedUnsecuredLoanApplication', new ArchivedUnsecuredLoanApplicationComponentNs.ArchivedUnsecuredLoanApplicationComponent())