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
var NTechDwApi;
(function (NTechDwApi) {
    var ApiClient = /** @class */ (function (_super) {
        __extends(ApiClient, _super);
        function ApiClient(onError, $http, $q) {
            return _super.call(this, onError, $http, $q) || this;
        }
        ApiClient.prototype.toQueryString = function (params) {
            var s = '';
            for (var _i = 0, _a = Object.keys(params); _i < _a.length; _i++) {
                var key = _a[_i];
                var value = params[key];
                if (value !== null && value !== '' && value !== undefined) {
                    if (s.length > 0) {
                        s += '&';
                    }
                    s += "".concat(key, "=").concat(encodeURIComponent(params[key]));
                }
            }
            return s;
        };
        ApiClient.prototype.fetchVintageReportData = function (request) {
            return this.post('/api/Reports/Vintage/FetchData', request);
        };
        ApiClient.prototype.fetchAllProviders = function () {
            return this.post('/api/Providers/FetchAll', {});
        };
        ApiClient.prototype.fetchAllRiskGroups = function () {
            return this.post('/api/RiskGroups/FetchAll', {});
        };
        ApiClient.prototype.fetchVintagePeriods = function (includeMonths) {
            return this.post('/api/Reports/Vintage/FetchPeriods', { IncludeMonths: includeMonths });
        };
        return ApiClient;
    }(NTechComponents.NTechApiClientBase));
    NTechDwApi.ApiClient = ApiClient;
})(NTechDwApi || (NTechDwApi = {}));
