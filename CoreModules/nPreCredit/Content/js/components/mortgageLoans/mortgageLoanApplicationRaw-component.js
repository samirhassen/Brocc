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
var MortgageLoanApplicationRawComponentNs;
(function (MortgageLoanApplicationRawComponentNs) {
    var MortgageApplicationRawController = /** @class */ (function (_super) {
        __extends(MortgageApplicationRawController, _super);
        function MortgageApplicationRawController($http, $q, ntechComponentService, modalDialogService) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.modalDialogService = modalDialogService;
            return _this;
        }
        MortgageApplicationRawController.prototype.componentName = function () {
            return 'mortgageLoanApplicationRaw';
        };
        MortgageApplicationRawController.prototype.onChanges = function () {
            var _this = this;
            if (!this.initialData) {
                return;
            }
            var ai = this.initialData.applicationInfo;
            this.apiClient.fetchCreditApplicationItemSimple(ai.ApplicationNr, ['*'], '').then(function (x) {
                _this.m = {
                    application: x
                };
            });
        };
        MortgageApplicationRawController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService'];
        return MortgageApplicationRawController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationRawComponentNs.MortgageApplicationRawController = MortgageApplicationRawController;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationRawComponentNs.Model = Model;
    var MortgageLoanApplicationRawComponent = /** @class */ (function () {
        function MortgageLoanApplicationRawComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationRawController;
            this.template = "<div ng-if=\"$ctrl.m\"><pre>{{$ctrl.m.application | json}}</pre></div>";
        }
        return MortgageLoanApplicationRawComponent;
    }());
    MortgageLoanApplicationRawComponentNs.MortgageLoanApplicationRawComponent = MortgageLoanApplicationRawComponent;
})(MortgageLoanApplicationRawComponentNs || (MortgageLoanApplicationRawComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationRaw', new MortgageLoanApplicationRawComponentNs.MortgageLoanApplicationRawComponent());
