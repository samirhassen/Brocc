app.controller('closuresCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService) {
    mainService.currentMenuItemName = 'closures'
    mainService.backUrl = null
    window.closuresScope = $scope

    var activeAccounts = []
    angular.forEach(ctrData.Accounts, function (a) {
        if (a.Status === 'Active') {
            activeAccounts.push(a)
        }
    })

    $scope.activeAccounts = activeAccounts
    $scope.mode = 'select'
    $scope.areWithdrawalsSuspended = ctrData.AreWithdrawalsSuspended

    $scope.beginCloseAccount = function (savingsAccountNr, evt) {
        if (evt) {
            evt.preventDefault()
        }

        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.apiUrls.closeaccountpreviewdata,
            data: { savingsAccountNr: savingsAccountNr }
        }).then(function successCallback(response) {
            $scope.mode = 'preview'
            $scope.preview = response.data
            mainService.isLoading = false
        }, function errorCallback(response) {
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }

    $scope.cancelCloseAccount = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.mode = 'select'
        $scope.preview = null
    }

    $scope.commitCloseAccount = function (evt) {
        if (evt) {
            evt.preventDefault()
        }

        mainService.isLoading = true
            /*
              Also allowed:
                customCustomerMessageText
                customTransactionText
             */
        $http({
            method: 'POST',
            url: initialData.apiUrls.closeaccount,
            data: {
                savingsAccountNr: $scope.preview.SavingsAccountNr,
                expectedToIban: $scope.preview.WithdrawalIbanRaw,
                uniqueOperationToken: $scope.preview.UniqueOperationToken
            }
        }).then(function successCallback(response) {
            $scope.mode = 'done'
            $scope.preview = null
            mainService.isLoading = false
        }, function errorCallback(response) {
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }
}])