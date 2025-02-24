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
var ManualDocumentsToSignComponentNs;
(function (ManualDocumentsToSignComponentNs) {
    var ManualDocumentsToSignController = /** @class */ (function (_super) {
        __extends(ManualDocumentsToSignController, _super);
        function ManualDocumentsToSignController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.endsWith = function (s, search, this_len) {
                //Polyfill: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
                if (this_len === undefined || this_len > s.length) {
                    this_len = s.length;
                }
                return s.substring(this_len - search.length, this_len) === search;
            };
            _this.checkValidtoCreateLink = function () {
                if (_this.m == null)
                    return;
                if (!_this.m.CivicNr || !_this.m.Comment || !_this.attachedFileName)
                    return true;
            };
            _this.createLink = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                if (_this.m == null)
                    return;
                if (!_this.m.CivicNr || !_this.m.Comment || !_this.attachedFileName)
                    return;
                //Create archivekey to ndocument
                _this.apiClient.createManualSignatureDocuments(_this.attachedFileDataUrl, _this.attachedFileName, _this.m.CivicNr, _this.m.Comment).then(function (documentsResponse) {
                    _this.m.UnsignedDocuments.push({
                        CreationDate: documentsResponse.CreationDate,
                        CommentText: _this.m.Comment,
                        SignatureUrl: documentsResponse.SignatureUrl,
                        UnSignedDocumentArchiveUrl: documentsResponse.ArchiveDocumentUrl,
                        SignedDocumentArchiveUrl: null,
                        SignatureSessionId: documentsResponse.SessionId,
                        IsRemoved: null,
                        RemovedDate: null,
                        IsHandled: null,
                        HandledDate: null,
                        SignedDate: null
                    });
                    _this.m.CivicNr = null;
                    _this.m.Comment = null;
                    _this.attachedFileDataUrl = null;
                    _this.attachedFileName = null;
                    _this.resetValidation(_this.$scope.civicRegNrForm);
                    _this.resetValidation(_this.$scope.textForm);
                });
            };
            _this.removeDocument = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                _this.attachedFileName = null;
            };
            _this.cancelRemove = function (idx, evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var signatureSessionId = _this.m.UnsignedDocuments[idx].SignatureSessionId;
                _this.m.UnsignedDocuments.splice(idx, 1);
                _this.apiClient.deleteManualSignatureDocuments(signatureSessionId.toString());
            };
            _this.handle = function (idx, evt) {
                if (evt) {
                    evt.preventDefault();
                }
                var signatureSessionId = _this.m.SignedDocuments[idx].SignatureSessionId;
                _this.m.SignedDocuments.splice(idx, 1);
                _this.apiClient.handleManualSignatureDocuments(signatureSessionId.toString());
            };
            _this.init = function () {
                _this.m = null;
                _this.resetAttachedFile();
                if (!_this.initialData) {
                    return;
                }
                _this.apiClient.getManualSignatureDocuments(false).then(function (ManualSignatureResponse) {
                    _this.apiClient.getManualSignatureDocuments(true).then(function (ManualSignatureResponseSignedDocuments) {
                        _this.m = {
                            CivicNr: null,
                            Comment: null,
                            UnsignedDocuments: ManualSignatureResponse,
                            SignedDocuments: ManualSignatureResponseSignedDocuments,
                            HeaderData: {
                                backTarget: null,
                                backContext: null,
                                host: _this.initialData
                            }
                        };
                    });
                });
            };
            return _this;
        }
        ManualDocumentsToSignController.prototype.componentName = function () {
            return 'manualDocumentsToSign';
        };
        ManualDocumentsToSignController.prototype.civicRegNrPlaceHolderText = function () {
            if (ntechClientCountry == 'SE') {
                return 'YYYYMMDD-XXXX';
            }
            else if (ntechClientCountry == 'FI') {
                return 'DDMMYY-XXXX';
            }
            else {
                return '';
            }
        };
        ManualDocumentsToSignController.prototype.isValidCivicRegNr = function (value) {
            if (ntech.forms.isNullOrWhitespace(value))
                return true;
            if (ntechClientCountry == 'SE') {
                return ntech.se.isValidCivicNr(value);
            }
            else if (ntechClientCountry == 'FI') {
                return ntech.fi.isValidCivicNr(value);
            }
            else {
                //So they can at least get the data in
                return true;
            }
        };
        ManualDocumentsToSignController.prototype.setupFiles = function () {
            var _this = this;
            if (!this.fileUpload) {
                this.fileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('file'), document.getElementById('documentform'), this.$scope, this.$q);
                this.fileUpload.addFileAttachedListener(function (filenames) {
                    var filename = filenames[0];
                    if (!_this.endsWith(filename, '.pdf')) {
                        _this.fileUpload.reset();
                        toastr.warning('Input file must be an pdf file');
                        return;
                    }
                    if (filenames.length == 0) {
                        _this.attachedFileName = null;
                    }
                    else if (filenames.length == 1) {
                        _this.attachedFileName = filenames[0];
                        _this.fileUpload.loadSingleAttachedFileAsDataUrl().then(function (result) {
                            _this.attachedFileDataUrl = result.dataUrl;
                        });
                    }
                    else {
                        _this.attachedFileName = 'Error - multiple files selected!';
                    }
                });
            }
            else {
                this.resetAttachedFile();
            }
        };
        ManualDocumentsToSignController.prototype.selectFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.setupFiles();
            this.fileUpload.showFilePicker();
        };
        ManualDocumentsToSignController.prototype.resetAttachedFile = function () {
            this.attachedFileName = null;
            if (this.fileUpload) {
                this.fileUpload.reset();
            }
        };
        ManualDocumentsToSignController.prototype.resetValidation = function (f) {
            f.$setPristine();
            f.$setUntouched();
        };
        ManualDocumentsToSignController.prototype.onChanges = function () {
            this.init();
        };
        ManualDocumentsToSignController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return ManualDocumentsToSignController;
    }(NTechComponents.NTechComponentControllerBase));
    ManualDocumentsToSignComponentNs.ManualDocumentsToSignController = ManualDocumentsToSignController;
    var ManualDocumentsToSignComponent = /** @class */ (function () {
        function ManualDocumentsToSignComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ManualDocumentsToSignController;
            this.template = "<page-header initial-data=\"$ctrl.m.HeaderData\" title-text=\"'Manual signatures'\"></page-header>\n        <div class=\"row\">\n        <div class=\"col-sm-8 col-sm-offset-2\">\n            <div class=\"editblock\">\n                <div class=\"form-horizontal\">\n                    <div class=\"form-group\">\n                        <form novalidate name=\"civicRegNrForm\">\n                            <ntech-input t=\"'custom'\" required=\"true\" label=\"'Civic nr'\" model=\"$ctrl.m.CivicNr\" label-classes=\"'col-xs-6'\" input-classes=\"'col-xs-4'\" placeholder-text=\"$ctrl.civicRegNrPlaceHolderText()\" custom-is-valid=\"$ctrl.isValidCivicRegNr\"></ntech-input>\n                        </form>\n                    </div>\n                    <div class=\"form-group\">\n                        <label class=\"control-label col-xs-6\">Choose document</label>\n                        <div class=\"pull-left col-xs-6 pt-1\">\n                            <button ng-click=\"$ctrl.selectFileToAttach($event)\" class=\"n-direct-btn n-white-btn\">Attach <span class=\"glyphicon glyphicon-paperclip\"></span></button>\n                            <form novalidate class=\"form-inline\" name=\"documentform\" id=\"documentform\">\n                                <div class=\"pull-right\" style=\"margin-top: -21px;\" ng-show=\"$ctrl.attachedFileName\">\n                                    <span class=\"custom-fileupload-preview\">\n                                        {{$ctrl.attachedFileName}}\n                                        <span ng-show=\"$ctrl.attachedFileName\" ng-click=\"$ctrl.removeDocument($event)\" class=\"glyphicon glyphicon-remove\" style=\"margin-left:10px;\"></span>\n                                    </span>\n                                </div>\n                                <input type=\"file\" id=\"file\" name=\"file\" style=\"display:none\" />\n                                <div class=\"clearfix\"></div>\n                            </form>\n                        </div>\n                    </div>\n                    <div class=\"form-group\">\n                        <form novalidate name=\"textForm\">\n                        <ntech-input t=\"'text'\" inputtype=\"'textarea'\" rows=\"3\" required=\"true\" label=\"'Comment'\" model=\"$ctrl.m.Comment\" label-classes=\"'col-xs-6'\" input-classes=\"'col-xs-4'\"></ntech-input>\n                        </form>\n                    </div>\n                    <div class=\"form-group\">\n                        <label class=\"control-label col-xs-6\"></label>\n                        <div class=\"pull-left col-xs-4 pt-1\">\n                            <button ng-click=\"$ctrl.createLink($event)\" ng-disabled=\"$ctrl.checkValidtoCreateLink()\" class=\"n-direct-btn n-blue-btn\">CREATE LINK</button>\n                        </div>\n                    </div>\n                </div>\n            </div>\n        </div>\n    </div>\n\n    <div class=\"row pt-3 pb-3\">\n        <h2>Waiting for signing</h2>\n        <hr class=\"hr-section\" />\n        <table class=\"table\">\n            <thead>\n                <tr>\n                    <th class=\"col-xs-1\">Date</th>\n                    <th class=\"col-xs-4\">Comment</th>\n                    <th class=\"col-xs-2\">Unsigned document</th>\n                    <th class=\"col-xs-4\">Link</th>\n                    <th class=\"col-xs-1\"></th>\n                </tr>\n            </thead>\n            <tbody>\n                <tr ng-repeat=\"i in $ctrl.m.UnsignedDocuments\">\n                    <td>{{i.CreationDate | date}}</td>\n                    <td>{{i.CommentText}}</td>\n                    <td><a ng-href=\"{{i.UnSignedDocumentArchiveUrl}}\" target=\"_blank\" class=\"n-direct-btn n-purple-btn\">Download <span class=\"glyphicon glyphicon-save\"></span></a></td>\n                    <td><span class=\"copyable\">{{i.SignatureUrl}}</span></td>\n                    <td> <button ng-click=\"$ctrl.cancelRemove($index, $event)\" class=\"n-direct-btn n-white-btn\">Cancel</button> </td>\n                </tr>\n            </tbody>\n        </table>\n    </div>\n\n    <div class=\"row pt-3\">\n        <h2>Signed documents</h2>\n        <hr class=\"hr-section\"/>\n        <table class=\"table\">\n            <thead>\n                <tr>\n                    <th class=\"col-xs-1\">Date</th>\n                    <th class=\"col-xs-4\">Comment</th>\n                    <th class=\"col-xs-2\">Signed document</th>\n                    <th class=\"col-xs-4\">Signed date</th>\n                    <th class=\"col-xs-1\"></th>\n                </tr>\n            </thead>\n            <tbody>\n                <tr ng-repeat=\"i in $ctrl.m.SignedDocuments\">\n                    <td>{{i.CreationDate | date}}</td>\n                    <td>{{i.CommentText}}</td>\n                    <td><a ng-href=\"{{i.SignedDocumentArchiveUrl}}\" target=\"_blank\" class=\"n-direct-btn n-purple-btn\">Download <span class=\"glyphicon glyphicon-save\"></span></a></td>\n                    <td><span class=\"copyable\">{{i.SignedDate| date}}</span></td>\n                    <td> <button ng-click=\"$ctrl.handle($index, $event)\" class=\"n-direct-btn n-green-btn\">Handled</button> </td>\n                </tr>\n            </tbody>\n        </table>\n    </div>";
        }
        return ManualDocumentsToSignComponent;
    }());
    ManualDocumentsToSignComponentNs.ManualDocumentsToSignComponent = ManualDocumentsToSignComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ManualDocumentsToSignComponentNs.Model = Model;
    var UnsignedDocumentModel = /** @class */ (function () {
        function UnsignedDocumentModel() {
        }
        return UnsignedDocumentModel;
    }());
    ManualDocumentsToSignComponentNs.UnsignedDocumentModel = UnsignedDocumentModel;
    var documentsResponse = /** @class */ (function () {
        function documentsResponse() {
        }
        return documentsResponse;
    }());
    ManualDocumentsToSignComponentNs.documentsResponse = documentsResponse;
})(ManualDocumentsToSignComponentNs || (ManualDocumentsToSignComponentNs = {}));
angular.module('ntech.components').component('manualDocumentsToSign', new ManualDocumentsToSignComponentNs.ManualDocumentsToSignComponent());
