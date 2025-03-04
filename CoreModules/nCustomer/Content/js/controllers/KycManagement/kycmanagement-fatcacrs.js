var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', function ($scope, $http, $q, trafficCop) {
        window.scope = $scope; //for console debugging
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        $scope.fatcaCrsInitialData = {
            backUrl: initialData.backUrl,
            customerId: initialData.customerId,
            allCountryCodesAndNames: initialData.allCountryCodesAndNames
        };
        var apiClient = new NTechCustomerApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q);
        };
    }]);
