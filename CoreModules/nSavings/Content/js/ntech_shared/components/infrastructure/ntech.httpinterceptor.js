var NTechComponents;
(function (NTechComponents) {
    class NTechHttpInterceptor {
        constructor($q, trafficCop) {
            this.$q = $q;
            this.trafficCop = trafficCop;
        }
        request(config) {
            this.trafficCop.startRequest(config.method);
            return (config);
        }
        requestError(rejection) {
            this.trafficCop.startRequest("get");
            return (this.$q.reject(rejection));
        }
        response(response) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (response);
        }
        responseError(response) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (this.$q.reject(response));
        }
        extractMethod(response) {
            try {
                return (response.config.method);
            }
            catch (error) {
                return ("get");
            }
        }
    }
    NTechHttpInterceptor.$inject = ['$q', 'trafficCop'];
    NTechComponents.NTechHttpInterceptor = NTechHttpInterceptor;
})(NTechComponents || (NTechComponents = {}));
