var app = angular.module('app', ['ntech.forms']);
app.controller('ctr', ['$scope', '$http', '$q', function ($scope, $http, $q) {
        window.scope = $scope;
        var apiClient = new NTechPreCreditApi.ApiClient(function (err) {
            toastr.error(err);
        }, $http, $q);
        $scope.onBack = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q);
        };
        $scope.search = function (civicRegNr, evt) {
            if (evt) {
                evt.preventDefault();
            }
            if (!civicRegNr) {
                return;
            }
            $scope.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.fetchStateAndHistoryUrl,
                data: {
                    civicRegNr: civicRegNr
                }
            }).then(function successCallback(response) {
                $scope.isLoading = false;
                $scope.customerModel = {
                    civicRegNr: civicRegNr,
                    customerId: response.data.customerId,
                    currentState: response.data.currentState,
                    historyStates: response.data.historyStates
                };
                if (!$scope.customerModel.currentState) {
                    $scope.customerModel.currentState = {};
                }
                if (!$scope.customerModel.historyStates) {
                    $scope.customerModel.historyStates = [];
                }
            }, function errorCallback(response) {
                toastr.error(response.data.message, 'Error');
                $scope.isLoading = false;
            });
        };
        if (initialData.civicRegNr) {
            $scope.searchModel = null;
            $scope.search(initialData.civicRegNr);
        }
        else {
            $scope.searchModel = {};
        }
        if (ntechClientCountry === 'FI') {
            $scope.civicRegNrMask = '(DDMMYYSNNNK)';
            $scope.isValidCivicNr = function (value) {
                if (ntech.forms.isNullOrWhitespace(value))
                    return true;
                return ntech.fi.isValidCivicNr(value);
            };
        }
        else if (ntechClientCountry === 'SE') {
            $scope.civicRegNrMask = '(YYYYMMDDRRRC)';
            $scope.isValidCivicNr = function (value) {
                if (ntech.forms.isNullOrWhitespace(value))
                    return true;
                return ntech.se.isValidCivicNr(value);
            };
        }
        $scope.resetSearch = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.searchModel = {};
            $('#searchCivicRegNr').focus();
        };
        function unlockReason(id, withResult) {
            if (!id) {
                withResult('');
            }
            else {
                $scope.isLoading = true;
                $http({
                    method: 'POST',
                    url: initialData.fetchReasonTextUrl,
                    data: {
                        checkpointId: id
                    }
                }).then(function successCallback(response) {
                    $scope.isLoading = false;
                    withResult(response.data.reasonText);
                }, function errorCallback(response) {
                    toastr.error(response.data.message, 'Error');
                    $scope.isLoading = false;
                });
            }
        }
        $scope.unlockCurrentReason = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            unlockReason($scope.customerModel.currentState.Id, function (clearText) {
                $scope.customerModel.currentState.clearTextReason = clearText;
                $scope.customerModel.currentState.isClearTextReasonUnlocked = true;
            });
        };
        $scope.unlockHistoricalReason = function (item, evt) {
            if (evt) {
                evt.preventDefault();
            }
            unlockReason(item.Id, function (clearText) {
                item.clearTextReason = clearText;
                item.isClearTextReasonUnlocked = true;
            });
        };
        $scope.beginEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            var actualBeginEdit = function () {
                $scope.customerModel.currentStateEditCopy = angular.copy($scope.customerModel.currentState);
            };
            if ($scope.customerModel.currentState.IsCheckpointActive && !$scope.customerModel.currentState.isClearTextReasonUnlocked) {
                unlockReason($scope.customerModel.currentState.Id, function (clearText) {
                    $scope.customerModel.currentState.clearTextReason = clearText;
                    $scope.customerModel.currentState.isClearTextReasonUnlocked = true;
                    actualBeginEdit();
                });
            }
            else {
                actualBeginEdit();
            }
        };
        $scope.commitEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.isLoading = true;
            var isActive = !!$scope.customerModel.currentStateEditCopy.IsCheckpointActive;
            $http({
                method: 'POST',
                url: initialData.setCheckpointStateUrl,
                data: {
                    customerId: $scope.customerModel.customerId,
                    isActive: isActive,
                    reasonText: isActive ? $scope.customerModel.currentStateEditCopy.clearTextReason : null
                }
            }).then(function successCallback(response) {
                $scope.search($scope.customerModel.civicRegNr);
            }, function errorCallback(response) {
                toastr.error(response.data.message, 'Error');
                $scope.isLoading = false;
            });
        };
        $scope.cancelEdit = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            $scope.customerModel.currentStateEditCopy = null;
        };
    }]);
