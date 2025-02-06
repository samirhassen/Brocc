declare function download(data: any, strFileName: string, strMimeType: string): any;

class PhoneListCtrl {
    static $inject = ['$scope', '$http', '$q']
    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        window.scope = this
        this.workListUrl = initialData.workListUrl
        this.overdueCount = ''
    }

    isLoading: boolean
    workListUrl: string
    overdueCount: string

    onBack(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q)
    }

    downloadPhoneList(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        let nrOfDueDatesPassedFilter = []
        if (this.overdueCount) {
            nrOfDueDatesPassedFilter.push(this.overdueCount)
        }

        this.isLoading = true
        var data = {}
        this.$http({
            method: 'POST',
            url: '/Api/PreCollection/PreviewWorkListCreditNrs',
            data: {
                nrOfDueDatesPassedFilter: nrOfDueDatesPassedFilter
            }
        }).then((response: ng.IHttpResponse<PhoneListNs.IPreviewWorkListResult>) => {
            this.$http({
                method: 'POST',
                url: '/Api/Reports/CreditPhoneList',
                data: { creditNrs: response.data.creditNrs },
                responseType: 'arraybuffer'
            }).then((response) => {
                this.isLoading = false
                let excelMimetype = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
                let blobData = new Blob([response.data as string], { type: excelMimetype })
                download(blobData, 'phonelist_' + moment().format('YYYY-MM-DD') + '.xlsx', excelMimetype)
            }, (response) => {
                this.isLoading = false
                toastr.error('Failed!')
            })
        }, (response) => {
            this.isLoading = false
            toastr.error('Failed!')
        })
    }
}

var app = angular.module('app', ['ntech.forms', 'ntech.components'])
app.controller('phoneListCtrl', PhoneListCtrl)

module PhoneListNs {
    export interface IPreviewWorkListResult {
        creditNrs: Array<string>
    }
}