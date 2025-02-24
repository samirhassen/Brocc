var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var ManualPaymentCtr = /** @class */ (function () {
    function ManualPaymentCtr($http, $q, $timeout) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        this.KvKeySpace = 'CreditManualPaymentsV1';
        this.KvKey = 'pendingPayments';
        this.isValidPositiveDecimal = function (value) {
            if (_this.isNullOrWhitespace(value))
                return true;
            var v = value.toString();
            return (/^([0]|[1-9]([0-9])*)([\.|,]([0-9])+)?$/).test(v);
        };
        this.isValidDate = function (value) {
            if (_this.isNullOrWhitespace(value))
                return true;
            return moment(value, "YYYY-MM-DD", true).isValid();
        };
        this.client = new NTechCreditApi.ApiClient(function (msg) { return toastr.error(msg); }, $http, $q);
        this.isDualityRequired = initialData.isDualityRequired;
        this.init();
        if (initialData.isTest) {
            this.testFunctions = [
                {
                    title: 'Force register payments',
                    run: function () {
                        if (!_this.pendingPayments) {
                            toastr.info('No pending payments');
                            return;
                        }
                        _this.registerPayments(null);
                    }
                }
            ];
        }
    }
    ManualPaymentCtr.prototype.onBack = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, this.client, this.$q);
    };
    ManualPaymentCtr.prototype.init = function () {
        var _this = this;
        this.isLoading = true;
        this.client.keyValueStoreGet(this.KvKey, this.KvKeySpace).then(function (x) {
            if (x.Value) {
                var pendingPayments_1 = JSON.parse(x.Value);
                _this.client.fetchUserNameByUserId(pendingPayments_1.initiatedByUserId).then(function (x) {
                    _this.isLoading = false;
                    pendingPayments_1.initiatedByUserName = x.UserName;
                    _this.pendingPayments = pendingPayments_1;
                    _this.p = null;
                    _this.payments = null;
                });
            }
            else {
                _this.isLoading = false;
                _this.pendingPayments = null;
                _this.payments = [];
                _this.p = {
                    amount: '',
                    bookKeepingDate: moment(initialData.today).format('YYYY-MM-DD'),
                    noteText: ''
                };
            }
        });
    };
    ManualPaymentCtr.prototype.isNullOrWhitespace = function (input) {
        if (typeof input === 'undefined' || input == null)
            return true;
        if ($.type(input) === 'string') {
            return $.trim(input).length < 1;
        }
        else {
            return false;
        }
    };
    ManualPaymentCtr.prototype.isApproveAllowed = function () {
        if (!this.pendingPayments) {
            return false;
        }
        return !this.isDualityRequired || (this.pendingPayments.initiatedByUserId !== initialData.userId);
    };
    ManualPaymentCtr.prototype.addPayment = function (evt) {
        if (evt) {
            evt.preventDefault();
        }
        if (!this.f.$valid) {
            return;
        }
        var pmt = {
            amount: parseFloat(this.p.amount.replace(',', '.')),
            bookKeepingDate: this.p.bookKeepingDate,
            noteText: this.p.noteText
        };
        var d = this.p.bookKeepingDate;
        this.p = {
            amount: '',
            bookKeepingDate: d,
            noteText: ''
        };
        this.payments.push(pmt);
        this.f.$setPristine();
        $('#amount').focus();
    };
    ManualPaymentCtr.prototype.removePayment = function (idx, evt) {
        if (evt) {
            evt.preventDefault();
        }
        this.payments.splice(idx, 1);
    };
    ManualPaymentCtr.prototype.paymentSum = function (payments) {
        var s = 0;
        angular.forEach(payments, function (p) {
            s = s + p.amount;
        });
        return s;
    };
    ManualPaymentCtr.prototype.cancelPendingPayments = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        this.isLoading = true;
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(function (x) {
            _this.isLoading = false;
            _this.init();
        });
    };
    ManualPaymentCtr.prototype.registerPayments = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        if (!this.pendingPayments || this.pendingPayments.payments.length == 0) {
            return;
        }
        var pendingPayments = this.pendingPayments;
        this.isLoading = true;
        //Remove before register since having to type them in again is way less bad than registering them twice
        this.client.keyValueStoreRemove(this.KvKey, this.KvKeySpace).then(function (x) {
            _this.$http({
                method: 'POST',
                url: initialData.registerManualPaymentUrl,
                data: { initiatedByUserId: pendingPayments.initiatedByUserId, requests: pendingPayments.payments }
            }).then(function (response) {
                _this.isLoading = false;
                toastr.info('Payments registered');
                _this.init();
            }, function (response) {
                _this.isLoading = false;
                toastr.error(response.statusText, 'Error');
            });
        });
    };
    ManualPaymentCtr.prototype.beginRegisterPayments = function (evt) {
        var _this = this;
        if (evt) {
            evt.preventDefault();
        }
        if (!this.payments || this.payments.length == 0) {
            return;
        }
        var model = {
            initiatedByUserId: initialData.userId,
            initiatedDate: moment(initialData.today).format('YYYY-MM-DD'),
            payments: this.payments
        };
        this.isLoading = true;
        this.client.keyValueStoreSet(this.KvKey, this.KvKeySpace, JSON.stringify(model)).then(function (x) {
            _this.isLoading = false;
            _this.init();
        });
    };
    ManualPaymentCtr.$inject = ['$http', '$q', '$timeout'];
    return ManualPaymentCtr;
}());
app.controller('ctr', ManualPaymentCtr);
var ManualPaymentNs;
(function (ManualPaymentNs) {
    var PaymentEditModel = /** @class */ (function () {
        function PaymentEditModel() {
        }
        return PaymentEditModel;
    }());
    ManualPaymentNs.PaymentEditModel = PaymentEditModel;
    var PaymentModel = /** @class */ (function () {
        function PaymentModel() {
        }
        return PaymentModel;
    }());
    ManualPaymentNs.PaymentModel = PaymentModel;
    var PendingPaymentsModel = /** @class */ (function () {
        function PendingPaymentsModel() {
        }
        return PendingPaymentsModel;
    }());
    ManualPaymentNs.PendingPaymentsModel = PendingPaymentsModel;
})(ManualPaymentNs || (ManualPaymentNs = {}));
