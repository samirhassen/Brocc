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
var MortgageLoanApplicationDualSettlementComponentNs;
(function (MortgageLoanApplicationDualSettlementComponentNs) {
    var MortgageLoanApplicationDualSettlementController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationDualSettlementController, _super);
        function MortgageLoanApplicationDualSettlementController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageLoanApplicationDualSettlementController.prototype.componentName = function () {
            return 'mortgageLoanApplicationDualSettlement';
        };
        MortgageLoanApplicationDualSettlementController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanApplicationDualSettlementController.prototype.reload = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            var areAllStepBeforeThisAccepted = this.initialData.workflowModel.areAllStepBeforeThisAccepted(ai);
            if (!areAllStepBeforeThisAccepted) {
                this.m = {
                    isHandleAllowed: areAllStepBeforeThisAccepted && ai.HasLockedAgreement
                };
            }
            else {
                this.apiClient.fetchItemBasedCreditDecision({
                    ApplicationNr: ai.ApplicationNr,
                    MustBeCurrent: true,
                    MustBeAccepted: true,
                    MaxCount: 1
                }).then(function (decisions) {
                    var decision = decisions.Decisions[0];
                    _this.m = {
                        decision: {
                            applicationType: decision.UniqueItems['applicationType']
                        },
                        isHandleAllowed: areAllStepBeforeThisAccepted && ai.HasLockedAgreement
                    };
                });
            }
        };
        MortgageLoanApplicationDualSettlementController.prototype.handle = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.getUserModuleUrl('nPreCredit', 'Ui/MortgageLoan/Handle-Settlement', {
                applicationNr: this.initialData.applicationInfo.ApplicationNr,
                backUrl: this.initialData.urlToHereFromOtherModule
            }).then(function (x) {
                document.location.href = x.UrlExternal;
            });
        };
        MortgageLoanApplicationDualSettlementController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationDualSettlementController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationDualSettlementComponentNs.MortgageLoanApplicationDualSettlementController = MortgageLoanApplicationDualSettlementController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationDualSettlementComponentNs.Model = Model;
    var MortgageLoanApplicationDualSettlementComponent = /** @class */ (function () {
        function MortgageLoanApplicationDualSettlementComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationDualSettlementController;
            this.template = "<div class=\"container\" ng-if=\"$ctrl.m\">\n\n        <div ng-if=\"$ctrl.m.decision\">\n            <div>\n                <div class=\"row\">\n                    <div class=\"col-xs-6\">\n                        <div class=\"form-horizontal\">\n                            <div class=\"form-group\">\n                                <label class=\"col-xs-6 control-label\">Type</label>\n                                <div class=\"col-xs-6 form-control-static\">{{$ctrl.m.decision.applicationType}}</div>\n                            </div>\n                        </div>\n                    </div>\n                    <div class=\"col-xs-6\">\n\n                    </div>\n                </div>\n            </div>\n        </div>\n\n        <div class=\"pt-3 text-center\" ng-if=\"$ctrl.m.isHandleAllowed\">\n            <a class=\"n-main-btn n-blue-btn\" ng-click=\"$ctrl.handle($event)\">\n                Handle payments and settlement <span class=\"glyphicon glyphicon-arrow-right\"></span>\n            </a>\n        </div>\n</div>";
        }
        return MortgageLoanApplicationDualSettlementComponent;
    }());
    MortgageLoanApplicationDualSettlementComponentNs.MortgageLoanApplicationDualSettlementComponent = MortgageLoanApplicationDualSettlementComponent;
})(MortgageLoanApplicationDualSettlementComponentNs || (MortgageLoanApplicationDualSettlementComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationDualSettlement', new MortgageLoanApplicationDualSettlementComponentNs.MortgageLoanApplicationDualSettlementComponent());
