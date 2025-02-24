var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', function ($scope, $http, $q, trafficCop) {
        window.scope = $scope; //for console debugging
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        $scope.addCustomerInitialData = {
            backUrl: initialData.backUrl,
            urlToHere: initialData.urlToHere
        };
    }]);
