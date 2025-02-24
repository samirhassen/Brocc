var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
var MortgageApplicationValuationHostCtr = /** @class */ (function () {
    function MortgageApplicationValuationHostCtr($http, $q, $timeout, $filter, $scope) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.$filter = $filter;
        this.$scope = $scope;
        window.scope = this; //for console debugging
        this.initialData = initialData;
        this.newInitialData = {
            applicationInfo: this.initialData.applicationInfo,
            backUrl: this.initialData.backUrl,
            callAutomateCustomerOnInit: true,
            autoAcceptSuggestion: this.initialData.autoAcceptSuggestion
        };
    }
    MortgageApplicationValuationHostCtr.$inject = ['$http', '$q', '$timeout', '$filter', '$scope'];
    return MortgageApplicationValuationHostCtr;
}());
app.controller('mortgageApplicationValuationHostCtr', MortgageApplicationValuationHostCtr);
