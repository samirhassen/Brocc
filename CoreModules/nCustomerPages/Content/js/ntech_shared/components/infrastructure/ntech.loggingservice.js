var NTechComponents;
(function (NTechComponents) {
    var NTechLoggingService = /** @class */ (function () {
        function NTechLoggingService() {
            this.isDebugMode = location && location.hostname === 'localhost';
        }
        NTechLoggingService.prototype.logDebug = function (message) {
            console.log(message);
        };
        NTechLoggingService.$inject = [];
        return NTechLoggingService;
    }());
    NTechComponents.NTechLoggingService = NTechLoggingService;
})(NTechComponents || (NTechComponents = {}));
//# sourceMappingURL=ntech.loggingservice.js.map