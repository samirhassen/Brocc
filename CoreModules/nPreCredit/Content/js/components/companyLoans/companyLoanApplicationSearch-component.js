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
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
var CompanyLoanApplicationSearchComponentNs;
(function (CompanyLoanApplicationSearchComponentNs) {
    var PageSize = 15;
    var FilterLocalStorageName = 'filter';
    var CompanyLoanApplicationSearchController = /** @class */ (function (_super) {
        __extends(CompanyLoanApplicationSearchController, _super);
        function CompanyLoanApplicationSearchController($http, $q, ntechComponentService, $timeout, ntechLocalStorageService, $translate) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.$timeout = $timeout;
            _this.ntechLocalStorageService = ntechLocalStorageService;
            _this.$translate = $translate;
            _this.pagingHelper = new NTechTables.PagingHelper($q, $http);
            return _this;
        }
        CompanyLoanApplicationSearchController.prototype.componentName = function () {
            return 'companyLoanApplicationSearch';
        };
        CompanyLoanApplicationSearchController.prototype.onChanges = function () {
            var _this = this;
            this.localStorage = this.ntechLocalStorageService.getUserContainer('CompanyLoanApplicationSearch', this.initialData.currentUserId, '20191023.1'); //Changes version if the format of local storage data is ever changed to prevent old data from being used
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.initialData.companyLoanApiClient.fetchApplicationWorkflowStepNames(true).then(function (x) {
                var s = _this.localStorage.get(FilterLocalStorageName);
                _this.m = {
                    searchResult: null,
                    listName: s ? s.listName : null,
                    omniSearchValue: null,
                    providerName: s ? s.providerName : null,
                    selectedSearchHit: null,
                    listCountsByName: null,
                    providers: x.Affiliates,
                    steps: []
                };
                for (var _i = 0, _a = x.StepNames; _i < _a.length; _i++) {
                    var stepName = _a[_i];
                    _this.m.steps.push({
                        initialListName: "".concat(stepName, "_Initial"),
                        stepName: stepName
                    });
                }
                _this.search(s ? s.pageNr : 0, null);
            });
        };
        CompanyLoanApplicationSearchController.prototype.showOptions = function (application, evt) {
            if (evt) {
                evt.preventDefault();
            }
            this.m.selectedSearchHit = {
                isDialogOpen: true,
                application: application
            };
        };
        CompanyLoanApplicationSearchController.prototype.getApplicationUrl = function (application) {
            if (!application) {
                return null;
            }
            return "/Ui/CompanyLoan/Application?applicationNr=".concat(encodeURIComponent(application.ApplicationNr), "&backTarget=").concat(NavigationTargetHelper.createCrossModule('CompanyLoanSearch', {}).targetCode);
        };
        CompanyLoanApplicationSearchController.prototype.reset = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.m.omniSearchValue = null;
            this.search(0, null);
        };
        CompanyLoanApplicationSearchController.prototype.search = function (pageNr, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.m.selectedSearchHit = null;
            var searchFilter = this.m.omniSearchValue
                ? null
                : { listName: this.m.listName, providerName: this.m.providerName, pageNr: pageNr };
            if (this.m.omniSearchValue) {
                this.initialData.companyLoanApiClient.searchForCompanyLoanApplicationByOmniValue(this.m.omniSearchValue, true).then(function (result) {
                    _this.cacheSearchFilter(searchFilter);
                    if (result && result.Applications && result.Applications.length === 1) {
                        _this.m.searchResult = null;
                        _this.initialData.setIsLoading(true);
                        _this.$timeout(function () {
                            document.location.href = _this.getApplicationUrl(result.Applications[0]);
                        });
                    }
                    else {
                        _this.m.searchResult = {
                            items: result.Applications,
                            paging: null
                        };
                    }
                });
            }
            else {
                this.initialData.companyLoanApiClient.fetchCompanyLoanWorkListDataPage(this.m.providerName, this.m.listName, this.initialData.isTest, true, PageSize, pageNr).then(function (x) {
                    _this.cacheSearchFilter(searchFilter);
                    _this.m.searchResult = {
                        items: x.PageApplications,
                        paging: _this.pagingHelper.createPagingObjectFromPageResult(x)
                    };
                    _this.m.listCountsByName = x.ListCountsByName;
                });
            }
        };
        CompanyLoanApplicationSearchController.prototype.cacheSearchFilter = function (f) {
            var copiedF = __assign({}, f);
            copiedF.pageNr = 0; //Caching the page can lead to really wierd result as you can end up on page two of a single page resultset where the paging buttons are hidden so you cant see why
            this.localStorage.set(FilterLocalStorageName, copiedF, 5);
        };
        CompanyLoanApplicationSearchController.prototype.getListCountLabel = function (listName) {
            if (!this.m || !this.m.listCountsByName) {
                return null;
            }
            var n = this.m.listCountsByName[listName];
            return n ? n : 0;
        };
        CompanyLoanApplicationSearchController.prototype.getProviderDisplayName = function (providerName) {
            if (!this.m || !this.m.providers) {
                return providerName;
            }
            for (var _i = 0, _a = this.m.providers; _i < _a.length; _i++) {
                var p = _a[_i];
                if (p.ProviderName == providerName) {
                    return p.DisplayToEnduserName;
                }
            }
            return providerName;
        };
        CompanyLoanApplicationSearchController.prototype.getDisplayListName = function (stepName) {
            if (this.initialData) {
                var m = this.initialData.workflowModel;
                var steps = m.Steps.filter(function (x) { return x.Name == stepName; });
                if (steps.length === 1) {
                    return 'Pending ' + steps[0].DisplayName;
                }
            }
            return 'Pending ' + stepName;
        };
        CompanyLoanApplicationSearchController.prototype.isSpecialSearchMode = function () {
            return this.m && this.m.omniSearchValue;
        };
        CompanyLoanApplicationSearchController.prototype.onBack = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!this.m) {
                return;
            }
            this.cacheSearchFilter(null); //We only want to remember the current page when going to an application and back not when leaving to the menu and coming back
            this.initialData.setIsLoading(true);
            this.$timeout(function () {
                NavigationTargetHelper.handleBackWithInitialDataDefaults(_this.initialData, _this.apiClient, _this.$q, {});
            });
        };
        CompanyLoanApplicationSearchController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout', 'ntechLocalStorageService', '$translate'];
        return CompanyLoanApplicationSearchController;
    }(NTechComponents.NTechComponentControllerBase));
    CompanyLoanApplicationSearchComponentNs.CompanyLoanApplicationSearchController = CompanyLoanApplicationSearchController;
    var CompanyLoanApplicationSearchComponent = /** @class */ (function () {
        function CompanyLoanApplicationSearchComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = CompanyLoanApplicationSearchController;
            this.templateUrl = 'company-loan-application-search.html';
        }
        return CompanyLoanApplicationSearchComponent;
    }());
    CompanyLoanApplicationSearchComponentNs.CompanyLoanApplicationSearchComponent = CompanyLoanApplicationSearchComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    CompanyLoanApplicationSearchComponentNs.Model = Model;
    var SelectedSearchHitModel = /** @class */ (function () {
        function SelectedSearchHitModel() {
        }
        return SelectedSearchHitModel;
    }());
    CompanyLoanApplicationSearchComponentNs.SelectedSearchHitModel = SelectedSearchHitModel;
    var SearchResultModel = /** @class */ (function () {
        function SearchResultModel() {
        }
        return SearchResultModel;
    }());
    CompanyLoanApplicationSearchComponentNs.SearchResultModel = SearchResultModel;
})(CompanyLoanApplicationSearchComponentNs || (CompanyLoanApplicationSearchComponentNs = {}));
angular.module('ntech.components').component('companyLoanApplicationSearch', new CompanyLoanApplicationSearchComponentNs.CompanyLoanApplicationSearchComponent());
