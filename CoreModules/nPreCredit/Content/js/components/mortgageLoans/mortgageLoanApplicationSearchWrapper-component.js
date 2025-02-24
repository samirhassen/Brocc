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
var MortgageLoanApplicationSearchWrapperComponentNs;
(function (MortgageLoanApplicationSearchWrapperComponentNs) {
    var MortgageLoanApplicationSearchWrapperController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationSearchWrapperController, _super);
        function MortgageLoanApplicationSearchWrapperController($http, $q, ntechComponentService, modalDialogService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$q = $q;
            _this.modalDialogService = modalDialogService;
            _this.$timeout = $timeout;
            _this.ntechComponentService.subscribeToReloadRequired(function () {
                _this.reload();
            });
            return _this;
        }
        MortgageLoanApplicationSearchWrapperController.prototype.componentName = function () {
            return 'mortgageLoanApplicationSearchWrapper';
        };
        MortgageLoanApplicationSearchWrapperController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanApplicationSearchWrapperController.prototype.reload = function () {
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.setMode(this.initialData.initialTabName || 'workList', null);
        };
        MortgageLoanApplicationSearchWrapperController.prototype.gotoApplication = function (applicationNr) {
            var _this = this;
            var tabName = this.m.currentTabName;
            this.m = null;
            this.$timeout(function () {
                var url = _this.initialData.applicationUrlPattern.replace('NNNNNN', applicationNr) + '&backTarget=' + _this.navigationCodeFromTabName(tabName);
                location.href = url;
            });
        };
        MortgageLoanApplicationSearchWrapperController.prototype.navigationCodeFromTabName = function (tabName) {
            if (tabName === 'createWorkList') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanCreateLeadWorkList;
            }
            else if (tabName === 'search') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanSearch;
            }
            else if (tabName === 'workList') {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplications;
            }
            else {
                return NavigationTargetHelper.NavigationTargetCode.MortgageLoanApplications;
            }
        };
        MortgageLoanApplicationSearchWrapperController.prototype.setMode = function (tabName, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (this.m && this.initialData.initialTabName !== tabName) {
                //When switching tab, update the url so these act like separate pages
                if (tabName === 'createWorkList' || tabName === 'search' || tabName === 'workList') {
                    NavigationTargetHelper.tryNavigateTo(this.navigationCodeFromTabName(tabName), null);
                    return;
                }
            }
            this.m = null;
            if (tabName == 'workList') {
                this.m = new Model(tabName, {
                    backUrl: this.initialData.urlToHere,
                    onGotoApplication: function (application) {
                        _this.apiClient.fetchApplicationInfo(application.ApplicationNr).then(function (x) {
                            _this.gotoApplication(application.ApplicationNr);
                        });
                    },
                    currentUserId: this.initialData.currentUserId,
                    workflowModel: this.initialData.workflowModel,
                }, null, null);
            }
            else if (tabName == 'createWorkList') {
                this.m = new Model(tabName, null, null, {
                    hostData: this.initialData,
                    backUrl: this.initialData.urlToHere
                });
            }
            else if (tabName == 'search') {
                this.m = new Model(tabName, null, {
                    backUrl: this.initialData.urlToHere,
                    onGotoApplication: function (searchHit) {
                        _this.gotoApplication(searchHit.ApplicationNr);
                    }
                }, null);
            }
        };
        MortgageLoanApplicationSearchWrapperController.prototype.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBack(NavigationTargetHelper.create(this.initialData.backUrl, null, null), this.apiClient, this.$q, null);
        };
        MortgageLoanApplicationSearchWrapperController.$inject = ['$http', '$q', 'ntechComponentService', 'modalDialogService', '$timeout'];
        return MortgageLoanApplicationSearchWrapperController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationSearchWrapperComponentNs.MortgageLoanApplicationSearchWrapperController = MortgageLoanApplicationSearchWrapperController;
    var MortgageLoanApplicationSearchWrapperComponent = /** @class */ (function () {
        function MortgageLoanApplicationSearchWrapperComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationSearchWrapperController;
            this.templateUrl = 'mortgage-application-search-wrapper.html';
        }
        return MortgageLoanApplicationSearchWrapperComponent;
    }());
    MortgageLoanApplicationSearchWrapperComponentNs.MortgageLoanApplicationSearchWrapperComponent = MortgageLoanApplicationSearchWrapperComponent;
    var Model = /** @class */ (function () {
        function Model(currentTabName, workListInitialData, searchInitialData, createWorkListInitialData) {
            this.currentTabName = currentTabName;
            this.workListInitialData = workListInitialData;
            this.searchInitialData = searchInitialData;
            this.createWorkListInitialData = createWorkListInitialData;
        }
        return Model;
    }());
    MortgageLoanApplicationSearchWrapperComponentNs.Model = Model;
})(MortgageLoanApplicationSearchWrapperComponentNs || (MortgageLoanApplicationSearchWrapperComponentNs = {}));
angular.module('ntech.components').component('mortgageApplicationSearchWrapper', new MortgageLoanApplicationSearchWrapperComponentNs.MortgageLoanApplicationSearchWrapperComponent());
