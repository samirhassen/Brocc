namespace MortgageLoanApplicationWorkListComponentNs {

    export class MortgageLoanApplicationWorkListController extends NTechComponents.NTechComponentControllerBase {
        pageSize: number

        initialData: InitialData
        pagingHelper: NTechTables.PagingHelper
        localStorage: NTechComponents.NTechLocalStorageContainer

        m: Model

        static $inject = ['$http', '$q', 'ntechComponentService', 'ntechLocalStorageService', '$timeout']
        constructor($http: ng.IHttpService,
            $q: ng.IQService,
            ntechComponentService: NTechComponents.NTechComponentService,
            private ntechLocalStorageService: NTechComponents.NTechLocalStorageService,
            private $timeout: ng.ITimeoutService) {
            super(ntechComponentService, $http, $q);
            this.pagingHelper = new NTechTables.PagingHelper($q, $http)

            let pageSizeOverride = this.ntechComponentService.getQueryStringParameterByName('pageSize')
            this.pageSize = !!pageSizeOverride ? parseInt(pageSizeOverride) : 20
        }

        componentName(): string {
            return 'mortgageLoanApplicationWorkList'
        }

        localStorageKey(key: string, list: ListWrapperModel) {
            return key + '_' + (list.isAssigned ? 'a' : 'u')
        }

        expandList(list: ListWrapperModel) {
            if (list.data) {
                return
            }

            this.setCode(list, 
                this.localStorage.get(this.localStorageKey('latestCode', list)), 
                this.localStorage.get(this.localStorageKey('latestHandlerUserId', list)),                
                0, true, null, null,
                this.localStorage.get(this.localStorageKey('latestSeparatedWorkList', list)))            
        }

        onChanges() {
            this.m = null

            if (!this.initialData) {
                return
            }

            this.localStorage = this.ntechLocalStorageService.getUserContainer(
                'MortgageLoanApplicationWorkListController',
                this.initialData.currentUserId,
                '20201024') //Changes version if the format of local storage data is ever changed to prevent old data from being used            

            this.apiClient.fetchApplicationAssignedHandlers({ returnAssignedHandlers: false, returnPossibleHandlers: true }).then(x => {
                let lists : ListWrapperModel[] = []

                //Unassigned
                let u: ListWrapperModel = {
                    data: null,
                    onExpanded: null,
                    isAssigned: false,
                    expandEventId: NTechComponents.generateUniqueId(6)
                }
                u.onExpanded = s => {
                    this.expandList(u)
                }

                lists.push(u)

                let a: ListWrapperModel = null
                if (x.PossibleHandlers && x.PossibleHandlers.length > 0) {
                    //Assigned
                    a = {
                        data: null,
                        onExpanded: null,
                        isAssigned: true,
                        expandEventId: NTechComponents.generateUniqueId(6)
                    }
                    a.onExpanded = s => {
                        this.expandList(a)
                    }
                    lists.push(a)
                }

                this.m = {
                    possibleHandlers: x.PossibleHandlers,
                    lists: lists
                }

                this.$timeout(() => {
                    //Wait for the dialog to be created
                    ToggleBlockComponentNs.EmitExpandEvent((a ? a : u).expandEventId, this.ntechComponentService)
                })                
            })
        }

        onCodeChanged(list : ListWrapperModel, newCode: string, evt: Event) {
            this.setCode(list, newCode, list.data.assignedHandlerUserId, 0, false, null, evt, list.data.separatedWorkList)
        }

        onAssignedUserChanged(list : ListWrapperModel, newUserId: string, evt: Event) {
            this.setCode(list, list.data.currentCode, newUserId, 0, false, null, evt, list.data.separatedWorkList)
        }

        onSeparatedWorkListChanged(list : ListWrapperModel, newWorkListName: string, evt: Event) {
            if (newWorkListName) {
                this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, 0, false, null, evt, newWorkListName)
            } else {
                this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, 0, false, null, evt, null)
            }
        }

        setCode(
            list : ListWrapperModel,
            code: string,
            assignedHandlerUserId: string,
            pageNr: number, autoSwitchToFirstNonEmptyAfterCurrent: boolean,
            codesWithCount: NTechPreCreditApi.MortgageApplicationWorkListCodeCount[],
            evt: Event,
            separatedWorkList: string) {

            if (evt) {
                evt.preventDefault()
            }

            if (!code && !separatedWorkList) {
                code = this.initialData.workflowModel.Steps[0].Name
            }

            if (list.isAssigned && !assignedHandlerUserId) {
                //Show the logged in user as default if they are a possible handler otherwise fall back to the first in the list
                if (NTechLinq.any(this.m.possibleHandlers, x => x.UserId === this.initialData.currentUserId)) {
                    assignedHandlerUserId = this.initialData.currentUserId.toString()
                } else {
                    assignedHandlerUserId = this.m.possibleHandlers[0].UserId.toString()
                }                
            }

            this.apiClient.fetchMortgageApplicationWorkListPage(code, pageNr, this.pageSize, !codesWithCount, separatedWorkList, { onlyUnassigned: !assignedHandlerUserId, assignedToHandlerUserId: assignedHandlerUserId ? parseInt(assignedHandlerUserId) : null }).then(x => {
                let setM = true
                if (autoSwitchToFirstNonEmptyAfterCurrent && x.Applications.length === 0 && x.CurrentBlockCodeCounts) {
                    let isCurrentPassed = false
                    for (let c of x.CurrentBlockCodeCounts) {
                        if (c.Code === code) {
                            isCurrentPassed = true
                        } else if (isCurrentPassed && c.Count > 0) {
                            this.setCode(list, c.Code, assignedHandlerUserId, 0, false, x.CurrentBlockCodeCounts, null, separatedWorkList)
                            setM = false
                            return
                        }
                    }
                }
                if (!setM) {
                    return
                }

                this.localStorage.set(this.localStorageKey('latestCode', list), code, 60)
                this.localStorage.set(this.localStorageKey('latestSeparatedWorkList', list), separatedWorkList,  60)

                if (list.isAssigned) {
                    this.localStorage.set(this.localStorageKey('latestHandlerUserId', list), assignedHandlerUserId, 60)
                }

                let csd: NTechPreCreditApi.IStringDictionary<number> = {}
                for (let i of (x.CurrentBlockCodeCounts || codesWithCount)) {
                    csd[i.Code] = i.Count
                }
                let csl: CountItem[] = []                
                for (let ws of this.initialData.workflowModel.Steps) {
                    let wm = new WorkflowHelper.WorkflowStepModel(this.initialData.workflowModel, ws.Name)
                    csl.push({ Code: ws.Name, Count: csd[wm.getInitialListName()] || 0, DisplayName: ws.DisplayName })
                }

                let wf = this.initialData.workflowModel

                list.data = {
                    assignedHandlerUserId: assignedHandlerUserId,
                    codesWithCount: csl,
                    currentCode: code,
                    result: x,
                    paging: this.pagingHelper.createPagingObjectFromPageResult({ CurrentPageNr: x.CurrentPageNr, TotalNrOfPages: x.TotalNrOfPages }),
                    latestNavigatedApplicationNr: pageNr === 0 ? this.localStorage.get(this.localStorageKey('latestNavigatedApplicationNr', list)) : null,
                    separatedWorkLists: wf && wf.SeparatedWorkLists ? wf.SeparatedWorkLists : null,
                    separatedWorkList: wf && wf.SeparatedWorkLists ? separatedWorkList : null,
                }
                list.data.paging.onGotoPage = (data: { pageNr: number }) => {
                    this.setCode(list, list.data.currentCode, list.data.assignedHandlerUserId, data.pageNr, false, null, null, list.data.separatedWorkList)
                }
            })
        }

        gotoApplication(list: ListWrapperModel, a: NTechPreCreditApi.MortgageApplicationWorkListApplication, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            list.data.latestNavigatedApplicationNr = a.ApplicationNr
            this.localStorage.set(this.localStorageKey('latestNavigatedApplicationNr', list), a.ApplicationNr, 60)
            this.initialData.onGotoApplication(a)
        }
    }

    export class MortgageLoanApplicationWorkListComponent implements ng.IComponentOptions {
        public bindings: any;
        public controller: any;
        public templateUrl: string;

        constructor() {
            this.bindings = {
                initialData: '<'
            };
            this.controller = MortgageLoanApplicationWorkListController;
            this.templateUrl = 'mortgage-loan-application-work-list.html';
        }
    }

    export class Model {
        possibleHandlers: NTechPreCreditApi.AssignedHandlerModel[]
        lists: ListWrapperModel[]
    }

    export class ListWrapperModel {
        data: ListModel 
        isAssigned: boolean
        onExpanded: (service: ToggleBlockComponentNs.Service) => void
        expandEventId: string
    }

    export class ListModel {
        currentCode: string
        assignedHandlerUserId: string
        codesWithCount: CountItem[]
        result: NTechPreCreditApi.MortgageApplicationWorkListPageResult
        paging: NTechTables.PagingObject
        latestNavigatedApplicationNr?: string
        separatedWorkList: string
        separatedWorkLists: WorkflowHelper.WorkflowSeparatedWorkListModel[]
    }

    export class InitialData {
        backUrl?: string
        onGotoApplication: (application: NTechPreCreditApi.MortgageApplicationWorkListApplication) => void
        currentUserId: number
        workflowModel: WorkflowHelper.WorkflowServerModel
    }

    export interface CountItem {
        Code: string
        Count: number
        DisplayName: string
    }
}

angular.module('ntech.components').component('mortgageLoanApplicationWorkList', new MortgageLoanApplicationWorkListComponentNs.MortgageLoanApplicationWorkListComponent())