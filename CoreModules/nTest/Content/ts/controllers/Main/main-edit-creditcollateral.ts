var app = angular.module('app', ['ntech.forms', 'angular-jsoneditor'])

class EditCreditCollateralCtr {
    static $inject = ['$scope', '$http', '$q', '$timeout']
    constructor(
        $scope: ng.IScope,
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {
        this.initialDataTyped = initialData;
        this.backUrl = this.initialDataTyped.backUrl
        window['ntechDebug'] = window['ntechDebug'] || {}
        window['ntechDebug']['editCreditCollateralCtr'] = this

        this.apiClient = new NTechTestApi.ApiClient(msg => toastr.error(msg), $http, $q, x => this.isLoading = x)
        this.initial = {
            creditNr: ''
        }
    }

    state: string = 'initial'
    backUrl: string

    apiClient: NTechTestApi.ApiClient
    isLoading: boolean = false
    initialDataTyped: EditCreditCollateralNs.IInitialData
    initial: {
        creditNr: string
    }

    edit: {
        creditNr: string
        model: any
    }

    loadCredit(evt?: Event) {
        if (evt) {
            evt.preventDefault()
        }

        this.apiClient.getCreditKeyValueItem(EditCreditCollateralNs.CollateralsKeySpace, this.initial.creditNr).then(x => {
            if (!x.Value) {
                toastr.warning('No collateral model found')
                return
            }
            this.edit = {
                creditNr: this.initial.creditNr,
                model: JSON.parse(x.Value)
            }
            this.initial = null
        })
    }

    saveEdit(evt?: Event) {
        if (evt) {
            evt.preventDefault()
        }
        this.apiClient.setCreditKeyValueItem(EditCreditCollateralNs.CollateralsKeySpace, this.edit.creditNr, JSON.stringify(this.edit.model)).then(x => {
            this.initial = {
                creditNr: this.edit.creditNr
            }
            this.edit = null
        })
    }
}

app.controller('editCreditCollateralCtr', EditCreditCollateralCtr)

module EditCreditCollateralNs {
    export const CollateralsKeySpace: string = 'MortgageLoanCollateralsV1'
    export interface IInitialData {
        backUrl: string
        currentTime: Date
    }
}