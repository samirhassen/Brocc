var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
var MortgageApplicationAmortizationHostCtr = /** @class */ (function () {
    function MortgageApplicationAmortizationHostCtr($http, $q, $filter, $scope) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$filter = $filter;
        this.$scope = $scope;
        window.scope = this; //for console debugging
        this.initialData = initialData;
        var client = new NTechPreCreditApi.ApiClient(function (x) { return toastr.error(x); }, $http, $q);
        var afterSet = null;
        if (this.initialData.backUrl != null) {
            afterSet = function (x) { return document.location.href = _this.initialData.backUrl; };
        }
        client.fetchApplicationInfo(this.initialData.applicationNr).then(function (x) {
            _this.amortizationInitialData = {
                mode: 'new',
                afterSet: afterSet,
                applicationInfo: x
            };
        });
    }
    MortgageApplicationAmortizationHostCtr.$inject = ['$http', '$q', '$filter', '$scope'];
    return MortgageApplicationAmortizationHostCtr;
}());
app.controller('mortgageApplicationAmortizationHostCtr', MortgageApplicationAmortizationHostCtr);
