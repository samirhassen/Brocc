var WithdrawalsController = app.controller('withdrawalsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', 'savingsAccountCommentsService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService, savingsAccountCommentsService) {
    $scope.backUrl = initialData.backUrl
    $scope.$routeParams = $routeParams
    $scope.d = ctrData
    $scope.editModel = {}

    window.withdrawalsScope = $scope

    $scope.calculate = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.confirmModel = {
            amount: parseFloat(ntech.forms.replaceAll($scope.editModel.amount.toString().replace(/\s/g, ""), ",", "."))
        }
        $scope.editModel = null
    }

    $scope.withdraw = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        mainService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.createWithdrawalUrl,
            data: {
                savingsAccountNr: $scope.d.savingsAccountNr,
                uniqueOperationToken: $scope.d.uniqueOperationToken,
                amount: $scope.confirmModel.amount
            }
        }).then(function successCallback(response) {
            $scope.d.uniqueOperationToken = response.data.newUniqueOperationToken
            $scope.confirmModel = null
            savingsAccountCommentsService.forceReload = true
            mainService.isLoading = false
        }, function errorCallback(response) {
            toastr.error(response.statusText)
            mainService.isLoading = false
        })
    }

    $scope.reset = function (evt) {
        $scope.confirmModel = null
        $scope.editModel = {}
    }
}])