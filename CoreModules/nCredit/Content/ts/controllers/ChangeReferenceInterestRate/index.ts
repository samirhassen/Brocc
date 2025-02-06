var app = angular.module('app', ['ntech.forms', 'ntech.components']);

namespace ChangeReferenceInterestRateComponentNs {
    export class ChangeReferenceInterestRateController {
        static $inject = ['$http', '$q', '$scope', '$timeout', 'ntechComponentService']
        constructor(private $http: ng.IHttpService,
            private $q: ng.IQService,
            private $scope: ng.IScope,
            private $timeout: ng.ITimeoutService,
            private ntechComponentService: NTechComponents.NTechComponentService) {
            this.p = {}
            this.currentReferenceInterestRate = initialData.currentReferenceInterestRate
            this.apiClient = new NTechCreditApi.ApiClient(x => toastr.error(x), $http, $q)
            this.pagingHelper = new NTechTables.PagingHelper($q, $http)
            this.gotoPage(0, null)

            window.scope = $scope
            window.ctrChangeReferenceInterestRate = this

            this.apiClient.fetchPendingReferenceInterestChange().then(x => {
                if (x) {
                    this.pending = x

                    if (initialData.isTest) {
                        this.testFunctions = [
                            {
                                title: 'Force approve',
                                run: () => {
                                    this.commitChange(true, null)
                                }
                            }
                        ]
                    }
                } else {
                    this.initial = {
                        newReferenceInterestRate: ''
                    }
                }
            })
        }

        apiClient: NTechCreditApi.ApiClient
        isLoading: boolean
        p: any
        files: NTechCreditApi.FetchReferenceInterestChangePageResult
        currentReferenceInterestRate: number

        filesPaging: NTechTables.PagingObject
        nrOfCreditsUpdated: number
        overrideSafeguard: boolean
        pagingHelper: NTechTables.PagingHelper
        testFunctions: any[]

        initial: InitialModel
        calculated: CalculateModel
        pending: NTechCreditApi.PendingReferenceInterestChangeModel

        onBack(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, new NTechCreditApi.ApiClient(toastr.error, this.$http, this.$q), this.$q)
        }

        calculate(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.isLoading = true
            this.$timeout(() => { //Fake serverside call to make it more apparent to the user that something happend
                this.calculated = {
                    newReferenceInterestRate: this.newValue(),
                    userName: initialData.currentUserDisplayName,
                    now: initialData.now
                }
                this.isLoading = false
            }, 100)
        }

        beginChange(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.isLoading = true
            this.apiClient.beginReferenceInterestChange(this.calculated.newReferenceInterestRate).then(x => {
                this.initial = null
                this.calculated = null
                this.pending = x
                this.isLoading = false
            })
        }

        cancelChange(evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.isLoading = true
            this.apiClient.cancelPendingReferenceInterestChange().then(() => {
                this.pending = null
                this.initial = {
                    newReferenceInterestRate: ''
                }
                this.isLoading = false
            })
        }

        commitChange(requestOverrideDuality: boolean, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }
            this.isLoading = true
            this.apiClient.commitPendingReferenceInterestChange(this.pending.NewInterestRatePercent, requestOverrideDuality).then(x => {
                this.currentReferenceInterestRate = this.pending.NewInterestRatePercent
                this.initial = {
                    newReferenceInterestRate: ''
                }
                this.pending = null
                this.isLoading = false
                this.gotoPage(0, evt)
            })
        }

        isChangeAllowed() {
            return (this.f().$valid && (this.isReasonableChange() || this.overrideSafeguard))
        }

        changeSize() {
            if (this.f().$invalid) {
                return NaN
            }
            return Math.abs(this.newValue() - this.currentReferenceInterestRate)
        }

        newValue() {
            if (this.f().$invalid) {
                return NaN
            }
            return parseFloat(this.initial.newReferenceInterestRate.replace(',', '.'))
        }

        isCurrentUser(userId: number) {
            return initialData.currentUserId === userId
        }

        f(): ng.IFormController {
            let s: any = this.$scope
            return s.f
        }

        isValidDecimal(v: any) {
            return ntech.forms.isValidDecimal(v)
        }

        isReasonableChange() {
            if (this.f().$invalid) {
                return false
            }
            return (this.changeSize() < 5.0)
        }

        gotoPage(pageNr: number, evt: Event) {
            if (evt) {
                evt.preventDefault()
            }

            this.isLoading = true
            this.apiClient.fetchReferenceInterestChangePage(50, pageNr).then(x => {
                this.isLoading = false
                this.files = x
                this.updatePaging()
            })
        }

        updatePaging() {
            if (!this.files) {
                return {}
            }
            var h = this.files

            this.filesPaging = this.pagingHelper.createPagingObjectFromPageResult({
                CurrentPageNr: h.CurrentPageNr,
                TotalNrOfPages: h.TotalNrOfPages
            })
        }
    }

    export class CalculateModel {
        newReferenceInterestRate: number
        userName: string
        now: Date
    }

    export class InitialModel {
        newReferenceInterestRate: string
    }
}

app.controller('ctr', ChangeReferenceInterestRateComponentNs.ChangeReferenceInterestRateController)