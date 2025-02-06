app.controller('repay', ['$scope', 'sharedService', '$http', function ($scope, sharedService, $http) {

    $scope.$watch(
        function () { return sharedService.uiMode },
        function () {
            $scope.uiMode = sharedService.uiMode
        }
    )
    $scope.$watch(
        function () { return sharedService.repayData },
        function () {
            $scope.d = sharedService.repayData
        }
    )
    $scope.leaveUnplacedAmount = function () {
        if (!$scope.d) {
            return null
        }
        return $scope.d.unplacedAmount - $scope.d.repaymentAmount
    }
    $scope.repay = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        sharedService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.repayPaymentUrl,
            data: {
                paymentId: $scope.d.paymentId,
                customerName : $scope.d.customerName,
                repaymentAmount: $scope.d.repaymentAmount,
                leaveUnplacedAmount: $scope.leaveUnplacedAmount(),
                iban : $scope.d.iban
            }
        }).then(function successCallback(response) {
            sharedService.isLoading = false
            document.location = initialData.afterPlacementUrl
        }, function errorCallback(response) {
            sharedService.isLoading = false
            toastr.error('Failed: ' + response.statusText)
        })
    }

    if (ntechClientCountry === 'FI') {
        $scope.accountNrFieldLabel = 'IBAN'
    } else {
        $scope.accountNrFieldLabel = 'Bank account nr'
    }

    window.repayScope = $scope
}])