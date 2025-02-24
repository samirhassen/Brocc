var NTechComponents;
(function (NTechComponents) {
    var NTechHttpInterceptor = /** @class */ (function () {
        function NTechHttpInterceptor($q, trafficCop) {
            this.$q = $q;
            this.trafficCop = trafficCop;
        }
        NTechHttpInterceptor.prototype.request = function (config) {
            this.trafficCop.startRequest(config.method);
            return (config);
        };
        NTechHttpInterceptor.prototype.requestError = function (rejection) {
            this.trafficCop.startRequest("get");
            return (this.$q.reject(rejection));
        };
        NTechHttpInterceptor.prototype.response = function (response) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (response);
        };
        NTechHttpInterceptor.prototype.responseError = function (response) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (this.$q.reject(response));
        };
        NTechHttpInterceptor.prototype.extractMethod = function (response) {
            try {
                return (response.config.method);
            }
            catch (error) {
                return ("get");
            }
        };
        NTechHttpInterceptor.$inject = ['$q', 'trafficCop'];
        return NTechHttpInterceptor;
    }());
    NTechComponents.NTechHttpInterceptor = NTechHttpInterceptor;
})(NTechComponents || (NTechComponents = {}));
