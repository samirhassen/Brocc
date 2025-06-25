class SavingsAccountDetailsCtr {
    constructor($scope, $http, $q, $timeout, $route, $routeParams, savingsAccountDetailsData, mainService) {
        $scope.backUrl = initialData.backUrl;
        $scope.$routeParams = $routeParams;
        $scope.details = savingsAccountDetailsData.details;
        $scope.capitalTransactions = savingsAccountDetailsData.capitalTransactions;
        $scope.accumulatedInterest = savingsAccountDetailsData.accumulatedInterest;
        window.detailsScope = $scope;
        $scope.toggleTransactionDetails = (t, evt) => {
            if (evt) {
                evt.preventDefault();
            }
            if (t.transactionDetails) {
                t.transactionDetails = null;
            }
            else {
                $scope.isLoading = true;
                $http({
                    method: 'POST',
                    url: initialData.savingsAccountLedgerTransactionDetailsUrl,
                    data: {
                        transactionId: t.id
                    }
                }).then((response) => {
                    $scope.isLoading = false;
                    t.transactionDetails = response.data;
                }, (response) => {
                    $scope.isLoading = false;
                    toastr.error(response.statusText);
                });
            }
        };
        $scope.showInterestHistory = (evt) => {
            if (evt) {
                evt.preventDefault();
            }
            mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.interestHistoryUrl).then((d) => {
                var dd = angular.copy(d.interestRates);
                dd.reverse();
                $scope.interestHistory = {
                    interestRates: dd
                };
                $('#accountdetailsInterestHistoryModal').modal('show');
            });
        };
        $scope.hideInterestHistory = (evt) => {
            if (evt) {
                evt.preventDefault();
            }
            $scope.interestHistory = null;
            $('#accountdetailsInterestHistoryModal').modal('hide');
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();
        };
    }
}
SavingsAccountDetailsCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'savingsAccountDetailsData', 'mainService'];
app.controller('detailsCtr', SavingsAccountDetailsCtr);
