var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app);

class ArchivedUnsecuredLoanApplicationCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout', '$translate', 'ntechComponentService', 'trafficCop', 'ntechLog']
    constructor(
        $scope: ArchivedUnsecuredLoanApplicationNs.ILocalScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        private $translate: any,
        private ntechComponentService: NTechComponents.NTechComponentService,
        trafficCop: NTechComponents.NTechHttpTrafficCopService,
        ntechLog: NTechComponents.NTechLoggingService
    ) {
        let serverData: ArchivedUnsecuredLoanApplicationNs.IInitialData = initialData;

        if (serverData && serverData.IsTest) {
            ntechLog.isDebugMode = true;
        }
        window.scope = $scope; //for console debugging

        $scope.applicationInitialData = {
            applicationNr: serverData.ApplicationNr,
            backTarget: serverData.BackTarget,
            urlToHereFromOtherModule: serverData.UrlToHereFromOtherModule
        }

        $scope.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(() => {
            $scope.isLoading = trafficCop.pending.all > 0
        })

        let apiClient = new NTechPreCreditApi.ApiClient(errMsg => {
            toastr.error(errMsg);
        }, $http, $q);
        apiClient.loggingContext = 'global::archived-unsecured-loan-application';
        ntechComponentService.subscribeToReloadRequired(c => {
        });
    }
}

app.controller('ctr', ArchivedUnsecuredLoanApplicationCtr)

module ArchivedUnsecuredLoanApplicationNs {
    export interface ILocalScope extends ng.IScope {
        isLoading: boolean,
        applicationInitialData: ArchivedUnsecuredLoanApplicationComponentNs.InitialData
    }

    export interface IInitialData {
        ApplicationNr: string
        IsTest: boolean,
        BackTarget: string
        UrlToHereFromOtherModule: string
    }
}