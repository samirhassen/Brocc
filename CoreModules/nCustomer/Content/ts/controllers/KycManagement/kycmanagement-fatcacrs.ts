var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);

ntech.angular.setupTranslation(app);

app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', ($scope: KycManagementManageNs.ILocalScope,
    $http: ng.IHttpService,
    $q: ng.IQService,
    trafficCop: NTechComponents.NTechHttpTrafficCopService) => {
    window.scope = $scope; //for console debugging

    $scope.isLoading = trafficCop.pending.all > 0;
    trafficCop.addStateChangeListener(() => {
        $scope.isLoading = trafficCop.pending.all > 0
    })
    $scope.fatcaCrsInitialData = {
        backUrl: initialData.backUrl,
        customerId: initialData.customerId,
        allCountryCodesAndNames: initialData.allCountryCodesAndNames
    }

    let apiClient = new NTechCustomerApi.ApiClient(x => toastr.error(x), $http, $q)
    $scope.onBack = (evt: Event) => {
        if (evt) {
            evt.preventDefault()
        }

        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q)
    }
}])

namespace KycManagementManageNs {
    export interface ILocalScope extends ng.IScope {
        isLoading: boolean
        fatcaCrsInitialData: KycManagementFatcaCrsComponentNs.InitialData
        onBack: (evt: Event) => void
    }
}