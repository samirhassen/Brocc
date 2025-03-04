var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components', 'ngRoute']);
ntech.angular.setupTranslation(app);
function createDataResolverStatic(create) {
    return ['$q', '$route', function ($q, $route) {
            var p = $q.defer();
            p.resolve(create($route));
            return p.promise;
        }];
}
function createDataResolver(load) {
    return ['$http', '$q', function ($http, $q) {
            var apiClient = new NTechDwApi.ApiClient(function (errorMessage) {
                toastr.error(errorMessage);
            }, $http, $q);
            apiClient.loggingContext = 'spa-host-router';
            return load(apiClient);
        }];
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
app.config(['$routeProvider', '$locationProvider', '$provide', function ($routeProvider, $locationProvider, $provide) {
        $locationProvider.html5Mode({
            enabled: true,
            requireBase: false
        });
        $routeProvider
            .when('/ui/s/vintage-reports', {
            template: '<vintage-reports initial-data="$resolve.initialData"></vintage-reports>',
            resolve: {
                initialData: createDataResolverStatic(function ($route) {
                    return {
                        backofficeUrl: initialData.backofficeUrl,
                        treatNotificationsAsClosedMaxBalance: initialData.treatNotificationsAsClosedMaxBalance
                    };
                })
            }
        })
            .when('/ui/s/vintage-report', {
            template: '<vintage-report initial-data="$resolve.initialData"></vintage-report>',
            resolve: {
                initialData: createDataResolverStatic(function ($route) {
                    return {
                        params: $route.current.params
                    };
                })
            }
        })
            .when('/ui/s/notfound', {
            template: '<not-found></not-found>'
        })
            .otherwise({ redirectTo: '/ui/s/notfound' });
    }]);
var SpaHostCtr = /** @class */ (function () {
    function SpaHostCtr($http, $q, $filter, $scope, trafficCop, ntechComponentService) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$filter = $filter;
        this.$scope = $scope;
        this.ntechComponentService = ntechComponentService;
        this.title = 'loading...';
        this.initialData = initialData;
        this.isLoading = trafficCop.pending.all > 0;
        trafficCop.addStateChangeListener(function () {
            _this.isLoading = trafficCop.pending.all > 0;
        });
        ntechComponentService.subscribeToNTechEvents(function (evt) {
            if (evt && evt.eventName === 'changePageTitle') {
                _this.title = evt.eventData;
            }
        });
    }
    SpaHostCtr.$inject = ['$http', '$q', '$filter', '$scope', 'trafficCop', 'ntechComponentService'];
    return SpaHostCtr;
}());
app.controller('spaHostCtr', SpaHostCtr);
var SpaHostNs;
(function (SpaHostNs) {
    var SpaHostService = /** @class */ (function () {
        function SpaHostService(ntechComponentService) {
            this.ntechComponentService = ntechComponentService;
        }
        SpaHostService.prototype.setTitle = function (title) {
            this.ntechComponentService.emitNTechEvent('changePageTitle', title);
        };
        SpaHostService.$inject = ['ntechComponentService'];
        return SpaHostService;
    }());
    SpaHostNs.SpaHostService = SpaHostService;
})(SpaHostNs || (SpaHostNs = {}));
app.service('spaHostService', SpaHostNs.SpaHostService);
