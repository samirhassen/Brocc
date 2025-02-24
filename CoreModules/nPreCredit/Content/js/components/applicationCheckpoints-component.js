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
var ApplicationCheckpointsComponentNs;
(function (ApplicationCheckpointsComponentNs) {
    var ApplicationCheckpointsController = /** @class */ (function (_super) {
        __extends(ApplicationCheckpointsController, _super);
        function ApplicationCheckpointsController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        ApplicationCheckpointsController.prototype.componentName = function () {
            return 'applicationCheckpoints';
        };
        ApplicationCheckpointsController.prototype.onChanges = function () {
            var _this = this;
            this.checkpoints = null;
            if (this.initialData) {
                this.apiClient.fetchAllCheckpointsForApplication(this.initialData.applicationNr, this.initialData.applicationType).then(function (result) {
                    for (var _i = 0, result_1 = result; _i < result_1.length; _i++) {
                        var c = result_1[_i];
                        c.isExpanded = true;
                    }
                    _this.checkpoints = result;
                });
            }
        };
        ApplicationCheckpointsController.prototype.getRoleDisplayName = function (roleName) {
            if (roleName === 'List_companyLoanAuthorizedSignatory') {
                return 'Authorized signatory';
            }
            else if (roleName === 'List_companyLoanCollateral') {
                return 'Collateral';
            }
            else if (roleName === 'List_companyLoanBeneficialOwner') {
                return 'Beneficial owner';
            }
            else {
                return roleName;
            }
        };
        ApplicationCheckpointsController.prototype.unlockCheckpointReasonText = function (checkpoint, event) {
            if (event) {
                event.preventDefault();
            }
            this.apiClient.fetchCheckpointReasonText(checkpoint.checkpointId).then(function (reasonText) {
                checkpoint.reasonText = reasonText;
                checkpoint.isReasonTextLoaded = true;
                checkpoint.isExpanded = true;
            });
        };
        ApplicationCheckpointsController.$inject = ['$http', '$q', 'ntechComponentService'];
        return ApplicationCheckpointsController;
    }(NTechComponents.NTechComponentControllerBase));
    ApplicationCheckpointsComponentNs.ApplicationCheckpointsController = ApplicationCheckpointsController;
    var ApplicationCheckpointsComponent = /** @class */ (function () {
        function ApplicationCheckpointsComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = ApplicationCheckpointsController;
            this.templateUrl = 'application-checkpoints.html';
        }
        return ApplicationCheckpointsComponent;
    }());
    ApplicationCheckpointsComponentNs.ApplicationCheckpointsComponent = ApplicationCheckpointsComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    ApplicationCheckpointsComponentNs.InitialData = InitialData;
})(ApplicationCheckpointsComponentNs || (ApplicationCheckpointsComponentNs = {}));
angular.module('ntech.components').component('applicationCheckpoints', new ApplicationCheckpointsComponentNs.ApplicationCheckpointsComponent());
