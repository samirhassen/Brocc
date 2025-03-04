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
var BookKeepingRulesEditComponentNs;
(function (BookKeepingRulesEditComponentNs) {
    var BookKeepingRulesEditController = /** @class */ (function (_super) {
        __extends(BookKeepingRulesEditController, _super);
        function BookKeepingRulesEditController($http, $q, ntechComponentService, ntechLocalStorageService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.ntechLocalStorageService = ntechLocalStorageService;
            _this.$scope = $scope;
            return _this;
        }
        BookKeepingRulesEditController.prototype.componentName = function () {
            return 'bookkeepingRulesEdit';
        };
        BookKeepingRulesEditController.prototype.onChanges = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.reload();
        };
        BookKeepingRulesEditController.prototype.reload = function () {
            var _this = this;
            this.apiClient.fetchBookKeepingRules().then(function (x) {
                var m = {
                    accountNames: x.allAccountNames,
                    accountNrByAccountName: x.accountNrByAccountName,
                    allConnections: x.allConnections,
                    ruleRows: x.ruleRows,
                    exportCode: null,
                    importText: null,
                    backUrl: _this.initialData.backTarget
                        ? _this.initialData.crossModuleNavigateUrlPattern.replace('[[[TARGET_CODE]]]', _this.initialData.backTarget)
                        : _this.initialData.backofficeMenuUrl,
                    isTest: _this.initialData.isTest
                };
                var code = {
                    accountNames: m.accountNames,
                    accountNrByAccountName: m.accountNrByAccountName
                };
                m.exportCode = "B_".concat(btoa(JSON.stringify(code)), "_B");
                _this.m = m;
            });
        };
        BookKeepingRulesEditController.prototype.beginEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.edit = {
                accountNrByAccountName: angular.copy(this.m.accountNrByAccountName)
            };
        };
        BookKeepingRulesEditController.prototype.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.edit = null;
        };
        BookKeepingRulesEditController.prototype.commitEdit = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var initialAccountNrByAccountName = this.m.accountNrByAccountName;
            var editedAccountNrByAccountName = this.m.edit.accountNrByAccountName;
            this.m.edit = null;
            var saves = [];
            for (var _i = 0, _a = this.m.accountNames; _i < _a.length; _i++) {
                var accountName = _a[_i];
                if (editedAccountNrByAccountName[accountName] !== initialAccountNrByAccountName[accountName]) {
                    saves.push(this.apiClient.keyValueStoreSet(accountName, 'BookKeepingAccountNrsV1', editedAccountNrByAccountName[accountName]));
                }
            }
            this.$q.all(saves).then(function (x) {
                _this.reload();
            });
        };
        BookKeepingRulesEditController.prototype.hasConnection = function (row, connectionName) {
            return row && row.Connections && row.Connections.indexOf(connectionName) >= 0;
        };
        BookKeepingRulesEditController.prototype.getRowAccountNr = function (row, isCredit) {
            var result = {
                isEdited: false,
                currentValue: null
            };
            if (!row || !this.m) {
                return result;
            }
            result.currentValue = isCredit ? row.CreditAccountNr : row.DebetAccountNr;
            var accountName = isCredit ? row.CreditAccountName : row.DebetAccountName;
            if (!this.m.edit || !accountName) {
                return result;
            }
            result.isEdited = this.m.accountNrByAccountName[accountName] !== this.m.edit.accountNrByAccountName[accountName];
            if (this.editform[accountName].$invalid) {
                result.currentValue = '-';
            }
            else {
                result.currentValue = this.m.edit.accountNrByAccountName[accountName];
            }
            return result;
        };
        BookKeepingRulesEditController.prototype.onImportTextChanged = function (importText) {
            if (!this.m || this.m.edit || !importText || importText.length < 5) {
                return;
            }
            if (importText.substr(0, 2) !== 'B_' || importText.substr(importText.length - 2, 2) !== '_B') {
                return;
            }
            var code = JSON.parse(atob(importText.substr(2, importText.length - 4)));
            var missingAccountNamesInImport = [];
            var extraAccountNamesInImport = [];
            this.beginEdit();
            for (var _i = 0, _a = this.m.accountNames; _i < _a.length; _i++) {
                var accountName = _a[_i];
                var importedAccountNr = code.accountNrByAccountName[accountName];
                if (importedAccountNr) {
                    this.m.edit.accountNrByAccountName[accountName] = importedAccountNr;
                }
                else {
                    missingAccountNamesInImport.push(accountName);
                }
            }
            for (var _b = 0, _c = code.accountNames; _b < _c.length; _b++) {
                var accountName = _c[_b];
                if (this.m.accountNames.indexOf(accountName) < 0) {
                    extraAccountNamesInImport.push(accountName);
                }
            }
            if (missingAccountNamesInImport.length > 0 || extraAccountNamesInImport.length > 0) {
                var stringJoin = function (a) {
                    var result = '';
                    for (var _i = 0, a_1 = a; _i < a_1.length; _i++) {
                        var s = a_1[_i];
                        if (result.length > 0) {
                            result += ', ';
                        }
                        result += s;
                    }
                };
                var warningMessage = '';
                if (missingAccountNamesInImport.length > 0) {
                    warningMessage += "These account names are in the import but not here: ".concat(stringJoin(missingAccountNamesInImport));
                }
                if (extraAccountNamesInImport.length > 0) {
                    warningMessage += "These account names are in the here but not in the import: ".concat(stringJoin(extraAccountNamesInImport));
                }
                toastr.warning(warningMessage);
            }
        };
        BookKeepingRulesEditController.$inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$scope'];
        return BookKeepingRulesEditController;
    }(NTechComponents.NTechComponentControllerBase));
    BookKeepingRulesEditComponentNs.BookKeepingRulesEditController = BookKeepingRulesEditController;
    var BookKeepingCode = /** @class */ (function () {
        function BookKeepingCode() {
        }
        return BookKeepingCode;
    }());
    BookKeepingRulesEditComponentNs.BookKeepingCode = BookKeepingCode;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    BookKeepingRulesEditComponentNs.Model = Model;
    var BookKeepingRulesEditComponent = /** @class */ (function () {
        function BookKeepingRulesEditComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = BookKeepingRulesEditController;
            this.templateUrl = 'bookkeeping-rules-edit.html';
        }
        return BookKeepingRulesEditComponent;
    }());
    BookKeepingRulesEditComponentNs.BookKeepingRulesEditComponent = BookKeepingRulesEditComponent;
})(BookKeepingRulesEditComponentNs || (BookKeepingRulesEditComponentNs = {}));
angular.module('ntech.components').component('bookkeepingRulesEdit', new BookKeepingRulesEditComponentNs.BookKeepingRulesEditComponent());
