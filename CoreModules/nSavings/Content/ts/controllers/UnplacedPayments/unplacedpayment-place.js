app
.controller('place', ['$scope', 'sharedService', '$filter', '$http', function ($scope, sharedService, $filter, $http) {
    /*
                        requestedPlaceAmount = placeAmount,
                    suggestedPlaceAmount = suggestedPlaceAmount,
                    customerBalance = a.CustomerBalance,
                    maxAllowedSavingsCustomerBalance = maxAllowedSavingsCustomerBalance
    */
    $scope.$watch(
        function () { return sharedService.uiMode },
        function () {
            $scope.uiMode = sharedService.uiMode
        }
    )
    $scope.$watch(
        function () { return sharedService.placeData },
        function () {
            $scope.d = sharedService.placeData
        }
    )
    $scope.leaveUnplacedAmount = function () {
        if (!$scope.d) {
            return null
        }
        return $scope.d.unplacedAmount - $scope.d.placeAmount
    }
    $scope.place = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        sharedService.isLoading = true
        $http({
            method: 'POST',
            url: initialData.placePaymentUrl,
            data: {
                paymentId: $scope.d.paymentId,
                savingsAccountNr: $scope.d.savingsAccountNr,
                placeAmount: $scope.d.placeAmount,
                leaveUnplacedAmount: $scope.d.unplacedAmountAfter
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

    window.placeScope = $scope
}])