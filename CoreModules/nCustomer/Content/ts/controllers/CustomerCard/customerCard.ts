var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);

ntech.angular.setupTranslation(app);

app.controller('ctr', ['$scope', '$http', '$q', 'trafficCop', ($scope: CustomerCardNs.ICustomerCardScope,
    $http: ng.IHttpService,
    $q: ng.IQService,
    trafficCop: NTechComponents.NTechHttpTrafficCopService) => {
    window.scope = $scope; //for console debugging

    $scope.isLoading = trafficCop.pending.all > 0;
    trafficCop.addStateChangeListener(() => {
        $scope.isLoading = trafficCop.pending.all > 0
    })

    let localItems = angular.copy(initialData) as CustomerCardNs.IAppModel
    if (localItems.useNewUi) {
        $scope.showNewUi = true
        let customerContactInfoInitialData: CustomerContactInfoComponentNs.InitialData = {
            backUrl: localItems.backUrl,
            customerId: localItems.customerId
        }
        $scope.customerContactInfoInitialData = customerContactInfoInitialData

        if (initialData.isTest) {
            let td = new ComponentHostNs.TestFunctionsModel()
            initialData.testFunctions = td
            $scope.testFunctions = td
        }
    } else {
        $scope.showLegacyUi = true
        let legacyCustomerCardInitialData: LegacyCustomerCardComponentNs.InitialData = {
            backUrl: localItems.backUrl,
            customerId: localItems.customerId
        }
        $scope.legacyCustomerCardInitialData = legacyCustomerCardInitialData
    }
}])

namespace CustomerCardNs {
    export interface ICustomerCardScope extends ng.IScope {
        legacyCustomerCardInitialData: LegacyCustomerCardComponentNs.InitialData
        customerContactInfoInitialData: CustomerContactInfoComponentNs.InitialData
        isLoading: boolean
        showLegacyUi: boolean
        showNewUi: boolean
        testFunctions?: ComponentHostNs.TestFunctionsModel
    }

    export interface ICustomerPropertyEditModel {
        Name: string
        Group: string
        CustomerId: number
        Value: any
        IsSensitive: boolean
        IsReadonly: boolean
        Locked: boolean
        UiType?: string
        FriendlyName?: string
        FriendlyValue?: string
    }

    export interface IAppModel {
        backUrl: string
        customerId: number
        useNewUi: boolean
    }
    export interface IAppCustomerCardModel {
        items: ICustomerPropertyEditModel[]
        customerId: number
        newCountry?: string
    }
}