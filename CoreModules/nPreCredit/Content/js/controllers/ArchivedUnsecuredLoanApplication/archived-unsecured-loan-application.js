var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
ntech.angular.setupTranslation(app);
var ArchivedUnsecuredLoanApplicationCtr = /** @class */ (function () {
    function ArchivedUnsecuredLoanApplicationCtr($scope, $http, $q, $timeout, $translate, ntechComponentService, trafficCop, ntechLog) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.$translate = $translate;
        this.ntechComponentService = ntechComponentService;
        var serverData = initialData;
        if (serverData && serverData.IsTest) {
            ntechLog.isDebugMode = true;
        }
        window.scope = $scope; //for console debugging
        $scope.applicationInitialData = {
            applicationNr: serverData.ApplicationNr,
            backTarget: serverData.BackTarget,
            urlToHereFromOtherModule: serverData.UrlToHereFromOtherModule
        };
        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            $scope.isLoading = trafficCop.pending.all > 0;
        });
        var apiClient = new NTechPreCreditApi.ApiClient(function (errMsg) {
            toastr.error(errMsg);
        }, $http, $q);
        apiClient.loggingContext = 'global::archived-unsecured-loan-application';
        ntechComponentService.subscribeToReloadRequired(function (c) {
        });
    }
    ArchivedUnsecuredLoanApplicationCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$translate', 'ntechComponentService', 'trafficCop', 'ntechLog'];
    return ArchivedUnsecuredLoanApplicationCtr;
}());
app.controller('ctr', ArchivedUnsecuredLoanApplicationCtr);
