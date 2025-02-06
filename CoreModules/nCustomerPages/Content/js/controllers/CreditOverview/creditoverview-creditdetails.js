app.controller('creditDetailsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService) {
    mainService.currentMenuItemName = 'credits'
    mainService.backUrl = initialData.productsOverviewUrl
    $scope.backUrl = mainService.backUrl
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
            creditNr: $scope.details.CreditNr,
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
            }, initialData.apiUrls.creditTransactionDetails).then(function (d) {                
                t.transactionDetails = d.TransactionDetails
                mainService.isLoading = false
            }, function (e) {
                toastr.error('Failed')
            })
        }
    }
    $scope.getAmortizationPlanPdf = function (creditNr) {
        return initialData.apiUrls.getAmortizationPlanPdfUrl + "?creditNr=" + creditNr;
    };
    window.creditDetailsScope = $scope
}])