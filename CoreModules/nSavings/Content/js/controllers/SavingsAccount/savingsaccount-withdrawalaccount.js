var WithdrawalsController = app.controller('withdrawalaccountCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', 'savingsAccountCommentsService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService, savingsAccountCommentsService) {
        $scope.backUrl = initialData.backUrl;
        $scope.d = {
            Status: ctrData.Status,
            SavingsAccountNr: ctrData.SavingsAccountNr
        };
        function update(accounts) {
            $scope.d.Current = accounts.length == 0 ? null : accounts[0];
            $scope.d.HistoricalWithdrawalAccounts = accounts;
        }
        update(ctrData.HistoricalWithdrawalAccounts);
        window.withdrawalaccountScope = $scope;
    }]);
