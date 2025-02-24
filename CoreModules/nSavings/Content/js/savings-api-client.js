var NTechSavingsApi;
(function (NTechSavingsApi) {
    var ApiClient = /** @class */ (function () {
        function ApiClient(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.activePostCount = 0;
            this.loggingContext = null;
        }
        ApiClient.prototype.post = function (url, data) {
            var _this = this;
            var startTimeMs = performance.now();
            this.activePostCount++;
            var d = this.$q.defer();
            this.$http.post(url, data).then(function (result) {
                d.resolve(result.data);
            }, function (err) {
                if (_this.onError) {
                    _this.onError(err.statusText);
                }
                d.reject(err.statusText);
            }).finally(function () {
                _this.activePostCount--;
                var totalTimeMs = performance.now() - startTimeMs;
                var c = _this.loggingContext == null ? '' : (_this.loggingContext + ': ');
                console.log("".concat(c, "post - ").concat(url, ": ").concat(totalTimeMs, "ms"));
            });
            return d.promise;
        };
        ApiClient.prototype.postUsingApiGateway = function (seviceName, serviceLocalUrl, data) {
            return this.post("/Api/Gateway/".concat(seviceName).concat(serviceLocalUrl[0] === '/' ? '' : '/').concat(serviceLocalUrl), data);
        };
        ApiClient.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        ApiClient.prototype.keyValueStoreGet = function (key, keySpace) {
            return this.post('/api/KeyValueStore/Get', {
                "Key": key,
                "KeySpace": keySpace
            });
        };
        ApiClient.prototype.keyValueStoreRemove = function (key, keySpace) {
            return this.post('/api/KeyValueStore/Remove', {
                "Key": key,
                "KeySpace": keySpace
            });
        };
        ApiClient.prototype.keyValueStoreSet = function (key, keySpace, value) {
            return this.post('/api/KeyValueStore/Set', {
                "Key": key,
                "KeySpace": keySpace,
                "Value": value
            });
        };
        ApiClient.prototype.fetchUserNameByUserId = function (userId) {
            return this.post('/api/UserName/ByUserId', { UserId: userId });
        };
        ApiClient.prototype.fetchFatcaExportFiles = function (pageSize, pageNr) {
            return this.post('/api/Fatca/FetchExportFiles', {
                PageSize: pageSize,
                PageNr: pageNr
            });
        };
        ApiClient.prototype.createFatcaExportFile = function (year, exportProfile) {
            return this.post('/api/Fatca/CreateExportFile', {
                Year: year,
                ExportProfile: exportProfile
            });
        };
        ApiClient.prototype.getCustomerMessagesTexts = function (messageIds) {
            return this.postUsingApiGateway('nCustomer', 'api/CustomerMessage/GetMessageTexts', {
                MessageIds: messageIds
            });
        };
        ApiClient.prototype.createAndDeliverFinnishCustomsAccountsExportFile = function (skipDeliver) {
            return this.post('/api/FinnishCustomsAccounts/CreateExportFile', { skipDeliver: skipDeliver });
        };
        ApiClient.prototype.fetchFinnishCustomsAccountsExportFiles = function (pageSize, pageNr) {
            return this.post('/api/FinnishCustomsAccounts/FetchExportFiles', { pageSize: pageSize, pageNr: pageNr });
        };
        return ApiClient;
    }());
    NTechSavingsApi.ApiClient = ApiClient;
})(NTechSavingsApi || (NTechSavingsApi = {}));
