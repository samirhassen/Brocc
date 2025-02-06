class FatcaExportCtrl {
    static $inject = ['$q', '$http']

    backUrl: string
    isLoading: boolean
    exportProfileName: string
    exportYear: string
    allYears: string[]
    rows: NTechSavingsApi.FatcaExportFileModel[]
    apiClient: NTechSavingsApi.ApiClient
    

    constructor(
        $q: ng.IQService,
        $http: ng.IHttpService
    ) {
        this.backUrl = initialData.backUrl
        this.exportProfileName = initialData.exportProfileName

        this.allYears = []
        for (var i = -1; i < 2; i++) {
            this.allYears.push(moment(initialData.today).startOf('year').subtract(1, 'days').subtract(i, 'years').format('YYYY'))
        }

        this.exportYear = this.allYears[1] //Last year is the norm but you will sometimes want to use this year to check what the file will be like.

        this.apiClient = new NTechSavingsApi.ApiClient(x => toastr.error(x), $http, $q)
        window.scope = this
        this.refresh()
    }

    createFile(evt: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true
        this.apiClient.createFatcaExportFile(parseInt(this.exportYear), this.exportProfileName).then(x => {
            let hasFailedProfiles = (x.ExportResult && x.ExportResult.FailedProfileNames && x.ExportResult.FailedProfileNames.length > 0)
            let hasErrors = (x.ExportResult && x.ExportResult.Errors && x.ExportResult.Errors.length > 0)
            if (hasFailedProfiles || hasErrors) {
                toastr.warning('There were problems with the export')
            } else {
                toastr.info('Ok')
            }
            this.refresh()
        })
    }

    refresh() {
        this.isLoading = true
        this.apiClient.fetchFatcaExportFiles().then(x => {
            this.rows = x.Files        
            this.isLoading = false
        })
    }
}

var app = angular.module('app', ['ntech.forms'])
app.controller('fatcaExportCtr', FatcaExportCtrl)