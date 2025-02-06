app.controller('withdrawalaccountsCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'mainService', 'ctrData', function ($scope, $http, $q, $timeout, $route, $routeParams, mainService, ctrData) {
    mainService.currentMenuItemName = 'withdrawalaccounts'    
    mainService.backUrl = null
    $scope.d = { savingsAccounts: ctrData.SavingsAccounts, withdrawalAccountChangeDocumentUrl: initialData.apiUrls.withdrawalAccountChangeDocumentUrl }
    window.withdrawalaccountsScope = $scope  
}])