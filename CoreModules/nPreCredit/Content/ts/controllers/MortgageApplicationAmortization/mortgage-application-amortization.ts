var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app)

class MortgageApplicationAmortizationHostCtr {
    static $inject = ['$http', '$q', '$filter', '$scope']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $filter: ng.IFilterService,
        private $scope: ng.IScope,
    ) {
        window.scope = this; //for console debugging

        this.initialData = initialData;

        var client = new NTechPreCreditApi.ApiClient(x => toastr.error(x), $http, $q)

        let afterSet: (d: NTechPreCreditApi.MortgageLoanAmortizationBasisModel) => void = null
        if (this.initialData.backUrl != null) {
            afterSet = x => document.location.href = this.initialData.backUrl
        }

        client.fetchApplicationInfo(this.initialData.applicationNr).then(x => {
            this.amortizationInitialData = {
                mode: 'new',
                afterSet: afterSet,
                applicationInfo: x
            }
        })
    }
    amortizationInitialData: MortgageLoanAmortizationComponentNs.InitialData
    initialData: MortgageApplicationAmortizationHostNs.IInitialData
}

app.controller('mortgageApplicationAmortizationHostCtr', MortgageApplicationAmortizationHostCtr)

module MortgageApplicationAmortizationHostNs {
    export interface IInitialData {
        applicationNr: string,
        backUrl: string,
        translation: any
    }
}
