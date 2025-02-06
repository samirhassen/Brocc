var app = angular.module('app', ['ntech.forms']);

app.controller("ctr", ['$scope', '$http', '$window', function ($scope, $http, $window) {
    window.scope = $scope
    $scope.whitelistedBackUrl = initialData.whitelistedBackUrl
    $scope.showFetchMore = false
    $scope.reports = []
    $scope.isLoading = true
    $scope.reportName = initialData.reportName

    $scope.fetchMore = function (evt) {
        if (evt) {
            evt.preventDefault()
        }        
        var b = 50
        $http({
            method: 'POST',
            url: initialData.fetchBatchUrl,
            data: {
                startBeforeId: $scope.startBeforeId,
                batchSize: b,
                reportName: initialData.reportName
            }
        }).then(function successCallback(response) {
            $scope.startBeforeId = response.data.OldestIdInBatch
            angular.forEach(response.data.ReportsBatch, function (r) {
                $scope.reports.push(r)
            })
            $scope.showFetchMore = (response.data.ReportsBatch && response.data.ReportsBatch.length === b)
            $scope.isLoading = false
        }, function errorCallback(response) {
            $scope.isLoading = false
            $scope.isBroken = true
        })
    }

    $scope.createReport = function (evt) {
        if (evt) {
            evt.preventDefault()
        }
        $scope.isLoading = true
        $http({
            method: 'POST',
            url: initialData.createReportUrl,
            data: {                
                reportName: initialData.reportName,
                reportDate: initialData.currentDate
            }
        }).then(function successCallback(response) {
            if (response.data.errors) {
                $scope.isLoading = false                
                $scope.isBroken = true
                $scope.errors = response.data.errors
            } else {
                location.reload()
            }            
        }, function errorCallback(response) {
            $scope.isLoading = false
            $scope.isBroken = true
        })
    }

    $scope.fetchMore()
}])