var app = angular.module('app', ['ntech.forms']);
app
    .controller('ctr', ['$scope', '$http', '$timeout', '$q', function ($scope, $http, $timeout, $q) {
        $scope.calculateModel = {}

        let apiClient = new NTechCreditApi.ApiClient(toastr.error, $http, $q)
        $scope.onBack = (evt) => {
            if (evt) {
                evt.preventDefault()
            }
            NavigationTargetHelper.handleBackWithInitialDataDefaults(initialData, apiClient, $q)
        }

        $scope.calculate = function (evt) {
            if (evt) {
                evt.preventDefault()
            }

            $scope.confirmationModel = null
            $scope.isLoading = true
            var creditNr = $scope.calculateModel.creditNr
            $http({
                method: 'POST',
                url: '/Api/CorrectAndCloseCredit/Calculate',
                data: { creditNr: creditNr }
            }).then(function successCallback(response) {
                $scope.isLoading = false
                $scope.calculateModel = {}
                if (response.data.isOk) {
                    $scope.suggestionModel = response.data.suggestion
                } else {
                    $scope.confirmationModel = { isOk: false, failedMessage: response.data.failedMessage }
                }
            }, function errorCallback(response) {
                $scope.calculateModel = {}
                $scope.confirmationModel = null
                $scope.suggestionModel = null
                $scope.isLoading = false
                toastr.error(response.statusText, 'Error');
            })
        }

        $scope.confirm = function (evt) {
            if (evt) {
                evt.preventDefault()
            }

            $scope.confirmationModel = null
            $scope.isLoading = true
            var creditNr = $scope.calculateModel.creditNr
            $http({
                method: 'POST',
                url: '/Api/CorrectAndCloseCredit/CorrectAndClose',
                data: { creditNr: $scope.suggestionModel.creditNr }
            }).then(function successCallback(response) {
                $scope.isLoading = false
                $scope.calculateModel = {}
                $scope.confirmationModel = { isOk: response.data.isOk, failedMessage: response.data.failedMessage, creditNr: response.data.creditNr }
                $scope.suggestionModel = null
            }, function errorCallback(response) {
                $scope.calculateModel = {}
                $scope.confirmationModel = null
                $scope.suggestionModel = null
                $scope.isLoading = false
                toastr.error(response.statusText, 'Error');
            })
        }
        window.scope = $scope
    }])