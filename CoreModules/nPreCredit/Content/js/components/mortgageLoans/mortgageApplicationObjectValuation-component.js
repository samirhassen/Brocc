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
var MortgageApplicationObjectValuationComponentNs;
(function (MortgageApplicationObjectValuationComponentNs) {
    var MortgageApplicationObjectValuationController = /** @class */ (function (_super) {
        __extends(MortgageApplicationObjectValuationController, _super);
        function MortgageApplicationObjectValuationController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageApplicationObjectValuationController.prototype.componentName = function () {
            return 'mortgageApplicationObjectValuation';
        };
        MortgageApplicationObjectValuationController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (this.initialData == null) {
                return;
            }
            this.apiClient.fetchMortgageApplicationValuationStatus(this.initialData.applicationInfo.ApplicationNr, this.initialData.backUrl, false).then(function (result) {
                var ai = _this.initialData.applicationInfo;
                _this.m = {
                    valuation: result,
                    isNewMortgageApplicationValuationPossible: result.IsNewMortgageApplicationValuationAllowed && ai.IsActive,
                    isReadOnly: !ai.IsActive,
                    twoColumns: true,
                    stepStatus: _this.initialData.workflowModel.getStepStatus(ai)
                };
            });
        };
        MortgageApplicationObjectValuationController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageApplicationObjectValuationController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationObjectValuationComponentNs.MortgageApplicationObjectValuationController = MortgageApplicationObjectValuationController;
    var MortgageApplicationObjectValuationComponent = /** @class */ (function () {
        function MortgageApplicationObjectValuationComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationObjectValuationController;
            this.templateUrl = 'mortgage-application-object-valuation.html';
        }
        return MortgageApplicationObjectValuationComponent;
    }());
    MortgageApplicationObjectValuationComponentNs.MortgageApplicationObjectValuationComponent = MortgageApplicationObjectValuationComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageApplicationObjectValuationComponentNs.Model = Model;
})(MortgageApplicationObjectValuationComponentNs || (MortgageApplicationObjectValuationComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationObjectValuation', new MortgageApplicationObjectValuationComponentNs.MortgageApplicationObjectValuationComponent());
