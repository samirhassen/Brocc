var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', '$timeout', 'savingsAccountCommentsService', function ($scope, $http, $q, $timeout, savingsAccountCommentsService) {
    $scope.backUrl = initialData.backUrl
    window.scope = $scope
    $scope.frozenAccounts = initialData.frozenAccounts

    $scope.showAccount = function (a, evt) {
        if(evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $scope.current = null
        savingsAccountCommentsService.savingsAccountNr = a.savingsAccountNr //Start loading before?
        $http({
            method: 'POST',
            url: initialData.fetchDetailsUrl,
            data: {
                savingsAccountNr: a.savingsAccountNr
            }
        }).then(function successCallback(response) {
            $scope.isLoading = false
            $scope.current = {
                source: a,
                account: response.data.account,
                customer: response.data.customer,
                latestKycScreenResult: response.data.latestKycScreenResult
            }
            $('#accountDialog').modal('show')
        }, function errorCallback(response) {
            $scope.isLoading = false
            toastr.error(response.statusText)
        })
    }

    $scope.hideAccount = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.current = null
        savingsAccountCommentsService.savingsAccountNr = null
        $('#accountDialog').modal('hide')
    }

    $scope.resolveAccountCreationRemarks = function (a, resolutionAction, evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $http({
            method: 'POST',
            url: initialData.resolveAccountCreationRemarksUrl,
            data: {
                savingsAccountNr: a.source.savingsAccountNr,
                resolutionAction: resolutionAction
            }
        }).then(function successCallback(response) {
            $scope.isLoading = false
            a.source.isHidden = true
            $scope.current = null
            $('#accountDialog').modal('hide')
        }, function errorCallback(response) {
            $scope.isLoading = false
            toastr.error(response.statusText)
        })
    }

    $scope.forceKycScreen = function(a, evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $http({
            method: 'POST',
            url: initialData.kycScreenUrl,
            data: {
                customerId: a.source.mainCustomerId,
                force: true
            }
        }).then(function successCallback(response) {
            $scope.isLoading = false
            $scope.current.latestKycScreenResult = response.data.latestKycScreenResult
        }, function errorCallback(response) {
            $scope.isLoading = false
            toastr.error(response.statusText)
        })
    }

    $scope.getRemarkCodeDisplayText = (code) => {
        if (code === 'CustomerCheckpoint') {
            return 'Customer checkpoint';
        } else {
            return code;
        }
    };
}])