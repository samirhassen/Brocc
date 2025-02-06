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

    $scope.localDecisionInitialData = { customerId: initialData.customerId }
    $scope.customerAnswersInitialData = { customerId: initialData.customerId }
    $scope.customerTrapetsDataInitialData = { customerId: initialData.customerId }
    $scope.customerInfoInitialData = { customerId: initialData.customerId, backUrl: initialData.urlToHere }
    $scope.customerCommentsInitialData = { customerId: initialData.customerId }
    $scope.localDecisionEditModeChanged = (mode) => {
        $scope.localDecisionEditMode = mode
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
        localDecisionInitialData: KycManagementLocalDecisionComponentNs.InitialData
        customerAnswersInitialData: KycManagementCustomerAnswersComponentNs.InitialData
        customerTrapetsDataInitialData: KycManagementCustomerTrapetsDataComponentNs.InitialData
        customerInfoInitialData: CustomerInfoComponentNs.InitialData
        customerCommentsInitialData: CustomerCommentsComponentNs.InitialData
        localDecisionEditModeChanged: (mode: KycManagementLocalDecisionComponentNs.IEditMode) => void
        localDecisionEditMode: KycManagementLocalDecisionComponentNs.IEditMode
        onBack: (evt: Event) => void
    }
}