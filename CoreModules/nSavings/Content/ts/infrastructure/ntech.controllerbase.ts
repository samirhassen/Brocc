namespace NTechComponents {
    export abstract class NTechComponentControllerBase extends NTechComponentControllerBaseTemplate {
        constructor(ntechComponentService: NTechComponents.NTechComponentService, $http: ng.IHttpService, $q: ng.IQService) {
            super(ntechComponentService)
            this.apiClient = new NTechSavingsApi.ApiClient(errorMessage => {
                toastr.error(errorMessage);
            }, $http, $q);
            this.apiClient.loggingContext = `component ${this.componentName()}`;
        }

        public apiClient: NTechSavingsApi.ApiClient

        public isLoading(): boolean {
            return this.apiClient.isLoading();
        }
    }
}