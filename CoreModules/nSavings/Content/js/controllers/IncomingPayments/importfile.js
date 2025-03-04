var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        $scope.backUrl = initialData.backUrl;
        window.scope = $scope;
        $scope.isLoading = false;
        $scope.fileFormatName = 'camt.054.001.02';
        $scope.selectFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $('#file').click();
        };
        $scope.loadFileInfo = function (fileFormatName, dataUrl, filename) {
            $http({
                method: 'POST',
                url: initialData.getFileDataUrl,
                data: {
                    fileAsDataUrl: dataUrl,
                    fileName: filename,
                    fileFormatName: fileFormatName
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.candidateFile = {
                    fileFormatName: fileFormatName,
                    dataUrl: dataUrl,
                    filename: filename,
                    data: response.data
                };
                $scope.successMessage = null;
            }, function errorCallback(response) {
                $scope.isLoading = false;
                $scope.successMessage = null;
                toastr.error(response.statusText);
            });
        };
        $scope.importFile = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.importFileUrl,
                data: {
                    fileAsDataUrl: $scope.candidateFile.dataUrl,
                    fileName: $scope.candidateFile.filename,
                    fileFormatName: $scope.fileFormatName,
                    autoPlace: true,
                    overrideDuplicateCheck: $scope.forceImport,
                    overrideIbanCheck: $scope.forceImportIban,
                    skipOutgoingPayments: $scope.skipOutgoingPayments
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.candidateFile = null;
                $scope.successMessage = response.data.message;
            }, function errorCallback(response) {
                $scope.isLoading = false;
                $scope.successMessage = null;
                toastr.error(response.statusText);
            });
        };
        $scope.refreshAttachedFile = function () {
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
            $scope.isLoading = true;
            var fileFormatName = $scope.fileFormatName;
            if (attachedFiles.length == 1) {
                var r = new FileReader();
                var f = attachedFiles[0];
                if (f.size > (10 * 1024 * 1024)) {
                    toastr.warning('Attached file is too big!');
                    $scope.isLoading = false;
                    return;
                }
                r.onloadend = function (e) {
                    var dataUrl = e.target.result;
                    var filename = f.name;
                    //Reset the file input
                    document.getElementById('fileform').reset();
                    $scope.loadFileInfo(fileFormatName, dataUrl, filename);
                };
                r.readAsDataURL(f);
            }
            else {
                toastr.warning('Error - multiple files selected!');
            }
        };
        window.scope = $scope;
    }]);
$('#fileform').on('change', '#file', function () {
    var scope = angular.element($("#importfileApp")).scope();
    scope.$apply(function () {
        scope.refreshAttachedFile();
    });
});
