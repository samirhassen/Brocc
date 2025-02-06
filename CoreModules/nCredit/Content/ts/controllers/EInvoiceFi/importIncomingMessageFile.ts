class ImportIncomingMessageFileCtrl {
    static $inject = ['$scope', '$http', '$q']
    constructor(
        private $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = this;
        this.backUrl = initialData.backUrl;

        (<any>$scope).refreshAttachedFile = () => {
            this.onAttachedFileChanged()
        }
    }

    isLoading: boolean
    backUrl: string
    candidateFile: ImportIncomingMessageFileNs.CurrentFile
    resultMessage: string
    processResult: ImportIncomingMessageFileNs.IProcessResult

    selectFile(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        $('#file').click()
    }

    onAttachedFileChanged() {
        if (!document) {
            return
        }

        var fd = (<HTMLInputElement>document.getElementById('file'));
        if (!fd) {
            return
        }

        var attachedFiles = fd.files
        if (!attachedFiles) {
            return
        }

        this.isLoading = true
        if (attachedFiles.length == 1) {
            var r = new FileReader();
            var f = attachedFiles[0]
            if (f.size > (10 * 1024 * 1024)) {
                toastr.warning('Attached file is too big!')
                this.isLoading = false
                return
            }
            r.onloadend = (e) => {
                this.candidateFile = {
                    dataUrl: (<any>e.target).result,
                    filename: f.name
                };
                (<any>document.getElementById('fileform')).reset();
                this.isLoading = false;
                this.$scope.$apply();
            }
            r.readAsDataURL(f)
        } else {
            toastr.warning('Error - multiple files selected!')
        }
    }

    importFile (evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.importFileUrl,
            data: {
                fileAsDataUrl: this.candidateFile.dataUrl,
                fileName: this.candidateFile.filename,
                processMessages: true
            }
        }).then((response: ng.IHttpResponse<ImportIncomingMessageFileNs.IImportResult>) => {
            this.isLoading = false
            this.candidateFile = null;
            this.resultMessage = 'Message count: ' + response.data.messageCount;
            this.processResult = response.data.processResult
        }, (response) => {
            this.isLoading = false
            this.resultMessage = 'Error: ' + response.statusText
        })
    }
}

$('#fileform').on('change', '#file', function () {
    var scope = angular.element($("#importfileApp")).scope();
    scope.$apply(function () {
        (<any>scope).refreshAttachedFile()
    })
})

var app = angular.module('app', ['ntech.forms', 'ntech.components'])
app.controller('importIncomingMessageFileCtrl', ImportIncomingMessageFileCtrl)

module ImportIncomingMessageFileNs {
    export class CurrentFile {
        dataUrl: string
        filename: string
    }
    export interface IImportResult {
        messageCount: number,
        processResult: IProcessResult
    }
    export interface IProcessResult {
        processedCountTotal: number
        processedCountByCode: Array<IProcessResultItem>
    }
    export interface IProcessResultItem {
        code: string,
        count: number
    }
}