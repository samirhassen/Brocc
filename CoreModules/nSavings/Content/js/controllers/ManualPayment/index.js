var app = angular.module('app', ['ntech.forms', 'ntech.components']);
class ManualPaymentCtr {
    constructor($http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.KvKeySpace = 'SavingsManualPaymentsV1';
        this.KvKey = 'pendingPayments';
        this.isValidPositiveDecimal = (value) => {
            if (this.isNullOrWhitespace(value))
                return true;
            var v = value.toString();
            return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v);
        };
        this.isValidDate = (value) => {
            if (this.isNullOrWhitespace(value))
                return true;
            return moment(value, "YYYY-MM-DD", true).isValid();
        };
        this.client = new NTechSavingsApi.ApiClient(msg => toastr.error(msg), $http, $q);
        this.backUrl = initialData.backUrl;
        this.init();
        if (initialData.isTest) {
            this.testFunctions = [
                {
                    title: 'Force register payments',
                    run: () => {
                        if (!this.pendingPayments) {
                            toastr.info('No pending payments');
                            return;
                        }
                        this.registerPayments(null);
                    }
                }
            ];
        }
    }
    init() {
        this.isLoading = true;
        this.client.keyValueStoreGet(this.KvKey, this.KvKeySpace).then(x => {
            this.isLoading = false;
            if (x.Value) {
                let pendingPayments = JSON.parse(x.Value);
                this.client.fetchUserNameByUserId(pendingPayments.initiatedByUserId).then(x => {
                    pendingPayments.initiatedByUserName = x.UserName;
                    this.pendingPayments = pendingPayments;
                    this.p = null;
                    this.payments = null;
                });
            }
            else {
                this.pendingPayments = null;
                this.payments = [];
                this.p = {
                    amount: '',
                    bookKeepingDate: moment(initialData.today).format('YYYY-MM-DD'),
                    noteText: ''
                };
            }
        });
    }
    isApproveAllowed() {
        if (!this.pendingPayments) {
            return false;
        }
        return this.pendingPayments.initiatedByUserId !== initialData.userId;
    }
    isNullOrWhitespace(input) {
        if (typeof input === 'undefined' || input == null)
            return true;
        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        }
        else {
            return false;
        }
    }
    addPayment(evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.f.$valid) {
            return;
        }
        let pmt = {
            amount: parseFloat(this.p.amount.replace(',', '.')),
            bookKeepingDate: this.p.bookKeepingDate,
            noteText: this.p.noteText
        };
        let d = this.p.bookKeepingDate;
        this.p = {
            amount: '',
            bookKeepingDate: d,
            noteText: ''
        };
        this.payments.push(pmt);
        this.f.$setPristine();
        $('#amount').focus();
    }
    removePayment(idx, evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.payments.splice(idx, 1);
    }
    paymentSum(payments) {
        let s = 0;
        angular.forEach(payments, function (p) {
            s = s + p.amount;
        });
        return s;
    }
    cancelPendingPayments(evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(x => {
            this.isLoading = false;
            this.init();
        });
    }
    registerPayments(evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.pendingPayments || this.pendingPayments.payments.length == 0) {
            return;
        }
        let pendingPayments = this.pendingPayments;
        this.isLoading = true;
        //Remove before regiser since having to type them in again is way less bad than registering them twice
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(x => {
            this.$http({
                method: 'POST',
                url: initialData.registerManualPaymentUrl,
                data: { initiatedByUserId: pendingPayments.initiatedByUserId, requests: pendingPayments.payments }
            }).then((response) => {
                this.isLoading = false;
                toastr.info('Payments registered');
                this.init();
            }, (response) => {
                this.isLoading = false;
                toastr.error(response.statusText, 'Error');
            });
        });
    }
    beginRegisterPayments(evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.payments || this.payments.length == 0) {
            return;
        }
        let model = {
            initiatedByUserId: initialData.userId,
            initiatedDate: moment(initialData.today).format('YYYY-MM-DD'),
            payments: this.payments
        };
        this.isLoading = true;
        this.client.keyValueStoreSet(this.KvKey, this.KvKeySpace, JSON.stringify(model)).then(x => {
            this.isLoading = false;
            this.init();
        });
    }
}
ManualPaymentCtr.$inject = ['$http', '$q', '$timeout'];
app.controller('ctr', ManualPaymentCtr);
var ManualPaymentNs;
(function (ManualPaymentNs) {
    class PaymentEditModel {
    }
    ManualPaymentNs.PaymentEditModel = PaymentEditModel;
    class PaymentModel {
    }
    ManualPaymentNs.PaymentModel = PaymentModel;
    class PendingPaymentsModel {
    }
    ManualPaymentNs.PendingPaymentsModel = PendingPaymentsModel;
})(ManualPaymentNs || (ManualPaymentNs = {}));
