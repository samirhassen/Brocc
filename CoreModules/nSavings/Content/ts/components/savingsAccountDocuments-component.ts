namespace SavingsAccountDocumentsComponentNs {

    export class SavingsAccountDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService) {
            super(ntechComponentService, $http, $q);
 
        }

        componentName(): string {
            return 'savingsAccountDocuments'
        }

        onChanges() {
            this.m = null
            if (!this.initialData) {
                return
            }
            if (this.initialData.documents) {
                this.m = {
                    documents : this.initialData.documents
                }
            }            
        }

        getDocumentDisplayName(d: DocumentModel) {
            if (d.DocumentType === 'YearlySummary') {
                return 'Annual summary for ' + d.DocumentData
            } else {
                return d.DocumentType
            }
        }
    }

    export class SavingsAccountDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = SavingsAccountDocumentsController;
            this.templateUrl = 'savings-account-documents.html';
        }
    }

    export class DocumentModel {
        DocumentType: string
        DocumentData: string
        CreationDate: Date
        DownloadUrl: string
    }

    export class InitialData {
        documents? : DocumentModel[]
    }

    export class Model {
        documents: DocumentModel[]
    }
}

angular.module('ntech.components').component('savingsAccountDocuments', new SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsComponent())