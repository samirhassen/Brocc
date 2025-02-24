var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var NTechComponents;
(function (NTechComponents) {
    var NTechComponentControllerBase = /** @class */ (function (_super) {
        __extends(NTechComponentControllerBase, _super);
        function NTechComponentControllerBase(ntechComponentService, $http, $q) {
            var _this = _super.call(this, ntechComponentService) || this;
            _this.apiClient = new NTechSavingsApi.ApiClient(function (errorMessage) {
                toastr.error(errorMessage);
            }, $http, $q);
            _this.apiClient.loggingContext = "component ".concat(_this.componentName());
            return _this;
        }
        NTechComponentControllerBase.prototype.isLoading = function () {
            return this.apiClient.isLoading();
        };
        return NTechComponentControllerBase;
    }(NTechComponents.NTechComponentControllerBaseTemplate));
    NTechComponents.NTechComponentControllerBase = NTechComponentControllerBase;
})(NTechComponents || (NTechComponents = {}));
