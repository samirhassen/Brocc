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
var MortgageLoanApplicationWorkListComponentNs;
(function (MortgageLoanApplicationWorkListComponentNs) {
    var MortgageLoanApplicationWorkListController = /** @class */ (function (_super) {
        __extends(MortgageLoanApplicationWorkListController, _super);
        function MortgageLoanApplicationWorkListController($http, $q, ntechComponentService, ntechLocalStorageService, $timeout) {
            var _this = _super.call(this, ntechComponentService, $http, $q) || this;
            _this.ntechLocalStorageService = ntechLocalStorageService;
            _this.$timeout = $timeout;
            _this.pagingHelper = new NTechTables.PagingHelper($q, $http);
            var pageSizeOverride = _this.ntechComponentService.getQueryStringParameterByName('pageSize');
            _this.pageSize = !!pageSizeOverride ? parseInt(pageSizeOverride) : 20;
            return _this;
        }
        MortgageLoanApplicationWorkListController.prototype.componentName = function () {
            return 'mortgageLoanApplicationWorkList';
        };
        MortgageLoanApplicationWorkListController.prototype.localStorageKey = function (key, list) {
            return key + '_' + (list.isAssigned ? 'a' : 'u');
        };
        MortgageLoanApplicationWorkListController.prototype.expandList = function (list) {
            if (list.data) {
                return;
            }
            this.setCode(list, this.localStorage.get(this.localStorageKey('latestCode', list)), this.localStorage.get(this.localStorageKey('latestHandlerUserId', list)), 0, true, null, null, this.localStorage.get(this.localStorageKey('latestSeparatedWorkList', list)));
        };
        MortgageLoanApplicationWorkListController.prototype.onChanges = function () {
            var _this = this;
            this.m = null;
            if (!this.initialData) {
                return;
            }
            this.localStorage = this.ntechLocalStorageService.getUserContainer('MortgageLoanApplicationWorkListController', this.initialData.currentUserId, '20201024'); //Changes version if the format of local storage data is ever changed to prevent old data from being used            
            this.apiClient.fetchApplicationAssignedHandlers({ returnAssignedHandlers: false, returnPossibleHandlers: true }).then(function (x) {
                var lists = [];
                //Unassigned
                var u = {
                    data: null,
                    onExpanded: null,
                    isAssigned: false,
                    expandEventId: NTechComponents.generateUniqueId(6)
                };
                u.onExpanded = function (s) {
                    _this.expandList(u);
                };
                lists.push(u);
                var a = null;
                if (x.PossibleHandlers && x.PossibleHandlers.length > 0) {
                    //Assigned
                    a = {
                        data: null,
                        onExpanded: null,
                        isAssigned: true,
                        expandEventId: NTechComponents.generateUniqueId(6)
                    };
                    a.onExpanded = function (s) {
                        _this.expandList(a);
                    };
                    lists.push(a);
                }
                _this.m = {
                    possibleHandlers: x.PossibleHandlers,
                    lists: lists
                };
                _this.$timeout(function () {
                    //Wait for the dialog to be created
                    ToggleBlockComponentNs.EmitExpandEvent((a ? a : u).expandEventId, _this.ntechComponentService);
                });
            });
        };
        MortgageLoanApplicationWorkListController.prototype.onCodeChanged = function (list, newCode, evt) {
            this.setCode(list, newCode, list.data.assignedHandlerUserId, 0, false, null, evt, list.data.separatedWorkList);
        };
        MortgageLoanApplicationWorkListController.prototype.onAssignedUserChanged = function (list, newUserId, evt) {
            this.setCode(list, list.data.currentCode, newUserId, 0, false, null, evt, list.data.separatedWorkList);
        };
        MortgageLoanApplicationWorkListController.prototype.onSeparatedWorkListChanged = function (list, newWorkListName, evt) {
            if (newWorkListName) {
                this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, 0, false, null, evt, newWorkListName);
            }
            else {
                this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, 0, false, null, evt, null);
            }
        };
        MortgageLoanApplicationWorkListController.prototype.setCode = function (list, code, assignedHandlerUserId, pageNr, autoSwitchToFirstNonEmptyAfterCurrent, codesWithCount, evt, separatedWorkList) {
            var _this = this;
            if (evt) {
                evt.preventDefault();
            }
            if (!code && !separatedWorkList) {
                code = this.initialData.workflowModel.Steps[0].Name;
            }
            if (list.isAssigned && !assignedHandlerUserId) {
                //Show the logged in user as default if they are a possible handler otherwise fall back to the first in the list
                if (NTechLinq.any(this.m.possibleHandlers, function (x) { return x.UserId === _this.initialData.currentUserId; })) {
                    assignedHandlerUserId = this.initialData.currentUserId.toString();
                }
                else {
                    assignedHandlerUserId = this.m.possibleHandlers[0].UserId.toString();
                }
            }
            this.apiClient.fetchMortgageApplicationWorkListPage(code, pageNr, this.pageSize, !codesWithCount, separatedWorkList, { onlyUnassigned: !assignedHandlerUserId, assignedToHandlerUserId: assignedHandlerUserId ? parseInt(assignedHandlerUserId) : null }).then(function (x) {
                var setM = true;
                if (autoSwitchToFirstNonEmptyAfterCurrent && x.Applications.length === 0 && x.CurrentBlockCodeCounts) {
                    var isCurrentPassed = false;
                    for (var _i = 0, _a = x.CurrentBlockCodeCounts; _i < _a.length; _i++) {
                        var c = _a[_i];
                        if (c.Code === code) {
                            isCurrentPassed = true;
                        }
                        else if (isCurrentPassed && c.Count > 0) {
                            _this.setCode(list, c.Code, assignedHandlerUserId, 0, false, x.CurrentBlockCodeCounts, null, separatedWorkList);
                            setM = false;
                            return;
                        }
                    }
                }
                if (!setM) {
                    return;
                }
                _this.localStorage.set(_this.localStorageKey('latestCode', list), code, 60);
                _this.localStorage.set(_this.localStorageKey('latestSeparatedWorkList', list), separatedWorkList, 60);
                if (list.isAssigned) {
                    _this.localStorage.set(_this.localStorageKey('latestHandlerUserId', list), assignedHandlerUserId, 60);
                }
                var csd = {};
                for (var _b = 0, _c = (x.CurrentBlockCodeCounts || codesWithCount); _b < _c.length; _b++) {
                    var i = _c[_b];
                    csd[i.Code] = i.Count;
                }
                var csl = [];
                for (var _d = 0, _e = _this.initialData.workflowModel.Steps; _d < _e.length; _d++) {
                    var ws = _e[_d];
                    var wm = new WorkflowHelper.WorkflowStepModel(_this.initialData.workflowModel, ws.Name);
                    csl.push({ Code: ws.Name, Count: csd[wm.getInitialListName()] || 0, DisplayName: ws.DisplayName });
                }
                var wf = _this.initialData.workflowModel;
                list.data = {
                    assignedHandlerUserId: assignedHandlerUserId,
                    codesWithCount: csl,
                    currentCode: code,
                    result: x,
                    paging: _this.pagingHelper.createPagingObjectFromPageResult({ CurrentPageNr: x.CurrentPageNr, TotalNrOfPages: x.TotalNrOfPages }),
                    latestNavigatedApplicationNr: pageNr === 0 ? _this.localStorage.get(_this.localStorageKey('latestNavigatedApplicationNr', list)) : null,
                    separatedWorkLists: wf && wf.SeparatedWorkLists ? wf.SeparatedWorkLists : null,
                    separatedWorkList: wf && wf.SeparatedWorkLists ? separatedWorkList : null,
                };
                list.data.paging.onGotoPage = function (data) {
                    _this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, data.pageNr, false, null, null, list.data.separatedWorkList);
                };
            });
        };
        MortgageLoanApplicationWorkListController.prototype.gotoApplication = function (list, a, evt) {
            if (evt) {
                evt.preventDefault();
            }
            list.data.latestNavigatedApplicationNr = a.ApplicationNr;
            this.localStorage.set(this.localStorageKey('latestNavigatedApplicationNr', list), a.ApplicationNr, 60);
            this.initialData.onGotoApplication(a);
        };
        MortgageLoanApplicationWorkListController.$inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$timeout'];
        return MortgageLoanApplicationWorkListController;
    }(NTechComponents.NTechComponentControllerBase));
    MortgageLoanApplicationWorkListComponentNs.MortgageLoanApplicationWorkListController = MortgageLoanApplicationWorkListController;
    var MortgageLoanApplicationWorkListComponent = /** @class */ (function () {
        function MortgageLoanApplicationWorkListComponent() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationWorkListController;
            this.templateUrl = 'mortgage-loan-application-work-list.html';
        }
        return MortgageLoanApplicationWorkListComponent;
    }());
    MortgageLoanApplicationWorkListComponentNs.MortgageLoanApplicationWorkListComponent = MortgageLoanApplicationWorkListComponent;
    var Model = /** @class */ (function () {
        function Model() {
        }
        return Model;
    }());
    MortgageLoanApplicationWorkListComponentNs.Model = Model;
    var ListWrapperModel = /** @class */ (function () {
        function ListWrapperModel() {
        }
        return ListWrapperModel;
    }());
    MortgageLoanApplicationWorkListComponentNs.ListWrapperModel = ListWrapperModel;
    var ListModel = /** @class */ (function () {
        function ListModel() {
        }
        return ListModel;
    }());
    MortgageLoanApplicationWorkListComponentNs.ListModel = ListModel;
    var InitialData = /** @class */ (function () {
        function InitialData() {
        }
        return InitialData;
    }());
    MortgageLoanApplicationWorkListComponentNs.InitialData = InitialData;
})(MortgageLoanApplicationWorkListComponentNs || (MortgageLoanApplicationWorkListComponentNs = {}));
angular.module('ntech.components').component('mortgageLoanApplicationWorkList', new MortgageLoanApplicationWorkListComponentNs.MortgageLoanApplicationWorkListComponent());
