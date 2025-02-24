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
var MortgageApplicationSearchComponentNs;
(function (MortgageApplicationSearchComponentNs) {
    var MortgageApplicationSearchController = /** @class */ (function (_super) {
        __extends(MortgageApplicationSearchController, _super);
        function MortgageApplicationSearchController($http, $q, ntechComponentService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$timeout = $timeout;
            return _this;
        }
        MortgageApplicationSearchController.prototype.componentName = function () {
            return 'mortgageApplicationSearch';
        };
        MortgageApplicationSearchController.prototype.onChanges = function () {
            this.searchResult = null;
            this.omniSearchValue = null;
        };
        MortgageApplicationSearchController.prototype.isGotoApplicationPossible = function (application) {
            return application && this.initialData && this.initialData.onGotoApplication;
        };
        MortgageApplicationSearchController.prototype.gotoApplication = function (application, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.$timeout(function () {
                if (_this.initialData && _this.initialData.onGotoApplication) {
                    _this.initialData.onGotoApplication(application);
                }
            });
        };
        MortgageApplicationSearchController.prototype.reset = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.onChanges();
        };
        MortgageApplicationSearchController.prototype.search = function (evt) {
            var _this = this;
            this.apiClient.searchForMortgageLoanApplicationOrLeadsByOmniValue(this.omniSearchValue).then(function (result) {
                var totalCount = (result.Applications ? result.Applications.length : 0) + (result.Leads ? result.Leads.length : 0);
                if (totalCount === 1) {
                    _this.gotoApplication(result.Applications && result.Applications.length > 0 ? result.Applications[0] : result.Leads[0], null);
                }
                else {
                    _this.searchResult = { totalCount: totalCount, applications: result.Applications, leads: result.Leads };
                }
            });
        };
        MortgageApplicationSearchController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout'];
        return MortgageApplicationSearchController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageApplicationSearchComponentNs.MortgageApplicationSearchController = MortgageApplicationSearchController;
    var MortgageApplicationSearchComponent = /** @class */ (function () {
        function MortgageApplicationSearchComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageApplicationSearchController;
            this.templateUrl = 'mortgage-application-search.html';
        }
        return MortgageApplicationSearchComponent;
    }());
    MortgageApplicationSearchComponentNs.MortgageApplicationSearchComponent = MortgageApplicationSearchComponent;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageApplicationSearchComponentNs.InitialData = InitialData;
    var SearchResultModel = /** @class */ (function () {
        function SearchResultModel() {
        }
        return SearchResultModel;
    }());
    MortgageApplicationSearchComponentNs.SearchResultModel = SearchResultModel;
})(MortgageApplicationSearchComponentNs || (MortgageApplicationSearchComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationSearch', new MortgageApplicationSearchComponentNs.MortgageApplicationSearchComponent());
