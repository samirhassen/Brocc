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
var MortgageLoanApplicationCreateWorkListComponentNs;
(function (MortgageLoanApplicationCreateWorkListComponentNs) {
    var MortgageLoanApplicationCreateWorkListController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationCreateWorkListController, _super);
        function MortgageLoanApplicationCreateWorkListController($http, $q, ntechComponentService, $timeout, $scope) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.$timeout = $timeout;
            return _this;
        }
        MortgageLoanApplicationCreateWorkListController.prototype.componentName = function () {
            return 'mortgageLoanApplicationCreateWorkList';
        };
        MortgageLoanApplicationCreateWorkListController.prototype.reload = function (thenDo) {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.apiClient.fetchMortgageLoanLeadsWorkListStatuses().then(function (x) {
                var hasActiveLists = x.WorkLists && x.WorkLists.length > 0;
                var worklists = [];
                for (var _i = 0, _a = x.WorkLists; _i < _a.length; _i++) {
                    var w = _a[_i];
                    worklists.push({
                        state: w,
                    });
                }
                _this.m = {
                    worklists: worklists,
                    showCreateWorkList: !hasActiveLists
                };
                if (thenDo) {
                    thenDo();
                }
            });
        };
        MortgageLoanApplicationCreateWorkListController.prototype.onChanges = function () {
            this.reload();
        };
        MortgageLoanApplicationCreateWorkListController.prototype.createWorkList = function (evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            this.apiClient.createMortgageLoanLeadsWorkList().then(function (x) {
                if (x.NoLeadsMatchFilter) {
                    toastr.warning('No leads match this selection');
                }
                else {
                    _this.reload();
                }
            });
        };
        MortgageLoanApplicationCreateWorkListController.prototype.closeWorkList = function (w, evt) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!w) {
                return;
            }
            this.apiClient.tryCloseMortgageLoanWorkList(w.WorkListHeaderId).then(function (x) {
                if (x.WasClosed) {
                    _this.reload();
                }
                else {
                    toastr.warning('Could not close worklist');
                }
            });
        };
        MortgageLoanApplicationCreateWorkListController.prototype.openItem = function (w, applicationNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                applicationNr: applicationNr,
                workListId: w.state.WorkListHeaderId.toString()
            });
        };
        MortgageLoanApplicationCreateWorkListController.prototype.takeAndOpenItem = function (w, evt) {
            if (evt) {
                evt.preventDefault();
            }
            var workListId = w.state.WorkListHeaderId;
            this.apiClient.tryTakeMortgageLoanWorkListItem(workListId).then(function (x) {
                if (!x.WasItemTaken) {
                    toastr.warning('No items left');
                    return;
                }
                NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                    applicationNr: x.TakenItemId,
                    workListId: workListId.toString()
                });
            });
        };
        MortgageLoanApplicationCreateWorkListController.$inject = ['$http', '$q', 'ntechComponentService', '$timeout', '$scope'];
        return MortgageLoanApplicationCreateWorkListController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationCreateWorkListComponentNs.MortgageLoanApplicationCreateWorkListController = MortgageLoanApplicationCreateWorkListController;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanApplicationCreateWorkListComponentNs.InitialData = InitialData;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationCreateWorkListComponentNs.Model = Model;
    var WorkListModel = /** @class */ (function () {
        function WorkListModel() {
        }
        return WorkListModel;
    }());
    MortgageLoanApplicationCreateWorkListComponentNs.WorkListModel = WorkListModel;
    var MortgageLoanApplicationCreateWorkListComponent = /** @class */ (function () {
        function MortgageLoanApplicationCreateWorkListComponent() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationCreateWorkListController;
            this.template = "<div>\n        <div>\n            <h2 class=\"custom-header\">\n                <span class=\"glyphicon chevron-bg\" ng-click=\"$ctrl.m.showCreateWorkList=!$ctrl.m.showCreateWorkList\" ng-class=\"{ 'glyphicon-chevron-down' : $ctrl.m.showCreateWorkList, 'glyphicon-chevron-right' : !$ctrl.m.showCreateWorkList }\"></span>\n                Create new worklist\n            </h2>\n            <hr class=\"hr-section\" />\n        </div>\n\n        <div style=\"margin-bottom: 200px;\" ng-show=\"$ctrl.m.showCreateWorkList\">\n            <div class=\"row pt-1\">\n                <div class=\"col-xs-8 col-sm-offset-2\">\n                    <div class=\"pt-3\">\n                        <div class=\"frame\">\n                            <div class=\"form-horizontal\">\n                                <div class=\"pt-2 text-center\">\n                                    <button class=\"n-main-btn n-green-btn\" ng-click=\"$ctrl.createWorkList($event)\">Create worklist</button>\n                                </div>\n                            </div>\n                        </div>\n                    </div>\n                </div>\n            </div>\n        </div>\n\n        <div class=\"pt-3\" ng-if=\"$ctrl.m.worklists && $ctrl.m.worklists.length > 0\">\n            <h2 class=\"custom-header\">Worklists</h2>\n            <hr class=\"hr-section\" />\n        </div>\n\n        <div class=\"row pb-3\" ng-repeat=\"w in $ctrl.m.worklists\">\n            <div class=\"col-xs-8 col-sm-offset-2\" ng-if=\"!w.ClosedByUserId\">\n                <div class=\"frame\">\n                    <div class=\"form-horizontal\">\n                        <button class=\"n-main-btn n-white-btn pull-right\" ng-click=\"$ctrl.closeWorkList(w.state, $event)\">Close</button>\n                        <div class=\"clearfix\"></div>\n                        <div class=\"row pb-2\">\n                            <div class=\"col-xs-4\" ng-if=\" w.filterModel\"> <!-- TODO: Will be added when there is more than one filter -->\n                                <div class=\"table-summery\">\n                                    <table class=\"table\">\n                                        <tbody>\n                                            <tr ng-repeat=\"f in w.filterModel.filterCounts\">\n                                                <td class=\"col-xs-6 text-right\">{{f.displayName}}</td>\n                                                <td class=\"col-xs-6 bold\">{{f.count}}</td>\n                                            </tr>\n                                        </tbody>\n                                        <tfoot>\n                                            <tr>\n                                                <td class=\"col-xs-6 text-right\">Total</td>\n                                                <td class=\"col-xs-6 bold\">{{w.filterModel.totalCount}}</td>\n                                            </tr>\n                                        </tfoot>\n                                    </table>\n                                </div>\n                            </div>\n                            <div class=\"col-xs-12 text-center\"> <!-- TODO: when more filters are added, change this to col-xs-4 again -->\n                                <div class=\"pb-2\">\n                                    <lable>Total</lable>\n                                    <p class=\"h3\"><b>{{w.state.CompletedCount}}/{{w.state.TotalCount}}</b></p>\n                                </div>\n                                <div class=\"pb-2\">\n                                    <lable>My count</lable>\n                                    <p class=\"h3\"><b>{{w.state.TakeOrCompletedByCurrentUserCount}}</b></p>\n                                </div>\n                            </div>\n                        </div>\n                        <div class=\"row pt-2\">\n                            <div class=\"col-sm-offset-4 col-xs-4 text-center\">\n                                <button ng-click=\"$ctrl.openItem(w, w.state.CurrentUserActiveItemId, $event)\" class=\"n-main-btn n-blue-btn\" ng-if=\"w.state.CurrentUserActiveItemId\">Continue <span class=\"glyphicon glyphicon-resize-full\"></span></button>\n                                <button ng-click=\"$ctrl.takeAndOpenItem(w, $event)\" class=\"n-main-btn n-blue-btn\" ng-if=\"!w.state.CurrentUserActiveItemId && w.state.IsTakePossible\">Start <span class=\"glyphicon glyphicon-resize-full\"></span></button>\n                            </div>\n                            <div class=\"col-xs-4 text-right\">{{w.state.CreationDate | date:'short'}}</div>\n                        </div>\n                    </div>\n                </div>\n            </div>\n            <div class=\"col-xs-10 col-sm-offset-1\" ng-if=\"w.state.ClosedByUserId\">\n                <p>This work list was closed {{w.state.ClosedDate | date}}</p>\n            </div>\n        </div>\n    </div>";
        }
        return MortgageLoanApplicationCreateWorkListComponent;
    }());
    MortgageLoanApplicationCreateWorkListComponentNs.MortgageLoanApplicationCreateWorkListComponent = MortgageLoanApplicationCreateWorkListComponent;
})(MortgageLoanApplicationCreateWorkListComponentNs || (MortgageLoanApplicationCreateWorkListComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationCreateWorkList', new MortgageLoanApplicationCreateWorkListComponentNs.MortgageLoanApplicationCreateWorkListComponent());
