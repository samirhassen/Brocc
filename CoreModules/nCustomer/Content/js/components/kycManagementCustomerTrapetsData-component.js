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
var KycManagementCustomerTrapetsDataComponentNs;
(function (KycManagementCustomerTrapetsDataComponentNs) {
    var KycManagementCustomerTrapetsDataController = /** @class */ (function (_super) {
        __extends(KycManagementCustomerTrapetsDataController, _super);
        function KycManagementCustomerTrapetsDataController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        KycManagementCustomerTrapetsDataController.prototype.componentName = function () {
            return 'kycManagementCustomerTrapetsData';
        };
        KycManagementCustomerTrapetsDataController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (this.initialData == null) {
                return;
            }
            this.apiClient.fetchLatestTrapetsQueryResult(this.initialData.customerId).then(function (result) {
                _this.m = {
                    latestTrapetsResult: result,
                    historySummary: null,
                    showHistoryDialogId: _this.modalDialogService.generateDialogId()
                };
            });
        };
        KycManagementCustomerTrapetsDataController.prototype.showHistory = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.apiClient.fetchTrapetsQueryHistorySummary(this.initialData.customerId).then(function (result) {
                _this.apiClient.fetchQueryResultHistoryDetails(_this.initialData.customerId, 60).then(function (x) {
                    _this.m.historySummary = result;
                    _this.m.historicalExternalIds = x.QueryDatesWithListHits;
                    _this.modalDialogService.openDialog(_this.m.showHistoryDialogId);
                });
            });
        };
        KycManagementCustomerTrapetsDataController.prototype.showLatestDetails = function (type, evt) {
            var _this = this;
            evt === null || evt === void 0 ? void 0 : evt.preventDefault();
            var handleEmpty = function (arr) { return !arr || arr.length === 0 ? ['-'] : arr; };
            this.apiClient.kycManagementQueryDetails(this.m.latestTrapetsResult.Id).then(function (x) {
                if (type === 'sanction') {
                    _this.m.latestSanctionExternalIds = handleEmpty(x.SactionExternalIds);
                }
                else if (type === 'pep') {
                    _this.m.latestPepExternalIds = handleEmpty(x.PepExternalIds);
                }
            });
        };
        KycManagementCustomerTrapetsDataController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return KycManagementCustomerTrapetsDataController;
    }(NTechComponents.NTechComponentControllerBase));
    KycManagementCustomerTrapetsDataComponentNs.KycManagementCustomerTrapetsDataController = KycManagementCustomerTrapetsDataController;
    var KycManagementCustomerTrapetsDataComponent = /** @class */ (function () {
        function KycManagementCustomerTrapetsDataComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = KycManagementCustomerTrapetsDataController;
            this.templateUrl = 'kyc-management-customer-trapets-data.html';
        }
        return KycManagementCustomerTrapetsDataComponent;
    }());
    KycManagementCustomerTrapetsDataComponentNs.KycManagementCustomerTrapetsDataComponent = KycManagementCustomerTrapetsDataComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    KycManagementCustomerTrapetsDataComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    KycManagementCustomerTrapetsDataComponentNs.Model = Model;
})(KycManagementCustomerTrapetsDataComponentNs || (KycManagementCustomerTrapetsDataComponentNs = {}));
angular.module('ntech.components').component('kycManagementCustomerTrapetsData', new KycManagementCustomerTrapetsDataComponentNs.KycManagementCustomerTrapetsDataComponent());
