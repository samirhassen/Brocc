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
var ApplicationDataEditorComponentNs;
(function (ApplicationDataEditorComponentNs) {
    function isNumberType(dt) {
        var d = (dt ? dt : '').toLowerCase();
        return d.indexOf('int') >= 0 || d.indexOf('decimal') >= 0;
    }
    ApplicationDataEditorComponentNs.isNumberType = isNumberType;
    function isIntegerType(dt) {
        return isNumberType(dt) && dt.toLowerCase().indexOf('int') > 0;
    }
    ApplicationDataEditorComponentNs.isIntegerType = isIntegerType;
    function getDropdownDisplayValue(value, m) {
        if (!m || !m.DropdownRawOptions || !m.DropdownRawDisplayTexts) {
            return value;
        }
        for (var i = 0; i < m.DropdownRawOptions.length; i++) {
            if (m.DropdownRawOptions[i] === value && m.DropdownRawDisplayTexts.length > i) {
                return m.DropdownRawDisplayTexts[i];
            }
        }
        return value;
    }
    ApplicationDataEditorComponentNs.getDropdownDisplayValue = getDropdownDisplayValue;
    var ApplicationDataEditorController = /** @class */ (function (_super) {
        __extends(ApplicationDataEditorController, _super);
        function ApplicationDataEditorController($http, $q, ntechComponentService, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$http = $http;
            _this.$q = $q;
            _this.$scope = $scope;
            _this.form = function () {
                if (!_this.$scope) {
                    return null;
                }
                var c = _this.$scope['formContainer'];
                if (!c) {
                    return null;
                }
                return c['editValueForm'];
            };
            _this.$scope['formContainer'] = {};
            return _this;
        }
        ApplicationDataEditorController.prototype.componentName = function () {
            return 'applicationDataEditor';
        };
        ApplicationDataEditorController.prototype.isReadOnly = function () {
            if (!this.m) {
                return true;
            }
            return this.initialData.isReadOnly;
        };
        ApplicationDataEditorController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(this.m.BackTarget, this.apiClient, this.$q, { applicationNr: this.initialData.applicationNr });
        };
        ApplicationDataEditorController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var i = this.initialData;
            var backTarget = i && i.backTarget ? NavigationTargetHelper.createCodeTarget(i.backTarget) : (i.backUrl ? NavigationTargetHelper.createUrlTarget(i.backUrl) : null);
            this.m = {
                HistoricalChanges: null,
                EditorInitialData: null,
                EditModel: null,
                BackTarget: backTarget
            };
            this.m.EditorInitialData = ApplicationEditorComponentNs.createInitialData(i.applicationNr, i.applicationType, backTarget, this.apiClient, this.$q, function (x) {
                x.addDataSourceItem(i.dataSourceName, i.itemName, i.isReadOnly, true);
            }, {
                afterInPlaceEditsCommited: function (commitedEdits) {
                    _this.reloadHistory();
                },
                afterDataLoaded: function (data) {
                    _this.m.EditModel = data.Results[0].Items[0].EditorModel;
                    _this.reloadHistory();
                },
                isInPlaceEditAllowed: !i.isReadOnly
            });
        };
        ApplicationDataEditorController.prototype.reloadHistory = function () {
            var _this = this;
            var i = this.initialData;
            this.apiClient.fetchApplicationEditItemData(i.applicationNr, i.dataSourceName, i.itemName, ApplicationDataSourceHelper.MissingItemReplacementValue, true).then(function (edits) {
                _this.m.HistoricalChanges = edits.HistoricalChanges;
            });
        };
        ApplicationDataEditorController.prototype.parseHistoryValue = function (v) {
            var _this = this;
            if (v === '-') {
                return null;
            }
            return ApplicationItemEditorComponentNs.getItemDisplayValueShared(v, this.m.EditModel, function (x) { return _this.parseDecimalOrNull(x); });
        };
        ApplicationDataEditorController.prototype.getUserDisplayName = function (userId) {
            if (!this.initialData || !this.initialData.userDisplayNameByUserId[userId.toString()]) {
                return 'User ' + userId;
            }
            else {
                return this.initialData.userDisplayNameByUserId[userId.toString()];
            }
        };
        ApplicationDataEditorController.$inject = ['$http', '$q', 'ntechComponentService', '$scope'];
        return ApplicationDataEditorController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationDataEditorComponentNs.ApplicationDataEditorController = ApplicationDataEditorController;
    var ApplicationDataEditorComponent = /** @class */ (function () {
        function ApplicationDataEditorComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationDataEditorController;
            this.templateUrl = 'application-data-editor.html';
        }
        return ApplicationDataEditorComponent;
    }());
    ApplicationDataEditorComponentNs.ApplicationDataEditorComponent = ApplicationDataEditorComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    ApplicationDataEditorComponentNs.Model = Model;
})(ApplicationDataEditorComponentNs || (ApplicationDataEditorComponentNs = {}));
angular.module('ntech.components').component('applicationDataEditor', new ApplicationDataEditorComponentNs.ApplicationDataEditorComponent());
