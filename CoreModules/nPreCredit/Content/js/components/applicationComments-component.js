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
var ApplicationCommentsComponentNs;
(function (ApplicationCommentsComponentNs) {
    var ApplicationCommentsController = /** @class */ (function (_super) {
        __extends(ApplicationCommentsController, _super);
        function ApplicationCommentsController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.isExpanded = false;
            ntechComponentService.subscribeToNTechEvents(function (evt) {
                if (evt.eventName !== 'reloadComments') {
                    return;
                }
                if (!_this.initialData) {
                    return;
                }
                if (_this.initialData.applicationInfo.ApplicationNr !== evt.eventData) {
                    return;
                }
                _this.onChanges();
            });
            return _this;
        }
        ApplicationCommentsController.prototype.componentName = function () {
            return 'applicationComments';
        };
        ApplicationCommentsController.prototype.onFocusGained = function () {
            this.isExpanded = true;
        };
        ApplicationCommentsController.prototype.onFocusLost = function () {
        };
        ApplicationCommentsController.prototype.onChanges = function () {
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
            this.comments = [];
            this.filterMode = '';
            this.newCommentText = '';
            this.attachedFileName = null;
            this.affiliateReportingInitialData = null;
            if (this.initialData) {
                this.reloadComments();
            }
        };
        ApplicationCommentsController.prototype.reloadComments = function () {
            var _this = this;
            var hideTheseEventTypes = this.initialData.hideTheseEventTypes ? angular.copy(this.initialData.hideTheseEventTypes) : [];
            var showOnlyTheseEventTypes = this.initialData.showOnlyTheseEventTypes ? angular.copy(this.initialData.showOnlyTheseEventTypes) : [];
            if (this.filterMode === 'user') {
                showOnlyTheseEventTypes.push('UserComment');
            }
            else if (this.filterMode === 'system') {
                hideTheseEventTypes.push('UserComment');
            }
            this.apiClient.fetchApplicationComments(this.initialData.applicationInfo.ApplicationNr, {
                hideTheseEventTypes: hideTheseEventTypes,
                showOnlyTheseEventTypes: showOnlyTheseEventTypes
            }).then(function (comments) {
                _this.comments = comments;
                _this.affiliateReportingInitialData = {
                    applicationNr: _this.initialData.applicationInfo.ApplicationNr
                };
            });
        };
        ApplicationCommentsController.prototype.onFilterModeChanged = function () {
            if (!this.initialData) {
                return;
            }
            this.reloadComments();
        };
        ApplicationCommentsController.prototype.addComment = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.newCommentText) {
                return;
            }
            var saveComment = function (attachedFileAsDataUrl, attachedFileName) {
                _this.apiClient.addApplicationComment(_this.initialData.applicationInfo.ApplicationNr, _this.newCommentText, {
                    attachedFileAsDataUrl: attachedFileAsDataUrl,
                    attachedFileName: attachedFileName,
                    eventType: _this.initialData.newCommentEventType
                }).then(function (result) {
                    _this.newCommentText = null;
                    _this.attachedFileName = null;
                    _this.comments.unshift(result);
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
        ApplicationCommentsController.prototype.toggleCommentDetails = function (c, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var cc = c;
            if (cc.commentDetails) {
                cc.commentDetails = null;
                return;
            }
            cc.commentDetails = {
                AttachmentFilename: c.AttachmentFilename,
                AttachmentUrl: c.AttachmentUrl,
                CommentByName: c.CommentByName,
                DirectUrl: c.DirectUrl,
                DirectUrlShortName: c.DirectUrlShortName,
                RequestIpAddress: c.RequestIpAddress,
                BankAccountPdfSummaryArchiveKey: c.BankAccountPdfSummaryArchiveKey,
                BankAccountRawJsonDataArchiveKey: c.BankAccountRawJsonDataArchiveKey
            };
        };
        ApplicationCommentsController.prototype.selectCommentFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.commentFileUpload.showFilePicker();
        };
        ApplicationCommentsController.prototype.toggleWaitingForInformation = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.initialData.applicationInfo.IsActive) {
                return;
            }
            this.apiClient.setApplicationWaitingForAdditionalInformation(this.initialData.applicationInfo.ApplicationNr, !this.initialData.applicationInfo.IsWaitingForAdditionalInformation).then(function (result) {
                if (_this.initialData.reloadPageOnWaitingForAdditionalInformation) {
                    document.location.reload(); //Temporary measure while refactoring
                }
                else {
                    _this.signalReloadRequired();
                }
            });
        };
        ApplicationCommentsController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return ApplicationCommentsController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationCommentsComponentNs.ApplicationCommentsController = ApplicationCommentsController;
    var ApplicationCommentsComponent = /** @class */ (function () {
        function ApplicationCommentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCommentsController;
            this.templateUrl = 'application-comments.html';
        }
        return ApplicationCommentsComponent;
    }());
    ApplicationCommentsComponentNs.ApplicationCommentsComponent = ApplicationCommentsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationCommentsComponentNs.InitialData = InitialData;
})(ApplicationCommentsComponentNs || (ApplicationCommentsComponentNs = {}));
angular.module('ntech.components').component('applicationComments', new ApplicationCommentsComponentNs.ApplicationCommentsComponent());
