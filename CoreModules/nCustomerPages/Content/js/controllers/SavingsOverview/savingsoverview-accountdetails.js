app.controller('accountDetailsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService) {
    mainService.currentMenuItemName = 'accounts'
    mainService.backUrl = '#!/accounts'
    var loadMoreCount = 10
    $scope.details = ctrData.Details
    $scope.transactions = []

    function appendLoadedTransactions(newTransactions, requestedCount) {
        $scope.areMoreTransactions = newTransactions.length > 0 && newTransactions.length >= requestedCount
        angular.forEach(newTransactions, function (tr) {
            $scope.transactions.push(tr)
            $scope.startBeforeTransactionId = tr.Id //Assumed to be ordered
        })
    }
    appendLoadedTransactions(ctrData.Transactions, mainService.transactionsBatchSize)
    
    $scope.loadMoreTransactions = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        
        mainService.loadDataAsync({
            savingsAccountNr: $scope.details.SavingsAccountNr,
            maxTransactionsCount: loadMoreCount,
            startBeforeTransactionId: $scope.startBeforeTransactionId
        }, initialData.apiUrls.accountTransactions).then(function (d) {
            appendLoadedTransactions(d.Transactions, loadMoreCount)
        }, function (e) {
            toastr.error('Failed')
        })
    }

    $scope.toggleTransactionDetails = function (t, evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (t.transactionDetails) {
            t.transactionDetails = null
        } else {
            mainService.isLoading = true
            mainService.loadDataAsync({
                transactionId: t.Id,
            }, initialData.apiUrls.accountTransactionDetails).then(function (d) {                
                t.transactionDetails = d.TransactionDetails
                mainService.isLoading = false
            }, function (e) {
                toastr.error('Failed')
            })
        }
    }

    $scope.showInterestHistory = function (evt) {
        if (evt) {
            evt.preventDefault()
        }

        mainService.loadDataAsync({
            savingsAccountNr: $scope.details.SavingsAccountNr
        }, initialData.apiUrls.accountInterestHistory).then(function (d) {
            var dd = angular.copy(d.InterestRates)
            dd.reverse()
            $scope.interestHistory = {
                interestRates: dd
            }
            $('#accountdetailsInterestHistoryModal').modal('show')
        }, function (e) {
            toastr.error('Failed')
        })        
    }

    $scope.hideInterestHistory = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.interestHistory = null
        $('#accountdetailsInterestHistoryModal').modal('hide')
        $('body').removeClass('modal-open')
        $('.modal-backdrop').remove()
    }

    window.accountDetailsScope = $scope
}])