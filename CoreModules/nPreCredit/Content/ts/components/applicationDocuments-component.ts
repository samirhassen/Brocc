namespace ApplicationDocumentsComponentNs {
    export class ApplicationDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        onDocumentsAddedOrRemoved: (() => void);

        documents: EditDocumentModel[]
        isEditMode: boolean
        fileUploadHelper: NtechAngularFileUpload.FileUploadHelper

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        getOnDocumentsAddedOrRemoved(): ((areAllDocumentAdded: boolean) => void) {
            if (!this.initialData) {
                return null
            } else if (this.initialData.onDocumentsAddedOrRemoved) {
                return this.initialData.onDocumentsAddedOrRemoved
            } else if (this.getOnDocumentsAddedOrRemoved) {
                return _ => this.getOnDocumentsAddedOrRemoved()
            } else {
                return null
            }
        }

        isEditAllowed() {
            let f = this.getOnDocumentsAddedOrRemoved()
            let i = this.initialData
            return f
                && i.applicationInfo.IsActive
                && !i.forceReadonly
                && !i.applicationInfo.IsPartiallyApproved && !i.applicationInfo.IsFinalDecisionMade;
        }

        componentName(): string {
            return 'applicationDocuments'
        }

        isAnyDocumentMissing() {
            for (let d of this.documents) {
                if (!d.localState && !d.serverState) {
                    return true
                }
            }
            return false
        }

        setEditMode(isEditMode: boolean, evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            this.isEditMode = isEditMode;
            //Always reset local state on change of edit mode
            for (let d of this.documents) {
                d.localState = null;
            }
        }

        saveEdits(evt: Event) {
            if (evt) {
                evt.preventDefault();
            }
            let promises = []
            let applicationNr = this.initialData.applicationInfo.ApplicationNr;
            for (let d of this.documents) {
                if (d.localState && d.localState.isAttach) {
                    if (d.serverState) {
                        //Replace serverside document
                        promises.push(this.apiClient.addAndRemoveApplicationDocument(applicationNr, d.model.documentType, d.model.applicantNr, d.localState.dataUrl, d.localState.filename, d.serverState.documentId, d.model.customerId, d.model.documentSubType).then(addedDocument => {
                            d.serverState = { documentId: addedDocument.DocumentId, downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(addedDocument), filename: addedDocument.Filename }
                        }))
                    } else {
                        //Add non existing
                        promises.push(this.apiClient.addApplicationDocument(applicationNr, d.model.documentType, d.model.applicantNr, d.localState.dataUrl, d.localState.filename, d.model.customerId, d.model.documentSubType).then(addedDocument => {
                            d.serverState = { documentId: addedDocument.DocumentId, downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(addedDocument), filename: addedDocument.Filename }
                        }))
                    }
                } else if (d.localState && !d.localState.isAttach && d.serverState) {
                    //Remove existing
                    promises.push(this.apiClient.removeApplicationDocument(applicationNr, d.serverState.documentId).then(_ => {
                        d.serverState = null
                    }));
                }
            }
            this.$q.all(promises).then(result => {
                this.setEditMode(false, null);
                let f = this.getOnDocumentsAddedOrRemoved()
                if (f) {
                    f(!this.isAnyDocumentMissing())
                }
            })
        }

        private areEqualOptionalNumbers(n1: number, n2: number) {
            if ((n1 === null || n1 === undefined) && (n2 === null || n2 === undefined)) {
                return true;
            } else {
                return n1 === n2;
            }
        }

        private areSameDocumentSubType(t1: string, t2: string) {
            if (!t1 && !t2) {
                return true
            } else {
                return t1 === t2
            }
        }

        onChanges() {
            this.isEditMode = false;
            this.documents = null;
            if (this.initialData) {
                let documentTypes = []
                for (let d of this.initialData.documents) {
                    documentTypes.push(d.documentType)
                }
                this.apiClient.fetchApplicationDocuments(this.initialData.applicationInfo.ApplicationNr, documentTypes).then(result => {
                    let documents: EditDocumentModel[] = []
                    for (let model of this.initialData.documents) {
                        let serverState: DocumentServerState = null

                        for (let sd of _.sortBy(result, x => x.DocumentId)) { //If there are several, show the latest. Could potentially show all as well
                            if (model.documentType === sd.DocumentType && this.areEqualOptionalNumbers(model.applicantNr, sd.ApplicantNr) && this.areEqualOptionalNumbers(model.customerId, sd.CustomerId) && this.areSameDocumentSubType(model.documentSubType, sd.DocumentSubType)) {
                                serverState = {
                                    documentId: sd.DocumentId,
                                    downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(sd),
                                    filename: sd.Filename
                                }
                            }
                        }

                        documents.push({
                            model: model,
                            serverState: serverState,
                            localState: null,
                            uploadHelper: null
                        })
                    }

                    this.documents = documents;
                    this.setupUploadHelpers();
                })
            }
        }

        setupUploadHelpers() {
            if (this.documents) {
                for (let d of this.documents) {
                    let input = document.createElement('input');
                    input.type = 'file';
                    let form = document.createElement('form');
                    form.appendChild(input)
                    d.uploadHelper = new NtechAngularFileUpload.FileUploadHelper(input,
                        form,
                        this.$scope, this.$q);
                    d.uploadHelper.addFileAttachedListener(filesNames => {
                        if (filesNames.length > 1) {
                            toastr.warning('More than one file attached')
                        } else if (filesNames.length == 1) {
                            d.uploadHelper.loadSingleAttachedFileAsDataUrl().then(result => {
                                d.localState = {
                                    isAttach: true,
                                    dataUrl: result.dataUrl,
                                    filename: result.filename
                                }
                            })
                        }
                    });
                }
            }
        }

        attachDocument(model: EditDocumentModel, evt: Event) {
            if (model.uploadHelper) {
                model.uploadHelper.showFilePicker();
            }
        }

        removeDocument(model: EditDocumentModel, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            model.localState = {
                isAttach: false,
                dataUrl: null,
                filename: null
            }
        }
    }

    export class ApplicationDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<',
                onDocumentsAddedOrRemoved: '&'
            };
            this.controller = ApplicationDocumentsController;
            this.templateUrl = 'application-documents.html';
        }
    }
    export class DocumentModel {
        applicantNr: number
        customerId: number
        documentType: string
        documentTitle: string
        documentSubType: string
    }
    export class InitialData {
        public onDocumentsAddedOrRemoved?: ((areAllDocumentAdded: boolean) => void)
        public forceReadonly: boolean

        constructor(public applicationInfo: NTechPreCreditApi.ApplicationInfoModel) {
            this.documents = []
        }

        /**
         * @param titleTemplate any occurances of [[applicantNr]] will be replaced with applicant nr and the document will be repeated for each applicant.
         */
        addDocumentForAllApplicants = (documentType: string, titleTemplate: string) => {
            for (let applicantNr = 1; applicantNr <= this.applicationInfo.NrOfApplicants; applicantNr++) {
                this.documents.push({
                    documentType: documentType,
                    documentTitle: titleTemplate.replace('[[applicantNr]]', applicantNr.toString()),
                    applicantNr: applicantNr,
                    customerId: null,
                    documentSubType: null
                })
            }
            return this;
        }

        addSharedDocument(documentType: string, title: string) {
            this.documents.push({
                documentType: documentType,
                documentTitle: title,
                applicantNr: null,
                customerId: null,
                documentSubType: null
            })
            return this;
        }

        addDocumentForSingleApplicant(documentType: string, title: string, applicantNr: number) {
            this.documents.push({
                documentType: documentType,
                documentTitle: title,
                applicantNr: applicantNr,
                customerId: null,
                documentSubType: null
            })
            return this;
        }

        addComplexDocument(documentType: string, documentTitle: string, applicantNr: number, customerId: number, documentSubType: string) {
            this.documents.push({
                documentType: documentType,
                documentTitle: documentTitle,
                customerId: customerId,
                applicantNr: applicantNr,
                documentSubType: documentSubType
            })
        }

        documents: DocumentModel[]
    }

    class EditDocumentModel {
        model: DocumentModel
        serverState: DocumentServerState
        localState: DocumentLocalState
        uploadHelper: NtechAngularFileUpload.FileUploadHelper
    }
    class DocumentServerState {
        documentId: number
        filename: string
        downloadUrl: string
    }
    class DocumentLocalState {
        isAttach: boolean
        dataUrl: string
        filename: string
    }
}

angular.module('ntech.components').component('applicationDocuments', new ApplicationDocumentsComponentNs.ApplicationDocumentsComponent())