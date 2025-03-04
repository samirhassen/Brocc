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
var CreditCommentsComponentNs;
(function (CreditCommentsComponentNs) {
    var CreditCommentsController = /** @class */ (function (_super) {
        __extends(CreditCommentsController, _super);
        function CreditCommentsController($http, $q, ntechComponentService, $scope, $sce) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.$sce = $sce;
            _this.isExpanded = false;
            _this.COMMENTS_PAGE_SIZE = 20;
            _this.getCommentsOnCurrentPage = function (evt) {
                if (evt) {
                    evt.preventDefault();
                }
                if (!_this.comments || _this.comments.length === 0) {
                    return [];
                }
                if (!_this.commentsPaging) {
                    return _this.comments;
                }
                else {
                    var currentPageNr = _.findWhere(_this.commentsPaging.pages, { isCurrentPage: true }).pageNr;
                    return _.first(_.rest(_this.comments, _this.COMMENTS_PAGE_SIZE * currentPageNr), _this.COMMENTS_PAGE_SIZE);
                }
            };
            _this.pagingHelper = new NTechTables.PagingHelper($q, $http);
            ntechComponentService.subscribeToNTechEvents(function (evt) {
                if (evt.eventName !== 'reloadComments') {
                    return;
                }
                if (!_this.initialData) {
                    return;
                }
                if (_this.initialData.CreditNr !== evt.eventData) {
                    return;
                }
                _this.onChanges();
            });
            return _this;
        }
        CreditCommentsController.prototype.componentName = function () {
            return 'creditComments';
        };
        CreditCommentsController.prototype.onChanges = function () {
            var _this = this;
            if (!this.commentFileUpload) {
                this.commentFileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('file'), document.getElementById('commentform'), this.$scope, this.$q);
                this.commentFileUpload.addFileAttachedListener(function (filenames) {
                    if (filenames.length == 0) {
                        _this.attachedFileName = null;
                    }
                    else if (filenames.length == 1) {
                        _this.attachedFileName = filenames[0];
                    }
                    else {
                        _this.attachedFileName = 'Error - multiple files selected!';
                    }
                });
            }
            else {
                this.commentFileUpload.reset();
            }
            this.filterMode = null;
            this.comments = [];
            this.commentsPaging = null;
            this.newCommentText = '';
            this.attachedFileName = null;
            if (this.initialData) {
                this.reloadComments();
            }
        };
        CreditCommentsController.prototype.reloadComments = function () {
            var _this = this;
            var excludeTheseEventTypes = null;
            var onlyTheseEventTypes = null;
            if (this.filterMode === 'user') {
                onlyTheseEventTypes = ['UserComment'];
            }
            else if (this.filterMode === 'system') {
                excludeTheseEventTypes = ['UserComment'];
            }
            this.apiClient.loadCreditComments(this.initialData.CreditNr, excludeTheseEventTypes, onlyTheseEventTypes).then(function (comments) {
                _this.comments = comments;
                _this.commentsPaging = _this.createPagingModel(_this.comments, 0);
            });
        };
        CreditCommentsController.prototype.onFilterModeChanged = function () {
            this.reloadComments();
        };
        CreditCommentsController.prototype.onFocusGained = function () {
            this.isExpanded = true;
        };
        CreditCommentsController.prototype.onFocusLost = function () {
        };
        CreditCommentsController.prototype.addComment = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.newCommentText) {
                return;
            }
            var saveComment = function (attachedFileAsDataUrl, attachedFileName) {
                _this.apiClient.createCreditComment(_this.initialData.CreditNr, _this.newCommentText, attachedFileAsDataUrl, attachedFileName).then(function (result) {
                    _this.newCommentText = null;
                    _this.attachedFileName = null;
                    _this.comments.unshift(result.comment);
                    _this.commentsPaging = _this.createPagingModel(_this.comments, 0);
                });
            };
            if (this.commentFileUpload.hasAttachedFiles()) {
                this.commentFileUpload.loadSingleAttachedFileAsDataUrl().then(function (result) {
                    saveComment(result.dataUrl, result.filename);
                }, function (err) {
                    toastr.warning(err);
                });
            }
            else {
                saveComment(null, null);
            }
        };
        CreditCommentsController.prototype.toggleCommentDetails = function (c, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var cc = c;
            if (cc.commentDetails) {
                cc.commentDetails = null;
                return;
            }
            if (c.CustomerSecureMessageId) {
                this.apiClient.getCustomerMessagesTexts([c.CustomerSecureMessageId]).then(function (x) {
                    cc.commentDetails = {
                        ArchiveLinks: null,
                        CommentByName: null,
                        CustomerSecureMessageText: x.MessageTextByMessageId[c.CustomerSecureMessageId],
                        CustomerSecureMessageTextFormat: x.MessageTextFormat[c.CustomerSecureMessageId],
                        CustomerSecureMessageBy: x.IsFromCustomerByMessageId[c.CustomerSecureMessageId] ? 'Customer' : 'System',
                        CustomerSecureMessageArchiveKey: x.AttachedDocumentsByMessageId ? x.AttachedDocumentsByMessageId[c.CustomerSecureMessageId] : null
                    };
                    if (cc.commentDetails.CustomerSecureMessageTextFormat === 'html')
                        cc.commentDetails.CustomerSecureMessageText = _this.$sce.trustAsHtml(x.MessageTextByMessageId[c.CustomerSecureMessageId]);
                });
            }
            else {
                cc.commentDetails = {
                    ArchiveLinks: c.ArchiveLinks,
                    CommentByName: c.DisplayUserName,
                    CustomerSecureMessageText: null,
                    CustomerSecureMessageTextFormat: null,
                    CustomerSecureMessageBy: null,
                    CustomerSecureMessageArchiveKey: null
                };
            }
        };
        CreditCommentsController.prototype.selectCommentFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.commentFileUpload.showFilePicker();
        };
        CreditCommentsController.prototype.createPagingModel = function (comments, currentPageNr) {
            var p = this.pagingHelper.createPagingObjectFromPageResult({
                CurrentPageNr: currentPageNr,
                TotalNrOfPages: Math.ceil(comments.length / this.COMMENTS_PAGE_SIZE)
            });
            return p;
        };
        CreditCommentsController.prototype.gotoCommentsPage = function (pageNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.comments) {
                return;
            }
            this.commentsPaging = this.createPagingModel(this.comments, pageNr);
        };
        CreditCommentsController.$inject = ['$http', '$q', 'ntechComponentService', '$scope', '$sce'];
        return CreditCommentsController;
    }(NTechComponents.NTechComponentControllerBase));
    CreditCommentsComponentNs.CreditCommentsController = CreditCommentsController;
    var CreditCommentsComponent = /** @class */ (function () {
        function CreditCommentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CreditCommentsController;
            this.templateUrl = 'credit-comments.html';
        }
        return CreditCommentsComponent;
    }());
    CreditCommentsComponentNs.CreditCommentsComponent = CreditCommentsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CreditCommentsComponentNs.InitialData = InitialData;
})(CreditCommentsComponentNs || (CreditCommentsComponentNs = {}));
angular.module('ntech.components').component('creditComments', new CreditCommentsComponentNs.CreditCommentsComponent());
