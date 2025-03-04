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
var CompanyLoanImportComponentNs;
(function (CompanyLoanImportComponentNs) {
    var CompanyLoanImportController = /** @class */ (function (_super) {
        __extends(CompanyLoanImportController, _super);
        function CompanyLoanImportController($http, $q, ntechComponentService, ntechLocalStorageService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.ntechLocalStorageService = ntechLocalStorageService;
            _this.$scope = $scope;
            _this.endsWith = function (s, search, this_len) {
                //Polyfill: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
                if (this_len === undefined || this_len > s.length) {
                    this_len = s.length;
                }
                return s.substring(this_len - search.length, this_len) === search;
            };
            return _this;
        }
        CompanyLoanImportController.prototype.componentName = function () {
            return 'companyLoanImport';
        };
        CompanyLoanImportController.prototype.init = function () {
            if (this.m && this.m.fileUpload) {
                this.m.fileUpload.reset();
            }
            this.m = {
                visible: true,
                fileUpload: null,
                result: null,
                cachedFile: null
            };
        };
        CompanyLoanImportController.prototype.selectAndLoadPreview = function () {
            var _this = this;
            if (!this.m.fileUpload) {
                this.m.fileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('climportfile'), document.getElementById('climportform'), this.$scope, this.$q);
                this.m.fileUpload.addFileAttachedListener(function (fn) {
                    if (fn.length == 0 || fn.length > 1) {
                        _this.m.fileUpload.reset();
                        toastr.warning('Pick exactly one file');
                        return;
                    }
                    var filename = fn[0];
                    if (!_this.endsWith(filename, '.xlsx')) {
                        _this.m.fileUpload.reset();
                        toastr.warning('Input file must be an xlsx file');
                        return;
                    }
                    _this.m.fileUpload.loadSingleAttachedFileAsDataUrl().then(function (x) {
                        _this.apiClient.importOrPreviewCompanCreditsFromFile({
                            ExcelFileAsDataUrl: x.dataUrl,
                            FileName: x.filename,
                            IncludeRaw: false,
                            IsPreviewMode: true,
                            IsImportMode: false
                        }).then(function (y) {
                            _this.m.result = y;
                            _this.m.cachedFile = {
                                url: x.dataUrl,
                                name: x.filename
                            };
                        });
                    });
                });
            }
            this.m.fileUpload.showFilePicker();
        };
        CompanyLoanImportController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.init();
        };
        CompanyLoanImportController.prototype.isImportAllowed = function () {
            return this.m && this.m.result && this.m.result.Preview && this.m.result.Shared && this.m.result.Shared.Errors.length == 0;
        };
        CompanyLoanImportController.prototype.import = function () {
            var _this = this;
            this.apiClient.importOrPreviewCompanCreditsFromFile({
                ExcelFileAsDataUrl: this.m.cachedFile.url,
                FileName: this.m.cachedFile.name,
                IncludeRaw: false,
                IsImportMode: true,
                IsPreviewMode: false
            }).then(function (x) {
                _this.m.cachedFile = null;
                _this.m.result = x;
            });
        };
        CompanyLoanImportController.prototype.reset = function () {
            this.init();
        };
        CompanyLoanImportController.$inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$scope'];
        return CompanyLoanImportController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanImportComponentNs.CompanyLoanImportController = CompanyLoanImportController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanImportComponentNs.Model = Model;
    var CompanyLoanImportComponent = /** @class */ (function () {
        function CompanyLoanImportComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanImportController;
            this.templateUrl = 'company-loan-import.html';
        }
        return CompanyLoanImportComponent;
    }());
    CompanyLoanImportComponentNs.CompanyLoanImportComponent = CompanyLoanImportComponent;
})(CompanyLoanImportComponentNs || (CompanyLoanImportComponentNs = {}));
angular.module('ntech.components').component('companyLoanImport', new CompanyLoanImportComponentNs.CompanyLoanImportComponent());
