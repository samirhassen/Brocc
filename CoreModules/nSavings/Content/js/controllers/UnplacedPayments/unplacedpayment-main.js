var app = angular.module('app', ['ntech.forms']);
app
    .factory('sharedService', ['$http', function ($http) {
        var d = { isLoading: false };
        if (initialData && initialData.backUrl) {
            d.backUrl = initialData.backUrl;
        }
        else {
            d.backUrl = null;
        }
        d.uiMode = 'select';
        d.gotoPlace = function (savingsAccountNr, placeAmount, allowOverMaxAllowedSavingsCustomerBalance, onFail, onSuccess) {
            d.repayData = null;
            d.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.paymentPlacementSuggestionUrl,
                data: { savingsAccountNr: savingsAccountNr, placeAmount: placeAmount, paymentId: initialData.payment.Id, allowOverMaxAllowedSavingsCustomerBalance: allowOverMaxAllowedSavingsCustomerBalance }
            }).then(function successCallback(response) {
                if (response.data.FailedMessage) {
                    if (onFail) {
                        onFail(response.data.FailedMessage);
                    }
                }
                else {
                    d.placeData = response.data;
                    d.uiMode = 'place';
                    if (onSuccess) {
                        onSuccess();
                    }
                }
                d.isLoading = false;
            }, function errorCallback(response) {
                d.isLoading = false;
                toastr.error('Failed!');
            });
        };
        d.gotoRepay = function (iban, repaymentAmount, repaymentName, unplacedAmount, paymentId, ibanFormatted) {
            d.placeData = null;
            d.repayData = {
                iban: iban,
                customerName: repaymentName,
                unplacedAmount: unplacedAmount,
                repaymentAmount: repaymentAmount,
                paymentId: paymentId,
                ibanFormatted: ibanFormatted
            };
            d.uiMode = 'repay';
        };
        return d;
    }])
    .controller('loadingApp', ['$scope', 'sharedService', function ($scope, sharedService) {
        $scope.isLoading = sharedService.isLoading;
        $scope.$watch(function () { return sharedService.isLoading; }, function () {
            $scope.isLoading = sharedService.isLoading;
        });
    }])
    .controller('backApp', ['$scope', 'sharedService', function ($scope, sharedService) {
        $scope.$watch(function () { return sharedService.backUrl; }, function () {
            $scope.backUrl = sharedService.backUrl;
        });
    }])
    .controller('placeorrepay', ['$scope', '$http', 'sharedService', '$q', '$timeout', function ($scope, $http, sharedService, $q, $timeout) {
        $scope.payment = initialData.payment;
        $scope.isPlaceMode = true;
        function isNullOrWhitespace(input) {
            if (typeof input === 'undefined' || input == null)
                return true;
            if ($.type(input) === 'string') {
                return $.trim(input).length < 1;
            }
            else {
                return false;
            }
        }
        $scope.verifyRepay = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if ($scope.repayform.$invalid) {
                return;
            }
            if ($scope.repaymentAmount > $scope.payment.UnplacedAmount || $scope.repaymentAmount <= 0) {
                $scope.isRepaymentAmountNotWithinBounds = true;
                return;
            }
            $scope.isRepaymentAmountNotWithinBounds = false;
            sharedService.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.validateAccountNrUrl,
                data: { value: $scope.repaymentIBAN }
            }).then(function (result) {
                sharedService.isLoading = false;
                if (result.data.isValid) {
                    sharedService.gotoRepay($scope.repaymentIBAN, $scope.repaymentAmount, $scope.repaymentName, $scope.payment.UnplacedAmount, initialData.payment.Id, result.data.ibanFormatted);
                }
            }, function (result) {
                toastr.error('Failed!');
                sharedService.isLoading = false;
            });
        };
        $scope.verifyPlace = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            if ($scope.placeform.$invalid) {
                return;
            }
            sharedService.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.findSavingsAccountByReferenceNrOrSavingsAccountNrUrl,
                data: { searchString: $scope.searchString }
            }).then(function successCallback(response) {
                var d = response.data;
                if (d.isOk) {
                    $scope.placeFailedMessage = null;
                    sharedService.gotoPlace(d.savingsAccountNr, $scope.placeAmount, $scope.allowOverMaxAllowedSavingsCustomerBalance, function (failedMsg) { $scope.placeFailedMessage = failedMsg; }, function () { $scope.placeFailedMessage = null; });
                }
                else {
                    $scope.placeFailedMessage = d.failedMessage;
                }
                sharedService.isLoading = false;
            }, function errorCallback(response) {
                sharedService.isLoading = false;
                toastr.error('Failed!');
            });
        };
        $scope.isValidBankAccount = function (input) {
            var deferred = $q.defer();
            $http({
                method: 'POST',
                url: initialData.validateAccountNrUrl,
                data: { value: input }
            }).then(function (result) {
                if (result.data.isValid) {
                    deferred.resolve(input);
                    $scope.validBankAccountInfo = {
                        displayValue: result.data.displayValue
                    };
                }
                else {
                    deferred.reject(result.data.message);
                    $scope.validBankAccountInfo = null;
                }
            }, function (result) {
                deferred.reject("Server did not respond properly");
                $scope.validBankAccountInfo = null;
            });
            return deferred.promise;
        };
        if (ntechClientCountry === 'FI') {
            $scope.accountNrMask = 'ex. FI1231432432';
            $scope.accountNrFieldLabel = 'IBAN';
        }
        else {
            $scope.accountNrMask = '';
            $scope.accountNrFieldLabel = 'Bank account nr';
        }
        $scope.itemsExcept = function (skippedNames) {
            var p = [];
            angular.forEach($scope.payment.Items, function (v) {
                if ($.inArray(v.Name, skippedNames) === -1) {
                    p.push(v);
                }
            });
            return p;
        };
        $scope.itemExists = function (name) {
            var exists = false;
            angular.forEach($scope.payment.Items, function (v) {
                if (v.Name === name) {
                    exists = true;
                }
            });
            return exists;
        };
        $scope.itemLabel = function (name) {
            return name;
        };
        $scope.itemValue = function (name) {
            var val = null;
            angular.forEach($scope.payment.Items, function (v) {
                if (v.Name === name) {
                    if (v.IsEncrypted) {
                        val = '----';
                    }
                    else {
                        val = v.Value;
                    }
                }
            });
            return val;
        };
        $scope.unlock = function (item, evt) {
            if (evt) {
                evt.preventDefault();
            }
            sharedService.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.fetchEncryptedPaymentItemValue,
                data: { paymentItemId: item.ItemId }
            }).then(function successCallback(response) {
                var d = response.data;
                item.IsEncrypted = false;
                item.Value = d.Value;
                sharedService.isLoading = false;
            }, function errorCallback(response) {
                sharedService.isLoading = false;
                toastr.error('Failed!');
            });
        };
        window.placeorrepayScope = $scope;
    }]);
