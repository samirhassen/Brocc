var app = angular.module('app', ['ntech.forms', 'ntech.components']);
var NewFraudCheckCtr = /** @class */ (function () {
    function NewFraudCheckCtr($scope, //
    $http, $q, $timeout) {
        var _this = this;
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        window.scope = $scope; //for console debugging
        $scope.editMode = false;
        $scope.app = initialData;
        $scope.currentLanguage = function () {
            return "sv";
        };
        var apiClient = new NTechPreCreditApi.ApiClient(function (err) {
            toastr.error(err);
        }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, _this.$q, { applicationNr: initialData.applicationNr }, NavigationTargetHelper.NavigationTargetCode.UnsecuredLoanApplication);
        };
        fraudCheckSharedData.translateApp($scope.app);
        $scope.reject = function (evt, item, applicationNr, applicantNr) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.rejectItemUrl,
                data: {
                    fraudControlItemId: item.Id,
                    applicationNr: applicationNr,
                    applicantNr: applicantNr
                }
            }).then(function successCallback(response) {
                item.Status = 'Rejected';
                item.DecisionByName = response.data;
            }, function errorCallback(response) {
                location.reload();
            });
        };
        $scope.approve = function (evt, item, applicationNr, applicantNr) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.approveItemUrl,
                data: {
                    fraudControlItemId: item.Id,
                    applicationNr: applicationNr,
                    applicantNr: applicantNr
                }
            }).then(function successCallback(response) {
                item.Status = 'Approved';
                item.DecisionByName = response.data;
            }, function errorCallback(response) {
                location.reload();
            });
        };
        $scope.verify = function (evt, item, applicationNr, applicantNr) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.verifyItemUrl,
                data: {
                    fraudControlItemId: item.Id,
                    applicationNr: applicationNr,
                    applicantNr: applicantNr
                }
            }).then(function successCallback(response) {
                item.Status = 'Verified';
                item.DecisionByName = response.data;
            }, function errorCallback(response) {
                location.reload();
            });
        };
        $scope.unlock = function (evt, sensitiveItem) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.unlockSensitiveItemUrl,
                data: { item: sensitiveItem }
            }).then(function successCallback(response) {
                sensitiveItem.Locked = false;
                sensitiveItem.Value = response.data;
            }, function errorCallback(response) {
                location.reload();
            });
        };
        $scope.edit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.editMode = true;
        };
        $scope.cancel = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.editMode = false;
        };
        $scope.save = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $http({
                method: 'POST',
                url: $scope.app.saveAmlCftUrl,
                data: {
                    amlCftModel: $scope.app.amlCftModel,
                    customerId: $scope.app.customerId
                }
            }).then(function successCallback(response) {
                $scope.editMode = false;
            }, function errorCallback(response) {
                location.reload();
            });
        };
        $scope.pickItemByKey = function (list, keyName, keyValue) {
            var item = null;
            angular.forEach(list, function (v) {
                if (v[keyName] === keyValue) {
                    item = v;
                }
            });
            return item;
        };
        $scope.formatPhoneNr = function (nr) {
            if (!nr) {
                return nr;
            }
            var p = ntech.libphonenumber.parsePhoneNr(nr, ntechClientCountry);
            if (p.isValid) {
                return p.validNumber.standardDialingNumber;
            }
            else {
                return nr;
            }
        };
        $scope.formatValue = function (i) {
            if (i.Key === 'PhoneCheck') {
                return $scope.formatPhoneNr(i.Value);
            }
            else {
                return i.Value;
            }
        };
    }
    NewFraudCheckCtr.$inject = ['$scope', '$http', '$q', '$timeout'];
    return NewFraudCheckCtr;
}());
app.controller('ctr', NewFraudCheckCtr);
