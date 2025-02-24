var ImportIncomingMessageFileCtrl = /** @class */ (function () {
    function ImportIncomingMessageFileCtrl($scope, $http, $q, $timeout) {
        var _this = this;
        this.$scope = $scope;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = this;
        this.backUrl = initialData.backUrl;
        $scope.refreshAttachedFile = function () {
            _this.onAttachedFileChanged();
        };
    }
    ImportIncomingMessageFileCtrl.prototype.selectFile = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        $('#file').click();
    };
    ImportIncomingMessageFileCtrl.prototype.onAttachedFileChanged = function () {
        var _this = this;
        if (!document) {
            return;
        }
        var fd = document.getElementById('file');
        if (!fd) {
            return;
        }
        var attachedFiles = fd.files;
        if (!attachedFiles) {
            return;
        }
        this.isLoading = true;
        if (attachedFiles.length == 1) {
            var r = new FileReader();
            var f = attachedFiles[0];
            if (f.size > (10 * 1024 * 1024)) {
                toastr.warning('Attached file is too big!');
                this.isLoading = false;
                return;
            }
            r.onloadend = function (e) {
                _this.candidateFile = {
                    dataUrl: e.target.result,
                    filename: f.name
                };
                document.getElementById('fileform').reset();
                _this.isLoading = false;
                _this.$scope.$apply();
            };
            r.readAsDataURL(f);
        }
        else {
            toastr.warning('Error - multiple files selected!');
        }
    };
    ImportIncomingMessageFileCtrl.prototype.importFile = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.$http({
            method: 'POST',
            url: initialData.importFileUrl,
            data: {
                fileAsDataUrl: this.candidateFile.dataUrl,
                fileName: this.candidateFile.filename,
                processMessages: true
            }
        }).then(function (response) {
            _this.isLoading = false;
            _this.candidateFile = null;
            _this.resultMessage = 'Message count: ' + response.data.messageCount;
            _this.processResult = response.data.processResult;
        }, function (response) {
            _this.isLoading = false;
            _this.resultMessage = 'Error: ' + response.statusText;
        });
    };
    ImportIncomingMessageFileCtrl.$inject = ['$scope', '$http', '$q'];
    return ImportIncomingMessageFileCtrl;
}());
$('#fileform').on('change', '#file', function () {
    var scope = angular.element($("#importfileApp")).scope();
    scope.$apply(function () {
        scope.refreshAttachedFile();
    });
});
var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('importIncomingMessageFileCtrl', ImportIncomingMessageFileCtrl);
var ImportIncomingMessageFileNs;
(function (ImportIncomingMessageFileNs) {
    var CurrentFile = /** @class */ (function () {
        function CurrentFile() {
        }
        return CurrentFile;
    }());
    ImportIncomingMessageFileNs.CurrentFile = CurrentFile;
})(ImportIncomingMessageFileNs || (ImportIncomingMessageFileNs = {}));
