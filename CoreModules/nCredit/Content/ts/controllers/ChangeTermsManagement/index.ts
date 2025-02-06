class ChangeTermsManagementCtrl {
    static $inject = ['$scope', '$http', '$q']
    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = this
        this.today = initialData.today
        this.creditsWithPendingTermChanges = initialData.creditsWithPendingTermChanges
    }

    onBack(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q)
    }

    isLoading: boolean
    today: any
    creditsWithPendingTermChanges: Array<ChangeTermsManagementNs.IPendingChange>
}

var app = angular.module('app', ['ntech.forms', 'ntech.components']);
app.controller('changeTermsManagementCtrl', ChangeTermsManagementCtrl)

module ChangeTermsManagementNs {
    export interface IPendingChange {
        creditNr: string,
        link: string
    }
}