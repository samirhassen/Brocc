namespace MortgageApplicationDocumentsComponentNs {

    export class MortgageApplicationDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: MortgageLoanApplicationDynamicComponentNs.StepInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'mortgageApplicationDocuments'
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            let id = new ApplicationDocumentsComponentNs.InitialData(this.initialData.applicationInfo)
            id.onDocumentsAddedOrRemoved = x => this.onDocumentsAddedOrRemoved(x)

            let customData = this.initialData.workflowModel.getCustomStepData<WorkflowCustomDataModel>()
            for (let d of customData.RequiredDocuments) {
                if (d.Scope == 'ForAllApplicants') {
                    id.addDocumentForAllApplicants(d.DocumentType, d.Text)
                } else if (d.Scope == 'Shared') {
                    id.addSharedDocument(d.DocumentType, d.Text)
                } else if (d.Scope == 'ForSingleApplicant') {
                    id.addDocumentForSingleApplicant(d.DocumentType, d.Text, d.ApplicantNr)
                }
            }
            this.m = {
                documentCheckInitialData: id
            }
        }
         
        onDocumentsAddedOrRemoved(areAllDocumentAdded: boolean) {
            //TODO: Find a way to move this serverside while allowing potentially multiple document steps on a single application
            //      so this can be completed when documents are added from other sources indirectly
            if (!this.initialData) {
                return
            }
            let i = this.initialData
            let w = i.workflowModel

            let changeToStatus: string = null
            if (areAllDocumentAdded && !w.isStatusAccepted(i.applicationInfo)) {
                changeToStatus = 'Accepted'
            } else if (!areAllDocumentAdded && w.isStatusAccepted(i.applicationInfo)) {
                changeToStatus = 'Initial'
            }
            if (changeToStatus) {
                this.apiClient.setMortgageApplicationWorkflowStatus(
                    i.applicationInfo.ApplicationNr,
                    w.currentStep.Name,
                    changeToStatus).then(x => {
                        if (x.WasChanged) {
                            this.signalReloadRequired()
                        }
                    })
            }
        }

    }

    export class MortgageApplicationDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationDocumentsController;
            this.templateUrl = 'mortgage-application-documents.html';
        }
    }

    export class Model {
        documentCheckInitialData: ApplicationDocumentsComponentNs.InitialData
    }

    export interface WorkflowCustomDataModel {
        RequiredDocuments: [{ Scope: string, DocumentType: string, Text: string, ApplicantNr?: number }]
    }
}

angular.module('ntech.components').component('mortgageApplicationDocuments', new MortgageApplicationDocumentsComponentNs.MortgageApplicationDocumentsComponent())