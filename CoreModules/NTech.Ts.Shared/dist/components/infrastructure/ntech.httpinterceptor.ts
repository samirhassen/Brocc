namespace NTechComponents {
    export class NTechHttpInterceptor {

        static $inject = ['$q', 'trafficCop']
        constructor(private $q: ng.IQService, private trafficCop: NTechComponents.NTechHttpTrafficCopService) {

        }

        request(config: any) {
            this.trafficCop.startRequest(config.method);
            return (config);
        }

        requestError(rejection: any) {
            this.trafficCop.startRequest("get");
            return (this.$q.reject(rejection));
        }

        response(response: any) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (response);
        }

        responseError(response: any) {
            this.trafficCop.endRequest(this.extractMethod(response));
            return (this.$q.reject(response));
        }

        extractMethod(response: any) {
            try {
                return (response.config.method);
            } catch (error) {
                return ("get");
            }
        }
    }
}