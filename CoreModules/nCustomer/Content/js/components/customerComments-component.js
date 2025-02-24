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
var CustomerCommentsComponentNs;
(function (CustomerCommentsComponentNs) {
    var CustomerCommentsController = /** @class */ (function (_super) {
        __extends(CustomerCommentsController, _super);
        function CustomerCommentsController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.isExpanded = false;
            return _this;
        }
        CustomerCommentsController.prototype.componentName = function () {
            return 'customerComments';
        };
        CustomerCommentsController.prototype.onFocusGained = function () {
            this.isExpanded = true;
        };
        CustomerCommentsController.prototype.onFocusLost = function () {
        };
        CustomerCommentsController.prototype.onChanges = function () {
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
            this.newCommentText = '';
            this.attachedFileName = null;
            if (this.initialData) {
                this.apiClient.fetchCustomerComments(this.initialData.customerId).then(function (comments) {
                    _this.comments = comments;
                });
            }
        };
        CustomerCommentsController.prototype.addComment = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.newCommentText) {
                return;
            }
            var saveComment = function (attachedFileAsDataUrl, attachedFileName) {
                _this.apiClient.addCustomerComment(_this.initialData.customerId, _this.newCommentText, attachedFileAsDataUrl, attachedFileName).then(function (result) {
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
        CustomerCommentsController.prototype.selectCommentFileToAttach = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.commentFileUpload.showFilePicker();
        };
        CustomerCommentsController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return CustomerCommentsController;
    }(NTechComponents.NTechComponentControllerBase));
    CustomerCommentsComponentNs.CustomerCommentsController = CustomerCommentsController;
    var CustomerCommentsComponent = /** @class */ (function () {
        function CustomerCommentsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CustomerCommentsController;
            this.templateUrl = 'customer-comments.html';
        }
        return CustomerCommentsComponent;
    }());
    CustomerCommentsComponentNs.CustomerCommentsComponent = CustomerCommentsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    CustomerCommentsComponentNs.InitialData = InitialData;
})(CustomerCommentsComponentNs || (CustomerCommentsComponentNs = {}));
angular.module('ntech.components').component('customerComments', new CustomerCommentsComponentNs.CustomerCommentsComponent());
