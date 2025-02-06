var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);

ntech.angular.setupTranslation(app);

app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', (
    $scope: AddCustomerNs.IScope,
    $http: ng.IHttpService,
    $q: ng.IQService,
    trafficCop: NTechComponents.NTechHttpTrafficCopService) => {
    window.scope = $scope; //for console debugging

    $scope.isLoading = trafficCop.pending.all > 0;
    trafficCop.addStateChangeListener(() => {
        $scope.isLoading = trafficCop.pending.all > 0
    })
    $scope.addCustomerInitialData = {
        backUrl: initialData.backUrl,
        urlToHere: initialData.urlToHere
    }
}])

namespace AddCustomerNs {
    export interface IScope extends ng.IScope {
        addCustomerInitialData: AddCustomerComponentNs.InitialData
        isLoading: boolean
    }
}