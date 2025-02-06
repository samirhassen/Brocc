var WithdrawalsController = app.controller('withdrawalaccountchangeCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', 'savingsAccountCommentsService', '$location', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService, savingsAccountCommentsService, $location) {
    function initUsingHistory(historicalWithdrawalAccounts) {
        $scope.d.HistoricalWithdrawalAccounts = historicalWithdrawalAccounts
        if (historicalWithdrawalAccounts && historicalWithdrawalAccounts.length > 0 && historicalWithdrawalAccounts[0].IsPending) {
            $scope.pendingModel = angular.copy(historicalWithdrawalAccounts[0])
            $scope.calculateModel = null
            $scope.previewModel = null
        } else {
            $scope.calculateModel = {}
            $scope.pendingModel = null
            $scope.previewModel = null
        }
    }

    $scope.backUrl = initialData.backUrl
    window.withdrawalaccountchangeScope = $scope
    $scope.d = {
        Status: ctrData.Status,
        SavingsAccountNr: ctrData.SavingsAccountNr,
        CurrentWithdrawalAccount: ctrData.CurrentWithdrawalAccount
    }

    initUsingHistory(ctrData.HistoricalWithdrawalAccounts)
    
    function isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }
    
    $scope.isValidIBAN = function (v) {
        if (isNullOrWhitespace(v)) {
            return true
        }
        return ntech.fi.isValidIBAN(v)
    }

    $scope.cancelPreview = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.previewModel = null
    }

    $scope.calculate = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        if ($scope.calculateform.$invalid) {
            toastr.warning('Form invalid!')
            return
        }
        if ($scope.calculateModel.newWithdrawalAccount == $scope.d.CurrentWithdrawalAccount.Raw) {
            toastr.warning('The new account is the same as the current one!')
            return
        }

        $scope.doneModel = null

        var calcInternal = function (attachedFileAsDataUrl, attachedFileName) {
            mainService.isLoading = true
            $http({
                method: 'POST',
                url: initialData.previewWithdrawalAccountChangeUrl,
                data: {
                    savingsAccountNr: $scope.d.SavingsAccountNr,
                    newWithdrawalIban: $scope.calculateModel.newWithdrawalAccount,
                    testUserId : initialData.testUserId
                }
            }).then(function successCallback(response) {
                $scope.previewModel = {
                    SavingsAccountNr: response.data.SavingsAccountNr,
                    WithdrawalAccount: response.data.WithdrawalAccount,
                    AttachedFile: {
                        FileName: attachedFileName,
                        FileAsDataUrl: attachedFileAsDataUrl
                    },
                    InitiatedDate: response.data.InitiatedDate
                }
                mainService.isLoading = false
            }, function errorCallback(response) {
                toastr.error(response.statusText)
                mainService.isLoading = false
            })
        }

        var attachedFiles = document.getElementById('attachedFile').files
        if (attachedFiles.length == 1) {
            var r = new FileReader();
            var f = attachedFiles[0]
            if (f.size > (10 * 1024 * 1024)) {
                toastr.warning('Attached file is too big!')
                return
            }
            r.onloadend = function (e) {
                var dataUrl = e.target.result
                var filename = f.name

                //Reset the file input
                //document.getElementById('id of calculateform').reset()

                //Save the document
                calcInternal(dataUrl, filename)
            }
            r.readAsDataURL(f)
        } else if (attachedFiles.length == 0) {
            toastr.warning('No letter of attorney attached!')
        } else {
            toastr.warning('Multiple files have been attached. Please reload the page and only attach a single file.')
        }
    }

    $scope.initiateChange = function (evt) {
        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.initiateChangeWithdrawalAccountUrl,
            data: {
                savingsAccountNr: $scope.previewModel.SavingsAccountNr,
                newWithdrawalIban: $scope.previewModel.WithdrawalAccount.Raw,
                letterOfAttorneyFileName: $scope.previewModel.AttachedFile.FileName,
                letterOfAttorneyFileAsDataUrl: $scope.previewModel.AttachedFile.FileAsDataUrl,
                testUserId: initialData.testUserId
            }
        }).then(function successCallback(response) {
            initUsingHistory(response.data.HistoricalWithdrawalAccounts)
            savingsAccountCommentsService.forceReload = true
            mainService.isLoading = false
        }, function errorCallback(response) {
            $scope.doneModel = { isOk: false }
            $scope.pendingModel = null
            $scope.calculateModel = null
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }

    $scope.commitChange = function (evt) {
        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.commitChangeWithdrawalAccountUrl,
            data: {
                pendingChangeId : $scope.pendingModel.PendingChangeId,
                testUserId: initialData.testUserId
            }
        }).then(function successCallback(response) {
            savingsAccountCommentsService.forceReload = true
            $location.path('/WithdrawalAccount/' + ctrData.SavingsAccountNr)
        }, function errorCallback(response) {
            $scope.doneModel = { isOk: false }
            $scope.pendingModel = null
            $scope.calculateModel = null
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }

    $scope.cancelChange = function (evt) {
        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.cancelChangeWithdrawalAccountUrl,
            data: {
                pendingChangeId: $scope.pendingModel.PendingChangeId,
                testUserId: initialData.testUserId
            }
        }).then(function successCallback(response) {
            savingsAccountCommentsService.forceReload = true
            $location.path('/WithdrawalAccount/' + ctrData.SavingsAccountNr)
        }, function errorCallback(response) {
            $scope.doneModel = { isOk: false }
            $scope.pendingModel = null
            $scope.calculateModel = null
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }
        
    $scope.selectFileToAttach = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $('#attachedFile').click()
    }

    $scope.refreshAttachedFile = function () {
        if (!document) {
            $scope.calculateModel.attachedFileName = null
            return
        }

        var f = document.getElementById('attachedFile')
        if (!f) {
            $scope.calculateModel.attachedFileName = null
            return
        }

        var attachedFiles = f.files
        if (!attachedFiles) {
            $scope.calculateModel.attachedFileName= null
            return
        }

        if (!attachedFiles || attachedFiles.length == 0) {
            $scope.calculateModel.attachedFileName = null
        } else if (attachedFiles.length == 1) {
            $scope.calculateModel.attachedFileName = attachedFiles[0].name
        } else {
            $scope.calculateModel.attachedFileName = 'Error - multiple files selected!'
        }
    }

    $('#attachedFileContainer').on('change', '#attachedFile', function () {
        $scope.$apply(function () {
            $scope.refreshAttachedFile()
        })
    })
}])