var SavingsAccountDetailsCtr = /** @class */ (function () {
    function SavingsAccountDetailsCtr($scope, $http, $q, $timeout, $route, $routeParams, savingsAccountDetailsData, mainService) {
        $scope.backUrl = initialData.backUrl;
        $scope.$routeParams = $routeParams;
        $scope.details = savingsAccountDetailsData.details;
        $scope.capitalTransactions = savingsAccountDetailsData.capitalTransactions;
        $scope.accumulatedInterest = savingsAccountDetailsData.accumulatedInterest;
        window.detailsScope = $scope;
        $scope.toggleTransactionDetails = function (t, evt) {
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
                }).then(function (response) {
                    $scope.isLoading = false;
                    t.transactionDetails = response.data;
                }, function (response) {
                    $scope.isLoading = false;
                    toastr.error(response.statusText);
                });
            }
        };
        $scope.showInterestHistory = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.interestHistoryUrl).then(function (d) {
                var dd = angular.copy(d.interestRates);
                dd.reverse();
                $scope.interestHistory = {
                    interestRates: dd
                };
                $('#accountdetailsInterestHistoryModal').modal('show');
            });
        };
        $scope.hideInterestHistory = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.interestHistory = null;
            $('#accountdetailsInterestHistoryModal').modal('hide');
            $('body').removeClass('modal-open');
            $('.modal-backdrop').remove();
        };
    }
    SavingsAccountDetailsCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'savingsAccountDetailsData', 'mainService'];
    return SavingsAccountDetailsCtr;
}());
app.controller('detailsCtr', SavingsAccountDetailsCtr);
