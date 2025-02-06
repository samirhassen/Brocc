namespace ApplicationFreeformDocumentsComponentNs {
    const FreeFormDocumentType: string = 'Freeform'

    export class ApplicationDocumentsController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        onDocumentsAddedOrRemoved: (() => void);

        documents: EditDocumentModel[]
        fileUploadHelper: NtechAngularFileUpload.FileUploadHelper

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        isEditAllowed() {
            return this.onDocumentsAddedOrRemoved && this.initialData && this.initialData.applicationInfo.IsActive && !this.initialData.applicationInfo.IsPartiallyApproved && !this.initialData.isReadOnly;
        }

        componentName(): string {
            return 'applicationFreeformDocuments'
        }

        hasEdits() {
            if (!this.documents) {
                return false
            }
            for (let d of this.documents) {
                if (d.localState) {
                    return true
                }
            }
        }

        saveEdits(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let promises = []
            let applicationNr = this.initialData.applicationInfo.ApplicationNr
            for (let d of this.documents) {
                if (d.localState) {
                    if (d.localState.isAttach) {
                        promises.push(this.apiClient.addApplicationDocument(applicationNr, FreeFormDocumentType, null, d.localState.dataUrl, d.localState.filename, null, null))
                    } else {
                        promises.push(this.apiClient.removeApplicationDocument(applicationNr, d.serverState.documentId))
                    }
                }
            }
            if (promises.length === 0) {
                return
            }

            this.$q.all(promises).then(result => {
                if (this.onDocumentsAddedOrRemoved) {
                    this.onDocumentsAddedOrRemoved()
                    this.onChanges()
                }
            })
        }

        cancelEdits(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let ds = []
            for (let d of this.documents) {
                if (!d.localState || !d.localState.isAttach) {
                    d.localState = null
                    ds.push(d)
                }
            }
            this.documents = ds
        }

        onChanges() {
            this.documents = null;
            if (!this.initialData) {
                return
            }

            this.apiClient.fetchFreeformApplicationDocuments(this.initialData.applicationInfo.ApplicationNr).then(result => {
                let documents: EditDocumentModel[] = []
                for (let d of result) {
                    let serverState: DocumentServerState = {
                        documentId: d.DocumentId,
                        downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(d),
                        filename: d.Filename,
                        date: d.DocumentDate
                    }
                    documents.push({
                        serverState: serverState,
                        localState: null,
                        uploadHelper: null
                    })
                }

                this.documents = documents;
                this.setupUploadHelpers();
            })
        }

        setupUploadHelpers() {
            let input = document.createElement('input');
            input.type = 'file';
            let form = document.createElement('form');
            form.appendChild(input);
            let ul = new NtechAngularFileUpload.FileUploadHelper(input,
                form,
                this.$scope, this.$q);
            ul.addFileAttachedListener(filesNames => {
                if (filesNames.length > 1) {
                    toastr.warning('More than one file attached')
                } else if (filesNames.length == 1) {
                    ul.loadSingleAttachedFileAsDataUrl().then(result => {
                        this.documents.push({
                            serverState: null,
                            localState: {
                                dataUrl: result.dataUrl,
                                isAttach: true,
                                filename: result.filename
                            },
                            uploadHelper: null
                        })
                    })
                }
            });

            this.fileUploadHelper = ul
        }

        attachDocument(evt: Event) {
            this.fileUploadHelper.showFilePicker()
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

    export class ApplicationFreeformDocumentsComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<',
                onDocumentsAddedOrRemoved: '&'
            };
            this.controller = ApplicationDocumentsController;
            this.templateUrl = 'application-freeform-documents.html';
        }
    }

    export class InitialData {
        public applicationInfo: NTechPreCreditApi.ApplicationInfoModel
        public isReadOnly?: boolean
    }

    class EditDocumentModel {
        serverState: DocumentServerState
        localState: DocumentLocalState
        uploadHelper: NtechAngularFileUpload.FileUploadHelper
    }
    class DocumentServerState {
        documentId: number
        filename: string
        downloadUrl: string
        date: Date
    }
    class DocumentLocalState {
        isAttach: boolean
        dataUrl: string
        filename: string
    }
}

angular.module('ntech.components').component('applicationFreeformDocuments', new ApplicationFreeformDocumentsComponentNs.ApplicationFreeformDocumentsComponent())