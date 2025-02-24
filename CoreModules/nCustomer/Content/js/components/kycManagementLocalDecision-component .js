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
var KycManagementLocalDecisionComponentNs;
(function (KycManagementLocalDecisionComponentNs) {
    var KycManagementLocalDecisionController = /** @class */ (function (_super) {
        __extends(KycManagementLocalDecisionController, _super);
        function KycManagementLocalDecisionController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        KycManagementLocalDecisionController.prototype.componentName = function () {
            return 'kycManagementLocalDecision';
        };
        KycManagementLocalDecisionController.prototype.onChanges = function () {
            this.setup(null);
        };
        KycManagementLocalDecisionController.prototype.setup = function (currentData) {
            var _this = this;
            this.m = null;
            this.setEditModel(null);
            if (!this.initialData) {
                return;
            }
            var withCd = function (x) {
                _this.m = {
                    localIsPep: x.IsPep,
                    localIsSanction: x.IsSanction,
                    amlRiskClass: x.AmlRiskClass
                };
            };
            if (currentData) {
                withCd(currentData);
            }
            else {
                this.apiClient.kycManagementFetchLocalDecisionData(this.initialData.customerId).then(function (result) {
                    withCd(result);
                });
            }
        };
        KycManagementLocalDecisionController.prototype.getKycStateDisplayName = function (b) {
            if (b === true) {
                return 'Yes';
            }
            else if (b === false) {
                return 'No';
            }
            else {
                return 'Unknown';
            }
        };
        KycManagementLocalDecisionController.prototype.setEditModel = function (e) {
            var _this = this;
            if (this.m) {
                this.m.editModel = e;
            }
            if (this.modeChanged) {
                var isEditMode = !!e;
                if (isEditMode) {
                    this.modeChanged({
                        isEditMode: isEditMode,
                        cancelEdit: function (evt) { return _this.cancelEdit(evt); },
                        isEditingPep: e.isEditingPep
                    });
                }
                else {
                    this.modeChanged({ isEditMode: false, isEditingPep: null, cancelEdit: null });
                }
            }
        };
        KycManagementLocalDecisionController.prototype.edit = function (isEditingPep, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.apiClient.kycManagementFetchLocalDecisionHistoryData(this.initialData.customerId, isEditingPep).then(function (result) {
                var historicalValues = [];
                if (result.CurrentValue) {
                    historicalValues.push(result.CurrentValue);
                }
                if (result.HistoricalValues) {
                    for (var _i = 0, _a = result.HistoricalValues; _i < _a.length; _i++) {
                        var h = _a[_i];
                        historicalValues.push(h);
                    }
                }
                _this.setEditModel({
                    currentState: result.CurrentValue ? _this.boolToString(result.CurrentValue.Value) : _this.boolToString(null),
                    isEditingPep: result.IsModellingPep,
                    historicalValues: historicalValues
                });
            });
        };
        KycManagementLocalDecisionController.prototype.editAmlRiskClass = function (evt) {
            var _this = this;
            evt === null || evt === void 0 ? void 0 : evt.preventDefault();
            this.m.editAmlRiskModel = {
                customerId: this.initialData.customerId,
                itemName: 'amlRiskClass',
                onClose: function () {
                    _this.setup(null);
                },
                hideHeader: true
            };
        };
        KycManagementLocalDecisionController.prototype.saveEdit = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m || !this.m.editModel) {
                return;
            }
            var newLocalValue = this.stringToBool(this.m.editModel.currentState);
            if (newLocalValue !== true && newLocalValue !== false) {
                toastr.warning('Cannot change back to unknown');
                return;
            }
            this.apiClient.kycManagementSetLocalDecision(this.initialData.customerId, this.m.editModel.isEditingPep, newLocalValue, true).then(function (result) {
                _this.setup(result.NewCurrentData);
            });
        };
        KycManagementLocalDecisionController.prototype.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.setEditModel(null);
        };
        KycManagementLocalDecisionController.prototype.boolToString = function (b) {
            if (b === true) {
                return 'true';
            }
            else if (b === false) {
                return 'false';
            }
            else {
                return '';
            }
        };
        KycManagementLocalDecisionController.prototype.stringToBool = function (s) {
            if (s === 'true') {
                return true;
            }
            else if (s === 'false') {
                return false;
            }
            else {
                return null;
            }
        };
        KycManagementLocalDecisionController.$inject = ['$http', '$q', 'ntechComponentService'];
        return KycManagementLocalDecisionController;
    }(NTechComponents.NTechComponentControllerBase));
    KycManagementLocalDecisionComponentNs.KycManagementLocalDecisionController = KycManagementLocalDecisionController;
    var KycManagementLocalDecisionComponent = /** @class */ (function () {
        function KycManagementLocalDecisionComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<',
                modeChanged: '<'
            };
            this.controller = KycManagementLocalDecisionController;
            this.templateUrl = 'kyc-management-local-decision.html';
        }
        return KycManagementLocalDecisionComponent;
    }());
    KycManagementLocalDecisionComponentNs.KycManagementLocalDecisionComponent = KycManagementLocalDecisionComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    KycManagementLocalDecisionComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    KycManagementLocalDecisionComponentNs.Model = Model;
    var EditModel = /** @class */ (function () {
        function EditModel() {
        }
        return EditModel;
    }());
    KycManagementLocalDecisionComponentNs.EditModel = EditModel;
})(KycManagementLocalDecisionComponentNs || (KycManagementLocalDecisionComponentNs = {}));
angular.module('ntech.components').component('kycManagementLocalDecision', new KycManagementLocalDecisionComponentNs.KycManagementLocalDecisionComponent());
