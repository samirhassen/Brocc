namespace ManualDocumentsToSignComponentNs {
    export class ManualDocumentsToSignController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData
        m: Model
        attachedFileName: string;
        attachedFileDataUrl: string;
        fileUpload: NtechAngularFileUpload.FileUploadHelper

        static $inject = ['$http', '$q', 'ntechComponentService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $scope: ManualDocumentsToSignComponentNs.LocalScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'manualDocumentsToSign'
        }

        civicRegNrPlaceHolderText() {
            if (ntechClientCountry == 'SE') {
                return 'YYYYMMDD-XXXX'
            } else if (ntechClientCountry == 'FI') {
                return 'DDMMYY-XXXX'
            } else {
                return ''
            }            
        }

        isValidCivicRegNr(value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            if (ntechClientCountry == 'SE') {
                return ntech.se.isValidCivicNr(value)
            } else if (ntechClientCountry == 'FI') {
                return ntech.fi.isValidCivicNr(value)
            } else {
                //So they can at least get the data in
                return true
            }
        }

        endsWith = (s: string, search: string, this_len?: number) => {
            //Polyfill: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
            if (this_len === undefined || this_len > s.length) {
                this_len = s.length;
            }
            return s.substring(this_len - search.length, this_len) === search;
        };

        setupFiles() {
            if (!this.fileUpload) {
                this.fileUpload = new NtechAngularFileUpload.FileUploadHelper((<HTMLInputElement>document.getElementById('file')),
                    (<HTMLFormElement>document.getElementById('documentform')),
                    this.$scope, this.$q);
                this.fileUpload.addFileAttachedListener(filenames => {
                    let filename = filenames[0]
                    if (!this.endsWith(filename, '.pdf')) {
                        this.fileUpload.reset()
                        toastr.warning('Input file must be an pdf file')
                        return
                    }
                    if (filenames.length == 0) {
                        this.attachedFileName = null
                    } else if (filenames.length == 1) {
                        this.attachedFileName = filenames[0];
                        this.fileUpload.loadSingleAttachedFileAsDataUrl().then(result => {
                            this.attachedFileDataUrl = result.dataUrl
                        })
                    } else {
                        this.attachedFileName = 'Error - multiple files selected!'
                    }
                });
            } else {
                this.resetAttachedFile()
            }
        }

        selectFileToAttach(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.setupFiles();
            this.fileUpload.showFilePicker();
        }

        resetAttachedFile() {
            this.attachedFileName = null;
            if (this.fileUpload) {
                this.fileUpload.reset();
            }
        }

        checkValidtoCreateLink = () => {
            if (this.m == null)
                return;
            if (!this.m.CivicNr || !this.m.Comment || !this.attachedFileName)
                return true;
        }

        createLink = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (this.m == null)
                return;
            if (!this.m.CivicNr || !this.m.Comment || !this.attachedFileName)
                return;

            //Create archivekey to ndocument
            this.apiClient.createManualSignatureDocuments(this.attachedFileDataUrl, this.attachedFileName, this.m.CivicNr, this.m.Comment).then(documentsResponse => {
                this.m.UnsignedDocuments.push({
                    CreationDate: documentsResponse.CreationDate,
                    CommentText: this.m.Comment,
                    SignatureUrl: documentsResponse.SignatureUrl,
                    UnSignedDocumentArchiveUrl: documentsResponse.ArchiveDocumentUrl,
                    SignedDocumentArchiveUrl: null,
                    SignatureSessionId: documentsResponse.SessionId,
                    IsRemoved: null,
                    RemovedDate: null,
                    IsHandled: null,
                    HandledDate: null,
                    SignedDate: null
                })
                this.m.CivicNr = null
                this.m.Comment = null
                this.attachedFileDataUrl = null
                this.attachedFileName = null
                this.resetValidation(this.$scope.civicRegNrForm)
                this.resetValidation(this.$scope.textForm)
            }
            )
        }

        resetValidation(f: ng.IFormController) {
            f.$setPristine()
            f.$setUntouched()
        }

        removeDocument = (evt: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            this.attachedFileName = null;
        }

        cancelRemove = (idx, evt) => {
            if (evt) {
                evt.preventDefault()
            }
            var signatureSessionId = this.m.UnsignedDocuments[idx].SignatureSessionId
            this.m.UnsignedDocuments.splice(idx, 1);
            this.apiClient.deleteManualSignatureDocuments(signatureSessionId.toString());
        }

        handle = (idx, evt) => {
            if (evt) {
                evt.preventDefault()
            }
            var signatureSessionId = this.m.SignedDocuments[idx].SignatureSessionId
            this.m.SignedDocuments.splice(idx, 1);
            this.apiClient.handleManualSignatureDocuments(signatureSessionId.toString());
        }

        init = () => {
            this.m = null
            this.resetAttachedFile()

            if (!this.initialData) {
                return
            }
            this.apiClient.getManualSignatureDocuments(false).then(ManualSignatureResponse => {
                this.apiClient.getManualSignatureDocuments(true).then(ManualSignatureResponseSignedDocuments => {
                    this.m = {
                        CivicNr: null,
                        Comment: null,
                        UnsignedDocuments: ManualSignatureResponse,
                        SignedDocuments: ManualSignatureResponseSignedDocuments,
                        HeaderData: {
                            backTarget: null,
                            backContext: null,
                            host: this.initialData
                        }
                    }
                })
            })
        }

        onChanges() {
            this.init()
        }
    }

    export class ManualDocumentsToSignComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ManualDocumentsToSignController;
            this.template = `<page-header initial-data="$ctrl.m.HeaderData" title-text="'Manual signatures'"></page-header>
        <div class="row">
        <div class="col-sm-8 col-sm-offset-2">
            <div class="editblock">
                <div class="form-horizontal">
                    <div class="form-group">
                        <form novalidate name="civicRegNrForm">
                            <ntech-input t="'custom'" required="true" label="'Civic nr'" model="$ctrl.m.CivicNr" label-classes="'col-xs-6'" input-classes="'col-xs-4'" placeholder-text="$ctrl.civicRegNrPlaceHolderText()" custom-is-valid="$ctrl.isValidCivicRegNr"></ntech-input>
                        </form>
                    </div>
                    <div class="form-group">
                        <label class="control-label col-xs-6">Choose document</label>
                        <div class="pull-left col-xs-6 pt-1">
                            <button ng-click="$ctrl.selectFileToAttach($event)" class="n-direct-btn n-white-btn">Attach <span class="glyphicon glyphicon-paperclip"></span></button>
                            <form novalidate class="form-inline" name="documentform" id="documentform">
                                <div class="pull-right" style="margin-top: -21px;" ng-show="$ctrl.attachedFileName">
                                    <span class="custom-fileupload-preview">
                                        {{$ctrl.attachedFileName}}
                                        <span ng-show="$ctrl.attachedFileName" ng-click="$ctrl.removeDocument($event)" class="glyphicon glyphicon-remove" style="margin-left:10px;"></span>
                                    </span>
                                </div>
                                <input type="file" id="file" name="file" style="display:none" />
                                <div class="clearfix"></div>
                            </form>
                        </div>
                    </div>
                    <div class="form-group">
                        <form novalidate name="textForm">
                        <ntech-input t="'text'" inputtype="'textarea'" rows="3" required="true" label="'Comment'" model="$ctrl.m.Comment" label-classes="'col-xs-6'" input-classes="'col-xs-4'"></ntech-input>
                        </form>
                    </div>
                    <div class="form-group">
                        <label class="control-label col-xs-6"></label>
                        <div class="pull-left col-xs-4 pt-1">
                            <button ng-click="$ctrl.createLink($event)" ng-disabled="$ctrl.checkValidtoCreateLink()" class="n-direct-btn n-blue-btn">CREATE LINK</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row pt-3 pb-3">
        <h2>Waiting for signing</h2>
        <hr class="hr-section" />
        <table class="table">
            <thead>
                <tr>
                    <th class="col-xs-1">Date</th>
                    <th class="col-xs-4">Comment</th>
                    <th class="col-xs-2">Unsigned document</th>
                    <th class="col-xs-4">Link</th>
                    <th class="col-xs-1"></th>
                </tr>
            </thead>
            <tbody>
                <tr ng-repeat="i in $ctrl.m.UnsignedDocuments">
                    <td>{{i.CreationDate | date}}</td>
                    <td>{{i.CommentText}}</td>
                    <td><a ng-href="{{i.UnSignedDocumentArchiveUrl}}" target="_blank" class="n-direct-btn n-purple-btn">Download <span class="glyphicon glyphicon-save"></span></a></td>
                    <td><span class="copyable">{{i.SignatureUrl}}</span></td>
                    <td> <button ng-click="$ctrl.cancelRemove($index, $event)" class="n-direct-btn n-white-btn">Cancel</button> </td>
                </tr>
            </tbody>
        </table>
    </div>

    <div class="row pt-3">
        <h2>Signed documents</h2>
        <hr class="hr-section"/>
        <table class="table">
            <thead>
                <tr>
                    <th class="col-xs-1">Date</th>
                    <th class="col-xs-4">Comment</th>
                    <th class="col-xs-2">Signed document</th>
                    <th class="col-xs-4">Signed date</th>
                    <th class="col-xs-1"></th>
                </tr>
            </thead>
            <tbody>
                <tr ng-repeat="i in $ctrl.m.SignedDocuments">
                    <td>{{i.CreationDate | date}}</td>
                    <td>{{i.CommentText}}</td>
                    <td><a ng-href="{{i.SignedDocumentArchiveUrl}}" target="_blank" class="n-direct-btn n-purple-btn">Download <span class="glyphicon glyphicon-save"></span></a></td>
                    <td><span class="copyable">{{i.SignedDate| date}}</span></td>
                    <td> <button ng-click="$ctrl.handle($index, $event)" class="n-direct-btn n-green-btn">Handled</button> </td>
                </tr>
            </tbody>
        </table>
    </div>`
        }
    }
    export class Model {
        CivicNr: string
        Comment: string
        UnsignedDocuments: UnsignedDocumentModel[]
        SignedDocuments: UnsignedDocumentModel[]
        HeaderData: PageHeaderComponentNs.InitialData
    }
    export class UnsignedDocumentModel {
        SignatureSessionId: string
        CreationDate: Date
        CommentText: string
        UnSignedDocumentArchiveUrl: string
        IsRemoved: boolean
        RemovedDate: Date
        IsHandled: boolean
        HandledDate: Date
        SignedDocumentArchiveUrl: string
        SignedDate: Date
        SignatureUrl: string
    }
    export class documentsResponse {
        ArchiveDocumentUrl: string
        SessionId: string
        RedirectAfterSuccessUrl: string
    }
    export interface LocalInitialData {
    }

    export interface InitialData extends LocalInitialData, ComponentHostNs.ComponentHostInitialData {
    }

    export interface LocalScope extends ng.IScope {
        textForm: ng.IFormController
        civicRegNrForm: ng.IFormController
    }
}

angular.module('ntech.components').component('manualDocumentsToSign', new ManualDocumentsToSignComponentNs.ManualDocumentsToSignComponent())