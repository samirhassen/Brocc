(function (angular: ng.IAngularStatic) {
    'use strict';
    var m = angular.module('ntech.components', []);

    m.service('ntechComponentService', NTechComponents.NTechComponentService);
    m.service('ntechLog', NTechComponents.NTechLoggingService);
    m.factory('ntechInterceptHttp', ['$q', 'trafficCop', function interceptHttp($q, trafficCop) {
        //So ideally this whole thing would just be m.service('ntechInterceptHttp', NTechComponents.NTechHttpInterceptor) but that and all kinds of other variations just randomly dont work while this does
        var c = new NTechComponents.NTechHttpInterceptor($q, trafficCop);
        return ({
            request: request,
            requestError: requestError,
            response: response,
            responseError: responseError
        });
        function request(config) {
            return c.request(config);
        }
        function requestError(rejection) {
            return c.requestError(rejection);
        }
        function response(response) {
            return c.response(response);
        }
        function responseError(response) {
            return c.responseError(response);
        }
    }]);
    m.config(['$httpProvider',
        function setupConfig($httpProvider: ng.IHttpProvider) {
            $httpProvider.interceptors.push('ntechInterceptHttp');            
        }
    ]);
    m.service("trafficCop", NTechComponents.NTechHttpTrafficCopService);
})(window.angular);