class PreCollectionManagementHistoryCtrl {
    static $inject = ['$scope', '$http', '$timeout', '$q']

    backUrl: string
    pending: PreCollectionManagementHistoryNs.IPending
    fromCreatedDate: string
    toCreatedDate: string
    isLoading: boolean
    files: PreCollectionManagementHistoryNs.IPageResult
    filesPaging: PreCollectionManagementHistoryNs.PagingObject
    apiClient: NTechCreditApi.ApiClient

    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $timeout: ng.ITimeoutService,
        private $q: ng.IQService
    ) {
        this.apiClient = new NTechCreditApi.ApiClient(x => toastr.error(x), $http, $q)
        this.backUrl = initialData.backUrl
        this.pending = initialData.pending
        this.fromCreatedDate = this.today().add(-10, 'days').format('YYYY-MM-DD')
        this.toCreatedDate = this.today().format('YYYY-MM-DD')

        this.gotoPage(0, { FromDate: this.fromCreatedDate, ToDate: this.toCreatedDate, WorkListTypeName: 'PreCollection1', OnlyClosed: true }, null)

        window.scope = this
    }

    today() {
        return moment(initialData.today, 'YYYY-MM-DD', true)
    }

    isValidDate(value: string) {
        if (ntech.forms.isNullOrWhitespace(value))
            return true;
        var d = moment(value, 'YYYY-MM-DD', true)
        return d.isValid()
    }

    onBack(evt?: Event) {
        if (evt) {
            evt.preventDefault()
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.apiClient, this.$q, {})
    }

    gotoPage(pageNr: number, filter: PreCollectionManagementHistoryNs.IFilter, evt: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.isLoading = true
        this.$http({
            method: 'POST',
            url: initialData.getFilesPageUrl,
            data: { pageSize: 50, pageNr: pageNr, filter: filter }
        }).then((response: ng.IHttpResponse<PreCollectionManagementHistoryNs.IPageResult>) => {
            this.isLoading = false
            this.files = response.data
            this.updatePaging()
        }, (response: ng.IHttpResponse<any>) => {
            this.isLoading = false
            toastr.error(response.statusText, 'Error');
        })
    }

    updatePaging() {
        if (!this.files) {
            return {}
        }
        var h = this.files
        var p: Array<PreCollectionManagementHistoryNs.PagingObjectPage> = []

        //9 items including separators are the most shown at one time
        //The two items before and after the current item are shown
        //The first and last item are always shown
        for (var i = 0; i < h.TotalNrOfPages; i++) {
            if (i >= (h.CurrentPageNr - 2) && i <= (h.CurrentPageNr + 2) || h.TotalNrOfPages <= 9) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }) //Primary pages are always visible
            } else if (i == 0 || i == (h.TotalNrOfPages - 1)) {
                p.push({ pageNr: i, isCurrentPage: h.CurrentPageNr == i, isSeparator: false }) //First and last page are always visible
            } else if (i == (h.CurrentPageNr - 3) || i == (h.CurrentPageNr + 3)) {
                p.push({ pageNr: i, isCurrentPage: false, isSeparator: true }) //First and last page are always visible
            }
        }

        this.filesPaging = {
            pages: p,
            isPreviousAllowed: h.CurrentPageNr > 0,
            previousPageNr: h.CurrentPageNr - 1,
            isNextAllowed: h.CurrentPageNr < (h.TotalNrOfPages - 1),
            nextPageNr: h.CurrentPageNr + 1
        }
    }
}

var app = angular.module('app', ['ntech.forms'])
app.controller('preCollectionManagementHistoryCtrl', PreCollectionManagementHistoryCtrl)

module PreCollectionManagementHistoryNs {
    export interface IPending {
        Dates: Array<Date>
    }
    export interface IFilter {
        FromDate: string
        ToDate: string,
        WorkListTypeName: string,
        OnlyClosed: boolean
    }
    export interface IPageRow {
        CreatedDate: Date,
        ClosedDate: Date,
        SelectionUrl: string,
        ResultUrl: string,
        UserId: number,
        UserDisplayName: string
    }
    export interface IPageResult {
        CurrentPageNr: number
        TotalNrOfPages: number
        Page: Array<IPageRow>
        Filter: IFilter
    }
    export class PagingObject {
        pages: Array<PagingObjectPage>
        isPreviousAllowed: boolean
        previousPageNr: number
        isNextAllowed: boolean
        nextPageNr: number
    }
    export class PagingObjectPage {
        pageNr: number
        isCurrentPage: boolean
        isSeparator: boolean
    }
}