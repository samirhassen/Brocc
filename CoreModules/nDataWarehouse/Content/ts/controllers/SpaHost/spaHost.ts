var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components', 'ngRoute'])

ntech.angular.setupTranslation(app)

declare type ApiLoader<TData> = (apiClient: NTechDwApi.ApiClient) => ng.IPromise<TData>;
declare type DataResolver<TData> = ($http: any, $q: any) => ng.IPromise<TData>;

function createDataResolverStatic<TData>(create: ($route: any) => TData): (string | DataResolver<TData>)[] {
    return ['$q', '$route', ($q: ng.IQService, $route: any) => {
        let p = $q.defer<TData>()
        p.resolve(create($route))
        return p.promise
    }]
}

function createDataResolver<TData>(load: ApiLoader<TData>): (string | DataResolver<TData>)[] {
    return ['$http', '$q', ($http, $q) => {
        let apiClient = new NTechDwApi.ApiClient(errorMessage => {
            toastr.error(errorMessage);
        }, $http, $q);
        apiClient.loggingContext = 'spa-host-router'
        return load(apiClient)
    }]
/*
 example use:

        .when('/vintage-report', {
            template: '<vintage-reports initial-data="$resolve.initialData"></vintage-reports>',
            resolve: {
                initialData: createDataResolver(apiClient =>
                    apiClient.map(apiClient.fetchVintageReportData({}), s => {
                        return { d: s } as (VintageReportComponentNs.InitialData)
                    })
                )
            }
        })
 */
}

app.config(['$routeProvider', '$locationProvider', '$provide', ($routeProvider, $locationProvider, $provide) => {
    $locationProvider.html5Mode({
        enabled: true,
        requireBase: false
    });

    $routeProvider
        .when('/ui/s/vintage-reports', {
            template: '<vintage-reports initial-data="$resolve.initialData"></vintage-reports>',
            resolve: {
                initialData: createDataResolverStatic($route => {
                    return {
                        backofficeUrl: (initialData as SpaHostNs.IInitialData).backofficeUrl,
                        treatNotificationsAsClosedMaxBalance: (initialData as SpaHostNs.IInitialData).treatNotificationsAsClosedMaxBalance
                    } as VintageReportsComponentNs.InitialData
                })
            }
        })
        .when('/ui/s/vintage-report', {
            template: '<vintage-report initial-data="$resolve.initialData"></vintage-report>',
            resolve: {
                initialData: createDataResolverStatic($route => {                    
                    return {
                        params: $route.current.params
                    } as VintageReportComponentNs.InitialData
                })
            }
        })
        .when('/ui/s/notfound', {
            template: '<not-found></not-found>'
        })
        .otherwise({ redirectTo: '/ui/s/notfound' })
}])

class SpaHostCtr {
    static $inject = ['$http', '$q', '$filter', '$scope', 'trafficCop', 'ntechComponentService']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $filter: ng.IFilterService,
        private $scope: ng.IScope,
        trafficCop: NTechComponents.NTechHttpTrafficCopService,
        private ntechComponentService: NTechComponents.NTechComponentService
    ) {      
        this.initialData = initialData;

        this.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(() => {
            this.isLoading = trafficCop.pending.all > 0;
        })
        ntechComponentService.subscribeToNTechEvents(evt => {
            if (evt && evt.eventName === 'changePageTitle') {
                this.title = evt.eventData
            }
        })
    }
    isLoading: boolean
    public title: string = 'loading...'
    initialData: SpaHostNs.IInitialData
}

app.controller('spaHostCtr', SpaHostCtr)

module SpaHostNs {
    export interface IInitialData {
        isTest: boolean
        spaHostUrlPrefix: string
        translation: any
        treatNotificationsAsClosedMaxBalance: number
        backofficeUrl: string
    }

    export class SpaHostService {
        static $inject = ['ntechComponentService']
        constructor(private ntechComponentService: NTechComponents.NTechComponentService) {
            
        }

        public setTitle(title: string) {
            this.ntechComponentService.emitNTechEvent('changePageTitle', title)
        }
    }
}

app.service('spaHostService', SpaHostNs.SpaHostService)