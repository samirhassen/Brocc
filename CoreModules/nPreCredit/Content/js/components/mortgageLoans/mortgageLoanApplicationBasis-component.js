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
var MortgageLoanApplicationBasisComponentNs;
(function (MortgageLoanApplicationBasisComponentNs) {
    var MortgageLoanApplicationBasisController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationBasisController, _super);
        function MortgageLoanApplicationBasisController($http, $q, ntechComponentService) {
            return _super.call(this, ntechComponentService, $http, $q) || this;
        }
        MortgageLoanApplicationBasisController.prototype.componentName = function () {
            return 'mortgageLoanApplicationBasis';
        };
        MortgageLoanApplicationBasisController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchMortgageLoanApplicationBasisCurrentValues(this.initialData.applicationInfo.ApplicationNr).then(function (x) {
                _this.m = {
                    onBack: (_this.initialData.onBack || _this.initialData.backUrl) ? (function (evt) {
                        if (evt) {
                            evt.preventDefault();
                        }
                        if (_this.initialData.onBack) {
                            _this.initialData.onBack(_this.m.wasChanged);
                        }
                        else if (_this.initialData.backUrl) {
                            document.location.href = _this.initialData.backUrl;
                        }
                    }) : null,
                    current: x,
                    wasChanged: false
                };
            });
        };
        MortgageLoanApplicationBasisController.prototype.editHouseholdIncome = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.initialData == null || this.m == null) {
                return;
            }
            this.m.editMode = 'householdIncome';
            this.m.householdIncomeInitialData = {
                onBack: function (newCombinedGrossMonthlyIncome) {
                    if (newCombinedGrossMonthlyIncome !== null) {
                        _this.m.wasChanged = true;
                        _this.m.current.CombinedGrossMonthlyIncome = newCombinedGrossMonthlyIncome;
                    }
                    _this.m.editMode = null;
                    _this.m.householdIncomeInitialData = null;
                },
                applicationInfo: this.initialData.applicationInfo
            };
        };
        MortgageLoanApplicationBasisController.$inject = ['$http', '$q', 'ntechComponentService'];
        return MortgageLoanApplicationBasisController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationBasisComponentNs.MortgageLoanApplicationBasisController = MortgageLoanApplicationBasisController;
    var MortgageLoanApplicationBasisComponent = /** @class */ (function () {
        function MortgageLoanApplicationBasisComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationBasisController;
            this.templateUrl = 'mortgage-loan-application-basis.html';
        }
        return MortgageLoanApplicationBasisComponent;
    }());
    MortgageLoanApplicationBasisComponentNs.MortgageLoanApplicationBasisComponent = MortgageLoanApplicationBasisComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanApplicationBasisComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationBasisComponentNs.Model = Model;
})(MortgageLoanApplicationBasisComponentNs || (MortgageLoanApplicationBasisComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationBasis', new MortgageLoanApplicationBasisComponentNs.MortgageLoanApplicationBasisComponent());
