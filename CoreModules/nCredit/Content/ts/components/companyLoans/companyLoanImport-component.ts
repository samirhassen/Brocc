namespace CompanyLoanImportComponentNs {
    export class CompanyLoanImportController extends NTechComponents.NTechComponentControllerBase {
        initialData: ComponentHostNs.ComponentHostInitialData
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$scope']
        constructor($http: ng.IHttpService,
            private $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private ntechLocalStorageService: NTechComponents.NTechLocalStorageService,
            private $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'companyLoanImport'
        }

        init() {
            if (this.m && this.m.fileUpload) {
                this.m.fileUpload.reset()
            }            
            
            this.m = {
                visible: true,
                fileUpload: null,
                result: null,
                cachedFile: null
            }
        }

        endsWith = (s: string, search: string, this_len?: number) => {
            //Polyfill: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/endsWith
            if (this_len === undefined || this_len > s.length) {
                this_len = s.length;
            }
            return s.substring(this_len - search.length, this_len) === search;
        };

        selectAndLoadPreview() {
            if (!this.m.fileUpload) {
                this.m.fileUpload = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('climportfile') as HTMLInputElement, document.getElementById('climportform') as HTMLFormElement, this.$scope, this.$q)
                this.m.fileUpload.addFileAttachedListener(fn => {
                    if (fn.length == 0 || fn.length > 1) {
                        this.m.fileUpload.reset()
                        toastr.warning('Pick exactly one file')
                        return
                    }
                    let filename = fn[0]
                    if (!this.endsWith(filename, '.xlsx')) {
                        this.m.fileUpload.reset()
                        toastr.warning('Input file must be an xlsx file')
                        return
                    }

                    this.m.fileUpload.loadSingleAttachedFileAsDataUrl().then(x => {
                        this.apiClient.importOrPreviewCompanCreditsFromFile({
                            ExcelFileAsDataUrl: x.dataUrl,
                            FileName: x.filename,
                            IncludeRaw: false,
                            IsPreviewMode: true,
                            IsImportMode: false
                        }).then(y => {
                            this.m.result = y
                            this.m.cachedFile = {
                                url: x.dataUrl,
                                name: x.filename
                            }
                        })        
                    })
                })
            }
            this.m.fileUpload.showFilePicker()
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.init()
        }

        isImportAllowed() {
            return this.m && this.m.result && this.m.result.Preview && this.m.result.Shared && this.m.result.Shared.Errors.length == 0
        }

        import() {
            this.apiClient.importOrPreviewCompanCreditsFromFile({
                ExcelFileAsDataUrl: this.m.cachedFile.url,
                FileName: this.m.cachedFile.name,
                IncludeRaw: false,
                IsImportMode: true,
                IsPreviewMode: false
            }).then(x => {
                this.m.cachedFile = null
                this.m.result = x
            })
        }

        reset() {
            this.init()
        }
    }

    export class Model {
        visible: boolean
        fileUpload: NtechAngularFileUpload.FileUploadHelper
        cachedFile: {
            url: string,
            name: string
        }
        result: NTechCreditApi.ImportOrPreviewCompanCreditsFromFileResponse
    }

    export class CompanyLoanImportComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanImportController;
            this.templateUrl = 'company-loan-import.html'
        }
    }
   
}

angular.module('ntech.components').component('companyLoanImport', new CompanyLoanImportComponentNs.CompanyLoanImportComponent())