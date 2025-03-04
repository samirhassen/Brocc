var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', function ($scope, $http, $q, trafficCop) {
        window.scope = $scope; //for console debugging
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        var localItems = angular.copy(initialData);
        if (localItems.useNewUi) {
            $scope.showNewUi = true;
            var customerContactInfoInitialData = {
                backUrl: localItems.backUrl,
                customerId: localItems.customerId
            };
            $scope.customerContactInfoInitialData = customerContactInfoInitialData;
            if (initialData.isTest) {
                var td = new ComponentHostNs.TestFunctionsModel();
                initialData.testFunctions = td;
                $scope.testFunctions = td;
            }
        }
        else {
            $scope.showLegacyUi = true;
            var legacyCustomerCardInitialData = {
                backUrl: localItems.backUrl,
                customerId: localItems.customerId
            };
            $scope.legacyCustomerCardInitialData = legacyCustomerCardInitialData;
        }
    }]);
