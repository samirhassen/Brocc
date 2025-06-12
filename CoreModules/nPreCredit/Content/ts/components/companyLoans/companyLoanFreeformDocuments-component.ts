namespace CompanyLoanFreeformDocumentsComponentNs {
    
    export class CompanyLoanFreeformDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: CompanyLoanApplicationComponentNs.StepInitialData;
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private modalDialogService: ModalDialogComponentNs.ModalDialogService) {
            super(ntechComponentService, $http, $q);

        }
        
        componentName(): string {
            return 'companyLoanFreeformDocuments'
        }
        
        onChanges() {
            if (!this.initialData) {
                return
            }

            let ai = this.initialData.applicationInfo
            this.m = {
                documentsInitialData: {
                    applicationInfo: ai,
                    isReadOnly: this.initialData.step.isStatusAccepted(ai)
                }                                    
            }
        }

        getCompanyLoanDocumentCheckStatus() {
            if (!this.initialData) {
                return null
            }                
            return this.initialData.step.getStepStatus(this.initialData.applicationInfo)
        }

        isToggleCompanyLoanDocumentCheckStatusAllowed() {            
            if (!this.initialData) {
                return false
            }
            
            let ai = this.initialData.applicationInfo            

            return ai.IsActive && !ai.IsPartiallyApproved && !ai.HasLockedAgreement
                && this.initialData.step.areAllStepBeforeThisAccepted(ai)
        }

        toggleCompanyLoanDocumentCheckStatus() {
            this.toggleCompanyLoanListBasedStatus()
        }

        toggleCompanyLoanListBasedStatus() {
            if (!this.initialData) {
                return
            }
            let ai  = this.initialData.applicationInfo
            let step = this.initialData.step
            
            //TODO: This is not a great way of doing this
            this.initialData.companyLoanApiClient.setApplicationWorkflowStatus(ai.ApplicationNr, step.stepName, step.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(() => {
                this.signalReloadRequired()
            })
        }
    }

    export class Model {
        documentsInitialData: ApplicationFreeformDocumentsComponentNs.InitialData
    }

    export class CompanyLoanFreeformDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanFreeformDocumentsController;
            this.template = `<div ng-if="$ctrl.m">
                    <application-freeform-documents initial-data="$ctrl.m.documentsInitialData">

                    </application-freeform-documents>
                    <div class="pt-3" ng-show="$ctrl.isToggleCompanyLoanDocumentCheckStatusAllowed()">
                        <label class="pr-2">Document control {{$ctrl.getCompanyLoanDocumentCheckStatus() === 'Accepted' ? 'done' : 'not done'}}</label>
                        <label class="n-toggle">
                            <input type="checkbox" ng-checked="$ctrl.getCompanyLoanDocumentCheckStatus() === 'Accepted'" ng-click="$ctrl.toggleCompanyLoanDocumentCheckStatus()" />
                            <span class="n-slider"></span>
                        </label>
                    </div></div>`
        }
    }
}

angular.module('ntech.components').component('companyLoanFreeformDocuments', new CompanyLoanFreeformDocumentsComponentNs.CompanyLoanFreeformDocumentsComponent())