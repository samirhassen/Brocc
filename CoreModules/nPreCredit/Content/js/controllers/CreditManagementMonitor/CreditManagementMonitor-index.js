var app = angular.module('app', ['pascalprecht.translate', 'ngCookies', 'ntech.forms', 'ntech.components']);
var CreditManagementMonitorCtr = /** @class */ (function () {
    function CreditManagementMonitorCtr($scope, //ng.IScope
    $http, $q, $timeout) {
        this.$http = $http;
        this.$q = $q;
        this.$timeout = $timeout;
        $scope.backUrl = initialData.backUrl;
        $scope.providers = initialData.providers;
        var providerDisplayNameByName = {};
        angular.forEach(initialData.providers, function (v) {
            providerDisplayNameByName[v.ProviderName] = v.DisplayName;
        });
        function settingWithMemory(variableName, defaultValue) {
            var storageSettingName = "ntech_cmm_filter_v1_" + variableName;
            if (localStorage) {
                var storedValue = localStorage.getItem(storageSettingName);
                if (storedValue) {
                    $scope[variableName] = JSON.parse(storedValue).value;
                }
                else {
                    $scope[variableName] = defaultValue;
                }
                $scope.$watch(function () { return $scope[variableName]; }, function (newVal, oldVal) {
                    if (newVal !== oldVal) {
                        localStorage.setItem(storageSettingName, JSON.stringify({ value: newVal }));
                    }
                });
            }
            else {
                $scope[variableName] = defaultValue;
            }
        }
        settingWithMemory('providerName', '*');
        settingWithMemory('timeSpan', 'yesterday');
        settingWithMemory('includeDetails', true);
        function unCamelCase(str) {
            return str
                // insert a space between lower & upper
                .replace(/([a-z])([A-Z])/g, '$1 $2')
                // space before last upper in a sequence followed by lower
                .replace(/\b([A-Z]+)([A-Z])([a-z])/, '$1 $2$3')
                // uppercase the first character
                .replace(/^./, function (str) { return str.toUpperCase(); });
        }
        $scope.getDisplayRejectionReason = function (e) {
            if (initialData.rejectionReasonToDisplayNameMapping[e]) {
                return initialData.rejectionReasonToDisplayNameMapping[e];
            }
            else {
                return unCamelCase(e);
            }
        };
        $scope.getProviderDisplayName = function (e) {
            if (providerDisplayNameByName[e]) {
                return providerDisplayNameByName[e];
            }
            else {
                return e;
            }
        };
        $scope.refresh = function (evt, onSuccess) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.refreshUrl,
                data: {
                    providerName: $scope.providerName,
                    timeSpan: $scope.timeSpan,
                    includeDetails: $scope.includeDetails,
                    nrOfAutoRejectionReasonsToShow: 4,
                    nrOfManualRejectionReasonsToShow: 4
                }
            }).then(function successCallback(response) {
                $scope.d = response.data.result;
                $scope.isLoading = false;
                if (onSuccess) {
                    onSuccess();
                }
            }, function errorCallback(response) {
                toastr.error(response.data.message, 'Error');
                $scope.isLoading = false;
                $scope.d = null;
                $scope.isBroken = true;
            });
        };
        function refreshAndQueueNext(skipQueue) {
            $scope.refresh(null, function () {
                $timeout(function () {
                    if (!skipQueue) {
                        refreshAndQueueNext(false);
                    }
                }, 1000 * 60 * 2);
            });
        }
        refreshAndQueueNext(false);
        $scope.getCount = function (categoryCode) {
            if (!$scope.d) {
                return;
            }
            var c;
            angular.forEach($scope.d.Categories, function (v) {
                if (v.CategoryCode == categoryCode) {
                    c = v;
                }
            });
            if (c) {
                return c.Count;
            }
            else {
                return 0;
            }
        };
        $scope.getPercent = function (categoryCode) {
            if (!$scope.d) {
                return;
            }
            var c;
            angular.forEach($scope.d.Categories, function (v) {
                if (v.CategoryCode == categoryCode) {
                    c = v;
                }
            });
            if (c) {
                return c.Percent;
            }
            else {
                return 0;
            }
        };
        $scope.$watch(function () { return $scope.timeSpan; }, function (newVal, oldVal) {
            if (newVal !== oldVal) {
                $scope.refresh();
            }
        });
        $scope.$watch(function () { return $scope.providerName; }, function (newVal, oldVal) {
            if (newVal !== oldVal) {
                $scope.refresh();
            }
        });
        $scope.$watch(function () { return $scope.includeDetails; }, function (newVal, oldVal) {
            if (newVal !== oldVal) {
                $scope.refresh();
            }
        });
        window.scope = $scope;
    }
    CreditManagementMonitorCtr.$inject = ['$scope', '$http', '$q', '$timeout', '$timeout', '$translate'];
    return CreditManagementMonitorCtr;
}());
app.controller('ctr', CreditManagementMonitorCtr);
