namespace MortgageLoanApplicationCreateWorkListComponentNs {
    export class MortgageLoanApplicationCreateWorkListController extends NTechComponents.NTechComponentControllerBase {
        initialData: InitialData;
        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', '$timeout', '$scope']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private $timeout: ng.ITimeoutService,
            $scope: ng.IScope) {
            super(ntechComponentService, $http, $q);
        }

        componentName(): string {
            return 'mortgageLoanApplicationCreateWorkList'
        }

        reload(thenDo?: () => void) {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.apiClient.fetchMortgageLoanLeadsWorkListStatuses().then(x => {
                let hasActiveLists = x.WorkLists && x.WorkLists.length > 0

                let worklists: { state: NTechPreCreditApi.FetchMortgageLoanLeadsWorkListStatusesResultItem }[] = []
                for (let w of x.WorkLists) {
                    worklists.push({
                        state: w,
                    })
                }
                this.m = {
                    worklists: worklists,
                    showCreateWorkList: !hasActiveLists
                }
                if (thenDo) {
                    thenDo()
                }
            })
        }

        onChanges() {
            this.reload()
        }

        createWorkList(evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.apiClient.createMortgageLoanLeadsWorkList().then(x => {
                if (x.NoLeadsMatchFilter) {
                    toastr.warning('No leads match this selection')
                } else {
                    this.reload()
                }
            })
        }

        closeWorkList(w : NTechPreCreditApi.FetchMortgageLoanLeadsWorkListStatusesResultItem, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            if (!w) {
                return
            }
            this.apiClient.tryCloseMortgageLoanWorkList(w.WorkListHeaderId).then(x => {
                if (x.WasClosed) {
                    this.reload()
                } else {
                    toastr.warning('Could not close worklist')
                }
            })
        }

        openItem(w: WorkListModel, applicationNr: string, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                applicationNr: applicationNr,
                workListId: w.state.WorkListHeaderId.toString()
            })
        }

        takeAndOpenItem(w: WorkListModel, evt?: Event) {
            if (evt) {
                evt.preventDefault()
            }
            let workListId = w.state.WorkListHeaderId
            this.apiClient.tryTakeMortgageLoanWorkListItem(workListId).then(x => {
                if (!x.WasItemTaken) {
                    toastr.warning('No items left')
                    return
                }
                NavigationTargetHelper.tryNavigateTo(NavigationTargetHelper.NavigationTargetCode.MortgageLoanLead, {
                    applicationNr: x.TakenItemId,
                    workListId: workListId.toString()
                })
            })
        }
    }
    
    export class InitialData {
        hostData: ComponentHostNs.ComponentHostInitialData
        backUrl: string
    }

    export class Model {        
        showCreateWorkList: boolean
        worklists: WorkListModel[]
    }

    export class WorkListModel {
        state: NTechPreCreditApi.FetchMortgageLoanLeadsWorkListStatusesResultItem

    }

    export class MortgageLoanApplicationCreateWorkListComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public template: string;
        public transclude: boolean;

        constructor() {
            this.transclude = true;
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationCreateWorkListController;
            this.template = `<div>
        <div>
            <h2 class="custom-header">
                <span class="glyphicon chevron-bg" ng-click="$ctrl.m.showCreateWorkList=!$ctrl.m.showCreateWorkList" ng-class="{ 'glyphicon-chevron-down' : $ctrl.m.showCreateWorkList, 'glyphicon-chevron-right' : !$ctrl.m.showCreateWorkList }"></span>
                Create new worklist
            </h2>
            <hr class="hr-section" />
        </div>

        <div style="margin-bottom: 200px;" ng-show="$ctrl.m.showCreateWorkList">
            <div class="row pt-1">
                <div class="col-xs-8 col-sm-offset-2">
                    <div class="pt-3">
                        <div class="frame">
                            <div class="form-horizontal">
                                <div class="pt-2 text-center">
                                    <button class="n-main-btn n-green-btn" ng-click="$ctrl.createWorkList($event)">Create worklist</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="pt-3" ng-if="$ctrl.m.worklists && $ctrl.m.worklists.length > 0">
            <h2 class="custom-header">Worklists</h2>
            <hr class="hr-section" />
        </div>

        <div class="row pb-3" ng-repeat="w in $ctrl.m.worklists">
            <div class="col-xs-8 col-sm-offset-2" ng-if="!w.ClosedByUserId">
                <div class="frame">
                    <div class="form-horizontal">
                        <button class="n-main-btn n-white-btn pull-right" ng-click="$ctrl.closeWorkList(w.state, $event)">Close</button>
                        <div class="clearfix"></div>
                        <div class="row pb-2">
                            <div class="col-xs-4" ng-if=" w.filterModel"> <!-- TODO: Will be added when there is more than one filter -->
                                <div class="table-summery">
                                    <table class="table">
                                        <tbody>
                                            <tr ng-repeat="f in w.filterModel.filterCounts">
                                                <td class="col-xs-6 text-right">{{f.displayName}}</td>
                                                <td class="col-xs-6 bold">{{f.count}}</td>
                                            </tr>
                                        </tbody>
                                        <tfoot>
                                            <tr>
                                                <td class="col-xs-6 text-right">Total</td>
                                                <td class="col-xs-6 bold">{{w.filterModel.totalCount}}</td>
                                            </tr>
                                        </tfoot>
                                    </table>
                                </div>
                            </div>
                            <div class="col-xs-12 text-center"> <!-- TODO: when more filters are added, change this to col-xs-4 again -->
                                <div class="pb-2">
                                    <lable>Total</lable>
                                    <p class="h3"><b>{{w.state.CompletedCount}}/{{w.state.TotalCount}}</b></p>
                                </div>
                                <div class="pb-2">
                                    <lable>My count</lable>
                                    <p class="h3"><b>{{w.state.TakeOrCompletedByCurrentUserCount}}</b></p>
                                </div>
                            </div>
                        </div>
                        <div class="row pt-2">
                            <div class="col-sm-offset-4 col-xs-4 text-center">
                                <button ng-click="$ctrl.openItem(w, w.state.CurrentUserActiveItemId, $event)" class="n-main-btn n-blue-btn" ng-if="w.state.CurrentUserActiveItemId">Continue <span class="glyphicon glyphicon-resize-full"></span></button>
                                <button ng-click="$ctrl.takeAndOpenItem(w, $event)" class="n-main-btn n-blue-btn" ng-if="!w.state.CurrentUserActiveItemId && w.state.IsTakePossible">Start <span class="glyphicon glyphicon-resize-full"></span></button>
                            </div>
                            <div class="col-xs-4 text-right">{{w.state.CreationDate | date:'short'}}</div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-xs-10 col-sm-offset-1" ng-if="w.state.ClosedByUserId">
                <p>This work list was closed {{w.state.ClosedDate | date}}</p>
            </div>
        </div>
    </div>`;
        }
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationCreateWorkList', new MortgageLoanApplicationCreateWorkListComponentNs.MortgageLoanApplicationCreateWorkListComponent())