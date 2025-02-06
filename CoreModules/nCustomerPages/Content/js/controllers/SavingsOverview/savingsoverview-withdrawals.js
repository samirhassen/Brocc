app.controller('withdrawalsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'mainService', 'ctrData', function ($scope, $http, $q, $timeout, $route, $routeParams, mainService, ctrData) {
    mainService.currentMenuItemName = 'withdrawals'    
    mainService.backUrl = null
    $scope.d = ctrData
    $scope.w = {}
    $scope.step = { current: 1 }

    window.withdrawalsScope = $scope

    var zeroDelta = 0.000000001
    $scope.isValidWithWithdrawalAmount = function (v) {
        if (ntech.forms.isNullOrWhitespace(v)) {
            return false
        } 
        var a = $scope.selectedSourceAccount()
        if (!a) {
            return false
        }
        var vv = ntech.forms.replaceAll(v.toString().replace(/\s/g, ""), ",", ".")
        var requestedAmount = parseFloat(vv)
        if (!requestedAmount) {
            return false
        }
        if (a.WithdrawableAmount < zeroDelta) {
            return false
        }
        if (requestedAmount > a.WithdrawableAmount + zeroDelta) {
            return false
        } else {
            return true
        }
    }

    $scope.selectedSourceAccount = function () {
        var a
        if ($scope.w && $scope.w.savingsAccountNr) {
            angular.forEach($scope.d.Accounts, function (v) {
                if (v.SavingsAccountNr === $scope.w.savingsAccountNr) {
                    a = v
                }
            })
        }
        return a
    }

    $scope.$watch('w.savingsAccountNr', function (toValue, fromValue) {
        if (toValue != fromValue && toValue) {
            $scope.step.current = 2
        }
    })

    $scope.addToPending = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        var a = $scope.selectedSourceAccount()
        if (!a) {
            return
        }
        $scope.step.current = $scope.step.current  + 1
        $scope.pendingWithdrawal = {
            withdrawalAmount: parseFloat(ntech.forms.replaceAll($scope.w.withdrawalAmount.toString().replace(/\s/g, ""), ",", ".")),
            savingsAccountNr: a.SavingsAccountNr,
            expectedToIban: a.ToIban,
            expectedToIbanFormatted: a.ToIbanFormatted,
            customCustomerMessageText: $scope.w.customCustomerMessageText,
            customTransactionText: $scope.w.customTransactionText,
            uniqueOperationToken: $scope.d.UniqueOperationToken
        }
        $scope.w = null
    }

    $scope.cancel = function (evt) {
        $scope.pendingWithdrawal = null
        $scope.w = {}
        $scope.step.current = 1
    }

    $scope.withdraw = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!$scope.pendingWithdrawal) {
            return
        }
        $scope.step.current = $scope.step.current + 1
        var r = angular.copy($scope.pendingWithdrawal)
        mainService.isLoading = true        
        $http({
            method: 'POST',
            url: initialData.apiUrls.createWithdrawal,
            data: r
        }).then(function successCallback(response) {
            angular.forEach($scope.d.Accounts, function (a) {
                if (a.SavingsAccountNr === r.savingsAccountNr) {
                    a.WithdrawableAmount = response.data.WithdrawableAmountAfter
                }                
            })            
            $scope.d.UniqueOperationToken = response.data.NewUniqueOperationToken
            $scope.pendingWithdrawal = null
            $scope.showEndMessage = true
            $scope.w = null
            mainService.isLoading = false
        }, function errorCallback(response) {
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }

    $scope.restart = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.pendingWithdrawal = null
        $scope.showEndMessage = false
        $scope.w = {}
        $scope.step.current = 1
    }
}])