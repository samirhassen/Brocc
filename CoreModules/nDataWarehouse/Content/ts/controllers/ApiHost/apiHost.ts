var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components'])

ntech.angular.setupTranslation(app)

class ApiHostCtr {
    static $inject = ['$http', '$q', '$filter', '$scope', 'trafficCop']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $filter: ng.IFilterService,
        private $scope: ng.IScope,
        trafficCop: NTechComponents.NTechHttpTrafficCopService
    ) {      
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
        })
    }    
    isLoading: boolean
    initialData: ApiHostNs.IInitialData
    wsDocsInitialData: NTechWsDocsComponentNs.InitialData
}

app.controller('apiHostCtr', ApiHostCtr)

module ApiHostNs {
    export interface IInitialData {
        isTest: boolean
        apiRootPath: string
        methods: NTechWsDocsComponentNs.ServiceMethodDocumentation[]
        whiteListedReturnUrl?: string
        translation: any
        testingToken?: string
    }
}
