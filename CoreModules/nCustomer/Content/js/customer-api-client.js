var NTechCustomerApi;
(function (NTechCustomerApi) {
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
            });
            return d.promise;
        };
        ApiClient.prototype.getUserModuleUrl = function (moduleName, serviceLocalUrl, parameters) {
            return this.post('/Api/GetUserModuleUrl', { moduleName: moduleName, moduleLocalUrl: serviceLocalUrl, parameters: parameters });
        };
        ApiClient.prototype.isLoading = function () {
            return this.activePostCount > 0;
        };
        ApiClient.prototype.unlockSensitiveItemByName = function (customerId, itemName) {
            return this.post('/Customer/UnlockSensitiveItemByName', { customerId: customerId, itemName: itemName });
        };
        ApiClient.prototype.updateCustomer = function (items, force) {
            return this.post('/Customer/UpdateCustomer', { items: items, force: force });
        };
        ApiClient.prototype.fetchLegacyCustomerCardUiData = function (customerId, backUrl) {
            return this.post('/Api/LegacyCustomerCard/FetchUiData', { customerId: customerId, backUrl: backUrl });
        };
        ApiClient.prototype.fetchCustomerContactInfo = function (customerId, includeSensitive, includeCivicRegNr) {
            return this.post('/Api/ContactInfo/Fetch', { customerId: customerId, includeSensitive: includeSensitive, includeCivicRegNr: includeCivicRegNr });
        };
        ApiClient.prototype.fetchCustomerContactInfoEditValueData = function (customerId, name) {
            return this.post('/Api/ContactInfo/FetchEditValueData', { customerId: customerId, name: name });
        };
        ApiClient.prototype.changeCustomerContactInfoValue = function (customerId, name, value, includesNewValuesInResponse) {
            return this.post('/Api/ContactInfo/ChangeValue', { customerId: customerId, name: name, value: value, includesNewValuesInResponse: includesNewValuesInResponse });
        };
        ApiClient.prototype.kycManagementFetchLocalDecisionData = function (customerId) {
            return this.post('/Api/KycManagement/FetchLocalDecisionCurrentData', { customerId: customerId });
        };
        ApiClient.prototype.kycManagementFetchLocalDecisionHistoryData = function (customerId, isModellingPep) {
            return this.post('/Api/KycManagement/FetchLocalDecisionHistoryData', { customerId: customerId, isModellingPep: isModellingPep });
        };
        ApiClient.prototype.kycManagementSetLocalDecision = function (customerId, isModellingPep, currentValue, includeNewCurrentData) {
            return this.post('/Api/KycManagement/SetLocalDecision', { customerId: customerId, isModellingPep: isModellingPep, currentValue: currentValue, includeNewCurrentData: includeNewCurrentData });
        };
        ApiClient.prototype.kycManagementFetchLatestCustomerQuestionsSet = function (customerId) {
            return this.post('/Api/KycManagement/FetchLatestCustomerQuestionsSet', { customerId: customerId });
        };
        ApiClient.prototype.kycManagementQueryDetails = function (queryId) {
            return this.post('/Api/KycScreening/QueryResultDetails', { queryId: queryId });
        };
        ApiClient.prototype.fetchLatestTrapetsQueryResult = function (customerId) {
            return this.post('/Api/KycManagement/FetchLatestTrapetsQueryResult', { customerId: customerId });
        };
        ApiClient.prototype.fetchTrapetsQueryHistorySummary = function (customerId) {
            return this.post('/Api/KycManagement/FetchTrapetsQueryHistorySummary', { customerId: customerId });
        };
        ApiClient.prototype.fetchQueryResultHistoryDetails = function (customerId, historyDayCount) {
            return this.post('/Api/KycScreening/QueryResultHistoryDetails', {
                customerId: customerId,
                historyDayCount: historyDayCount
            });
        };
        ApiClient.prototype.fetchCustomerPropertiesWithGroupedEditHistory = function (customerId, propertyNames) {
            return this.post('/Api/KycManagement/FetchPropertiesWithGroupedEditHistory', { customerId: customerId, propertyNames: propertyNames });
        };
        ApiClient.prototype.fetchCustomerItemsDict = function (customerId, itemNames) {
            var deferred = this.$q.defer();
            this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                propertyNames: itemNames,
                customerIds: [customerId]
            }).then(function (result) {
                var r = {};
                if (result.customers && result.customers.length > 0 && result.customers[0].Properties) {
                    for (var _i = 0, _a = result.customers[0].Properties; _i < _a.length; _i++) {
                        var p = _a[_i];
                        r[p.Name] = p.Value;
                    }
                    deferred.resolve(r);
                }
            });
            return deferred.promise;
        };
        ApiClient.prototype.fetchCustomerItems = function (customerId, itemNames) {
            var deferred = this.$q.defer();
            this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                propertyNames: itemNames,
                customerIds: [customerId]
            }).then(function (result) {
                var items = [];
                if (result.customers && result.customers.length > 0 && result.customers[0].Properties) {
                    for (var _i = 0, _a = result.customers[0].Properties; _i < _a.length; _i++) {
                        var p = _a[_i];
                        items.push({ name: p.Name, value: p.Value });
                    }
                    deferred.resolve(items);
                }
            });
            return deferred.promise;
        };
        ApiClient.prototype.fetchCustomerComments = function (customerId) {
            return this.post('/Api/CustomerComments/FetchAllForCustomer', { customerId: customerId });
        };
        ApiClient.prototype.addCustomerComment = function (customerId, newCommentText, attachedFileAsDataUrl, attachedFileName) {
            return this.post('/Api/CustomerComments/Add', { customerId: customerId, newCommentText: newCommentText, attachedFileAsDataUrl: attachedFileAsDataUrl, attachedFileName: attachedFileName });
        };
        ApiClient.prototype.fetchCustomerRelations = function (customerId) {
            return this.post('/Api/CustomerRelations/FetchForCustomer', { customerId: customerId });
        };
        ApiClient.prototype.getCustomerIdsByCivicRegNrs = function (civicRegNrs) {
            return this.post('/Api/CustomerIdByCivicRegNr', { civicRegNr: civicRegNrs });
        };
        ApiClient.prototype.CreateOrUpdateCustomer = function (CivicRegNr, EventType, items) {
            return this.post('/Api/PersonCustomer/CreateOrUpdate', { CivicRegNr: CivicRegNr, EventType: EventType, Properties: items });
        };
        ApiClient.prototype.parseCivicRegNr = function (civicRegNr) {
            return this.post('/Api/ParseCivicRegNr', { civicRegNr: civicRegNr });
        };
        ApiClient.prototype.findCustomerChannels = function (searchText, searchType, includeGeneralChannel) {
            if (searchType === void 0) { searchType = FindCustomerChannelsSearchType.Omni; }
            if (includeGeneralChannel === void 0) { includeGeneralChannel = false; }
            return this.post('/api/CustomerMessage/FindCustomerChannels', {
                SearchText: searchText,
                SearchType: searchType.toString(),
                includeGeneralChannel: includeGeneralChannel
            });
        };
        ApiClient.prototype.bulkFetchPropertiesByCustomerIds = function (customerIds, itemNames) {
            return this.post('/Customer/BulkFetchPropertiesByCustomerIds', {
                customerIds: customerIds,
                propertyNames: itemNames
            });
        };
        ApiClient.prototype.fetchCustomerItemsBulk = function (customerIds, itemNames) {
            return this.bulkFetchPropertiesByCustomerIds(customerIds, itemNames).then(function (x) {
                var r = {};
                if (x.customers) {
                    for (var _i = 0, _a = x.customers; _i < _a.length; _i++) {
                        var c = _a[_i];
                        var cd = {};
                        for (var _b = 0, _c = c.Properties; _b < _c.length; _b++) {
                            var p = _c[_b];
                            cd[p.Name] = p.Value;
                        }
                        r[c.CustomerId] = cd;
                    }
                }
                return r;
            });
        };
        ApiClient.prototype.getSecureCustomerMessages = function (request) {
            return this.post('/api/CustomerMessage/GetMessages', request);
        };
        ApiClient.prototype.GetCustomerMessagesByChannel = function (request) {
            return this.post('/api/CustomerMessage/GetCustomerMessagesByChannel', request);
        };
        ApiClient.prototype.createSecureCustomerMessage = function (request) {
            return this.post('/api/CustomerMessage/CreateMessage', request);
        };
        ApiClient.prototype.attachMessageDocument = function (request) {
            return this.post('/api/CustomerMessage/attachMessageDocument', request);
        };
        ApiClient.prototype.handleMessages = function (request) {
            return this.post('/api/CustomerMessage/HandleMessages', request);
        };
        ApiClient.prototype.wipeCustomerContactInfo = function (customerIds) {
            return this.post('/api/TestSupport/Wipe-Customer-ContactInfo', { CustomerIds: customerIds });
        };
        return ApiClient;
    }());
    NTechCustomerApi.ApiClient = ApiClient;
    var FindCustomerChannelsSearchType;
    (function (FindCustomerChannelsSearchType) {
        FindCustomerChannelsSearchType["Omni"] = "Omni";
        FindCustomerChannelsSearchType["Email"] = "Email";
        FindCustomerChannelsSearchType["CustomerName"] = "CustomerName";
        FindCustomerChannelsSearchType["OrgOrCivicRegNr"] = "OrgOrCivicRegNr";
        FindCustomerChannelsSearchType["RelationId"] = "RelationId";
    })(FindCustomerChannelsSearchType || (FindCustomerChannelsSearchType = {}));
    var PropertyModel = /** @class */ (function () {
        function PropertyModel() {
        }
        return PropertyModel;
    }());
    NTechCustomerApi.PropertyModel = PropertyModel;
    var CustomerComment = /** @class */ (function () {
        function CustomerComment() {
        }
        return CustomerComment;
    }());
    NTechCustomerApi.CustomerComment = CustomerComment;
    var CustomerItem = /** @class */ (function () {
        function CustomerItem() {
        }
        return CustomerItem;
    }());
    NTechCustomerApi.CustomerItem = CustomerItem;
    var TrapetsQueryHistorySummaryModel = /** @class */ (function () {
        function TrapetsQueryHistorySummaryModel() {
        }
        return TrapetsQueryHistorySummaryModel;
    }());
    NTechCustomerApi.TrapetsQueryHistorySummaryModel = TrapetsQueryHistorySummaryModel;
    var TrapetsQueryHistorySummaryItem = /** @class */ (function () {
        function TrapetsQueryHistorySummaryItem() {
        }
        return TrapetsQueryHistorySummaryItem;
    }());
    NTechCustomerApi.TrapetsQueryHistorySummaryItem = TrapetsQueryHistorySummaryItem;
    var TrapetsQueryResultModel = /** @class */ (function () {
        function TrapetsQueryResultModel() {
        }
        return TrapetsQueryResultModel;
    }());
    NTechCustomerApi.TrapetsQueryResultModel = TrapetsQueryResultModel;
    var CustomerQuestionsSet = /** @class */ (function () {
        function CustomerQuestionsSet() {
        }
        return CustomerQuestionsSet;
    }());
    NTechCustomerApi.CustomerQuestionsSet = CustomerQuestionsSet;
    var CustomerQuestionsSetItem = /** @class */ (function () {
        function CustomerQuestionsSetItem() {
        }
        return CustomerQuestionsSetItem;
    }());
    NTechCustomerApi.CustomerQuestionsSetItem = CustomerQuestionsSetItem;
    var KycLocalDecisionCurrentModel = /** @class */ (function () {
        function KycLocalDecisionCurrentModel() {
        }
        return KycLocalDecisionCurrentModel;
    }());
    NTechCustomerApi.KycLocalDecisionCurrentModel = KycLocalDecisionCurrentModel;
    var KycLocalDecisionHistoryModel = /** @class */ (function () {
        function KycLocalDecisionHistoryModel() {
        }
        return KycLocalDecisionHistoryModel;
    }());
    NTechCustomerApi.KycLocalDecisionHistoryModel = KycLocalDecisionHistoryModel;
    var KycLocalDecisionHistoryItem = /** @class */ (function () {
        function KycLocalDecisionHistoryItem() {
        }
        return KycLocalDecisionHistoryItem;
    }());
    NTechCustomerApi.KycLocalDecisionHistoryItem = KycLocalDecisionHistoryItem;
    var SetLocalDecisionResponse = /** @class */ (function () {
        function SetLocalDecisionResponse() {
        }
        return SetLocalDecisionResponse;
    }());
    NTechCustomerApi.SetLocalDecisionResponse = SetLocalDecisionResponse;
    var ChangeCustomerContactInfoValueResponse = /** @class */ (function () {
        function ChangeCustomerContactInfoValueResponse() {
        }
        return ChangeCustomerContactInfoValueResponse;
    }());
    NTechCustomerApi.ChangeCustomerContactInfoValueResponse = ChangeCustomerContactInfoValueResponse;
    var FetchCustomerContactInfoEditValueDataResponse = /** @class */ (function () {
        function FetchCustomerContactInfoEditValueDataResponse() {
        }
        return FetchCustomerContactInfoEditValueDataResponse;
    }());
    NTechCustomerApi.FetchCustomerContactInfoEditValueDataResponse = FetchCustomerContactInfoEditValueDataResponse;
    var FetchCustomerContactInfoResponse = /** @class */ (function () {
        function FetchCustomerContactInfoResponse() {
        }
        return FetchCustomerContactInfoResponse;
    }());
    NTechCustomerApi.FetchCustomerContactInfoResponse = FetchCustomerContactInfoResponse;
    var FetchLegacyCustomerCardUiDataResponse = /** @class */ (function () {
        function FetchLegacyCustomerCardUiDataResponse() {
        }
        return FetchLegacyCustomerCardUiDataResponse;
    }());
    NTechCustomerApi.FetchLegacyCustomerCardUiDataResponse = FetchLegacyCustomerCardUiDataResponse;
    var CustomerPropertyModel = /** @class */ (function () {
        function CustomerPropertyModel() {
        }
        return CustomerPropertyModel;
    }());
    NTechCustomerApi.CustomerPropertyModel = CustomerPropertyModel;
})(NTechCustomerApi || (NTechCustomerApi = {}));
