class SavingsAccountDetailsCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'savingsAccountDetailsData', 'mainService']
    constructor(
        $scope: SavingsAccountDetailsNs.Scope, $http: ng.IHttpService, $q: ng.IQService, $timeout: ng.ITimeoutService,
        $route: any, $routeParams: any, savingsAccountDetailsData: any, mainService: any) {

        $scope.backUrl = initialData.backUrl
        $scope.$routeParams = $routeParams
        $scope.details = savingsAccountDetailsData.details
        $scope.capitalTransactions = savingsAccountDetailsData.capitalTransactions
        $scope.accumulatedInterest = savingsAccountDetailsData.accumulatedInterest
        window.detailsScope = $scope

        $scope.toggleTransactionDetails = (t: any, evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            if (t.transactionDetails) {
                t.transactionDetails = null
            } else {
                $scope.isLoading = true
                $http({
                    method: 'POST',
                    url: initialData.savingsAccountLedgerTransactionDetailsUrl,
                    data: {
                        transactionId: t.id
                    }
                }).then((response) => {
                    $scope.isLoading = false
                    t.transactionDetails = response.data
                }, (response) => {
                    $scope.isLoading = false
                    toastr.error(response.statusText)
                })
            }
        }

        $scope.showInterestHistory = (evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            mainService.loadBySavingsAccountNr($route.current.params.savingsAccountNr, initialData.interestHistoryUrl).then((d) => {
                var dd = angular.copy(d.interestRates)
                dd.reverse()
                $scope.interestHistory = {
                    interestRates: dd
                };
                
                ($('#accountdetailsInterestHistoryModal') as any).modal('show');
            })
        }

        $scope.hideInterestHistory = (evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            $scope.interestHistory = null;
            ($('#accountdetailsInterestHistoryModal') as any).modal('hide');
            $('body').removeClass('modal-open')
            $('.modal-backdrop').remove()
        }
    }
}

app.controller('detailsCtr', SavingsAccountDetailsCtr)

module SavingsAccountDetailsNs {
    export interface Scope extends ng.IScope {
        backUrl: string
        $routeParams: any
        details: any
        capitalTransactions: any
        accumulatedInterest: any
        isLoading: boolean
        toggleTransactionDetails: (t: any, evt?: Event) => void
        showInterestHistory: (evt?: Event) => void
        interestHistory: any
        hideInterestHistory: (evt?: Event) => void
    }
}