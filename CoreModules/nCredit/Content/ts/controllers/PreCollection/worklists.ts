class WorkListsCtrl {
    static $inject = ['$scope', '$http', '$q', '$timeout', 'ntechComponentService']
    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService,
        private ntechComponentService: NTechComponents.NTechComponentService,
    ) {
        window.scope = this
        this.phoneListUrl = initialData.phoneListUrl
        this.currentUserId = initialData.userId
        this.setupWorklists(initialData.worklists)
        this.calculateModel = {}
        this.isAlternatePaymentPlansEnabled = initialData.isAlternatePaymentPlansEnabled;
    }

    setupWorklists(worklists: any) {
        let d = []
        for (let w of worklists) {
            d.push({
                workListSummary: w.workListSummary,
                filterModel: this.formatFiltersForDisplay(w.filterSummary, w.workListSummary)
            })
        }
        this.worklists = d
    }

    onBack(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q)
    }

    phoneListUrl: string
    worklists: Array<WorkListsNs.IWorkList>
    isLoading: boolean
    showCreateWorkList: boolean
    currentUserId: number
    calculateModel: any
    calculateResult: any
    wl: WorkListsNs.ICurrentWorkListModel
    isAlternatePaymentPlansEnabled: boolean

    formatFiltersForDisplay(filterSummary: WorkListsNs.IInitialDataFilterSummary, workListSummary: WorkListsNs.IWorkListSummary): WorkListsNs.IDisplayFilterModel {
        if (!filterSummary) {
            return null;
        }
        let result: WorkListsNs.IDisplayFilterModel = {
            totalCount: workListSummary.TotalCount,
            filterShortText: '',
            filterCounts: []
        }

        let addToFilterShortText = s => {
            if (result.filterShortText.length > 0) {
                result.filterShortText = result.filterShortText + ', '
            }
            result.filterShortText = result.filterShortText + s
        };

        for (let filter of filterSummary.Filters) {
            if (filter.Name == 'NrOfDueDatesPassed') {
                let kk1NrOfDueDatesPassedItem: WorkListsNs.IDisplayFilterItem = null
                let kk2NrOfDueDatesPassedItem: WorkListsNs.IDisplayFilterItem = null
                let kk3NrOfDueDatesPassedItem: WorkListsNs.IDisplayFilterItem = null
                let kk4plusNrOfDueDatesPassedItem: WorkListsNs.IDisplayFilterItem = null
                if (filter.Value.indexOf('1') !== -1) {
                    kk1NrOfDueDatesPassedItem = {
                        displayName: 'KK1',
                        count: 0
                    }
                    addToFilterShortText('KK1')
                }
                if (filter.Value.indexOf('2') !== -1) {
                    kk2NrOfDueDatesPassedItem = {
                        displayName: 'KK2',
                        count: 0
                    }
                    addToFilterShortText('KK2')
                }
                if (filter.Value.indexOf('3') !== -1) {
                    kk3NrOfDueDatesPassedItem = {
                        displayName: 'KK3',
                        count: 0
                    }
                    addToFilterShortText('KK3')
                }
                if (filter.Value.indexOf('4+') !== -1) {
                    kk4plusNrOfDueDatesPassedItem = {
                        displayName: 'KK4+',
                        count: 0
                    }
                    addToFilterShortText('KK4+')
                }

                for (let entry of filterSummary.FilterDataNrOfDueDatesPassed) {
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 1) {
                        kk1NrOfDueDatesPassedItem.count = kk1NrOfDueDatesPassedItem.count + entry.Count
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 2) {
                        kk2NrOfDueDatesPassedItem.count = kk2NrOfDueDatesPassedItem.count + entry.Count
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification === 3) {
                        kk3NrOfDueDatesPassedItem.count = kk3NrOfDueDatesPassedItem.count + entry.Count
                    }
                    if (entry.NrOfPassedDueDatesWithoutFullPaymentSinceNotification >= 4) {
                        kk4plusNrOfDueDatesPassedItem.count = kk4plusNrOfDueDatesPassedItem.count + entry.Count
                    }
                }

                if (kk1NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk1NrOfDueDatesPassedItem)
                }
                if (kk2NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk2NrOfDueDatesPassedItem)
                }
                if (kk3NrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk3NrOfDueDatesPassedItem)
                }
                if (kk4plusNrOfDueDatesPassedItem != null) {
                    result.filterCounts.push(kk4plusNrOfDueDatesPassedItem)
                }
            }
        }

        return result
    }

    isAnyFilterSelected() {
        var m = this.calculateModel
        var isAnyKkFilterSelected = m.kk1 || m.kk2 || m.kk3 || m.kk4plus

        return isAnyKkFilterSelected
    }

    calculate(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        var kkFilterModel = {}
        var nrOfDueDatesPassedFilter = []
        if (this.calculateModel.kk1) {
            nrOfDueDatesPassedFilter.push('1')
            kkFilterModel['kk1'] = true
        }
        if (this.calculateModel.kk2) {
            nrOfDueDatesPassedFilter.push('2')
            kkFilterModel['kk2'] = true
        }
        if (this.calculateModel.kk3) {
            nrOfDueDatesPassedFilter.push('3')
            kkFilterModel['kk3'] = true
        }
        if (this.calculateModel.kk4plus) {
            nrOfDueDatesPassedFilter.push('4+')
            kkFilterModel['kk4plus'] = true
        }

        var calculateRequestData = {
            nrOfDueDatesPassedFilter: nrOfDueDatesPassedFilter,
            includeActiveAlternatePaymentPlans: this.calculateModel.includeActiveAlternatePaymentPlans
        }
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.calculateWorkListUrl,
            data: calculateRequestData
        }).then(response => {
            this.isLoading = false
            var kkViewModel = { 'kk1': 0, 'kk2': 0, 'kk3': 0, 'kk4plus': 0 }
            let d: any = response.data
            angular.forEach(d.countByNrOfPassedDueDatesWithoutFullPaymentSinceNotification, (v, k) => {
                if (k === '1') {
                    kkViewModel['kk1'] = kkViewModel['kk1'] + v
                } else if (k === '2') {
                    kkViewModel['kk2'] = kkViewModel['kk2'] + v
                } else if (k === '3') {
                    kkViewModel['kk3'] = kkViewModel['kk3'] + v
                } else {
                    kkViewModel['kk4plus'] = kkViewModel['kk4plus'] + v
                }
            })
            this.calculateResult = {
                calculateRequestData: calculateRequestData,
                kkViewModel: kkViewModel,
                kkFilterModel: kkFilterModel,
                totalCount: d.totalCount
            }
        }, err => {
            toastr.error('Error')
            this.isLoading = false
        })
    }

    createWorkList(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.isLoading = true

        var d = angular.copy(this.calculateResult.calculateRequestData)
        d.testUserId = initialData.testUserId
        d.backUrl = initialData.backUrl
        d.includeWorkListsInResponse = true

        this.$http({
            method: 'POST',
            url: initialData.createWorkListUrl,
            data: d,
        }).then(response => {
            let data: any = response.data
            this.setupWorklists(data.worklists)
            this.calculateModel = {}
            this.calculateResult = null
            this.isLoading = false
        }, err => {
            toastr.error('Error')
            this.isLoading = false
        })
    }

    closeWorkList(w: WorkListsNs.IWorkList, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true

        this.$http({
            method: 'POST',
            url: initialData.closeWorkListUrl,
            data: { workListHeaderId: w.workListSummary.WorkListHeaderId, userId: this.currentUserId, includeWorkListsInResponse: true }
        }).then((response) => {
            let data: any = response.data
            this.setupWorklists(data.worklists)
            this.calculateModel = {}
            this.calculateResult = null
            this.isLoading = false
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    openItem(wl: WorkListsNs.IWorkList, itemId: string, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true

        this.$http({
            method: 'POST',
            url: initialData.openWorkListItemUrl,
            data: { workListHeaderId: wl.workListSummary.WorkListHeaderId, userId: this.currentUserId, itemId: itemId }
        }).then((response: ng.IHttpResponse<WorkListsNs.ICurrentItemModel>) => {
            angular.forEach(response.data.applicants, function (a) {
                a.customerCardUrl = ntech.forms.replaceAll(initialData.customerCardUrlPattern, 'NNNNN', a.customerId)
                a.phoneParsed = ntech.libphonenumber.parsePhoneNr(a.phone, ntechClientCountry)
            })
            this.wl = {
                workListSummary: response.data.workListSummary,
                current: response.data,
                openedFrom: wl,
                commentsInitialData: { CreditNr: response.data.itemId }
            };
            (<any>$('#itemDialog')).modal('show');

            this.isLoading = false
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    closeOpenItem(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true

        this.$http({
            method: 'POST',
            url: initialData.replaceWorkListItemUrl,
            data: { workListHeaderId: this.wl.workListSummary.WorkListHeaderId, itemId: this.wl.current.itemId }
        }).then((response: ng.IHttpResponse<WorkListsNs.IWorkListReplaceResult>) => {
            if (!response.data.wasReplaced) {
                toastr.warning('The item could not be replaced')
            }
            document.location.reload()
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    takeAndOpenItem(wl: WorkListsNs.IWorkList, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.takeItem(wl, evt, takenItemId => {
            this.openItem(wl, takenItemId, null)
        })
    }

    takeItem(wl: WorkListsNs.IWorkList, evt: Event, onItemTaken: (takenItemId: string) => any) {
        if (evt) {
            evt.preventDefault()
        }
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.takeWorkListItemUrl,
            data: { workListHeaderId: wl.workListSummary.WorkListHeaderId, userId: this.currentUserId }
        }).then((response: ng.IHttpResponse<WorkListsNs.IWorkListTakeResult>) => {
            if (!response.data.wasItemTaken) {
                if (response.data.isConcurrencyProblem) {
                    toastr.warning('Take item failed but there might be more items. Please try again in a few seconds.')
                } else {
                    toastr.warning('There were no more items available')
                }
                document.location.reload()
            } else {
                onItemTaken(response.data.takenItemId)
            }
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    beginEditPromisedToPayDate(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        if (this.wl.current.isPromisedToPayDateEditMode) {
            return
        }
        this.wl.current.isPromisedToPayDateEditMode = true
        if (this.wl.current.creditSummary.promisedToPayDate) {
            this.wl.current.promisedToPayDateEdit = this.wl.current.creditSummary.promisedToPayDate
        }
    }

    cancelAddPromisedToPayDate(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.wl.current.isPromisedToPayDateEditMode = false
        this.wl.current.promisedToPayDateEdit = null
    }

    removePromisedToPayDate(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true
        this.$http({
            method: 'POST',
            url: '/Api/Credit/PromisedToPayDate/Remove',
            data: { creditNr: this.wl.current.itemId }
        }).then((response) => {
            this.isLoading = false
            this.wl.current.creditSummary.promisedToPayDate = null
            this.wl.current.isPromisedToPayDateEditMode = false
            this.wl.current.promisedToPayDateEdit = null
            this.ntechComponentService.emitNTechEvent('reloadComments', this.wl.current.itemId)
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    isValidPromisedToPayDate(value) {
        if (ntech.forms.isNullOrWhitespace(value))
            return false;
        return moment(value, "YYYY-MM-DD", true).isValid()
    }

    addPromisedToPayDate(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        var editValue = this.wl.current.promisedToPayDateEdit

        this.isLoading = true

        if (!this.isValidPromisedToPayDate(editValue)) {
            var e = (<any>$('#promisedToPayDateEdit'));
            e.popover({ content: 'Invalid date', animation: true });
            e.popover('show');
            this.$timeout(() => {
                e.popover('hide');
            }, 2000);
            this.isLoading = false;
            return
        }

        this.$http({
            method: 'POST',
            url: '/Api/Credit/PromisedToPayDate/Add',
            data: { creditNr: this.wl.current.itemId, promisedToPayDate: editValue, avoidReaddingSameValue: true }
        }).then((response) => {
            this.isLoading = false;
            this.wl.current.creditSummary.promisedToPayDate = editValue
            this.wl.current.isPromisedToPayDateEditMode = false
            this.wl.current.promisedToPayDateEdit = null
            this.ntechComponentService.emitNTechEvent('reloadComments', this.wl.current.itemId)
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    isNextOnActionAllowed(evt: Event, hasFuturePromisedToPayDateCallback: any) {
        if (evt) {
            evt.preventDefault()
        }

        if (!this.wl || !this.wl.current || !this.wl.current.creditSummary) {
            return false
        }
        var hasFuturePromisedToPayDate = false
        if (this.wl.current.creditSummary.promisedToPayDate) {
            let ppdate = moment(this.wl.current.creditSummary.promisedToPayDate, 'YYYY-MM-DD', true)
            let today = moment(initialData.today, 'YYYY-MM-DD', true)
            hasFuturePromisedToPayDate = ppdate > today
        }
        if (hasFuturePromisedToPayDateCallback) {
            hasFuturePromisedToPayDateCallback(hasFuturePromisedToPayDate)
        }
        return hasFuturePromisedToPayDate || this.wl.current.isSettlementDateAdded || this.wl.current.isNewTermsSent
    }

    nextOnAction(evt: Event) {
        var hadFuturePromisedToPayDate = false

        this.isNextOnActionAllowed(null, h => { hadFuturePromisedToPayDate = h })

        this.next({
            isSkipped: false,
            isNewTermsSent: this.wl.current.isNewTermsSent,
            isSettlementDateAdded: this.wl.current.isSettlementDateAdded,
            hadFuturePromisedToPayDate: hadFuturePromisedToPayDate,
            tryLaterChoice: null
        }, evt)
    }

    nextOnTryLater(evt: Event) {
        this.next({
            isSkipped: false,
            isNewTermsSent: false,
            isSettlementDateAdded: false,
            hadFuturePromisedToPayDate: false,
            tryLaterChoice: this.wl.current.tryLaterChoice
        }, evt)
    }

    nextOnSkipped(evt: Event) {
        this.next({
            isSkipped: true,
            isNewTermsSent: false,
            isSettlementDateAdded: false,
            hadFuturePromisedToPayDate: false,
            tryLaterChoice: null
        }, evt)
    }

    next(actionTaken: WorkListsNs.ActionTaken, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true

        this.$http({
            method: 'POST',
            url: initialData.completeWorkListItemUrl,
            data: {
                workListHeaderId: this.wl.workListSummary.WorkListHeaderId, userId: this.currentUserId, itemId: this.wl.current.itemId, actionTaken: actionTaken
            }
        }).then((response) => {
            if ((<any>response.data).wasCompleted) {
                this.takeAndOpenItem(this.wl.openedFrom, evt)
            } else {
                toastr.warning('Could not be completed!')
                document.location.reload()
            }
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }

    getAlternatePaymentPlanStatusText() {
        if (!this.wl || !this.wl.current || !this.wl.current.creditSummary.alternatePaymentPlanStateCode) {
            return '';
        }
        let code = this.wl.current.creditSummary.alternatePaymentPlanStateCode;
        if (code === 'active') {
            return 'Active';
        } else if (code === 'activeButLate') {
            return 'Active but late';
        } else if (code === 'recentlyCancelled') {
            return 'Recently cancelled';
        } else {
            return code;
        }
    }
}

var app = angular.module('app', ['ntech.forms', 'ntech.components'])
app.controller('workListsCtr', WorkListsCtrl)

module WorkListsNs {
    export interface IWorkList {
        filterModel: IDisplayFilterModel,
        workListSummary: IWorkListSummary
    }
    export interface ICurrentWorkListModel {
        workListSummary: IWorkListSummary
        current: ICurrentItemModel,
        openedFrom: IWorkList
        commentsInitialData: CreditCommentsComponentNs.InitialData
    }
    export interface IWorkListSummary {
        WorkListHeaderId: number,
        CloseByUserId: number,
        ClosedDate: Date,
        TotalCount: number,
        TakenCount: number,
        CompletedCount: number,
        TakeOrCompletedByCurrentUserCount: number,
        CurrentUserActiveItemId: string,
        IsTakePossible: boolean
    }

    export interface IWorkListTakeResult {
        wasItemTaken: boolean,
        takenItemId: string,
        isConcurrencyProblem: boolean
    }

    export interface IWorkListReplaceResult {
        wasReplaced: boolean
    }

    export interface IOpenWorkListResult {
        itemId: string,
        applicants: any,
        unpaidnotifications: any,
        creditSummary: ICreditSummary,
        workListSummary: IWorkListSummary
    }

    export interface ICurrentItemModel extends IOpenWorkListResult {
        promisedToPayDateEdit: string,
        isPromisedToPayDateEditMode: boolean,
        isNewTermsSent: boolean,
        isSettlementDateAdded: boolean,
        tryLaterChoice: string
    }

    export interface ICreditSummary {
        nrOfOverdueNotifications: number,
        nrOfDaysPastDueDate: number,
        overdueBalance: number,
        totalCapitalDebt: number,
        promisedToPayDate: string
        alternatePaymentPlanStateCode: string
    }

    export interface IInitialDataFilterSummary {
        Filters: Array<IInitialDataFilterItem>,
        FilterDataNrOfDueDatesPassed: Array<IInitialDataFilterDataNrOfDueDatesPassedItem>,
    }

    export interface IInitialDataFilterItem {
        Name: string,
        Value: string
    }

    export interface IInitialDataFilterDataNrOfDueDatesPassedItem {
        NrOfPassedDueDatesWithoutFullPaymentSinceNotification: number,
        Count: number
    }

    export interface IDisplayFilterModel {
        filterCounts: Array<IDisplayFilterItem>,
        filterShortText: string,
        totalCount: number
    }
    export interface IDisplayFilterItem {
        displayName: string
        count: number
    }

    export class ActionTaken {
        isSkipped: boolean
        hadFuturePromisedToPayDate: boolean
        isSettlementDateAdded: boolean
        isNewTermsSent: boolean
        tryLaterChoice: string
    }
}

$(function () {
    $("#itemDialog").on("hide.bs.modal", function (evt) {
        document.location.reload()
    })
})