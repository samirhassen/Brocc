var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
var ApiHostCtr = /** @class */ (function () {
    function ApiHostCtr($http, $q, $filter, $scope, trafficCop) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$filter = $filter;
        this.$scope = $scope;
        this.initialData = initialData;
        this.wsDocsInitialData = {
            methods: this.initialData.methods,
            isTest: this.initialData.isTest,
            apiRootPath: this.initialData.apiRootPath,
            testingToken: this.initialData.testingToken,
            whiteListedReturnUrl: this.initialData.whiteListedReturnUrl
        };
        this.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            _this.isLoading = trafficCop.pending.all > 0;
        });
    }
    ApiHostCtr.$inject = ['$http', '$q', '$filter', '$scope', 'trafficCop'];
    return ApiHostCtr;
}());
app.controller('apiHostCtr', ApiHostCtr);
