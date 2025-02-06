var app = angular.module('app', ['ntech.forms', 'ntech.components']);

class ManualPaymentCtr {
    static $inject = ['$http', '$q', '$timeout']
    constructor(
        private $http: ng.IHttpService,
        private $q: ng.IQService,
        private $timeout: ng.ITimeoutService
    ) {

        this.client = new NTechSavingsApi.ApiClient(msg => toastr.error(msg), $http, $q)
        this.backUrl = initialData.backUrl

        this.init()

        if (initialData.isTest) {
            this.testFunctions = [
                {
                    title: 'Force register payments',
                    run: () => {
                        if (!this.pendingPayments) {
                            toastr.info('No pending payments')
                            return
                        }
                        this.registerPayments(null)
                    }
                }
            ]
        }
    }

    testFunctions: any[]

    init() {
        this.isLoading = true
        this.client.keyValueStoreGet(this.KvKey, this.KvKeySpace).then(x => {
            this.isLoading = false
            if (x.Value) {
                let pendingPayments: ManualPaymentNs.PendingPaymentsModel = JSON.parse(x.Value)
                this.client.fetchUserNameByUserId(pendingPayments.initiatedByUserId).then(x => {
                    pendingPayments.initiatedByUserName = x.UserName
                    this.pendingPayments = pendingPayments
                    this.p = null
                    this.payments = null
                })
            } else {
                this.pendingPayments = null
                this.payments = []
                this.p = {
                    amount: '',
                    bookKeepingDate: moment(initialData.today).format('YYYY-MM-DD'),
                    noteText: ''
                }
            }
        })
    }

    isApproveAllowed() {
        if (!this.pendingPayments) {
            return false
        }
        return this.pendingPayments.initiatedByUserId !== initialData.userId
    }

    client: NTechSavingsApi.ApiClient

    KvKeySpace = 'SavingsManualPaymentsV1'
    KvKey = 'pendingPayments'

    pendingPayments: ManualPaymentNs.PendingPaymentsModel
    isLoading: boolean
    payments: ManualPaymentNs.PaymentModel[]
    f: ng.IFormController
    p: ManualPaymentNs.PaymentEditModel
    backUrl: string

    isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null) return true;

        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        } else {
            return false
        }
    }

    isValidPositiveDecimal = (value) => {
        if (this.isNullOrWhitespace(value))
            return true;
        var v = value.toString()
        return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v)
    }

    isValidDate = (value) => {
        if (this.isNullOrWhitespace(value))
            return true;
        return moment(value, "YYYY-MM-DD", true).isValid()
    }

    addPayment (evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!this.f.$valid) {
            return
        }

        let pmt: ManualPaymentNs.PaymentModel = {
            amount: parseFloat(this.p.amount.replace(',', '.')),
            bookKeepingDate: this.p.bookKeepingDate,
            noteText: this.p.noteText
        }

        let d = this.p.bookKeepingDate
        this.p = {
            amount: '',
            bookKeepingDate: d,
            noteText: ''
        }

        this.payments.push(pmt)

        this.f.$setPristine()
        $('#amount').focus()
    }

    removePayment(idx, evt) {
        if (evt) {
            evt.preventDefault()
        }
        this.payments.splice(idx, 1);
    }

   paymentSum(payments: ManualPaymentNs.PaymentModel[]) : number {
        let s = 0
        angular.forEach(payments, function (p) {
            s = s + p.amount
        })
        return s
    }

    cancelPendingPayments(evt) {
        if (evt) {
            evt.preventDefault()
        }

        this.isLoading = true
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(x => {
            this.isLoading = false
            this.init()
        })
    }

    registerPayments(evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!this.pendingPayments || this.pendingPayments.payments.length == 0) {
            return
        }

        let pendingPayments = this.pendingPayments
        this.isLoading = true

        //Remove before regiser since having to type them in again is way less bad than registering them twice
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(x => {
            this.$http({
                method: 'POST',
                url: initialData.registerManualPaymentUrl,
                data: { initiatedByUserId: pendingPayments.initiatedByUserId, requests: pendingPayments.payments }
            }).then((response) => {
                this.isLoading = false
                toastr.info('Payments registered')
                this.init()
            }, (response) => {
                this.isLoading = false
                toastr.error(response.statusText, 'Error');
            })
        })
    }

    beginRegisterPayments(evt) {
        if (evt) {
            evt.preventDefault()
        }
        if (!this.payments || this.payments.length == 0) {
            return
        }

        let model: ManualPaymentNs.PendingPaymentsModel = {
            initiatedByUserId: initialData.userId,
            initiatedDate: moment(initialData.today).format('YYYY-MM-DD'),
            payments: this.payments
        }

        this.isLoading = true
        this.client.keyValueStoreSet(this.KvKey, this.KvKeySpace, JSON.stringify(model)).then(x => {
            this.isLoading = false
            this.init()
        })
    }
}

app.controller('ctr', ManualPaymentCtr);

module ManualPaymentNs {
    export class PaymentEditModel {
        bookKeepingDate: string
        noteText: string
        amount: string
    }
    export class PaymentModel {
        bookKeepingDate: string
        noteText: string
        amount: number
    }
    export class PendingPaymentsModel {
        initiatedByUserId: number
        initiatedDate: string
        payments: PaymentModel[]
        initiatedByUserName?: string
    }
}