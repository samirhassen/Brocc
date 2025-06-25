var SavingsAccountDocumentsComponentNs;
(function (SavingsAccountDocumentsComponentNs) {
    class SavingsAccountDocumentsController extends NTechComponents.NTechComponentControllerBase {
        constructor($http, $q, ntechComponentService) {
            super(ntechComponentService, $http, $q);
        }
        componentName() {
            return 'savingsAccountDocuments';
        }
        onChanges() {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            if (this.initialData.documents) {
                this.m = {
                    documents: this.initialData.documents
                };
            }
        }
        getDocumentDisplayName(d) {
            if (d.DocumentType === 'YearlySummary') {
                return 'Annual summary for ' + d.DocumentData;
            }
            else {
                return d.DocumentType;
            }
        }
    }
    SavingsAccountDocumentsController.$inject = ['$http', '$q', 'ntechComponentService'];
    SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsController = SavingsAccountDocumentsController;
    class SavingsAccountDocumentsComponent {
        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = SavingsAccountDocumentsController;
            this.templateUrl = 'savings-account-documents.html';
        }
    }
    SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsComponent = SavingsAccountDocumentsComponent;
    class DocumentModel {
    }
    SavingsAccountDocumentsComponentNs.DocumentModel = DocumentModel;
    class InitialData {
    }
    SavingsAccountDocumentsComponentNs.InitialData = InitialData;
    class Model {
    }
    SavingsAccountDocumentsComponentNs.Model = Model;
})(SavingsAccountDocumentsComponentNs || (SavingsAccountDocumentsComponentNs = {}));
angular.module('ntech.components').component('savingsAccountDocuments', new SavingsAccountDocumentsComponentNs.SavingsAccountDocumentsComponent());
