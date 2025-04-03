if (typeof angular !== 'undefined' && angular.module('app')) {
    try {
        var a = angular.module('app')
        a.factory('handleJsonPostAccessDeniedInterceptor', ['$q', '$injector', function ($q, $injector) {
            return {
                response: function (response) {
                    if (typeof response.data === 'string' && response.config && response.config.method === 'POST') {
                        //The guid here is included in the text in the access denied page since we seem to be able to change that but not the response code
                        if (response.data.indexOf('a0f9a6b5-3101-4bce-a0ca-39b9ddf01d09') != -1) {
                            location.reload()
                        }
                    }
                    return response
                }
            }
        }])
        a.config(['$httpProvider', function ($httpProvider) {
            $httpProvider.interceptors.push('handleJsonPostAccessDeniedInterceptor');
        }])
    } catch (err) { /* failed to require */ }
}