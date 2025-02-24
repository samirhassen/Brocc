var ApplicationDataSourceHelper;
(function (ApplicationDataSourceHelper) {
    ApplicationDataSourceHelper.MissingItemReplacementValue = 'e0d32aa3-6d36-4f07-9b66-2cc43087483c';
    var ApplicationDataSourceService = /** @class */ (function () {
        function ApplicationDataSourceService(applicationNr, apiClient, $q, afterSave, afterDataLoaded) {
            this.applicationNr = applicationNr;
            this.apiClient = apiClient;
            this.$q = $q;
            this.afterSave = afterSave;
            this.afterDataLoaded = afterDataLoaded;
            this.items = [];
        }
        ApplicationDataSourceService.prototype.getIncludedItems = function () {
            return this.items;
        };
        ApplicationDataSourceService.prototype.addDataSourceItem = function (dataSourceName, itemName, forceReadonly, isNavigationEditOrViewPossible) {
            this.items.push({ dataSourceName: dataSourceName, itemName: itemName, forceReadonly: forceReadonly, isNavigationEditOrViewPossible: isNavigationEditOrViewPossible });
        };
        ApplicationDataSourceService.prototype.addDataSourceItems = function (dataSourceName, itemNames, forceReadonly, isNavigationEditOrViewPossible) {
            if (!itemNames) {
                return;
            }
            for (var _i = 0, itemNames_1 = itemNames; _i < itemNames_1.length; _i++) {
                var n = itemNames_1[_i];
                this.addDataSourceItem(dataSourceName, n, forceReadonly, isNavigationEditOrViewPossible);
            }
        };
        ApplicationDataSourceService.prototype.addComplexApplicationListItem = function (itemName, forceReadonly) {
            this.addComplexApplicationListItems([itemName], forceReadonly);
        };
        ApplicationDataSourceService.prototype.addComplexApplicationListItems = function (itemNames, forceReadonly) {
            this.addDataSourceItems('ComplexApplicationList', itemNames, forceReadonly === true, true);
        };
        ApplicationDataSourceService.prototype.loadItems = function () {
            var _this = this;
            var requests = {};
            for (var _i = 0, _a = this.items; _i < _a.length; _i++) {
                var i = _a[_i];
                var r = requests[i.dataSourceName];
                if (!r) {
                    r = {
                        DataSourceName: i.dataSourceName,
                        ErrorIfMissing: false,
                        IncludeEditorModel: true,
                        IncludeIsChanged: true,
                        ReplaceIfMissing: true,
                        MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                        Names: []
                    };
                    requests[i.dataSourceName] = r;
                }
                r.Names.push(i.itemName);
            }
            var requestsArray = [];
            for (var _b = 0, _c = Object.keys(requests); _b < _c.length; _b++) {
                var k = _c[_b];
                requestsArray.push(requests[k]);
            }
            var p = this.apiClient.fetchApplicationDataSourceItems(this.applicationNr, requestsArray);
            if (!this.afterDataLoaded) {
                return p;
            }
            else {
                return p.then(function (x) {
                    _this.afterDataLoaded(x);
                    return x;
                });
            }
        };
        ApplicationDataSourceService.prototype.saveItems = function (edits) {
            var _this = this;
            var perDataSourceEdits = {};
            for (var _i = 0, edits_1 = edits; _i < edits_1.length; _i++) {
                var edit = edits_1[_i];
                if (!perDataSourceEdits[edit.dataSourceName]) {
                    perDataSourceEdits[edit.dataSourceName] = [];
                }
                perDataSourceEdits[edit.dataSourceName].push(edit);
            }
            var promises = [];
            for (var _a = 0, _b = Object.keys(perDataSourceEdits); _a < _b.length; _a++) {
                var dataSourceName = _b[_a];
                for (var _c = 0, _d = perDataSourceEdits[dataSourceName]; _c < _d.length; _c++) {
                    var editItem = _d[_c];
                    var isDelete = !editItem.toValue; //Could possibly delete false boolean? make more rigourous?
                    promises.push(this.apiClient.setApplicationEditItemData(this.applicationNr, dataSourceName, editItem.itemName, isDelete ? null : editItem.toValue, isDelete));
                }
            }
            var result = this.$q.all(promises);
            if (this.afterSave) {
                return result.then(function (x) {
                    _this.afterSave(x);
                    return x;
                });
            }
            else {
                return result;
            }
        };
        return ApplicationDataSourceService;
    }());
    ApplicationDataSourceHelper.ApplicationDataSourceService = ApplicationDataSourceService;
})(ApplicationDataSourceHelper || (ApplicationDataSourceHelper = {}));
