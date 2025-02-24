var ComplexApplicationListHelper;
(function (ComplexApplicationListHelper) {
    var RepeatableCode;
    (function (RepeatableCode) {
        RepeatableCode["Yes"] = "r";
        RepeatableCode["No"] = "u";
        RepeatableCode["All"] = "*";
    })(RepeatableCode = ComplexApplicationListHelper.RepeatableCode || (ComplexApplicationListHelper.RepeatableCode = {}));
    ComplexApplicationListHelper.DataSourceName = 'ComplexApplicationList';
    function getDataSourceItemName(listName, nr, itemName, repeatableCode) {
        return "".concat(listName, "#").concat(nr, "#").concat(repeatableCode, "#").concat(itemName);
    }
    ComplexApplicationListHelper.getDataSourceItemName = getDataSourceItemName;
    function setValue(applicationNr, itemName, value, apiClient) {
        return apiClient.setApplicationEditItemData(applicationNr, ComplexApplicationListHelper.DataSourceName, itemName, value, false).then(function (x) {
        });
    }
    ComplexApplicationListHelper.setValue = setValue;
    function deleteRow(applicationNr, listName, nr, apiClient) {
        return apiClient.setApplicationEditItemData(applicationNr, ComplexApplicationListHelper.DataSourceName, getDataSourceItemName(listName, nr.toString(), '*', RepeatableCode.All), null, true).then(function (x) {
        });
    }
    ComplexApplicationListHelper.deleteRow = deleteRow;
    function parseCompoundItemName(n) {
        var r = /([\w]+)#([\d\*]+)#([ur\*])#([\w\*]+)/;
        var m = r.exec(n);
        return {
            listName: m[1],
            itemName: m[4],
            nr: m[2],
            repeatable: m[3]
        };
    }
    ComplexApplicationListHelper.parseCompoundItemName = parseCompoundItemName;
    function getNrs(applicationNr, listName, apiClient) {
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
                DataSourceName: 'ComplexApplicationList',
                ErrorIfMissing: false,
                IncludeEditorModel: false,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                ReplaceIfMissing: true,
                Names: [getDataSourceItemName(listName, '*', '*', RepeatableCode.All)]
            }]).then(function (x) {
            var nrs = [];
            var ds = {};
            for (var _i = 0, _a = x.Results[0].Items; _i < _a.length; _i++) {
                var i = _a[_i];
                var nr = parseCompoundItemName(i.Name).nr;
                if (nr && nr != '*') {
                    var nrP = parseInt(nr);
                    if (ds[nrP] !== true) {
                        nrs.push(nrP);
                        ds[nrP] = true;
                    }
                }
            }
            return nrs;
        });
    }
    ComplexApplicationListHelper.getNrs = getNrs;
    function getAllCustomerIds(applicationNr, listNames, apiClient, startingData) {
        var names = [];
        for (var _i = 0, listNames_1 = listNames; _i < listNames_1.length; _i++) {
            var n = listNames_1[_i];
            names.push(getDataSourceItemName(n, '*', 'customerIds', RepeatableCode.Yes));
        }
        var listNamesByCustomerId;
        if (startingData) {
            listNamesByCustomerId = startingData;
        }
        if (!listNamesByCustomerId) {
            listNamesByCustomerId = {};
        }
        var add = function (customerId, roleName) {
            if (!listNamesByCustomerId[customerId]) {
                listNamesByCustomerId[customerId] = [];
            }
            listNamesByCustomerId[customerId].push(roleName);
        };
        return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
                DataSourceName: 'ComplexApplicationList',
                ErrorIfMissing: false,
                IncludeEditorModel: false,
                IncludeIsChanged: false,
                MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                ReplaceIfMissing: false,
                Names: names
            }]).then(function (x) {
            for (var _i = 0, _a = x.Results[0].Items; _i < _a.length; _i++) {
                var i = _a[_i];
                if (i.Value) {
                    var localValues = JSON.parse(i.Value);
                    var itemNameParsed = parseCompoundItemName(i.Name);
                    for (var _b = 0, localValues_1 = localValues; _b < localValues_1.length; _b++) {
                        var s = localValues_1[_b];
                        add(parseInt(s), itemNameParsed.listName);
                    }
                }
            }
            var customerIds = Object.keys(listNamesByCustomerId);
            for (var _c = 0, customerIds_1 = customerIds; _c < customerIds_1.length; _c++) {
                var k = customerIds_1[_c];
                listNamesByCustomerId[k] = NTechLinq.distinct(listNamesByCustomerId[k]);
            }
            return listNamesByCustomerId;
        });
    }
    ComplexApplicationListHelper.getAllCustomerIds = getAllCustomerIds;
    function fetch(applicationNr, listName, apiClient, uniqueItemNames, repeatingNames) {
        return ComplexApplicationListHelper.getNrs(applicationNr, listName, apiClient).then(function (nrs) {
            var names = [];
            if (uniqueItemNames) {
                for (var _i = 0, uniqueItemNames_1 = uniqueItemNames; _i < uniqueItemNames_1.length; _i++) {
                    var itemName = uniqueItemNames_1[_i];
                    for (var _a = 0, nrs_1 = nrs; _a < nrs_1.length; _a++) {
                        var nr = nrs_1[_a];
                        names.push(getDataSourceItemName(listName, nr.toString(), itemName, RepeatableCode.No));
                    }
                }
            }
            if (repeatingNames) {
                for (var _b = 0, repeatingNames_1 = repeatingNames; _b < repeatingNames_1.length; _b++) {
                    var itemName = repeatingNames_1[_b];
                    for (var _c = 0, nrs_2 = nrs; _c < nrs_2.length; _c++) {
                        var nr = nrs_2[_c];
                        names.push(getDataSourceItemName(listName, nr.toString(), itemName, RepeatableCode.Yes));
                    }
                }
            }
            return apiClient.fetchApplicationDataSourceItems(applicationNr, [{
                    DataSourceName: 'ComplexApplicationList',
                    ErrorIfMissing: false,
                    IncludeEditorModel: true,
                    IncludeIsChanged: false,
                    MissingItemReplacementValue: ApplicationDataSourceHelper.MissingItemReplacementValue,
                    ReplaceIfMissing: true,
                    Names: names
                }]).then(function (x) {
                var r = x.Results[0];
                var d = new ComplexApplicationListData(listName);
                for (var _i = 0, _a = r.Items; _i < _a.length; _i++) {
                    var i = _a[_i];
                    var n = parseCompoundItemName(i.Name);
                    var nr = parseInt(n.nr);
                    d.ensureNr(nr);
                    d.setEditorModel(n.itemName, i.EditorModel);
                    if (i.Value !== ApplicationDataSourceHelper.MissingItemReplacementValue) {
                        if (n.repeatable === RepeatableCode.Yes) {
                            d.setRepeatableItem(nr, n.itemName, JSON.parse(i.Value));
                        }
                        else {
                            d.setUniqueItem(nr, n.itemName, i.Value);
                        }
                    }
                }
                return d;
            });
        });
    }
    ComplexApplicationListHelper.fetch = fetch;
    var ComplexApplicationListData = /** @class */ (function () {
        function ComplexApplicationListData(listName) {
            this.listName = listName;
        }
        ComplexApplicationListData.prototype.getNrs = function () {
            if (!this.nrs) {
                return [];
            }
            var clonedArray = this.nrs.slice();
            clonedArray.sort();
            return clonedArray;
        };
        ComplexApplicationListData.prototype.getEditorModel = function (name) {
            if (!this.editorModelByName || !this.editorModelByName[name]) {
                return null;
            }
            return this.editorModelByName[name];
        };
        ComplexApplicationListData.prototype.ensureNr = function (nr) {
            if (!this.nrs) {
                this.nrs = [];
            }
            if (this.nrs.indexOf(nr) < 0) {
                this.nrs.push(nr);
            }
        };
        ComplexApplicationListData.prototype.setEditorModel = function (name, editorModel) {
            if (!editorModel) {
                return;
            }
            if (!this.editorModelByName) {
                this.editorModelByName = {};
            }
            this.editorModelByName[name] = editorModel;
        };
        ComplexApplicationListData.prototype.setRepeatableItem = function (nr, name, value) {
            if (!this.repeatableItems) {
                this.repeatableItems = {};
            }
            if (!this.repeatableItems[nr]) {
                this.repeatableItems[nr] = {};
            }
            this.ensureNr(nr);
            this.repeatableItems[nr][name] = value;
        };
        ComplexApplicationListData.prototype.setUniqueItem = function (nr, name, value) {
            if (!this.uniqueItems) {
                this.uniqueItems = {};
            }
            if (!this.uniqueItems[nr]) {
                this.uniqueItems[nr] = {};
            }
            this.ensureNr(nr);
            this.uniqueItems[nr][name] = value;
        };
        ComplexApplicationListData.prototype.getRepeatableItems = function (nr) {
            if (!this.repeatableItems || !this.repeatableItems[nr]) {
                return {};
            }
            return this.repeatableItems[nr];
        };
        ComplexApplicationListData.prototype.getUniqueItems = function (nr) {
            if (!this.uniqueItems || !this.uniqueItems[nr]) {
                return {};
            }
            return this.uniqueItems[nr];
        };
        ComplexApplicationListData.prototype.getOptionalUniqueValue = function (nr, name) {
            if (!this.uniqueItems || !this.uniqueItems[nr]) {
                return null;
            }
            return this.uniqueItems[nr][name];
        };
        ComplexApplicationListData.prototype.getOptionalRepeatingValue = function (nr, name) {
            if (!this.repeatableItems || !this.repeatableItems[nr]) {
                return null;
            }
            return this.repeatableItems[nr][name];
        };
        return ComplexApplicationListData;
    }());
    ComplexApplicationListHelper.ComplexApplicationListData = ComplexApplicationListData;
})(ComplexApplicationListHelper || (ComplexApplicationListHelper = {}));
