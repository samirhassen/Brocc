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
var __spreadArray = (this && this.__spreadArray) || function (to, from, pack) {
    if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
        if (ar || !(i in from)) {
            if (!ar) ar = Array.prototype.slice.call(from, 0, i);
            ar[i] = from[i];
        }
    }
    return to.concat(ar || Array.prototype.slice.call(from));
};
var MortgageLoanApplicationCollateralComponentNs;
(function (MortgageLoanApplicationCollateralComponentNs) {
    function createNewCollateral(applicationNr, nr, apiClient) {
        var itemName = MortgageApplicationCollateralEditComponentNs.getDataSourceItemName(nr.toString(), 'exists', ComplexApplicationListHelper.RepeatableCode.No);
        return apiClient.setApplicationEditItemData(applicationNr, 'ComplexApplicationList', itemName, 'true', false).then(function (x) {
        });
    }
    MortgageLoanApplicationCollateralComponentNs.createNewCollateral = createNewCollateral;
    function getAdditionalCollateralNrs(applicationNr, apiClient) {
        return ComplexApplicationListHelper.getNrs(applicationNr, MortgageApplicationCollateralEditComponentNs.ListName, apiClient).then(function (x) { return NTechLinq.where(x, function (y) { return y !== 1; }); });
    }
    MortgageLoanApplicationCollateralComponentNs.getAdditionalCollateralNrs = getAdditionalCollateralNrs;
    var MortgageLoanApplicationCollateralController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationCollateralController, _super);
        function MortgageLoanApplicationCollateralController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageLoanApplicationCollateralController.prototype.componentName = function () {
            return 'mortgageLoanApplicationCollateral';
        };
        MortgageLoanApplicationCollateralController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData || !this.initialData.applicationInfo) {
                return;
            }
            var i = this.initialData;
            var ai = i.applicationInfo;
            var wf = this.initialData.workflowModel;
            var isEditAllowed = ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement;
            getAdditionalCollateralNrs(ai.ApplicationNr, this.apiClient).then(function (x) {
                _this.m = {
                    isEditAllowed: ai.IsActive && !ai.IsFinalDecisionMade && !ai.HasLockedAgreement,
                    isToggleAcceptedAllowed: wf.areAllStepBeforeThisAccepted(ai) && wf.areAllStepsAfterInitial(ai) && isEditAllowed,
                    isStepAccepted: wf.isStatusAccepted(ai),
                    objectCollateralData: {
                        applicationNr: ai.ApplicationNr,
                        allowDelete: isEditAllowed,
                        allowViewDetails: true,
                        onlyMainCollateral: true,
                        onlyOtherCollaterals: false,
                        viewDetailsUrlTargetCode: NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication
                    },
                    otherCollateralData: {
                        applicationNr: ai.ApplicationNr,
                        allowDelete: isEditAllowed,
                        allowViewDetails: true,
                        onlyMainCollateral: false,
                        onlyOtherCollaterals: true,
                        viewDetailsUrlTargetCode: NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplication
                    }
                };
            });
        };
        MortgageLoanApplicationCollateralController.prototype.addCollateral = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var currentMax = 1;
            var ai = this.initialData.applicationInfo;
            ComplexApplicationListHelper.getNrs(ai.ApplicationNr, MortgageApplicationCollateralEditComponentNs.ListName, this.apiClient).then(function (nrs) {
                var currentMax = Math.max.apply(Math, __spreadArray(__spreadArray([], nrs, false), [1], false));
                createNewCollateral(_this.initialData.applicationInfo.ApplicationNr, currentMax + 1, _this.apiClient).then(function (x) {
                    _this.signalReloadRequired();
                });
            });
        };
        MortgageLoanApplicationCollateralController.prototype.toggleStepAccepted = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            var i = this.initialData;
            var ai = i.applicationInfo;
            var wf = this.initialData.workflowModel;
            this.initialData.apiClient.setMortgageApplicationWorkflowStatus(ai.ApplicationNr, wf.stepName, wf.isStatusAccepted(ai) ? 'Initial' : 'Accepted').then(function () {
                _this.signalReloadRequired();
            });
        };
        MortgageLoanApplicationCollateralController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageLoanApplicationCollateralController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationCollateralComponentNs.MortgageLoanApplicationCollateralController = MortgageLoanApplicationCollateralController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationCollateralComponentNs.Model = Model;
    var MortgageLoanApplicationCollateralComponent = /** @class */ (function () {
        function MortgageLoanApplicationCollateralComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationCollateralController;
            this.template = "<div ng-if=\"$ctrl.m\">\n\n    <div class=\"row\">\n        <div class=\"col-sm-12\">\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.objectCollateralData\"></mortgage-loan-dual-collateral-compact>\n        </div>\n    </div>\n\n    <div>\n        <h3>Other properties</h3>\n        <hr class=\"hr-section\" />\n        <button class=\"n-direct-btn n-green-btn\" ng-click=\"$ctrl.addCollateral($event)\"\n            ng-if=\"$ctrl.m.isEditAllowed\">Add</button>\n    </div>\n    <hr class=\"hr-section dotted\" />\n\n    <div class=\"row\">\n        <div class=\"col-sm-12\">\n            <mortgage-loan-dual-collateral-compact initial-data=\"$ctrl.m.otherCollateralData\"></mortgage-loan-dual-collateral-compact>\n        </div>\n    </div>\n\n    <div class=\"pt-3\" ng-show=\"$ctrl.m.isToggleAcceptedAllowed\">\n        <label class=\"pr-2\">Collateral {{$ctrl.m.isStepAccepted ? 'done' : 'not done'}}</label>\n        <label class=\"n-toggle\">\n            <input type=\"checkbox\" ng-checked=\"$ctrl.m.isStepAccepted\" ng-click=\"$ctrl.toggleStepAccepted($event)\" />\n            <span class=\"n-slider\"></span>\n        </label>\n    </div>\n\n</div>";
        }
        return MortgageLoanApplicationCollateralComponent;
    }());
    MortgageLoanApplicationCollateralComponentNs.MortgageLoanApplicationCollateralComponent = MortgageLoanApplicationCollateralComponent;
})(MortgageLoanApplicationCollateralComponentNs || (MortgageLoanApplicationCollateralComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationCollateral', new MortgageLoanApplicationCollateralComponentNs.MortgageLoanApplicationCollateralComponent());
