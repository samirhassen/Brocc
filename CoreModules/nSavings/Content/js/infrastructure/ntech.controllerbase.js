var NTechComponents;
(function (NTechComponents) {
    class NTechComponentControllerBase extends NTechComponents.NTechComponentControllerBaseTemplate {
        constructor(ntechComponentService, $http, $q) {
            super(ntechComponentService);
            this.apiClient = new NTechSavingsApi.ApiClient(errorMessage => {
                toastr.error(errorMessage);
            }, $http, $q);
            this.apiClient.loggingContext = `component ${this.componentName()}`;
        }
        isLoading() {
            return this.apiClient.isLoading();
        }
    }
    NTechComponents.NTechComponentControllerBase = NTechComponentControllerBase;
})(NTechComponents || (NTechComponents = {}));
