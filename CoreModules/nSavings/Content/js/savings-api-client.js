var NTechSavingsApi;
(function (NTechSavingsApi) {
    class ApiClient {
        constructor(onError, $http, $q) {
            this.onError = onError;
            this.$http = $http;
            this.$q = $q;
            this.activePostCount = 0;
            this.loggingContext = null;
        }
        post(url, data) {
            let startTimeMs = performance.now();
            this.activePostCount++;
            let d = this.$q.defer();
            this.$http.post(url, data).then((result) => {
                d.resolve(result.data);
            }, err => {
                if (this.onError) {
                    this.onError(err.statusText);
                }
                d.reject(err.statusText);
            }).finally(() => {
                this.activePostCount--;
                let totalTimeMs = performance.now() - startTimeMs;
                let c = this.loggingContext == null ? '' : (this.loggingContext + ': ');
                console.log(`${c}post - ${url}: ${totalTimeMs}ms`);
            });
            return d.promise;
        }
        postUsingApiGateway(seviceName, serviceLocalUrl, data) {
            return this.post(`/Api/Gateway/${seviceName}${serviceLocalUrl[0] === '/' ? '' : '/'}${serviceLocalUrl}`, data);
        }
        isLoading() {
            return this.activePostCount > 0;
        }
        keyValueStoreGet(key, keySpace) {
            return this.post('/api/KeyValueStore/Get', {
                "Key": key,
                "KeySpace": keySpace
            });
        }
        keyValueStoreRemove(key, keySpace) {
            return this.post('/api/KeyValueStore/Remove', {
                "Key": key,
                "KeySpace": keySpace
            });
        }
        keyValueStoreSet(key, keySpace, value) {
            return this.post('/api/KeyValueStore/Set', {
                "Key": key,
                "KeySpace": keySpace,
                "Value": value
            });
        }
        fetchUserNameByUserId(userId) {
            return this.post('/api/UserName/ByUserId', { UserId: userId });
        }
        fetchFatcaExportFiles(pageSize, pageNr) {
            return this.post('/api/Fatca/FetchExportFiles', {
                PageSize: pageSize,
                PageNr: pageNr
            });
        }
        createFatcaExportFile(year, exportProfile) {
            return this.post('/api/Fatca/CreateExportFile', {
                Year: year,
                ExportProfile: exportProfile
            });
        }
        getCustomerMessagesTexts(messageIds) {
            return this.postUsingApiGateway('nCustomer', 'api/CustomerMessage/GetMessageTexts', {
                MessageIds: messageIds
            });
        }
        createAndDeliverFinnishCustomsAccountsExportFile(skipDeliver) {
            return this.post('/api/FinnishCustomsAccounts/CreateExportFile', { skipDeliver: skipDeliver });
        }
        fetchFinnishCustomsAccountsExportFiles(pageSize, pageNr) {
            return this.post('/api/FinnishCustomsAccounts/FetchExportFiles', { pageSize, pageNr });
        }
    }
    NTechSavingsApi.ApiClient = ApiClient;
})(NTechSavingsApi || (NTechSavingsApi = {}));
