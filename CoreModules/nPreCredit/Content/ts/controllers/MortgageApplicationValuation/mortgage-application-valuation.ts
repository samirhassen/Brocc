var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app)

class MortgageApplicationValuationHostCtr {
    static $inject = ['$http', '$q', '$timeout', '$filter', '$scope']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        private $filter: ng.IFilterService,
        private $scope: ng.IScope
    ) {
        window.scope = this; //for console debugging

        this.initialData = initialData;

        this.newInitialData = {
            applicationInfo: this.initialData.applicationInfo,
            backUrl: this.initialData.backUrl,
            callAutomateCustomerOnInit: true,
            autoAcceptSuggestion: this.initialData.autoAcceptSuggestion
        }
    }
    initialData: MortgageApplicationValuationHostNs.IInitialData
    newInitialData: MortgageApplicationObjectValuationNewComponentNs.InitialData
}

app.controller('mortgageApplicationValuationHostCtr', MortgageApplicationValuationHostCtr)

module MortgageApplicationValuationHostNs {
    export interface IInitialData {
        applicationNr: string,
        backUrl: string,
        autoAcceptSuggestion: boolean
        translation: any
        applicationInfo: NTechPreCreditApi.ApplicationInfoModel
    }
}
