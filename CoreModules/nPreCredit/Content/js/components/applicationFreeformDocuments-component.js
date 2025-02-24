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
var ApplicationFreeformDocumentsComponentNs;
(function (ApplicationFreeformDocumentsComponentNs) {
    var FreeFormDocumentType = 'Freeform';
    var ApplicationDocumentsController = /** @class */ (function (_super) {
        __extends(ApplicationDocumentsController, _super);
        function ApplicationDocumentsController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            return _this;
        }
        ApplicationDocumentsController.prototype.isEditAllowed = function () {
            return this.onDocumentsAddedOrRemoved && this.initialData && this.initialData.applicationInfo.IsActive && !this.initialData.applicationInfo.IsPartiallyApproved && !this.initialData.isReadOnly;
        };
        ApplicationDocumentsController.prototype.componentName = function () {
            return 'applicationFreeformDocuments';
        };
        ApplicationDocumentsController.prototype.hasEdits = function () {
            if (!this.documents) {
                return false;
            }
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                if (d.localState) {
                    return true;
                }
            }
        };
        ApplicationDocumentsController.prototype.saveEdits = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var promises = [];
            var applicationNr = this.initialData.applicationInfo.ApplicationNr;
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                if (d.localState) {
                    if (d.localState.isAttach) {
                        promises.push(this.apiClient.addApplicationDocument(applicationNr, FreeFormDocumentType, null, d.localState.dataUrl, d.localState.filename, null, null));
                    }
                    else {
                        promises.push(this.apiClient.removeApplicationDocument(applicationNr, d.serverState.documentId));
                    }
                }
            }
            if (promises.length === 0) {
                return;
            }
            this.$q.all(promises).then(function (result) {
                if (_this.onDocumentsAddedOrRemoved) {
                    _this.onDocumentsAddedOrRemoved();
                    _this.onChanges();
                }
            });
        };
        ApplicationDocumentsController.prototype.cancelEdits = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var ds = [];
            for (var _i = 0, _a = this.documents; _i < _a.length; _i++) {
                var d = _a[_i];
                if (!d.localState || !d.localState.isAttach) {
                    d.localState = null;
                    ds.push(d);
                }
            }
            this.documents = ds;
        };
        ApplicationDocumentsController.prototype.onChanges = function () {
            var _this = this;
            this.documents = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchFreeformApplicationDocuments(this.initialData.applicationInfo.ApplicationNr).then(function (result) {
                var documents = [];
                for (var _i = 0, result_1 = result; _i < result_1.length; _i++) {
                    var d = result_1[_i];
                    var serverState = {
                        documentId: d.DocumentId,
                        downloadUrl: NTechPreCreditApi.ApplicationDocument.GetDownloadUrl(d),
                        filename: d.Filename,
                        date: d.DocumentDate
                    };
                    documents.push({
                        serverState: serverState,
                        localState: null,
                        uploadHelper: null
                    });
                }
                _this.documents = documents;
                _this.setupUploadHelpers();
            });
        };
        ApplicationDocumentsController.prototype.setupUploadHelpers = function () {
            var _this = this;
            var input = document.createElement('input');
            input.type = 'file';
            var form = document.createElement('form');
            form.appendChild(input);
            var ul = new NtechAngularFileUpload.FileUploadHelper(input, form, this.$scope, this.$q);
            ul.addFileAttachedListener(function (filesNames) {
                if (filesNames.length > 1) {
                    toastr.warning('More than one file attached');
                }
                else if (filesNames.length == 1) {
                    ul.loadSingleAttachedFileAsDataUrl().then(function (result) {
                        _this.documents.push({
                            serverState: null,
                            localState: {
                                dataUrl: result.dataUrl,
                                isAttach: true,
                                filename: result.filename
                            },
                            uploadHelper: null
                        });
                    });
                }
            });
            this.fileUploadHelper = ul;
        };
        ApplicationDocumentsController.prototype.attachDocument = function (evt) {
            this.fileUploadHelper.showFilePicker();
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
    ApplicationFreeformDocumentsComponentNs.ApplicationDocumentsController = ApplicationDocumentsController;
    var ApplicationFreeformDocumentsComponent = /** @class */ (function () {
        function ApplicationFreeformDocumentsComponent() {
            this.bindings = {
                initialData: '<',
                onDocumentsAddedOrRemoved: '&'
            };
            this.controller = ApplicationDocumentsController;
            this.templateUrl = 'application-freeform-documents.html';
        }
        return ApplicationFreeformDocumentsComponent;
    }());
    ApplicationFreeformDocumentsComponentNs.ApplicationFreeformDocumentsComponent = ApplicationFreeformDocumentsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationFreeformDocumentsComponentNs.InitialData = InitialData;
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
})(ApplicationFreeformDocumentsComponentNs || (ApplicationFreeformDocumentsComponentNs = {}));
angular.module('ntech.components').component('applicationFreeformDocuments', new ApplicationFreeformDocumentsComponentNs.ApplicationFreeformDocumentsComponent());
