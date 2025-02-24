var WithdrawalsController = app.controller('accountclosureCtr', ['$scope', '$http', '$q', '$timeout', '$route', '$routeParams', 'ctrData', 'mainService', 'savingsAccountCommentsService', function ($scope, $http, $q, $timeout, $route, $routeParams, ctrData, mainService, savingsAccountCommentsService) {
        window.accountclosureScope = $scope;
        $scope.d = ctrData;
        $scope.calculate = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            mainService.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.previewCloseAccountUrl,
                data: {
                    savingsAccountNr: ctrData.SavingsAccountNr
                }
            }).then(function successCallback(response) {
                mainService.isLoading = false;
                $scope.previewData = response.data;
            }, function errorCallback(response) {
                toastr.error(response.statusText);
                mainService.isLoading = false;
            });
        };
        $scope.closeAccount = function (evt) {
            if (evt) {
                evt.preventDefault();
            }
            mainService.isLoading = true;
            $http({
                method: 'POST',
                url: initialData.closeAccountAccountUrl,
                data: {
                    savingsAccountNr: $scope.previewData.SavingsAccountNr,
                    uniqueOperationToken: $scope.previewData.UniqueOperationToken
                }
            }).then(function successCallback(response) {
                mainService.isLoading = false;
                $scope.previewData = null;
                savingsAccountCommentsService.forceReload = true;
                $scope.isDone = true;
            }, function errorCallback(response) {
                toastr.error(response.statusText);
                mainService.isLoading = false;
            });
        };
    }]);
