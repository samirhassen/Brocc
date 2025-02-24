var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', function ($scope, $http, $q, trafficCop) {
        window.scope = $scope; //for console debugging
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        $scope.localDecisionInitialData = { customerId: initialData.customerId };
        $scope.customerAnswersInitialData = { customerId: initialData.customerId };
        $scope.customerTrapetsDataInitialData = { customerId: initialData.customerId };
        $scope.customerInfoInitialData = { customerId: initialData.customerId, backUrl: initialData.urlToHere };
        $scope.customerCommentsInitialData = { customerId: initialData.customerId };
        $scope.localDecisionEditModeChanged = function (mode) {
            $scope.localDecisionEditMode = mode;
        };
        var apiClient = new NTechCustomerApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q);
        };
    }]);
