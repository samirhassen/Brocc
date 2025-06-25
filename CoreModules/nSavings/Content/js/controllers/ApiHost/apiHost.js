var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
class ApiHostCtr {
    constructor($http, $q, $filter, $scope, trafficCop) {
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
        trafficCop.addStateChangeListener(() => {
            this.isLoading = trafficCop.pending.all > 0;
        });
    }
}
ApiHostCtr.$inject = ['$http', '$q', '$filter', '$scope', 'trafficCop'];
app.controller('apiHostCtr', ApiHostCtr);
