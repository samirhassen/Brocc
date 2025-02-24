var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var ApplicationDocumentsComponentNs;
(function (ApplicationDocumentsComponentNs) {
    var ApplicationDocumentsController = /** @class */ (function (_super) {
        __extends(ApplicationDocumentsController, _super);
        function ApplicationDocumentsController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            return _this;
        }
        ApplicationDocumentsController.prototype.getOnDocumentsAddedOrRemoved = function () {
            var _this = this;
            if (!this.initialData) {
                return null;
            }
            else if (this.initialData.onDocumentsAddedOrRemoved) {
                return this.initialData.onDocumentsAddedOrRemoved;
            }
            else if (this.getOnDocumentsAddedOrRemoved) {
                return function (_) { return _this.getOnDocumentsAddedOrRemoved(); };
            }
            else {
                return null;
            }
        };
        ApplicationDocumentsController.prototype.isEditAllowed = function () {
            var f = this.getOnDocumentsAddedOrRemoved();
            var i = this.initialData;
            return f
                && i.applicationInfo.IsActive
                && !i.forceReadonly
                && !i.applicationInfo.IsPartiallyApproved && !i.applicationInfo.IsFinalDecisionMade;
        };
        ApplicationDocumentsController.prototype.componentName = function () {
            return 'applicationDocuments';
        };
        ApplicationDocumentsController.prototype.isAnyDocumentMissing = function () {
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                if (!d.localState && !d.serverState) {
                    return true;
                }
            }
            return false;
        };
        ApplicationDocumentsController.prototype.setEditMode = function (isEditMode, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.isEditMode = isEditMode;
            //Always reset local state on change of edit mode
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                d.localState = null;
            }
        };
        ApplicationDocumentsController.prototype.saveEdits = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var promises = [];
            var applicationNr = this.initialData.applicationInfo.ApplicationNr;
            var _loop_1 = function (d) {
                if (d.localState && d.localState.isAttach) {
                    if (d.serverState) {
                        //Replace serverside document
                        promises.push(this_1.apiClient.addAndRemoveApplicationDocument(applicationNr, d.model.documentType, d.model.applicantNr, d.localState.dataUrl, d.localState.filename, d.serverState.documentId, d.model.customerId, d.model.documentSubType).then(function (addedDocument) {
                            d.serverState = { documentId: addedDocument.DocumentId, downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(addedDocument), filename: addedDocument.Filename };
                        }));
                    }
                    else {
                        //Add non existing
                        promises.push(this_1.apiClient.addApplicationDocument(applicationNr, d.model.documentType, d.model.applicantNr, d.localState.dataUrl, d.localState.filename, d.model.customerId, d.model.documentSubType).then(function (addedDocument) {
                            d.serverState = { documentId: addedDocument.DocumentId, downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(addedDocument), filename: addedDocument.Filename };
                        }));
                    }
                }
                else if (d.localState && !d.localState.isAttach && d.serverState) {
                    //Remove existing
                    promises.push(this_1.apiClient.removeApplicationDocument(applicationNr, d.serverState.documentId).then(function (_) {
                        d.serverState = null;
                    }));
                }
            };
            var this_1 = this;
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                _loop_1(d);
            }
            this.$q.all(promises).then(function (result) {
                _this.setEditMode(false, null);
                var f = _this.getOnDocumentsAddedOrRemoved();
                if (f) {
                    f(!_this.isAnyDocumentMissing());
                }
            });
        };
        ApplicationDocumentsController.prototype.areEqualOptionalNumbers = function (n1, n2) {
            if ((n1 === null || n1 === undefined) && (n2 === null || n2 === undefined)) {
                return true;
            }
            else {
                return n1 === n2;
            }
        };
        ApplicationDocumentsController.prototype.areSameDocumentSubType = function (t1, t2) {
            if (!t1 && !t2) {
                return true;
            }
            else {
                return t1 === t2;
            }
        };
        ApplicationDocumentsController.prototype.onChanges = function () {
            var _this = this;
            this.isEditMode = false;
            this.documents = null;
            if (this.initialData) {
                var documentTypes = [];
                for (var _i = 0, _a = this.initialData.documents; _i < _a.length; _i++) {
                    var d = _a[_i];
                    documentTypes.push(d.documentType);
                }
                this.apiClient.fetchApplicationDocuments(this.initialData.applicationInfo.ApplicationNr, documentTypes).then(function (result) {
                    var documents = [];
                    for (var _i = 0, _a = _this.initialData.documents; _i < _a.length; _i++) {
                        var model = _a[_i];
                        var serverState = null;
                        for (var _b = 0, _c = _.sortBy(result, function (x) { return x.DocumentId; }); _b < _c.length; _b++) { //If there are several, show the latest. Could potentially show all as well
                            var sd = _c[_b];
                            if (model.documentType === sd.DocumentType && _this.areEqualOptionalNumbers(model.applicantNr, sd.ApplicantNr) && _this.areEqualOptionalNumbers(model.customerId, sd.CustomerId) && _this.areSameDocumentSubType(model.documentSubType, sd.DocumentSubType)) {
                                serverState = {
                                    documentId: sd.DocumentId,
                                    downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(sd),
                                    filename: sd.Filename
                                };
                            }
                        }
                        documents.push({
                            model: model,
                            serverState: serverState,
                            localState: null,
                            uploadHelper: null
                        });
                    }
                    _this.documents = documents;
                    _this.setupUploadHelpers();
                });
            }
        };
        ApplicationDocumentsController.prototype.setupUploadHelpers = function () {
            if (this.documents) {
                var _loop_2 = function (d) {
                    var input = document.createElement('input');
                    input.type = 'file';
                    var form = document.createElement('form');
                    form.appendChild(input);
                    d.uploadHelper = new NtechAngularFileUpload.FileUploadHelper(input, form, this_2.$scope, this_2.$q);
                    d.uploadHelper.addFileAttachedListener(function (filesNames) {
                        if (filesNames.length > 1) {
                            toastr.warning('More than one file attached');
                        }
                        else if (filesNames.length == 1) {
                            d.uploadHelper.loadSingleAttachedFileAsDataUrl().then(function (result) {
                                d.localState = {
                                    isAttach: true,
                                    dataUrl: result.dataUrl,
                                    filename: result.filename
                                };
                            });
                        }
                    });
                };
                var this_2 = this;
                for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                    var d = _a[_i];
                    _loop_2(d);
                }
            }
        };
        ApplicationDocumentsController.prototype.attachDocument = function (model, evt) {
            if (model.uploadHelper) {
                model.uploadHelper.showFilePicker();
            }
        };
        ApplicationDocumentsController.prototype.removeDocument = function (model, evt) {
            if (evt) {
                evt.preventDefault();
            }
            model.localState = {
                isAttach: false,
                dataUrl: null,
                filename: null
            };
        };
        ApplicationDocumentsController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return ApplicationDocumentsController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationDocumentsComponentNs.ApplicationDocumentsController = ApplicationDocumentsController;
    var ApplicationDocumentsComponent = /** @class */ (function () {
        function ApplicationDocumentsComponent() {
            this.bindings = {
                initialData: '<',
                onDocumentsAddedOrRemoved: '&'
            };
            this.controller = ApplicationDocumentsController;
            this.templateUrl = 'application-documents.html';
        }
        return ApplicationDocumentsComponent;
    }());
    ApplicationDocumentsComponentNs.ApplicationDocumentsComponent = ApplicationDocumentsComponent;
    var DocumentModel = /** @class */ (function () {
        function DocumentModel() {
        }
        return DocumentModel;
    }());
    ApplicationDocumentsComponentNs.DocumentModel = DocumentModel;
    var InitialData = /** @class */ (function () {
        function InitialData(applicationInfo) {
            var _this = this;
            this.applicationInfo = applicationInfo;
            /**
             * @param titleTemplate any occurances of [[applicantNr]] will be replaced with applicant nr and the document will be repeated for each applicant.
             */
            this.addDocumentForAllApplicants = function (documentType, titleTemplate) {
                for (var applicantNr = 1; applicantNr <= _this.applicationInfo.NrOfApplicants; applicantNr++) {
                    _this.documents.push({
                        documentType: documentType,
                        documentTitle: titleTemplate.replace('[[applicantNr]]', applicantNr.toString()),
                        applicantNr: applicantNr,
                        customerId: null,
                        documentSubType: null
                    });
                }
                return _this;
            };
            this.documents = [];
        }
        InitialData.prototype.addSharedDocument = function (documentType, title) {
            this.documents.push({
                documentType: documentType,
                documentTitle: title,
                applicantNr: null,
                customerId: null,
                documentSubType: null
            });
            return this;
        };
        InitialData.prototype.addDocumentForSingleApplicant = function (documentType, title, applicantNr) {
            this.documents.push({
                documentType: documentType,
                documentTitle: title,
                applicantNr: applicantNr,
                customerId: null,
                documentSubType: null
            });
            return this;
        };
        InitialData.prototype.addComplexDocument = function (documentType, documentTitle, applicantNr, customerId, documentSubType) {
            this.documents.push({
                documentType: documentType,
                documentTitle: documentTitle,
                customerId: customerId,
                applicantNr: applicantNr,
                documentSubType: documentSubType
            });
        };
        return InitialData;
    }());
    ApplicationDocumentsComponentNs.InitialData = InitialData;
    var EditDocumentModel = /** @class */ (function () {
        function EditDocumentModel() {
        }
        return EditDocumentModel;
    }());
    var DocumentServerState = /** @class */ (function () {
        function DocumentServerState() {
        }
        return DocumentServerState;
    }());
    var DocumentLocalState = /** @class */ (function () {
        function DocumentLocalState() {
        }
        return DocumentLocalState;
    }());
})(ApplicationDocumentsComponentNs || (ApplicationDocumentsComponentNs = {}));
angular.module('ntech.components').component('applicationDocuments', new ApplicationDocumentsComponentNs.ApplicationDocumentsComponent());
