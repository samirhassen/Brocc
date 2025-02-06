var app = angular.module('app', []);
app
    .controller('ctr', ['$scope', '$http', '$timeout', ($scope: MainPaymentPlanCalculationNs.Scope, $http: ng.IHttpService, $timeout: ng.ITimeoutService) => {
        $scope.backUrl = initialData.backUrl

        $scope.tabName = 'general'
        $scope.getUrl = () => {
            var u = $scope.tabName === 'general' ? initialData.urlCreditAnnuityCalculation : initialData.urlCreditPaymentPlanCalculation
            var symbol = '?'
            angular.forEach($scope.m, (v, k) => {
                if (v !== '') {
                    u = u + symbol + encodeURIComponent(k) + '=' + encodeURIComponent(v)
                    symbol = '&'
                }
            })
            return u
        }
        $scope.setTab = (tabName: string, evt?: Event) => {
            if (evt) {
                evt.preventDefault()
            }
            $scope.tabName = tabName
            if (tabName === 'general') {
                $scope.m = {
                }
            } else {
                $scope.m = {
                    loanCreationDate: moment(initialData.currentTime).format('YYYY-MM-DD')
                }
            }
        }
        $scope.setTab('general', null)
        window.scope = $scope
    }])

module MainPaymentPlanCalculationNs {
    export interface Scope extends ng.IScope {
        backUrl: string
        m: any
        tabName: string
        getUrl: () => string
        setTab(tabName: string, evt?: Event): void
    }
}