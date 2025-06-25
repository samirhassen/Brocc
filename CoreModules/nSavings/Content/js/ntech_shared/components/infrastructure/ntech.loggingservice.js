var NTechComponents;
(function (NTechComponents) {
    class NTechLoggingService {
        constructor() {
            this.isDebugMode = location && location.hostname === 'localhost';
        }
        logDebug(message) {
            console.log(message);
        }
    }
    NTechLoggingService.$inject = [];
    NTechComponents.NTechLoggingService = NTechLoggingService;
})(NTechComponents || (NTechComponents = {}));
