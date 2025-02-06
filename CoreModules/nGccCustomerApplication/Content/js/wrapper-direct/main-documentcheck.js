var initDocumentCheck = function ($scope, initialData, $translate, $http, $q, $timeout, initBasedOnState) {
    var dc = {
        isForcedBankAccountDataSharing: initialData.isForcedBankAccountDataSharing
    }

    function getState(token, onOk) {
        return $http.post(initialData.getStateUrl, {
            token: token
        }).then(function (response) {
            onOk(response.data.state)
        }, function (err) {
            toastr.error('Failed to refresh state')
        })
    }

    function setIsLoading(isLoading) {
        dc.isLoading = isLoading
    }

    $scope.dc = dc

    if ($scope.state.ActiveState.IsWaitingForSharedAccountDataCallback && hasAttachedFiles() === false) {
        setIsLoading(true);

        dc.isWaitingForSharedAccountDataCallback = true
        var waitCount = 0
        let wait = () => {
            waitCount++
            if (waitCount > 3) {
                setIsLoading(false)
                dc.isWaitingForSharedAccountDataCallback = false
                dc.gaveUpWaitingForSharedAccountDataCallback = true
            }
            $timeout(() => {
                getState($scope.state.Token, state => {
                    if (!state.ActiveState.IsWaitingForSharedAccountDataCallback) {
                        initBasedOnState(state) //<-- will load the pdfs
                        setIsLoading(false)
                    } else {
                        wait()
                    }
                })
            }, 3000)
        }
        wait()
    }

    if ($scope.state.ActiveState.IsWaitingForSharedAccountDataCallback && dc.isForcedBankAccountDataSharing && hasAttachedFiles()) {
        dc.isWaitingForSharedAccountDataCallback = false;
        dc.gaveUpWaitingForSharedAccountDataCallback = true;
        getState($scope.state.Token, state => {
            if (!state.ActiveState.IsWaitingForSharedAccountDataCallback) {
                initBasedOnState(state) //<-- will load the pdfs
                setIsLoading(false)
            }
        })
    }

    dc.startUploadSession = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        dc.editModel = {
        }
        var appendApplicant = function (applicantNr) {
            var files = []
            if ($scope.state.NrOfApplicants >= applicantNr) {
                var s = $scope.state.ActiveState.DocumentUploadData['Applicant' + applicantNr]
                angular.forEach(s.AttachedFiles, function (f) {
                    files.push({ isLocal: false, id: f.Id, filename: f.FileName })
                })
            }
            dc.editModel['applicant' + applicantNr] = { files: files }
        }
        appendApplicant(1)
        appendApplicant(2)
    }

    var uls = {

    }
    function setupFileHelper(applicantNr) {
        var ul = new NtechAngularFileUpload.FileUploadHelper(document.getElementById('fileupload' + applicantNr),
            document.getElementById('fileuploadform' + applicantNr),
            $scope, $q);
        ul.addFileAttachedListener(function (filenames) {
            if (filenames.length > 1) {
                toastr.error('Multiple files selected. Please do one at a time.')
            } else if (filenames.length == 1) {
                ul.loadSingleAttachedFileAsDataUrl().then(function (result) {
                    dc.editModel['applicant' + applicantNr].files.push({
                        isLocal: true,
                        dataurl: result.dataUrl,
                        filename: result.filename
                    })
                    ul.reset()
                })
            }
        });
        uls['u' + applicantNr] = ul
    }

    setupFileHelper(1)
    setupFileHelper(2)

    var ulByApplicant = function (applicantNr) {
        return uls['u' + applicantNr]
    }

    dc.cancelUploadSession = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        dc.editModel = null
    }

    dc.commitUploadSession = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        var pendingOperations = []
        var queue = []
        for (var i = 0; i < dc.editModel.applicant1.files.length; i++) {
            queue.push({ n: 1, i: dc.editModel.applicant1.files[i] })
        }
        for (var j = 0; j < dc.editModel.applicant2.files.length; j++) {
            queue.push({ n: 2, i: dc.editModel.applicant2.files[j] })
        }
        if (queue.length == 0) {
            return;
        }
        setIsLoading(true);
        for (var k = 0; k < queue.length; k++) {
            var applicantNr = queue[k].n
            var f = queue[k].i
            if (f.isLocal && !f.isPendingRemoval) {
                pendingOperations.push(
                    $http.post(initialData.addAttachedFileUrl, {
                        token: $scope.state.Token,
                        applicantNr: applicantNr,
                        filename: f.filename,
                        dataurl: f.dataurl
                    }).then(function (response) {
                        f.isLocal = false;
                        f.id = response.data.id
                    }, function (err) {
                        toastr.error('Failed to add file: ' + f.filename)
                    }))
            } else if (!f.isLocal && f.isPendingRemoval) {
                pendingOperations.push(
                    $http.post(initialData.removeAttachedFileUrl, {
                        token: $scope.state.Token,
                        fileId: f.id
                    }).then(function (response) {
                        f.isLocal = false;
                        f.id = response.data.id
                    }, function (err) {
                        toastr.error('Failed to remove file: ' + f.id)
                    }))
            }
        }
        Promise.all(pendingOperations).then(_ => {
            dc.editModel = null
            getState($scope.state.Token, state => {
                initBasedOnState(state)
                setIsLoading(false);
            })
        })
    }

    dc.commitDocumentCheckDocuments = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        setIsLoading(true);
        $http.post(initialData.commitDocumentCheckDocumentsUrl, {
            token: $scope.state.Token
        }).then(function (response) {
            initBasedOnState(response.data.state)
            setIsLoading(false);
        }, function (err) {
            toastr.error('Failed to upload documents')
            setIsLoading(false);
        })
    }

    dc.removeAttachedFile = function (f, evt) {
        if (evt) {
            evt.preventDefault()
        }
        f.isPendingRemoval = true
    }
    dc.chooseFile = function (applicantNr, evt) {
        if (evt) {
            evt.preventDefault();
        }

        ulByApplicant(applicantNr).showFilePicker();
    }
    dc.isAllowedToSubmitDocuments = function () {
        if (!($scope.state && $scope.state.ActiveState && $scope.state.ActiveState.DocumentUploadData)) {
            return false
        }
        var isAllowed = hasAttachedFiles();

        return isAllowed;
    }

    function hasAttachedFiles() {
        var hasAttachedFiles = false;

        var d = $scope.state.ActiveState.DocumentUploadData

        var nrOfApplicantsWithDocuments = 0

        if (d.Applicant1 && d.Applicant1.AttachedFiles && d.Applicant1.AttachedFiles.length > 0) {
            nrOfApplicantsWithDocuments = nrOfApplicantsWithDocuments + 1
        }

        if ($scope.state.NrOfApplicants > 1 && d.Applicant2 && d.Applicant2.AttachedFiles && d.Applicant2.AttachedFiles.length > 0) {
            nrOfApplicantsWithDocuments = nrOfApplicantsWithDocuments + 1
        }

        if (nrOfApplicantsWithDocuments >= $scope.state.NrOfApplicants) {
            hasAttachedFiles = true
        }

        return hasAttachedFiles;
    }
}