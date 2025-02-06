var app = angular.module('app', []);
app
    .controller('ctr', ['$scope', '$http', '$timeout', function ($scope, $http, $timeout) {
        $scope.reset = function () {
            $scope.p = {
                payments: [],
                bookkeepingDate: moment(initialData.currentTime).format('YYYY-MM-DD'),
                clientIban: initialData.baseCountry == 'SE' ? '9020033' : 'FI2112345600000785'
            }
            $scope.download = null
        }
        $scope.reset()

        function post<TPostData>(url: string, data: TPostData, callback: (response: any) => void) {
            $scope.isLoading = true
            $http({
                method: 'POST',
                url: url,
                data: data
            }).then((response:any) => {
                $scope.isLoading = false
                return callback(response);
            }, (_: any) => {
                $scope.isLoading = false
                toastr.error('Failed!');
            })
        }

        $scope.addPayment = function () {
            if ($scope.p.paymentCreditNr) {
                post('/Api/GetCreditOrSavingsPaymentInfo', {
                    nr: $scope.p.paymentCreditNr
                }, (response : any) => {
                    if (response.data.exists) {
                        $scope.p.payments.push({
                            amount: response.data.amount,
                            reference: response.data.reference,
                            payerName: response.data.payerName,
                            bookKeepingDate: $scope.p.bookkeepingDate,
                            active: '1'
                        })
                        $scope.p.paymentCreditNr = ''
                    } else {
                        toastr.error('No such credit/savings account!')
                    }
                });
            } else {
                $scope.p.payments.push({ amount: '', reference: '', payerName: '', bookKeepingDate: '', active: '1' })
            }
        }
        $scope.autoFillUnpaidInvoices = function () {
            post('/Api/GetUnpaidInvoices', {}, (response: any) => {
                for (let x of response.data.payments) {
                    $scope.p.payments.push({ amount: x.UnpaidAmount, reference: x.OcrPaymentReference, payerName: 'Payment on ' + x.CreditNrsText, bookKeepingDate: $scope.p.bookkeepingDate, active: '1' })
                }
            });
        }
        $scope.createFile = function () {
            $scope.isLoading = true

            var ps: any[] = []
            angular.forEach($scope.p.payments, function (v) {
                if (v.active === '1') {
                    ps.push(v)
                }
            })

            post('/Api/CreatePaymentFile', {
                fileformat: initialData.baseCountry == 'SE' ? 'bgmax' : 'camt.054.001.02',
                payments: ps,
                bookkeepingDate: $scope.p.bookkeepingDate,
                clientIban: $scope.p.clientIban
            }, (response: any) => {
                $scope.download = {
                    url: response.data.url,
                    fileName: response.data.fileName
                }
            });
        }
    }])